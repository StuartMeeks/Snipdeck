using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Snipdeck.Core.Abstractions;
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

        private readonly ISnipStore _store;
        private readonly IClipboardService _clipboard;
        private readonly IClock _clock;
        private readonly IShellInteractions _interactions;
        private readonly IIconAssetStorage _iconStorage;
        private SnipStoreDocument _document = new();
        private bool _suppressShellRefresh;

        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial CliChoice? SelectedCliChoice { get; set; }

        [ObservableProperty]
        public partial string? SelectedTag { get; set; }

        [ObservableProperty]
        public partial object? CurrentContent { get; set; }

        public ShellViewModel(
            ISnipStore store,
            IClipboardService clipboard,
            IClock clock,
            IShellInteractions interactions,
            IIconAssetStorage iconStorage)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(clipboard);
            ArgumentNullException.ThrowIfNull(clock);
            ArgumentNullException.ThrowIfNull(interactions);
            ArgumentNullException.ThrowIfNull(iconStorage);

            _store = store;
            _clipboard = clipboard;
            _clock = clock;
            _interactions = interactions;
            _iconStorage = iconStorage;
        }

        public ObservableCollection<CliChoice> CliChoices { get; } = [];

        public ObservableCollection<string> Tags { get; } = [];

        public bool CanCreateNewSnip => SelectedCliChoice?.Cli is not null;

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            _document = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            RebuildCliChoices();
            SelectedCliChoice = CliChoices.FirstOrDefault();
        }

        public void OpenSettings(SettingsViewModel settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            CurrentContent = settings;
        }

        public void GoHome()
        {
            SelectedCliChoice = CliChoices.FirstOrDefault(c => c.IsHome);
        }

        [RelayCommand]
        private async Task CopySnipAsync(SnipCardViewModel? cardVm)
        {
            if (cardVm is null)
            {
                return;
            }
            var snip = cardVm.Snip;

            string commandToCopy;
            if (snip.Parameters.Count == 0)
            {
                commandToCopy = snip.CommandTemplate;
            }
            else
            {
                var result = await _interactions.FillParametersAsync(snip).ConfigureAwait(true);
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

        partial void OnSelectedCliChoiceChanged(CliChoice? value)
        {
            _suppressShellRefresh = true;
            try
            {
                RebuildTags();
                SelectedTag = Tags.Count > 0 ? AllTagsSentinel : null;
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
            CliChoices.Add(new CliChoice { Display = "All / Home" });
            foreach (var cli in _document.Clis.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                CliChoices.Add(new CliChoice { Cli = cli, Display = cli.Name });
            }
        }

        private void RebuildTags()
        {
            Tags.Clear();
            if (SelectedCliChoice?.Cli is not { } cli)
            {
                return;
            }
            var snipsForCli = _document.Snips.Where(s => s.CliId == cli.Id);
            Tags.Add(AllTagsSentinel);
            foreach (var tag in SnipFilter.DistinctTagsFor(snipsForCli)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
            {
                Tags.Add(tag);
            }
        }

        private void ApplyShellContent()
        {
            if (SelectedCliChoice?.Cli is { } cli)
            {
                var cliSnips = _document.Snips.Where(s => s.CliId == cli.Id);
                var effectiveTag = SelectedTag == AllTagsSentinel ? null : SelectedTag;
                var filtered = SnipFilter.Apply(cliSnips, SearchText, effectiveTag).ToList();
                CurrentContent = new CliViewModel(cli, filtered);
            }
            else
            {
                CurrentContent = new HomeViewModel(_document, SearchText);
            }
        }

        private async Task SaveAndRefreshAsync()
        {
            await _store.SaveAsync(_document).ConfigureAwait(true);
            var previousCliId = SelectedCliChoice?.Cli?.Id;

            _suppressShellRefresh = true;
            try
            {
                RebuildCliChoices();
                SelectedCliChoice = CliChoices.FirstOrDefault(c => c.Cli?.Id == previousCliId)
                    ?? CliChoices.FirstOrDefault();
                RebuildTags();
                SelectedTag = Tags.Count > 0 ? AllTagsSentinel : null;
            }
            finally
            {
                _suppressShellRefresh = false;
            }
            ApplyShellContent();
        }
    }
}
