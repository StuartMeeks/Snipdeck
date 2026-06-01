using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// A two-way map between catalogue glyph characters and their friendly names,
    /// used by the Tags view to show (and accept) an icon's name rather than a raw
    /// code point. Names are matched case-insensitively; glyph characters exactly.
    /// </summary>
    public sealed class GlyphNameLookup
    {
        private readonly Dictionary<string, string> _glyphToName = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _nameToGlyph = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>An empty lookup — every query misses (callers fall back to hex).</summary>
        public static GlyphNameLookup Empty { get; } = new([]);

        public GlyphNameLookup(IEnumerable<GlyphCatalogueEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);
            foreach (var entry in entries)
            {
                // First entry wins for any duplicate glyph or name, matching the
                // catalogue's own first-listed-wins behaviour.
                _ = _glyphToName.TryAdd(entry.Glyph, entry.Name);
                _ = _nameToGlyph.TryAdd(entry.Name, entry.Glyph);
            }
        }

        /// <summary>The friendly name for a glyph character, or null if unknown.</summary>
        public string? NameFor(string glyph) =>
            _glyphToName.TryGetValue(glyph, out var name) ? name : null;

        /// <summary>The glyph character for a friendly name, or null if unknown.</summary>
        public string? GlyphFor(string name) =>
            _nameToGlyph.TryGetValue(name.Trim(), out var glyph) ? glyph : null;
    }
}
