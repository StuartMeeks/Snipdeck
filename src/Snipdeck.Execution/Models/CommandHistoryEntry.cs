namespace Snipdeck.Execution.Models
{
    /// <summary>
    /// One persisted execution. Stored in the SQLite history database (NOT the JSON
    /// snip store): the cleaned transcript is the canonical, human-readable record;
    /// the gzipped raw VT stream is retained so a past run can be replayed in full
    /// colour ("Run again").
    /// </summary>
    public sealed class CommandHistoryEntry
    {
        /// <summary>Stable identifier for this run.</summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>The CLI the run's Snip belonged to.</summary>
        public Guid CliId { get; init; }

        /// <summary>The Snip that was run.</summary>
        public Guid SnipId { get; init; }

        /// <summary>The exact command line that was executed (after token substitution).</summary>
        public string ResolvedCommand { get; init; } = string.Empty;

        /// <summary>JSON map of the parameter values supplied for this run (for display / re-run).</summary>
        public string? ParameterValuesJson { get; init; }

        /// <summary>When the run started.</summary>
        public DateTimeOffset ExecutedAt { get; init; }

        /// <summary>Wall-clock duration of the run, in milliseconds.</summary>
        public int DurationMs { get; init; }

        /// <summary>Process exit code (0 = success). Undefined when <see cref="Cancelled"/>.</summary>
        public int ExitCode { get; init; }

        /// <summary>True when the user cancelled the run before it completed.</summary>
        public bool Cancelled { get; init; }

        /// <summary>The clean plain-text transcript: no ANSI, no cursor artefacts, no stacked frames.</summary>
        public string CleanedOutput { get; init; } = string.Empty;

        /// <summary>The gzipped raw VT byte stream, for full-fidelity replay. May be null.</summary>
        public byte[]? RawStreamGz { get; init; }

        /// <summary>True when capture hit the size cap and the stored output is truncated.</summary>
        public bool OutputTruncated { get; init; }
    }
}
