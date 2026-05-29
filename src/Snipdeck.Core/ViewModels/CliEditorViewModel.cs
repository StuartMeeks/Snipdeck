using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class CliEditorViewModel : ObservableObject
    {
        public CliEditorViewModel(Cli cli)
        {
            ArgumentNullException.ThrowIfNull(cli);

            Cli = cli;
            Name = cli.Name;
        }

        public Cli Cli { get; }

        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial byte[]? PickedIconBytes { get; set; }

        [ObservableProperty]
        public partial string? PickedIconFileName { get; set; }

        public bool CanSave => !string.IsNullOrWhiteSpace(Name);

        public Cli BuildUpdatedCli()
        {
            return new Cli
            {
                Id = Cli.Id,
                Name = Name.Trim(),
                IconRef = Cli.IconRef,
            };
        }
    }
}
