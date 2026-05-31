using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>The snip category shown by the Home page's segmented selector.</summary>
    public enum HomeSnipCategory
    {
        MostUsed,
        Recent,
        Favourites,
    }

    public sealed partial class HomeViewModel : ObservableObject
    {
        /// <summary>How many snips each Home category shows.</summary>
        public const int CategoryLimit = 9;

        public HomeViewModel(SnipStoreDocument document, string? searchText)
        {
            ArgumentNullException.ThrowIfNull(document);

            CliCards = new ObservableCollection<CliCardViewModel>(BuildCliCards(document, searchText));

            // Category lists are unscoped — they draw from every CLI's snips.
            var snips = SnipFilter.Apply(document.Snips, searchText, selectedTag: null).ToList();

            MostUsedSnips = new ObservableCollection<SnipCardViewModel>(
                snips.Where(s => s.UsageCount > 0)
                    .OrderByDescending(s => s.UsageCount)
                    .ThenByDescending(s => s.LastUsedAt ?? DateTimeOffset.MinValue)
                    .Take(CategoryLimit)
                    .Select(s => new SnipCardViewModel(s)));

            RecentSnips = new ObservableCollection<SnipCardViewModel>(
                snips.Where(s => s.LastUsedAt is not null)
                    .OrderByDescending(s => s.LastUsedAt)
                    .Take(CategoryLimit)
                    .Select(s => new SnipCardViewModel(s)));

            FavouriteSnips = new ObservableCollection<SnipCardViewModel>(
                snips.Where(s => s.IsFavourite)
                    .OrderByDescending(s => s.LastUsedAt ?? DateTimeOffset.MinValue)
                    .ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
                    .Take(CategoryLimit)
                    .Select(s => new SnipCardViewModel(s)));
        }

        public ObservableCollection<CliCardViewModel> CliCards { get; }

        public ObservableCollection<SnipCardViewModel> MostUsedSnips { get; }

        public ObservableCollection<SnipCardViewModel> RecentSnips { get; }

        public ObservableCollection<SnipCardViewModel> FavouriteSnips { get; }

        public bool HasCliCards => CliCards.Count > 0;

        [ObservableProperty]
        public partial HomeSnipCategory SelectedCategory { get; set; } = HomeSnipCategory.MostUsed;

        /// <summary>The snips shown below the selector, for the chosen category.</summary>
        public ObservableCollection<SnipCardViewModel> ActiveSnips => SelectedCategory switch
        {
            HomeSnipCategory.MostUsed => MostUsedSnips,
            HomeSnipCategory.Recent => RecentSnips,
            HomeSnipCategory.Favourites => FavouriteSnips,
            _ => MostUsedSnips,
        };

        public bool HasActiveSnips => ActiveSnips.Count > 0;

        // OneWay flags for the selector buttons' checked state.
        public bool IsMostUsedSelected => SelectedCategory == HomeSnipCategory.MostUsed;

        public bool IsRecentSelected => SelectedCategory == HomeSnipCategory.Recent;

        public bool IsFavouritesSelected => SelectedCategory == HomeSnipCategory.Favourites;

        [RelayCommand]
        private void SelectMostUsed() => SelectedCategory = HomeSnipCategory.MostUsed;

        [RelayCommand]
        private void SelectRecent() => SelectedCategory = HomeSnipCategory.Recent;

        [RelayCommand]
        private void SelectFavourites() => SelectedCategory = HomeSnipCategory.Favourites;

        partial void OnSelectedCategoryChanged(HomeSnipCategory value)
        {
            OnPropertyChanged(nameof(ActiveSnips));
            OnPropertyChanged(nameof(HasActiveSnips));
            OnPropertyChanged(nameof(IsMostUsedSelected));
            OnPropertyChanged(nameof(IsRecentSelected));
            OnPropertyChanged(nameof(IsFavouritesSelected));
        }

        private static IEnumerable<CliCardViewModel> BuildCliCards(SnipStoreDocument document, string? searchText)
        {
            var snipsByCli = document.Snips
                .Where(s => !s.IsTrash)
                .GroupBy(s => s.CliId)
                .ToDictionary(g => g.Key, g => g.Count());

            var matcher = string.IsNullOrWhiteSpace(searchText)
                ? null
                : searchText.Trim();

            foreach (var cli in document.Clis.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (matcher is not null
                    && !cli.Name.Contains(matcher, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                _ = snipsByCli.TryGetValue(cli.Id, out var count);
                yield return new CliCardViewModel(cli, count);
            }
        }
    }
}
