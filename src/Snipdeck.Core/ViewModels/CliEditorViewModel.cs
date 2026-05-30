using System.Collections.ObjectModel;

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
            Parameters = new ObservableCollection<ParameterEditorRowViewModel>(
                cli.Parameters.Select(p => new ParameterEditorRowViewModel(p)));
        }

        public Cli Cli { get; }

        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial byte[]? PickedIconBytes { get; set; }

        [ObservableProperty]
        public partial string? PickedIconFileName { get; set; }

        /// <summary>CLI-scoped shared parameter definitions (inherited by this CLI's snips).</summary>
        public ObservableCollection<ParameterEditorRowViewModel> Parameters { get; }

        public bool CanSave => !string.IsNullOrWhiteSpace(Name);

        public void AddParameter()
        {
            Parameters.Add(new ParameterEditorRowViewModel(new Parameter { Name = "param" }));
        }

        public void RemoveParameter(ParameterEditorRowViewModel row)
        {
            _ = Parameters.Remove(row);
        }

        public Cli BuildUpdatedCli()
        {
            return new Cli
            {
                Id = Cli.Id,
                Name = Name.Trim(),
                IconRef = Cli.IconRef,
                Parameters = [.. Parameters.Select(r => r.BuildParameter())],
            };
        }
    }
}
