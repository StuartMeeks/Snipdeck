using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Engine;
using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// The shell's view model owns the cross-cutting state — current CLI
    /// selection, search text, tag filter, content view-model — and exposes
    /// the commands that drive authoring (copy, edit, delete, new).
    /// </summary>
    public sealed partial class ShellViewModel : ObservableObject
    {
        public const string AllTagsSentinel = "All";

        /// <summary>The project documentation opened by the "Documentation" nav item.</summary>
        public const string DocumentationUrl = "https://github.com/StuartMeeks/Snipdeck#readme";

        // Glyph for the "All" tag entry (Segoe Fluent Icons "Filter").
        private const string _allTagsGlyph = "\uE71C";

        private readonly ISnipStore _store;
        private readonly IClipboardService _clipboard;
        private readonly IClock _clock;
        private readonly IShellInteractions _interactions;
        private readonly IIconAssetStorage _iconStorage;
        private readonly IExternalLinkService _externalLinks;
        private SnipStoreDocument _document = new();
        private bool _suppressShellRefresh;

        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial CliChoice? SelectedCliChoice { get; set; }

        [ObservableProperty]
        public partial string? SelectedTag { get; set; }

        [ObservableProperty]
        public partial TagItemViewModel? SelectedTagItem { get; set; }

        [ObservableProperty]
        public partial object? CurrentContent { get; set; }

        public ShellViewModel(
            ISnipStore store,
            IClipboardService clipboard,
            IClock clock,
            IShellInteractions interactions,
            IIconAssetStorage iconStorage,
            IExternalLinkService externalLinks)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(clipboard);
            ArgumentNullException.ThrowIfNull(clock);
            ArgumentNullException.ThrowIfNull(interactions);
            ArgumentNullException.ThrowIfNull(iconStorage);
            ArgumentNullException.ThrowIfNull(externalLinks);

            _store = store;
            _clipboard = clipboard;
            _clock = clock;
            _interactions = interactions;
            _iconStorage = iconStorage;
            _externalLinks = externalLinks;
        }

        public ObservableCollection<CliChoice> CliChoices { get; } = [];

        public ObservableCollection<TagItemViewModel> Tags { get; } = [];

        public bool CanCreateNewSnip => SelectedCliChoice?.Cli is not null;

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            // Stay on the UI thread after the await — RebuildCliChoices mutates
            // an ObservableCollection that XAML is already bound to, and WinRT
            // collection-change marshalling requires the original thread.
            _document = await _store.LoadAsync(cancellationToken).ConfigureAwait(true);
            RebuildCliChoices();
            // Start on Home (no tag selected), scope = "All". Suppress so setting
            // the choice doesn't auto-switch to the snip list (that's the
            // user-driven behaviour); build the All-scope tag list explicitly.
            _suppressShellRefresh = true;
            try
            {
                SelectedCliChoice = CliChoices.FirstOrDefault();
                RebuildTags();
                SelectedTagItem = null;
            }
            finally
            {
                _suppressShellRefresh = false;
            }
            ApplyShellContent();
        }

        public void OpenSettings(SettingsViewModel settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            CurrentContent = settings;
        }

        public void OpenTrash()
        {
            CurrentContent = BuildTrashViewModel();
        }

        public void OpenGlobalParameters()
        {
            CurrentContent = new GlobalParametersViewModel(_document.GlobalParameters);
        }

        [RelayCommand]
        private async Task SaveGlobalParametersAsync()
        {
            if (CurrentContent is not GlobalParametersViewModel globals)
            {
                return;
            }
            // Global parameters only affect fill-time resolution (read from the
            // document on copy), so no shell rebuild is needed — just persist.
            _document.GlobalParameters = globals.BuildParameters();
            await _store.SaveAsync(_document).ConfigureAwait(true);
            globals.StatusMessage = "Saved.";
        }

        public void OpenTagIcons()
        {
            CurrentContent = new TagIconsViewModel(SnipFilter.DistinctTagsFor(_document.Snips), _document.TagIcons);
        }

        [RelayCommand]
        private async Task SaveTagIconsAsync()
        {
            if (CurrentContent is not TagIconsViewModel tags)
            {
                return;
            }
            _document.TagIcons = tags.BuildTagIcons();
            await _store.SaveAsync(_document).ConfigureAwait(true);

            // Refresh the left-nav glyphs in place, keeping the user on this view
            // (suppress the content swap a selection change would otherwise cause).
            var wasHome = SelectedTagItem is null;
            var previousTagName = SelectedTagItem?.Name;
            _suppressShellRefresh = true;
            try
            {
                RebuildTags();
                RestoreTagSelection(wasHome, previousTagName);
            }
            finally
            {
                _suppressShellRefresh = false;
            }
            tags.StatusMessage = "Saved.";
        }

        /// <summary>Show the Home launcher (no tag selected). Scope is unchanged.</summary>
        public void ShowHome()
        {
            SelectedTagItem = null;
            ApplyShellContent();
        }

        /// <summary>Open the project documentation (GitHub readme) in the browser.</summary>
        public Task OpenDocumentationAsync() => _externalLinks.OpenAsync(DocumentationUrl);

        /// <summary>
        /// Snip-name autocomplete for the title-bar search, scoped to the current
        /// CLI switcher value. Each result carries its CLI name for the badge.
        /// </summary>
        public IReadOnlyList<SnipSearchResult> GetSearchSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }
            var trimmed = query.Trim();
            return [.. ScopedSnips()
                .Where(s => !s.IsTrash && s.Title.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
                .Select(s => new SnipSearchResult(s.Title, CliNameFor(s.CliId), s.CliId, s.Id))];
        }

        /// <summary>Filter the snip list down to a chosen search result, switching CLI scope if needed.</summary>
        public void SelectSearchResult(SnipSearchResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var choice = CliChoices.FirstOrDefault(c => c.Cli?.Id == result.CliId);
            if (choice is not null && !ReferenceEquals(choice, SelectedCliChoice))
            {
                // Switching the CLI shows that scope's snips (selects the "All" tag).
                SelectedCliChoice = choice;
            }
            else
            {
                // Same scope but possibly on Home — move to the snip list.
                SelectedTagItem ??= Tags.FirstOrDefault(t => t.IsAll);
            }

            SearchText = result.Title;
        }

        /// <summary>Filter the snip list by free-text search, moving off Home if needed.</summary>
        public void ApplySearch(string query)
        {
            // Search is snips-only: ensure the snip list is showing.
            SelectedTagItem ??= Tags.FirstOrDefault(t => t.IsAll);
            SearchText = query ?? string.Empty;
        }

        private string CliNameFor(Guid cliId) =>
            _document.Clis.FirstOrDefault(c => c.Id == cliId)?.Name ?? string.Empty;

        [RelayCommand]
        private async Task CopySnipAsync(SnipCardViewModel? cardVm)
        {
            if (cardVm is null)
            {
                return;
            }
            var snip = cardVm.Snip;
            // Effective parameters: local overrides plus any shared (CLI/global)
            // definitions the template's tokens inherit by name.
            var cli = _document.Clis.FirstOrDefault(c => c.Id == snip.CliId);
            var parameters = ParameterResolver.Resolve(snip, cli, _document.GlobalParameters);

            string commandToCopy;
            // Skip the flyout only when there's nothing to show: no parameters to
            // fill and no description to read. A described-but-parameterless snip
            // still opens the flyout so its (rendered) description is visible
            // before the copy.
            if (parameters.Count == 0 && string.IsNullOrWhiteSpace(snip.Description))
            {
                commandToCopy = snip.CommandTemplate;
            }
            else
            {
                var result = await _interactions.FillParametersAsync(snip, parameters).ConfigureAwait(true);
                if (result is null)
                {
                    return;
                }
                commandToCopy = result.ResolvedCommand;
            }

            await _clipboard.SetTextAsync(commandToCopy).ConfigureAwait(true);
            snip.UsageCount++;
            snip.LastUsedAt = _clock.UtcNow;
            await SaveAndRefreshAsync().ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task EditSnipAsync(SnipCardViewModel? cardVm)
        {
            if (cardVm is null)
            {
                return;
            }
            var result = await _interactions
                .EditSnipAsync(cardVm.Snip, [.. _document.Clis])
                .ConfigureAwait(true);
            if (result is null)
            {
                return;
            }

            var index = _document.Snips.FindIndex(s => s.Id == cardVm.Snip.Id);
            if (index < 0)
            {
                return;
            }
            _document.Snips[index] = result.Snip;
            await SaveAndRefreshAsync().ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task DeleteSnipAsync(SnipCardViewModel? cardVm)
        {
            if (cardVm is null)
            {
                return;
            }
            var confirmed = await _interactions.ConfirmAsync(
                "Delete snip",
                $"Move “{cardVm.Snip.Title}” to trash?",
                "Delete",
                "Cancel").ConfigureAwait(true);
            if (!confirmed)
            {
                return;
            }
            cardVm.Snip.IsTrash = true;
            await SaveAndRefreshAsync().ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task RestoreSnipAsync(SnipCardViewModel? cardVm)
        {
            if (cardVm is null)
            {
                return;
            }
            cardVm.Snip.IsTrash = false;
            await SaveAndRefreshTrashAsync().ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task DeleteForeverAsync(SnipCardViewModel? cardVm)
        {
            if (cardVm is null)
            {
                return;
            }
            var confirmed = await _interactions.ConfirmAsync(
                "Delete permanently",
                $"Permanently delete “{cardVm.Snip.Title}”? This can't be undone.",
                "Delete",
                "Cancel").ConfigureAwait(true);
            if (!confirmed)
            {
                return;
            }
            _ = _document.Snips.RemoveAll(s => s.Id == cardVm.Snip.Id);
            await SaveAndRefreshTrashAsync().ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task ToggleFavouriteAsync(SnipCardViewModel? cardVm)
        {
            if (cardVm is null)
            {
                return;
            }
            cardVm.Snip.IsFavourite = !cardVm.Snip.IsFavourite;
            await SaveAndRefreshAsync().ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task NewSnipAsync()
        {
            if (SelectedCliChoice?.Cli is not { } cli)
            {
                return;
            }
            var fresh = new Snip { CliId = cli.Id };
            var result = await _interactions
                .EditSnipAsync(fresh, [.. _document.Clis])
                .ConfigureAwait(true);
            if (result is null)
            {
                return;
            }

            _document.Snips.Add(result.Snip);
            await SaveAndRefreshAsync().ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task NewCliAsync()
        {
            var fresh = new Cli();
            var result = await _interactions.EditCliAsync(fresh).ConfigureAwait(true);
            if (result is null)
            {
                return;
            }

            var saved = result.Cli;
            if (result.RawIconBytes is { Length: > 0 } bytes)
            {
                saved = new Cli
                {
                    Id = saved.Id,
                    Name = saved.Name,
                    IconRef = await _iconStorage.SaveIconAsync(saved.Id, bytes).ConfigureAwait(true),
                    Parameters = saved.Parameters,
                };
            }

            _document.Clis.Add(saved);
            await SaveAndRefreshAsync().ConfigureAwait(true);
            // Switch to the freshly-created CLI so the user can immediately add Snips.
            SelectedCliChoice = CliChoices.FirstOrDefault(c => c.Cli?.Id == saved.Id) ?? SelectedCliChoice;
        }

        [RelayCommand]
        private void SelectCli(CliCardViewModel? card)
        {
            if (card is null)
            {
                return;
            }
            var choice = CliChoices.FirstOrDefault(c => c.Cli?.Id == card.Id);
            if (choice is not null)
            {
                SelectedCliChoice = choice;
            }
        }

        [RelayCommand]
        private async Task EditCurrentCliAsync()
        {
            if (SelectedCliChoice?.Cli is not { } current)
            {
                return;
            }
            var result = await _interactions.EditCliAsync(current).ConfigureAwait(true);
            if (result is null)
            {
                return;
            }

            var updated = result.Cli;
            if (result.RawIconBytes is { Length: > 0 } bytes)
            {
                updated = new Cli
                {
                    Id = updated.Id,
                    Name = updated.Name,
                    IconRef = await _iconStorage.SaveIconAsync(updated.Id, bytes).ConfigureAwait(true),
                    Parameters = updated.Parameters,
                };
            }

            var index = _document.Clis.FindIndex(c => c.Id == current.Id);
            if (index < 0)
            {
                return;
            }
            _document.Clis[index] = updated;
            await SaveAndRefreshAsync().ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task DeleteCurrentCliAsync()
        {
            if (SelectedCliChoice?.Cli is not { } cli)
            {
                return;
            }

            // Must-be-empty semantics: a CLI can only be deleted once its visible
            // (non-trashed) snips are gone. Trashed snips don't block — they're
            // already soft-deleted and the user can't see them.
            var activeSnipCount = _document.Snips.Count(s => s.CliId == cli.Id && !s.IsTrash);
            if (activeSnipCount > 0)
            {
                await _interactions.NotifyAsync(
                    "Can't delete CLI",
                    $"“{cli.Name}” still has {activeSnipCount} snip{(activeSnipCount == 1 ? "" : "s")}. " +
                    "Delete those snips first, then delete the CLI.").ConfigureAwait(true);
                return;
            }

            var confirmed = await _interactions.ConfirmAsync(
                "Delete CLI",
                $"Delete “{cli.Name}”? This can't be undone.",
                "Delete",
                "Cancel").ConfigureAwait(true);
            if (!confirmed)
            {
                return;
            }

            // Remove the CLI and any trashed snips that belonged to it — otherwise
            // those snips would be orphaned, pointing at a CLI that no longer exists.
            _ = _document.Snips.RemoveAll(s => s.CliId == cli.Id);
            _ = _document.Clis.RemoveAll(c => c.Id == cli.Id);

            // Persist the removal first; the deleted CLI is no longer in CliChoices
            // so SaveAndRefreshAsync falls back to the first choice (Home).
            await SaveAndRefreshAsync().ConfigureAwait(true);

            // Only after the store is safely persisted do we clean up the icon —
            // a best-effort side effect. Doing it earlier would risk deleting the
            // asset while a failed save left the store still referencing it.
            if (!string.IsNullOrEmpty(cli.IconRef))
            {
                await _iconStorage.DeleteIconAsync(cli.IconRef).ConfigureAwait(true);
            }
        }

        partial void OnSelectedCliChoiceChanged(CliChoice? value)
        {
            // The initial/programmatic set is orchestrated by the caller (LoadAsync /
            // SaveAndRefresh) under suppression; only react to user switcher changes.
            if (_suppressShellRefresh)
            {
                return;
            }
            _suppressShellRefresh = true;
            try
            {
                RebuildTags();
                // Changing the CLI shows that scope's snips (the "All" tag).
                SelectedTagItem = Tags.FirstOrDefault(t => t.IsAll);
            }
            finally
            {
                _suppressShellRefresh = false;
            }
            ApplyShellContent();
            OnPropertyChanged(nameof(CanCreateNewSnip));
        }

        partial void OnSelectedTagChanged(string? value)
        {
            if (_suppressShellRefresh)
            {
                return;
            }
            ApplyShellContent();
        }

        // The nav binds its selection to SelectedTagItem; mirror it onto the
        // SelectedTag filter string (the "All" item maps to the sentinel).
        partial void OnSelectedTagItemChanged(TagItemViewModel? value)
        {
            SelectedTag = value?.Name;
        }

        partial void OnSearchTextChanged(string value)
        {
            if (_suppressShellRefresh)
            {
                return;
            }
            ApplyShellContent();
        }

        private void RebuildCliChoices()
        {
            CliChoices.Clear();
            CliChoices.Add(new CliChoice { Display = "All" });
            foreach (var cli in _document.Clis.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                CliChoices.Add(new CliChoice { Cli = cli, Display = cli.Name });
            }
        }

        // Snips in the current switcher scope: a single CLI, or all CLIs when "All".
        private IEnumerable<Snip> ScopedSnips() =>
            SelectedCliChoice?.Cli is { } cli
                ? _document.Snips.Where(s => s.CliId == cli.Id)
                : _document.Snips;

        private void RebuildTags()
        {
            Tags.Clear();
            Tags.Add(new TagItemViewModel(AllTagsSentinel, _allTagsGlyph, isAll: true));
            foreach (var tag in SnipFilter.DistinctTagsFor(ScopedSnips())
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
            {
                var glyph = _document.TagIcons.TryGetValue(tag, out var g) && !string.IsNullOrWhiteSpace(g)
                    ? g
                    : TagItemViewModel.DefaultGlyph;
                Tags.Add(new TagItemViewModel(tag, glyph));
            }
        }

        private void ApplyShellContent()
        {
            // No tag selected => the Home launcher. A tag (or "All") => the snip
            // list for the current scope, filtered by that tag and the search text.
            if (SelectedTagItem is null)
            {
                CurrentContent = new HomeViewModel(_document, SearchText);
                return;
            }

            var effectiveTag = SelectedTagItem.IsAll ? null : SelectedTagItem.Name;
            var filtered = SnipFilter.Apply(ScopedSnips(), SearchText, effectiveTag).ToList();
            CurrentContent = new CliViewModel(SelectedCliChoice?.Cli, filtered);
        }

        private TrashViewModel BuildTrashViewModel()
        {
            var trashed = _document.Snips.Where(s => s.IsTrash);
            return new TrashViewModel(trashed);
        }

        // Trash actions (restore / delete-forever) never add or remove a CLI, so
        // the CLI switcher doesn't need rebuilding. But the pane tag list for the
        // currently-selected CLI can go stale — a restore can surface a tag no
        // visible snip had, and a purge can orphan one — and that list stays on
        // screen while the user is on the Trash view. So rebuild the tags in
        // place, then refresh the Trash list, all while keeping the user on the
        // Trash view (suppressing the shell-content swap that a tag change would
        // otherwise trigger).
        private async Task SaveAndRefreshTrashAsync()
        {
            await _store.SaveAsync(_document).ConfigureAwait(true);

            var wasHome = SelectedTagItem is null;
            var previousTagName = SelectedTagItem?.Name;
            _suppressShellRefresh = true;
            try
            {
                RebuildTags();
                RestoreTagSelection(wasHome, previousTagName);
            }
            finally
            {
                _suppressShellRefresh = false;
            }

            CurrentContent = BuildTrashViewModel();
        }

        private async Task SaveAndRefreshAsync()
        {
            await _store.SaveAsync(_document).ConfigureAwait(true);
            var previousCliId = SelectedCliChoice?.Cli?.Id;
            var wasHome = SelectedTagItem is null;
            var previousTagName = SelectedTagItem?.Name;

            _suppressShellRefresh = true;
            try
            {
                RebuildCliChoices();
                SelectedCliChoice = CliChoices.FirstOrDefault(c => c.Cli?.Id == previousCliId)
                    ?? CliChoices.FirstOrDefault();
                RebuildTags();
                RestoreTagSelection(wasHome, previousTagName);
            }
            finally
            {
                _suppressShellRefresh = false;
            }
            ApplyShellContent();
        }

        // Re-apply the prior nav selection after a tag rebuild: stay on Home when
        // the user was on Home, otherwise reselect the same tag (falling back to
        // "All" if that tag no longer exists in scope).
        private void RestoreTagSelection(bool wasHome, string? previousTagName)
        {
            SelectedTagItem = wasHome
                ? null
                : Tags.FirstOrDefault(t => string.Equals(t.Name, previousTagName, StringComparison.Ordinal))
                  ?? Tags.FirstOrDefault(t => t.IsAll);
        }
    }
}
