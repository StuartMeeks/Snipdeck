using Snipdeck.Core.Models;

namespace Snipdeck.Importer.Sources
{
    /// <summary>
    /// Reads snippet candidates from a source export. Each adapter (SnipCommand today, others later)
    /// isolates all format-specific parsing behind this interface and yields format-agnostic
    /// <see cref="SnippetCandidate"/>s for the merger to consume.
    /// </summary>
    public interface ISnippetSource
    {
        /// <summary>The human-friendly source name, used in messages (e.g. "SnipCommand").</summary>
        string DisplayName { get; }

        /// <summary>Reads the file at <paramref name="path"/> and returns the candidates it contains.</summary>
        IReadOnlyList<SnippetCandidate> Read(string path);
    }

    /// <summary>
    /// A snip ready to import, plus the source's suggestion for which CLI it belongs to and whether
    /// that suggestion is trustworthy. The merger resolves the final CLI from this and the CLI options.
    /// </summary>
    public sealed record SnippetCandidate(string SuggestedCliName, bool CliConfident, Snip Snip);
}
