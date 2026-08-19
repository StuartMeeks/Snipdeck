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
            Load(trashedSnips);
        }

        /// <summary>The trashed snips currently shown, ordered by title.</summary>
        public ObservableCollection<SnipCardViewModel> Snips { get; } = [];

        public bool HasSnips => Snips.Count > 0;

        public bool IsEmpty => Snips.Count == 0;

        /// <summary>
        /// Repopulates the list in place. The shell reuses the live instance after a
        /// restore or a permanent delete rather than building a replacement, so the
        /// content area sees collection-change notifications: its bindings resolve
        /// once per template instantiation, and swapping in a new view model of the
        /// same type leaves the stale list on screen until the user navigates away.
        /// </summary>
        public void Load(IEnumerable<Snip> trashedSnips)
        {
            ArgumentNullException.ThrowIfNull(trashedSnips);

            Snips.Clear();
            foreach (var snip in trashedSnips.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase))
            {
                Snips.Add(new SnipCardViewModel(snip));
            }

            OnPropertyChanged(nameof(HasSnips));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
}
