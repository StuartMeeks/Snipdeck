using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>One editable row in the "Tags" management view: a tag and its icon glyph.</summary>
    public sealed partial class TagIconRowViewModel(string tagName, string glyph) : ObservableObject
    {
        public string TagName { get; } = tagName;

        /// <summary>The raw glyph the user has entered; empty means "use the default".</summary>
        [ObservableProperty]
        public partial string Glyph { get; set; } = glyph;

        /// <summary>What to show in the preview — the entered glyph, or the default when blank.</summary>
        public string PreviewGlyph => string.IsNullOrWhiteSpace(Glyph) ? TagItemViewModel.DefaultGlyph : Glyph.Trim();

        partial void OnGlyphChanged(string value) => OnPropertyChanged(nameof(PreviewGlyph));
    }

    /// <summary>
    /// The "Tags" content view (left-pane footer): lists every tag in use, each
    /// with an editable icon glyph. The shell persists the result to the store's
    /// global tag-icon map.
    /// </summary>
    public sealed partial class TagIconsViewModel : ObservableObject
    {
        public TagIconsViewModel(IEnumerable<string> tagNames, IReadOnlyDictionary<string, string> tagIcons)
        {
            ArgumentNullException.ThrowIfNull(tagNames);
            ArgumentNullException.ThrowIfNull(tagIcons);

            Rows = new ObservableCollection<TagIconRowViewModel>(
                tagNames
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                    .Select(t => new TagIconRowViewModel(t, tagIcons.TryGetValue(t, out var g) ? g : string.Empty)));
        }

        public ObservableCollection<TagIconRowViewModel> Rows { get; }

        public bool IsEmpty => Rows.Count == 0;

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// The tag→glyph map to persist: only rows with a non-default glyph, so
        /// default "#" tags stay implicit and the map stays small.
        /// </summary>
        public Dictionary<string, string> BuildTagIcons()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in Rows)
            {
                var glyph = row.Glyph?.Trim();
                if (!string.IsNullOrEmpty(glyph) && glyph != TagItemViewModel.DefaultGlyph)
                {
                    map[row.TagName] = glyph;
                }
            }
            return map;
        }
    }
}
