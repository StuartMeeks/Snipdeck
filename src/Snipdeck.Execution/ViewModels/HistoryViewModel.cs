using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Snipdeck.Core.Abstractions;
using Snipdeck.Execution.Abstractions;

namespace Snipdeck.Execution.ViewModels
{
    /// <summary>
    /// The History content state: runs newest-first with text search, opening a run into
    /// the shared run view (replay mode), deleting one, or clearing all. UI-free; the
    /// shell handles <see cref="OpenRequested"/> by loading the replay view.
    /// </summary>
    public sealed partial class HistoryViewModel(
        ICommandHistoryStore store,
        IShellInteractions interactions,
        Func<Guid, string> snipTitleResolver,
        Func<Guid, string> cliNameResolver) : ObservableObject
    {
        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmpty))]
        public partial bool IsLoaded { get; set; }

        /// <summary>Raised when the user opens a run; the argument is the run id to load in replay mode.</summary>
        public event EventHandler<Guid>? OpenRequested;

        /// <summary>The runs currently shown, newest-first.</summary>
        public ObservableCollection<HistoryItemViewModel> Items { get; } = [];

        /// <summary>True when a load has completed and there are no runs to show.</summary>
        public bool IsEmpty => IsLoaded && Items.Count == 0;

        /// <summary>Loads (or reloads) the list using the current <see cref="SearchText"/>.</summary>
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            var entries = await store.QueryAsync(SearchText, snipId: null, cancellationToken).ConfigureAwait(false);

            Items.Clear();
            foreach (var entry in entries)
            {
                Items.Add(new HistoryItemViewModel(entry, snipTitleResolver, cliNameResolver));
            }

            IsLoaded = true;
            OnPropertyChanged(nameof(IsEmpty));
        }

        [RelayCommand]
        private async Task SearchAsync() => await LoadAsync().ConfigureAwait(false);

        [RelayCommand]
        private void Open(HistoryItemViewModel? item)
        {
            if (item is not null)
            {
                OpenRequested?.Invoke(this, item.Id);
            }
        }

        [RelayCommand]
        private async Task DeleteAsync(HistoryItemViewModel? item)
        {
            if (item is null)
            {
                return;
            }

            var confirmed = await interactions.ConfirmAsync(
                "Delete run",
                $"Delete this run of \"{item.SnipTitle}\"? The recorded output will be lost.",
                confirmButtonText: "Delete",
                destructive: true).ConfigureAwait(false);

            if (!confirmed)
            {
                return;
            }

            await store.DeleteAsync(item.Id).ConfigureAwait(false);
            await LoadAsync().ConfigureAwait(false);
        }

        [RelayCommand]
        private async Task ClearAllAsync()
        {
            if (Items.Count == 0)
            {
                return;
            }

            var confirmed = await interactions.ConfirmAsync(
                "Clear history",
                "Delete every recorded run? This cannot be undone.",
                confirmButtonText: "Clear all",
                destructive: true).ConfigureAwait(false);

            if (!confirmed)
            {
                return;
            }

            await store.ClearAsync().ConfigureAwait(false);
            await LoadAsync().ConfigureAwait(false);
        }
    }
}
