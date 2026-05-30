using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// The Trash content view: the cross-CLI list of soft-deleted snips, each of
    /// which can be restored or permanently deleted. Reuses
    /// <see cref="SnipCardViewModel"/> so the card visuals stay consistent with
    /// the rest of the shell.
    /// </summary>
    public sealed partial class TrashViewModel : ObservableObject
    {
        public TrashViewModel(IEnumerable<Snip> trashedSnips)
        {
            ArgumentNullException.ThrowIfNull(trashedSnips);

            Snips = new ObservableCollection<SnipCardViewModel>(
                trashedSnips
                    .OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
                    .Select(s => new SnipCardViewModel(s)));
        }

        public ObservableCollection<SnipCardViewModel> Snips { get; }

        public bool HasSnips => Snips.Count > 0;

        public bool IsEmpty => Snips.Count == 0;
    }
}
