namespace Snipdeck.Importer.Sources
{
    /// <summary>
    /// Mirrors a SnipCommand export document. Despite the <c>.db</c> extension SnipCommand uses,
    /// the export is pretty-printed JSON of the shape <c>{ "commands": [ … ] }</c>.
    /// Deserialised case-insensitively, so the camelCase JSON keys map onto these members.
    /// </summary>
    internal sealed class SnipCommandExport
    {
        public List<SnipCommandEntry> Commands { get; set; } = [];
    }

    /// <summary>One SnipCommand entry. The opaque <c>id</c> is intentionally not modelled — it is discarded.</summary>
    internal sealed class SnipCommandEntry
    {
        public string? Title { get; set; }

        public string? Command { get; set; }

        /// <summary>Comma-separated tag list (a single string, not an array).</summary>
        public string? Tags { get; set; }

        public string? Description { get; set; }

        public bool IsFavourite { get; set; }

        public bool IsTrash { get; set; }

        public int? UsageCount { get; set; }

        public DateTimeOffset? LastUsedAt { get; set; }
    }
}
