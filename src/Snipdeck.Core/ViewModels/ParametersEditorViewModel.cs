using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// Editable list of shared parameter definitions, shown in the edit modal for
    /// both the global set and a CLI's set. Add/remove rows, then BuildParameters.
    /// </summary>
    public sealed partial class ParametersEditorViewModel : ObservableObject
    {
        public ParametersEditorViewModel(string title, IReadOnlyList<Parameter> parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            Title = title;
            Parameters = new ObservableCollection<ParameterEditorRowViewModel>(
                parameters.Select(p => new ParameterEditorRowViewModel(p)));
        }

        public string Title { get; }

        public ObservableCollection<ParameterEditorRowViewModel> Parameters { get; }

        public bool IsEmpty => Parameters.Count == 0;

        [RelayCommand]
        private void AddParameter()
        {
            Parameters.Add(new ParameterEditorRowViewModel(new Parameter { Name = "param" }));
            OnPropertyChanged(nameof(IsEmpty));
        }

        [RelayCommand]
        private void RemoveParameter(ParameterEditorRowViewModel? row)
        {
            if (row is not null)
            {
                _ = Parameters.Remove(row);
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public List<Parameter> BuildParameters() => [.. Parameters.Select(r => r.BuildParameter())];
    }
}
