using Snipdeck.Execution.Models;

namespace Snipdeck.Execution.Abstractions
{
    /// <summary>
    /// Persists and queries execution history. Backed by SQLite (a separate
    /// database from the JSON snip store), because run output is not "small" and a
    /// power user accumulates thousands of runs.
    /// </summary>
    public interface ICommandHistoryStore
    {
        /// <summary>
        /// Returns runs newest-first. When <paramref name="snipId"/> is set, restricts to
        /// that Snip; when <paramref name="search"/> is non-empty, matches the resolved
        /// command or the cleaned output (case-insensitive substring).
        /// </summary>
        Task<IReadOnlyList<CommandHistoryEntry>> QueryAsync(
            string? search = null,
            Guid? snipId = null,
            CancellationToken cancellationToken = default);

        /// <summary>Returns a single run by id, or <c>null</c> if it does not exist.</summary>
        Task<CommandHistoryEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a completed run, then prunes the oldest runs for that Snip beyond the
        /// retention count. Pass <paramref name="retentionPerSnip"/> ≤ 0 to skip pruning.
        /// </summary>
        Task AddAsync(CommandHistoryEntry entry, int retentionPerSnip, CancellationToken cancellationToken = default);

        /// <summary>Deletes a single run.</summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Deletes all runs.</summary>
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
