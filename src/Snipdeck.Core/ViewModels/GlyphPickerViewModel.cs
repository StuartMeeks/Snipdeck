using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// Backs the glyph picker: a searchable view over the curated catalogue. The
    /// user filters by name, keyword or code point and selects a glyph; the dialog
    /// reads <see cref="SelectedGlyph"/> on confirm.
    /// </summary>
    public sealed partial class GlyphPickerViewModel : ObservableObject
    {
        private readonly List<GlyphCatalogueEntry> _all;

        public GlyphPickerViewModel(IEnumerable<GlyphCatalogueEntry> entries, string? currentGlyph = null)
        {
            ArgumentNullException.ThrowIfNull(entries);

            _all = [.. entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)];
            Results = new ObservableCollection<GlyphCatalogueEntry>(_all);

            // Pre-select the row matching the tag's current glyph, so opening the
            // picker on an already-iconed tag highlights what's in effect.
            var resolved = GlyphInput.Resolve(currentGlyph);
            if (resolved.Length != 0)
            {
                SelectedEntry = _all.FirstOrDefault(e => e.Glyph == resolved);
            }
        }

        public ObservableCollection<GlyphCatalogueEntry> Results { get; }

        /// <summary>The free-text filter over name, keywords and code point.</summary>
        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial GlyphCatalogueEntry? SelectedEntry { get; set; }

        public bool IsEmpty => _all.Count == 0;

        public bool HasNoResults => Results.Count == 0 && _all.Count != 0;

        /// <summary>The chosen glyph character, or empty when nothing is selected.</summary>
        public string SelectedGlyph => SelectedEntry?.Glyph ?? string.Empty;

        partial void OnSearchTextChanged(string value)
        {
            var matches = _all.Where(e => e.Matches(value)).ToList();

            // Rebuild in place so the bound GridView animates rather than resets.
            Results.Clear();
            foreach (var entry in matches)
            {
                Results.Add(entry);
            }

            // Drop a selection that's been filtered out, so confirm can't return a
            // glyph the user can no longer see.
            if (SelectedEntry is not null && !matches.Contains(SelectedEntry))
            {
                SelectedEntry = null;
            }

            OnPropertyChanged(nameof(HasNoResults));
        }

        partial void OnSelectedEntryChanged(GlyphCatalogueEntry? value) =>
            OnPropertyChanged(nameof(SelectedGlyph));
    }
}
