using System.Runtime.CompilerServices;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Execution.Abstractions;
using Snipdeck.Execution.Models;

namespace Snipdeck.Execution.Tests.Support
{
    /// <summary>Runs queued continuations synchronously on the calling thread.</summary>
    public sealed class InlineDispatcher : IDispatcher
    {
        public bool HasUiThreadAccess => true;

        public void Enqueue(Action action) => action();
    }

    /// <summary>A clock with a settable, advanceable now.</summary>
    public sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => UtcNow += by;
    }

    /// <summary>Records the last text copied.</summary>
    public sealed class FakeClipboardService : IClipboardService
    {
        public string? LastText { get; private set; }

        public Task SetTextAsync(string text)
        {
            LastText = text;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Programmable <see cref="ICommandRunner"/>: yields preset chunks, records input and
    /// resize calls, and optionally throws <see cref="OperationCanceledException"/> at the
    /// end to simulate a cancelled run.
    /// </summary>
    public sealed class FakeCommandRunner(IEnumerable<byte[]> chunks, int exitCode = 0, bool throwCancellation = false)
        : ICommandRunner
    {
        private readonly List<byte[]> _chunks = [.. chunks];

        public int LastExitCode { get; } = exitCode;

        public List<byte[]> WrittenInput { get; } = [];

        public (int Columns, int Rows)? LastResize { get; private set; }

        public CommandSpec? LastSpec { get; private set; }

        public async IAsyncEnumerable<byte[]> RunAsync(
            CommandSpec spec,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastSpec = spec;
            foreach (var chunk in _chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return chunk;
            }

            if (throwCancellation)
            {
                throw new OperationCanceledException();
            }
        }

        public ValueTask WriteInputAsync(ReadOnlyMemory<byte> input, CancellationToken cancellationToken = default)
        {
            WrittenInput.Add(input.ToArray());
            return ValueTask.CompletedTask;
        }

        public void Resize(int columns, int rows) => LastResize = (columns, rows);
    }

    /// <summary>In-memory <see cref="ICommandHistoryStore"/> for view-model tests.</summary>
    public sealed class FakeCommandHistoryStore : ICommandHistoryStore
    {
        public List<CommandHistoryEntry> Entries { get; } = [];

        public int? LastRetentionPerSnip { get; private set; }

        public string? LastSearch { get; private set; }

        public Task<IReadOnlyList<CommandHistoryEntry>> QueryAsync(
            string? search = null,
            Guid? snipId = null,
            CancellationToken cancellationToken = default)
        {
            LastSearch = search;
            IEnumerable<CommandHistoryEntry> query = Entries;
            if (snipId.HasValue)
            {
                query = query.Where(e => e.SnipId == snipId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.ResolvedCommand.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.CleanedOutput.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult<IReadOnlyList<CommandHistoryEntry>>([.. query]);
        }

        public Task<CommandHistoryEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Entries.FirstOrDefault(e => e.Id == id));

        public Task AddAsync(CommandHistoryEntry entry, int retentionPerSnip, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            LastRetentionPerSnip = retentionPerSnip;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _ = Entries.RemoveAll(e => e.Id == id);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Entries.Clear();
            return Task.CompletedTask;
        }
    }

    /// <summary>Programmable <see cref="IShellInteractions"/> for coordinator/history tests.</summary>
    public sealed class FakeShellInteractions : IShellInteractions
    {
        public ParameterFillResult? NextRunFillResult { get; set; }

        public bool NextConfirmResult { get; set; }

        public int NotifyCount { get; private set; }

        public string? LastRunShellDisplay { get; private set; }

        public string? LastRunWorkingDirectoryDisplay { get; private set; }

        public Task<ParameterFillResult?> FillParametersForRunAsync(
            Snip snip,
            IReadOnlyList<Parameter> parameters,
            string shellDisplay,
            string workingDirectoryDisplay)
        {
            LastRunShellDisplay = shellDisplay;
            LastRunWorkingDirectoryDisplay = workingDirectoryDisplay;
            return Task.FromResult(NextRunFillResult);
        }

        public Task<bool> ConfirmAsync(string title, string message, string confirmButtonText = "Yes", string cancelButtonText = "Cancel", bool destructive = false)
            => Task.FromResult(NextConfirmResult);

        public Task NotifyAsync(string title, string message, string buttonText = "OK")
        {
            NotifyCount++;
            return Task.CompletedTask;
        }

        public Task<ParameterFillResult?> FillParametersAsync(Snip snip, IReadOnlyList<Parameter> parameters)
            => Task.FromResult<ParameterFillResult?>(null);

        public Task<SnipEditResult?> EditSnipAsync(Snip snip, IReadOnlyList<Cli> availableClis)
            => Task.FromResult<SnipEditResult?>(null);

        public Task<CliEditResult?> EditCliAsync(Cli cli) => Task.FromResult<CliEditResult?>(null);

        public Task<Parameter?> EditParameterAsync(string title, Parameter? existing)
            => Task.FromResult<Parameter?>(null);

        public Task<string?> PickGlyphAsync(string? currentGlyph) => Task.FromResult<string?>(null);
    }
}
