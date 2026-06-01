using System.Text.Json;

using Snipdeck.Core.Engine;
using Snipdeck.Core.Models;
using Snipdeck.Importer.Translation;

namespace Snipdeck.Importer.Sources
{
    /// <summary>
    /// Reads a SnipCommand JSON export and yields import candidates, translating inline
    /// <c>sc_*</c> markup into structured parameters and suggesting a CLI per command.
    /// All SnipCommand-specific knowledge lives here.
    /// </summary>
    public sealed class SnipCommandSource : ISnippetSource
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        public string DisplayName => "SnipCommand";

        public IReadOnlyList<SnippetCandidate> Read(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"SnipCommand export not found: {path}", path);
            }

            var text = File.ReadAllText(path);
            return ReadFromText(text);
        }

        /// <summary>Parses export content already loaded into memory. Exposed for testing.</summary>
        public static IReadOnlyList<SnippetCandidate> ReadFromText(string text)
        {
            // Sniff the format: SnipCommand exports are a JSON object, regardless of the .db extension.
            var trimmed = text.AsSpan().TrimStart();
            if (trimmed.IsEmpty || trimmed[0] != '{')
            {
                throw new InvalidDataException(
                    "The file does not look like a SnipCommand JSON export (expected a JSON object starting with '{').");
            }

            SnipCommandExport? export;
            try
            {
                export = JsonSerializer.Deserialize<SnipCommandExport>(text, _jsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"The SnipCommand export could not be parsed: {ex.Message}", ex);
            }

            if (export?.Commands is not { Count: > 0 } commands)
            {
                return [];
            }

            var candidates = new List<SnippetCandidate>(commands.Count);
            foreach (var entry in commands)
            {
                if (entry is null || entry.IsTrash)
                {
                    continue;
                }

                candidates.Add(ToCandidate(entry));
            }

            return candidates;
        }

        private static SnippetCandidate ToCandidate(SnipCommandEntry entry)
        {
            var translation = ScMarkupTranslator.Translate(entry.Command);
            var suggestion = CliSuggester.Suggest(translation.CommandTemplate);

            var snip = new Snip
            {
                Title = (entry.Title ?? string.Empty).Trim(),
                CommandTemplate = translation.CommandTemplate,
                Description = string.IsNullOrWhiteSpace(entry.Description) ? null : entry.Description.Trim(),
                Tags = TagParser.Parse(entry.Tags),
                Parameters = [.. translation.Parameters],
                IsFavourite = entry.IsFavourite,
                UsageCount = entry.UsageCount is > 0 ? entry.UsageCount.Value : 0,
                LastUsedAt = entry.LastUsedAt,
            };

            return new SnippetCandidate(suggestion.Name, suggestion.Confident, snip);
        }
    }
}
