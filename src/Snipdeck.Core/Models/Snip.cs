namespace Snipdeck.Core.Models
{
    public sealed class Snip
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public Guid CliId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string CommandTemplate { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<Parameter> Parameters { get; set; } = [];

        public List<string> Tags { get; set; } = [];

        public bool IsFavourite { get; set; }

        public bool IsTrash { get; set; }

        public int UsageCount { get; set; }

        public DateTimeOffset? LastUsedAt { get; set; }

        // --- Execution overrides (schema v5) ---

        /// <summary>Overrides the CLI's <see cref="Cli.Shell"/> for this Snip when set.</summary>
        public ShellKind? ShellOverride { get; set; }

        /// <summary>Overrides the CLI's <see cref="Cli.WorkingDirectory"/> for this Snip when set.</summary>
        public string? WorkingDirectoryOverride { get; set; }
    }
}
