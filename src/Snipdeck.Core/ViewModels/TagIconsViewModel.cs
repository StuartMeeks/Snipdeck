using System.Collections.ObjectModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>One editable row in the "Tags" management view: a tag and its icon glyph.</summary>
    public sealed partial class TagIconRowViewModel(
        string tagName,
        string glyph,
        IShellInteractions? interactions = null,
        GlyphNameLookup? names = null) : ObservableObject
    {
        private readonly GlyphNameLookup _names = names ?? GlyphNameLookup.Empty;

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

        /// <summary>
        /// The value shown in (and edited through) the icon text box. A glyph that's
        /// in the catalogue is shown by its friendly name (e.g. "Settings"); an
        /// unknown glyph character falls back to its hex code point (e.g. "E8EC")
        /// rather than an unreadable tofu box. Typed codes and other text pass
        /// through unchanged. The box commits on focus loss, so the friendly form
        /// only replaces what was typed once editing finishes — never mid-keystroke.
        /// On the way in, a recognised name is resolved back to its glyph.
        /// </summary>
        public string GlyphText
        {
            get
            {
                if (string.IsNullOrEmpty(Glyph))
                {
                    return string.Empty;
                }

                // Prefer the friendly name of whatever the value resolves to (covers
                // both a stored glyph character and a typed code that maps to one).
                var resolved = GlyphInput.Resolve(Glyph);
                if (resolved.Length != 0 && _names.NameFor(resolved) is { } name)
                {
                    return name;
                }

                // A glyph character with no catalogue entry: show its code point.
                if (IsGlyphCharacter(Glyph, out var codePoint))
                {
                    return codePoint.ToString("X4", CultureInfo.InvariantCulture);
                }

                // Typed code / free text: leave as entered.
                return Glyph;
            }
            set => Glyph = _names.GlyphFor(value ?? string.Empty) ?? value ?? string.Empty;
        }

        partial void OnGlyphChanged(string value)
        {
            OnPropertyChanged(nameof(PreviewGlyph));
            OnPropertyChanged(nameof(GlyphText));
        }

        // True when the value is a single literal glyph character (as opposed to a
        // typed hex code or other text), so the text box can show its code point.
        private static bool IsGlyphCharacter(string value, out int codePoint)
        {
            codePoint = 0;
            if (value.Length == 1 && value[0] > 0x7F)
            {
                codePoint = value[0];
                return true;
            }

            if (value.Length == 2 && char.IsHighSurrogate(value[0]) && char.IsLowSurrogate(value[1]))
            {
                codePoint = char.ConvertToUtf32(value[0], value[1]);
                return true;
            }

            return false;
        }

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
            IShellInteractions? interactions = null,
            GlyphNameLookup? names = null)
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
                    .Select(t => new TagIconRowViewModel(t, tagIcons.TryGetValue(t, out var g) ? g : string.Empty, interactions, names)));
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
