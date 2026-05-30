using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class CliViewModel : ObservableObject
    {
        public CliViewModel(Cli? cli, IEnumerable<Snip> filteredSnips)
        {
            ArgumentNullException.ThrowIfNull(filteredSnips);

            Cli = cli;
            Snips = new ObservableCollection<SnipCardViewModel>(
                filteredSnips
                    .OrderByDescending(s => s.IsFavourite)
                    .ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
                    .Select(s => new SnipCardViewModel(s)));
        }

        /// <summary>The CLI in scope, or null for the unscoped "All" view across every CLI.</summary>
        public Cli? Cli { get; }

        /// <summary>True for a single CLI — gates the CLI-specific header actions (edit/delete/new).</summary>
        public bool HasCli => Cli is not null;

        public string Name => Cli?.Name ?? "All snips";

        public ObservableCollection<SnipCardViewModel> Snips { get; }

        public bool HasSnips => Snips.Count > 0;

        public bool IsEmpty => Snips.Count == 0;
    }
}
