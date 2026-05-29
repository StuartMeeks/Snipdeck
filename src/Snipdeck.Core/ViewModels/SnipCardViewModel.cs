using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class SnipCardViewModel : ObservableObject
    {
        public SnipCardViewModel(Snip snip)
        {
            ArgumentNullException.ThrowIfNull(snip);
            Snip = snip;
            Tags = new ObservableCollection<string>(snip.Tags);
        }

        public Snip Snip { get; }

        public Guid Id => Snip.Id;

        public string Title => Snip.Title;

        public string CommandTemplate => Snip.CommandTemplate;

        public bool IsFavourite => Snip.IsFavourite;

        public int UsageCount => Snip.UsageCount;

        public ObservableCollection<string> Tags { get; }
    }
}
