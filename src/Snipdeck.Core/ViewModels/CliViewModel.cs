using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class CliViewModel : ObservableObject
    {
        public CliViewModel(Cli cli, IEnumerable<Snip> filteredSnips)
        {
            ArgumentNullException.ThrowIfNull(cli);
            ArgumentNullException.ThrowIfNull(filteredSnips);

            Cli = cli;
            Snips = new ObservableCollection<SnipCardViewModel>(
                filteredSnips
                    .OrderByDescending(s => s.IsFavourite)
                    .ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
                    .Select(s => new SnipCardViewModel(s)));
        }

        public Cli Cli { get; }

        public string Name => Cli.Name;

        public ObservableCollection<SnipCardViewModel> Snips { get; }

        public bool HasSnips => Snips.Count > 0;

        public bool IsEmpty => Snips.Count == 0;
    }
}
