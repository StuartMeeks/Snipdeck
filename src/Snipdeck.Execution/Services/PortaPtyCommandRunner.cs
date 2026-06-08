using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

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

        // ConPTY keeps the output pipe open until the pseudoconsole is closed, so after
        // the child exits there is no EOF until we close. On exit we wait this long for
        // the reader to drain trailing output, then close to end the stream.
        private const int _drainGraceMs = 150;

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

            var connection = await PtyProvider.SpawnAsync(options, cancellationToken).ConfigureAwait(false);
            _connection = connection;

            var channel = Channel.CreateUnbounded<byte[]>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

            // The process exiting is the authoritative "done" signal (the pipe won't
            // EOF on its own). Record the code, let the reader drain, then close.
            void OnExited(object? sender, PtyExitedEventArgs e)
            {
                LastExitCode = e.ExitCode;
                _ = CloseAfterGraceAsync(connection);
            }

            connection.ProcessExited += OnExited;

            // User cancel: kill the tree, then close to unblock the blocking reader.
            await using var cancelReg = cancellationToken.Register(() =>
            {
                KillQuietly();
                _ = CloseAfterGraceAsync(connection);
            }).ConfigureAwait(false);

            // ReaderStream is a synchronous pipe FileStream, so read on a background
            // thread and hand chunks to the channel; the iterator drains the channel.
            var readerTask = Task.Run(() =>
            {
                var buffer = new byte[_readBufferSize];
                try
                {
                    while (true)
                    {
                        var read = connection.ReaderStream.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                        {
                            break;
                        }

                        _ = channel.Writer.TryWrite(buffer[..read]);
                    }
                }
                catch (IOException)
                {
                    // Pipe closed (we closed the connection on exit/cancel).
                }
                catch (ObjectDisposedException)
                {
                    // Stream disposed concurrently with close.
                }
                finally
                {
                    _ = channel.Writer.TryComplete();
                }
            }, CancellationToken.None);

            try
            {
                await foreach (var chunk in channel.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    yield return chunk;
                }
            }
            finally
            {
                connection.ProcessExited -= OnExited;
                CloseQuietly();
                try
                {
                    await readerTask.ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Reader teardown errors are not actionable.
                }

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

        private static async Task CloseAfterGraceAsync(IPtyConnection connection)
        {
            try
            {
                await Task.Delay(_drainGraceMs).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Ignore.
            }

            try
            {
                (connection as IDisposable)?.Dispose();
            }
            catch (Exception)
            {
                // Disposal is best-effort and idempotent.
            }
        }

        private void CloseQuietly()
        {
            try
            {
                (_connection as IDisposable)?.Dispose();
            }
            catch (Exception)
            {
                // Disposal is best-effort and idempotent.
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
