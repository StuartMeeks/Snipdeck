using System.Collections;
using System.Runtime.CompilerServices;

using Porta.Pty;

using Snipdeck.Execution.Abstractions;
using Snipdeck.Execution.Models;

namespace Snipdeck.Execution.Services
{
    /// <summary>
    /// Runs a command under a real pseudo-terminal via Porta.Pty (ConPTY on Windows).
    /// Because the child sees a TTY, it emits colour, spinners and progress bars and
    /// renders interactive prompts; keyboard input is forwarded straight to the PTY.
    /// Stateful and single-use: one instance per run.
    /// </summary>
    public sealed class PortaPtyCommandRunner : ICommandRunner
    {
        private const int _readBufferSize = 4096;

        private IPtyConnection? _connection;
        private int _columns = 120;
        private int _rows = 30;

        /// <inheritdoc/>
        public int LastExitCode { get; private set; }

        /// <inheritdoc/>
        public async IAsyncEnumerable<byte[]> RunAsync(
            CommandSpec spec,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(spec);

            var options = new PtyOptions
            {
                App = spec.Launch.FileName,
                CommandLine = [.. spec.Launch.Arguments],
                // Verbatim: join the arguments raw rather than quoting each one. The
                // default quotes every argument, which wraps the resolved command in
                // quotes and makes `cmd /c "the whole command"` look for a program by
                // that literal name. ShellCommandBuilder already lays out each shell's
                // command line so a raw space-join is correct (quoting the command
                // itself only where the shell needs it, e.g. bash -lc).
                VerbatimCommandLine = true,
                Cwd = string.IsNullOrWhiteSpace(spec.WorkingDirectory)
                    ? Environment.CurrentDirectory
                    : spec.WorkingDirectory,
                Cols = _columns,
                Rows = _rows,
                Environment = CaptureEnvironment(),
            };

            _connection = await PtyProvider.SpawnAsync(options, cancellationToken).ConfigureAwait(false);
            var reader = _connection.ReaderStream;
            var buffer = new byte[_readBufferSize];

            try
            {
                while (true)
                {
                    var read = 0;
                    var stop = false;
                    try
                    {
                        read = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        KillQuietly();
                        stop = true;
                    }
                    catch (IOException)
                    {
                        // Reader closed as the process exited.
                        stop = true;
                    }

                    if (stop || read <= 0)
                    {
                        break;
                    }

                    yield return buffer[..read];
                }
            }
            finally
            {
                _ = _connection.WaitForExit(2000);
                try
                {
                    LastExitCode = _connection.ExitCode;
                }
                catch (InvalidOperationException)
                {
                    LastExitCode = -1;
                }

                (_connection as IDisposable)?.Dispose();
                _connection = null;
            }
        }

        /// <inheritdoc/>
        public async ValueTask WriteInputAsync(ReadOnlyMemory<byte> input, CancellationToken cancellationToken = default)
        {
            var connection = _connection;
            if (connection is null)
            {
                return;
            }

            try
            {
                await connection.WriterStream.WriteAsync(input, cancellationToken).ConfigureAwait(false);
                await connection.WriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // Process exited between the focus check and the write.
            }
            catch (ObjectDisposedException)
            {
                // Stream torn down concurrently with exit.
            }
        }

        /// <inheritdoc/>
        public void Resize(int columns, int rows)
        {
            if (columns <= 0 || rows <= 0)
            {
                return;
            }

            _columns = columns;
            _rows = rows;

            try
            {
                _connection?.Resize(columns, rows);
            }
            catch (IOException)
            {
                // Terminal already gone.
            }
            catch (InvalidOperationException)
            {
                // Terminal already gone.
            }
        }

        private void KillQuietly()
        {
            try
            {
                _connection?.Kill();
            }
            catch (IOException)
            {
                // Already dead.
            }
            catch (InvalidOperationException)
            {
                // Already dead.
            }
        }

        private static Dictionary<string, string> CaptureEnvironment()
        {
            // Inherit the current process environment so the child resolves PATH etc.
            var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                environment[(string)entry.Key] = entry.Value?.ToString() ?? string.Empty;
            }

            return environment;
        }
    }
}
