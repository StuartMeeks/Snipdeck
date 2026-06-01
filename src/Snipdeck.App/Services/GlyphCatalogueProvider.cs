using System.Text.Json;
using System.Text.Json.Serialization;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Services
{
    /// <summary>
    /// Supplies the icon picker's catalogue from a user-editable JSON file in
    /// LocalAppData (so edits survive Velopack updates). On first run the file is
    /// seeded from a default bundled into the app; thereafter the user's copy is
    /// read, re-read on every call so edits take effect without a restart.
    /// Degrades to the bundled default — never throws — when the user file is
    /// missing or malformed, so a bad edit can't break the picker.
    /// </summary>
    internal sealed class GlyphCatalogueProvider : IGlyphCatalogueProvider
    {
        // The bundled default, embedded so it can't be deleted and ships fresh
        // with each release. Its logical name is pinned in the csproj.
        private const string _defaultResourceName = "Snipdeck.App.icon-catalogue.json";

        private readonly string _path;

        public GlyphCatalogueProvider(string catalogueFilePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(catalogueFilePath);
            _path = catalogueFilePath;
        }

        public IReadOnlyList<GlyphCatalogueEntry> GetEntries()
        {
            SeedIfMissing();

            // The user's copy wins; fall back to the bundled default if it's
            // absent or unreadable (e.g. mid-edit or malformed).
            var fromFile = ReadFile();
            return fromFile.Count > 0 ? fromFile : ReadBundledDefault();
        }

        private void SeedIfMissing()
        {
            // Only seed when truly absent — never overwrite a user's file, even a
            // broken one (they can fix it, or delete it to restore the default).
            try
            {
                if (File.Exists(_path))
                {
                    return;
                }

                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    _ = Directory.CreateDirectory(directory);
                }

                using var resource = OpenBundledDefault();
                if (resource is null)
                {
                    return;
                }

                // Write-then-rename so an interrupted seed can't leave a partial file.
                var tempPath = _path + ".tmp";
                using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    resource.CopyTo(file);
                    file.Flush();
                }

                File.Move(tempPath, _path, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Seeding is best-effort: the bundled default still serves the picker.
            }
        }

        private List<GlyphCatalogueEntry> ReadFile()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return [];
                }

                using var stream = File.OpenRead(_path);
                return Parse(stream);
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                return [];
            }
        }

        private static List<GlyphCatalogueEntry> ReadBundledDefault()
        {
            try
            {
                using var stream = OpenBundledDefault();
                return stream is null ? [] : Parse(stream);
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static Stream? OpenBundledDefault() =>
            typeof(GlyphCatalogueProvider).Assembly.GetManifestResourceStream(_defaultResourceName);

        private static List<GlyphCatalogueEntry> Parse(Stream stream)
        {
            var file = JsonSerializer.Deserialize(stream, GlyphCatalogueJsonContext.Default.GlyphCatalogueFile);
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

    /// <summary>The icon-catalogue JSON shape the catalogue is read from.</summary>
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
