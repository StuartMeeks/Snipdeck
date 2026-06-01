using System.Globalization;

namespace Snipdeck.Core.Models
{
    /// <summary>
    /// One entry in the glyph picker's browsable catalogue: a Segoe Fluent Icons
    /// glyph, a friendly name, and optional search keywords. The catalogue is a
    /// curated, user-editable subset (see the App's appsettings.json) — not the
    /// full ~1.5k font, which would need heavier virtualisation to stay usable.
    /// </summary>
    /// <param name="Glyph">The resolved glyph character to render (e.g. "").</param>
    /// <param name="Name">The friendly display name, e.g. "Home".</param>
    /// <param name="Keywords">Extra terms the search matches beyond the name.</param>
    public sealed record GlyphCatalogueEntry(string Glyph, string Name, IReadOnlyList<string> Keywords)
    {
        /// <summary>True when <paramref name="term"/> matches the name, a keyword,
        /// or the glyph's code point (e.g. "e80f"). Blank matches everything.</summary>
        public bool Matches(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return true;
            }

            var needle = term.Trim();
            if (Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var keyword in Keywords)
            {
                if (keyword.Contains(needle, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return CodePoint.Contains(needle, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The glyph's Unicode code point as hex (e.g. "E80F"), for display
        /// and search. Empty when the glyph isn't a single code point.</summary>
        public string CodePoint =>
            Glyph.Length == 0 ? string.Empty : char.ConvertToUtf32(Glyph, 0).ToString("X4", CultureInfo.InvariantCulture);
    }
}
