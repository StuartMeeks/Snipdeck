namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// A tag entry in the left-navigation tag list: the tag name plus its icon
    /// glyph (Segoe Fluent Icons). The "All" sentinel is also represented here.
    /// Immutable — the nav refreshes by rebuilding the whole tag collection.
    /// </summary>
    public sealed class TagItemViewModel(string name, string glyph, bool isAll = false)
    {
        /// <summary>Fallback glyph for a tag with no assigned icon.</summary>
        public const string DefaultGlyph = "#";

        public string Name { get; } = name;

        public string Glyph { get; } = glyph;

        public bool IsAll { get; } = isAll;
    }
}
