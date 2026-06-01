using System.Text.Json;
using System.Text.Json.Serialization;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Services
{
    /// <summary>
    /// Reads the curated glyph catalogue from appsettings.json (next to the
    /// executable). Re-reads on every call so editing the file is reflected the
    /// next time the picker opens. Degrades to an empty catalogue — never throws —
    /// when the file is missing or malformed, so a bad edit can't break the picker.
    /// </summary>
    internal sealed class GlyphCatalogueProvider : IGlyphCatalogueProvider
    {
        private readonly string _path;

        public GlyphCatalogueProvider()
        {
            _path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        }

        public IReadOnlyList<GlyphCatalogueEntry> GetEntries()
        {
            GlyphCatalogueFile? file;
            try
            {
                if (!File.Exists(_path))
                {
                    return [];
                }

                using var stream = File.OpenRead(_path);
                file = JsonSerializer.Deserialize(stream, GlyphCatalogueJsonContext.Default.GlyphCatalogueFile);
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                return [];
            }

            if (file?.GlyphCatalogue is not { Count: > 0 } raw)
            {
                return [];
            }

            var entries = new List<GlyphCatalogueEntry>(raw.Count);
            foreach (var entry in raw)
            {
                // A code point that won't resolve to a glyph (or a row with no
                // name) is skipped rather than rendered as a blank cell.
                var glyph = GlyphInput.Resolve(entry.Code);
                if (glyph.Length == 0 || string.IsNullOrWhiteSpace(entry.Name))
                {
                    continue;
                }

                entries.Add(new GlyphCatalogueEntry(glyph, entry.Name.Trim(), entry.Keywords ?? []));
            }

            return entries;
        }
    }

    /// <summary>The appsettings.json shape the catalogue is read from.</summary>
    internal sealed class GlyphCatalogueFile
    {
        public List<GlyphCatalogueFileEntry>? GlyphCatalogue { get; set; }
    }

    /// <summary>One raw catalogue row before the code point is resolved to a glyph.</summary>
    internal sealed class GlyphCatalogueFileEntry
    {
        public string? Code { get; set; }

        public string? Name { get; set; }

        public List<string>? Keywords { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(GlyphCatalogueFile))]
    internal sealed partial class GlyphCatalogueJsonContext : JsonSerializerContext
    {
    }
}
