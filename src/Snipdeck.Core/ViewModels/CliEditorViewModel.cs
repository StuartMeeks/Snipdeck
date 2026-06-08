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
            Description = cli.Description;
            ShellIndex = (int)cli.Shell;
            CustomShellPath = cli.CustomShellPath ?? string.Empty;
            CustomShellArgsTemplate = cli.CustomShellArgsTemplate ?? string.Empty;
            ExecutablePath = cli.ExecutablePath ?? string.Empty;
            WorkingDirectory = cli.WorkingDirectory ?? string.Empty;
            Parameters = new ObservableCollection<ParameterEditorRowViewModel>(
                cli.Parameters.Select(p => new ParameterEditorRowViewModel(p)));
        }

        public Cli Cli { get; }

        /// <summary>The shell, as a <see cref="ShellKind"/> backed combo-box index.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustomShell))]
        public partial int ShellIndex { get; set; }

        /// <summary>True when the custom shell is selected, revealing its path/args fields.</summary>
        public bool IsCustomShell => ShellIndex == (int)ShellKind.Custom;

        [ObservableProperty]
        public partial string CustomShellPath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string CustomShellArgsTemplate { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string ExecutablePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string WorkingDirectory { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Description { get; set; } = string.Empty;

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
                Description = Description.Trim(),
                IconRef = Cli.IconRef,
                Parameters = [.. Parameters.Select(r => r.BuildParameter())],
                Shell = (ShellKind)ShellIndex,
                CustomShellPath = NullIfBlank(CustomShellPath),
                CustomShellArgsTemplate = NullIfBlank(CustomShellArgsTemplate),
                ExecutablePath = NullIfBlank(ExecutablePath),
                WorkingDirectory = NullIfBlank(WorkingDirectory),
            };
        }

        private static string? NullIfBlank(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
