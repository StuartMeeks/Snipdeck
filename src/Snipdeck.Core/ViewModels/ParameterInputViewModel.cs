using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class ParameterInputViewModel : ObservableObject
    {
        private readonly Action<ParameterInputViewModel> _onValueChanged;

        public ParameterInputViewModel(Parameter parameter, Action<ParameterInputViewModel> onValueChanged)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            ArgumentNullException.ThrowIfNull(onValueChanged);

            Parameter = parameter;
            // Assign the callback first so the Value setter's change handler can use it.
            _onValueChanged = onValueChanged;
            Value = parameter.Default;
        }

        public Parameter Parameter { get; }

        public string Name => Parameter.Name;

        public ParameterType Type => Parameter.Type;

        public bool IsChoice => Parameter.Type == ParameterType.Choice;

        public bool IsText => Parameter.Type == ParameterType.Text;

        public IReadOnlyList<string> Options => Parameter.Options;

        [ObservableProperty]
        public partial string? Value { get; set; }

        partial void OnValueChanged(string? value)
        {
            _onValueChanged(this);
        }
    }
}
