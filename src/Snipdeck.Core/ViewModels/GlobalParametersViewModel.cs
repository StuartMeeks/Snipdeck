using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// The "Shared parameters" content view: edits the global (cross-CLI)
    /// parameter definitions. Add/remove rows here; the shell persists them via
    /// its SaveGlobalParameters command.
    /// </summary>
    public sealed partial class GlobalParametersViewModel : ObservableObject
    {
        public GlobalParametersViewModel(IReadOnlyList<Parameter> parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            Parameters = new ObservableCollection<ParameterEditorRowViewModel>(
                parameters.Select(p => new ParameterEditorRowViewModel(p)));
        }

        public ObservableCollection<ParameterEditorRowViewModel> Parameters { get; }

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        [RelayCommand]
        private void AddParameter()
        {
            Parameters.Add(new ParameterEditorRowViewModel(new Parameter { Name = "param" }));
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private void RemoveParameter(ParameterEditorRowViewModel? row)
        {
            if (row is not null)
            {
                _ = Parameters.Remove(row);
                StatusMessage = string.Empty;
            }
        }

        public List<Parameter> BuildParameters() => [.. Parameters.Select(r => r.BuildParameter())];
    }
}
