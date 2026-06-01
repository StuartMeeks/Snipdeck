using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>One editable row in the "Tags" management view: a tag and its icon glyph.</summary>
    public sealed partial class TagIconRowViewModel(string tagName, string glyph, IShellInteractions? interactions = null) : ObservableObject
    {
        public string TagName { get; } = tagName;

        /// <summary>The raw glyph the user has entered; empty means "use the default".</summary>
        [ObservableProperty]
        public partial string Glyph { get; set; } = glyph;

        /// <summary>
        /// What to show in the preview — the resolved glyph (a typed code point like
        /// "E8EC" becomes its character), or the default when blank.
        /// </summary>
        public string PreviewGlyph
        {
            get
            {
                var resolved = GlyphInput.Resolve(Glyph);
                return resolved.Length == 0 ? TagItemViewModel.DefaultGlyph : resolved;
            }
        }

        partial void OnGlyphChanged(string value) => OnPropertyChanged(nameof(PreviewGlyph));

        /// <summary>
        /// Opens the glyph picker and, if the user chooses, stores the picked
        /// glyph. Picking persists the resolved character directly, so it reads
        /// back identically through <see cref="GlyphInput.Resolve"/>.
        /// </summary>
        [RelayCommand]
        private async Task ChooseGlyphAsync()
        {
            if (interactions is null)
            {
                return;
            }

            var picked = await interactions.PickGlyphAsync(Glyph).ConfigureAwait(true);
            if (picked is not null)
            {
                Glyph = picked;
            }
        }
    }

    /// <summary>
    /// The "Tags" content view (left-pane footer): lists every tag in use, each
    /// with an editable icon glyph. The shell persists the result to the store's
    /// global tag-icon map.
    /// </summary>
    public sealed partial class TagIconsViewModel : ObservableObject
    {
        public TagIconsViewModel(
            IEnumerable<string> tagNames,
            IReadOnlyDictionary<string, string> tagIcons,
            IShellInteractions? interactions = null)
        {
            ArgumentNullException.ThrowIfNull(tagNames);
            ArgumentNullException.ThrowIfNull(tagIcons);

            // Tags are matched case-insensitively across the shell, so collapse
            // casing variants to a single editable row (avoids a saved icon
            // appearing not to apply to the nav's collapsed tag entry).
            Rows = new ObservableCollection<TagIconRowViewModel>(
                tagNames
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                    .Select(t => new TagIconRowViewModel(t, tagIcons.TryGetValue(t, out var g) ? g : string.Empty, interactions)));
        }

        public ObservableCollection<TagIconRowViewModel> Rows { get; }

        public bool IsEmpty => Rows.Count == 0;

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// The tag→glyph map to persist: only rows with a non-default glyph, so
        /// default-glyph tags stay implicit and the map stays small. Keyed
        /// case-insensitively to match how tags are matched across the shell.
        /// </summary>
        public Dictionary<string, string> BuildTagIcons()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in Rows)
            {
                // Store the resolved character, so a typed code point ("E8EC") is
                // persisted (and later rendered) as its glyph.
                var glyph = GlyphInput.Resolve(row.Glyph);
                if (glyph.Length != 0 && glyph != TagItemViewModel.DefaultGlyph)
                {
                    map[row.TagName] = glyph;
                }
            }
            return map;
        }
    }
}
