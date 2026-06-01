using Snipdeck.Core.Models;

namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Supplies the glyph picker's browsable catalogue. The implementation lives
    /// in the App project and reads the user-editable icon-catalogue.json, so
    /// editing that file (adding or removing glyphs) is reflected the next time
    /// the picker opens — no rebuild required.
    /// </summary>
    public interface IGlyphCatalogueProvider
    {
        /// <summary>
        /// Returns the current catalogue. Re-read on each call so edits to the
        /// backing file take effect without a restart. Never null; returns an
        /// empty list when the file is missing or malformed.
        /// </summary>
        IReadOnlyList<GlyphCatalogueEntry> GetEntries();
    }
}
