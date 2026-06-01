namespace Snipdeck.Core.Engine
{
    /// <summary>
    /// Normalises a comma-separated tag string into a tag list: split on commas, trim, drop
    /// empties, and de-duplicate case-insensitively. Shared by the snip editor and the importer
    /// so a tag list is normalised the same way wherever it enters the store.
    /// </summary>
    public static class TagParser
    {
        public static List<string> Parse(string? text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? []
                : [.. text
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)];
        }
    }
}
