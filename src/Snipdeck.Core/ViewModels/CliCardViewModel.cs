using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class CliCardViewModel : ObservableObject
    {
        public CliCardViewModel(Cli cli, int snipCount)
        {
            ArgumentNullException.ThrowIfNull(cli);
            Cli = cli;
            SnipCount = snipCount;
        }

        public Cli Cli { get; }

        public Guid Id => Cli.Id;

        public string Name => Cli.Name;

        public string? IconRef => Cli.IconRef;

        public int SnipCount { get; }

        public string SnipCountDisplay => SnipCount == 1
            ? "1 snip"
            : $"{SnipCount} snips";
    }
}
