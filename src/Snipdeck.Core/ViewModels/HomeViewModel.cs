using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class HomeViewModel : ObservableObject
    {
        public const int MostUsedLimit = 6;

        public HomeViewModel(SnipStoreDocument document, string? searchText)
        {
            ArgumentNullException.ThrowIfNull(document);

            CliCards = new ObservableCollection<CliCardViewModel>(BuildCliCards(document, searchText));
            MostUsedSnips = new ObservableCollection<SnipCardViewModel>(BuildMostUsedSnips(document, searchText));
        }

        public ObservableCollection<CliCardViewModel> CliCards { get; }

        public ObservableCollection<SnipCardViewModel> MostUsedSnips { get; }

        public bool HasCliCards => CliCards.Count > 0;

        public bool HasMostUsedSnips => MostUsedSnips.Count > 0;

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

        private static IEnumerable<SnipCardViewModel> BuildMostUsedSnips(SnipStoreDocument document, string? searchText)
        {
            var filtered = SnipFilter.Apply(document.Snips, searchText, selectedTag: null);
            return filtered
                .Where(s => s.UsageCount > 0)
                .OrderByDescending(s => s.UsageCount)
                .ThenByDescending(s => s.LastUsedAt ?? DateTimeOffset.MinValue)
                .Take(MostUsedLimit)
                .Select(s => new SnipCardViewModel(s));
        }
    }
}
