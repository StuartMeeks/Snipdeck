using System.Globalization;

using Snipdeck.Execution.Models;

namespace Snipdeck.Execution.ViewModels
{
    /// <summary>Read-only display projection of a stored run for the history list.</summary>
    public sealed class HistoryItemViewModel
    {
        /// <summary>Builds a list item from a stored entry, resolving display names via the supplied lookups.</summary>
        public HistoryItemViewModel(
            CommandHistoryEntry entry,
            Func<Guid, string> snipTitleResolver,
            Func<Guid, string> cliNameResolver)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(snipTitleResolver);
            ArgumentNullException.ThrowIfNull(cliNameResolver);

            Id = entry.Id;
            SnipId = entry.SnipId;
            SnipTitle = snipTitleResolver(entry.SnipId);
            CliName = cliNameResolver(entry.CliId);
            ResolvedCommand = entry.ResolvedCommand;
            ExecutedAtDisplay = entry.ExecutedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture);
            DurationDisplay = FormatDuration(entry.DurationMs);
            Cancelled = entry.Cancelled;
            ExitCode = entry.ExitCode;
            IsSuccess = !entry.Cancelled && entry.ExitCode == 0;
            StatusDisplay = entry.Cancelled
                ? "Cancelled"
                : string.Create(CultureInfo.InvariantCulture, $"Exit {entry.ExitCode}");
            OutputPreview = BuildPreview(entry.CleanedOutput);
        }

        /// <summary>The run's identifier.</summary>
        public Guid Id { get; }

        /// <summary>The Snip this run belongs to.</summary>
        public Guid SnipId { get; }

        /// <summary>The Snip's current title (or a placeholder if it no longer exists).</summary>
        public string SnipTitle { get; }

        /// <summary>The CLI's current name (or a placeholder if it no longer exists).</summary>
        public string CliName { get; }

        /// <summary>The exact command line that was executed.</summary>
        public string ResolvedCommand { get; }

        /// <summary>Local execution time, formatted for display.</summary>
        public string ExecutedAtDisplay { get; }

        /// <summary>Human-readable duration (e.g. "850 ms", "1.2 s").</summary>
        public string DurationDisplay { get; }

        /// <summary>True when the run was cancelled.</summary>
        public bool Cancelled { get; }

        /// <summary>The process exit code.</summary>
        public int ExitCode { get; }

        /// <summary>True when the run exited with code 0.</summary>
        public bool IsSuccess { get; }

        /// <summary>Short status label ("Exit 0", "Cancelled", …).</summary>
        public string StatusDisplay { get; }

        /// <summary>The first couple of non-blank lines of clean output, for the card preview.</summary>
        public string OutputPreview { get; }

        private static string FormatDuration(int durationMs)
        {
            return durationMs < 1000
                ? string.Create(CultureInfo.InvariantCulture, $"{durationMs} ms")
                : string.Create(CultureInfo.InvariantCulture, $"{durationMs / 1000.0:0.0} s");
        }

        private static string BuildPreview(string cleanedOutput)
        {
            if (string.IsNullOrWhiteSpace(cleanedOutput))
            {
                return string.Empty;
            }

            var lines = cleanedOutput
                .Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Take(2);
            return string.Join("\n", lines);
        }
    }
}
