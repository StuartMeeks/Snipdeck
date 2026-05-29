using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// The shell's view model owns the cross-cutting state — current CLI
    /// selection, search text, tag filter, and the content view model
    /// currently displayed in the main area.
    /// </summary>
    public sealed partial class ShellViewModel : ObservableObject
    {
        public const string AllTagsSentinel = "All";

        private readonly ISnipStore _store;
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

        public ShellViewModel(ISnipStore store)
        {
            ArgumentNullException.ThrowIfNull(store);
            _store = store;
        }

        public ObservableCollection<CliChoice> CliChoices { get; } = [];

        public ObservableCollection<string> Tags { get; } = [];

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            _document = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            RebuildCliChoices();
            SelectedCliChoice = CliChoices.FirstOrDefault();
        }

        public void OpenSettings()
        {
            CurrentContent = new SettingsViewModel();
        }

        public void GoHome()
        {
            SelectedCliChoice = CliChoices.FirstOrDefault(c => c.IsHome);
        }

        partial void OnSelectedCliChoiceChanged(CliChoice? value)
        {
            // Avoid double-applying the shell content: rebuild tags + set the
            // default tag without triggering the tag-changed handler, then
            // apply the shell content explicitly once.
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
    }
}
