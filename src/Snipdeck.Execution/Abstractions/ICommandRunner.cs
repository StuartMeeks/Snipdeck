using Snipdeck.Execution.Models;

namespace Snipdeck.Execution.Abstractions
{
    /// <summary>
    /// Spawns a command under a pseudo-terminal and exposes its output as a byte
    /// stream while accepting keyboard input — so the process behaves as if running
    /// in a real terminal (colour, spinners, progress bars, interactive prompts).
    /// Implementations are stateful and single-use per run; create one per execution.
    /// They must not reference any UI framework.
    /// </summary>
    public interface ICommandRunner
    {
        /// <summary>
        /// Starts the process described by <paramref name="spec"/> and yields raw VT
        /// output chunks as they arrive. The enumerable completes when the process
        /// exits or the run is cancelled; <see cref="LastExitCode"/> is then valid.
        /// </summary>
        IAsyncEnumerable<byte[]> RunAsync(CommandSpec spec, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forwards user input (keystrokes from the terminal, already VT-encoded) to the
        /// running process's standard input. No-op if the process has exited.
        /// </summary>
        ValueTask WriteInputAsync(ReadOnlyMemory<byte> input, CancellationToken cancellationToken = default);

        /// <summary>Resizes the pseudo-terminal so the child reflows its output.</summary>
        void Resize(int columns, int rows);

        /// <summary>The process exit code, valid only after <see cref="RunAsync"/> completes.</summary>
        int LastExitCode { get; }
    }
}
