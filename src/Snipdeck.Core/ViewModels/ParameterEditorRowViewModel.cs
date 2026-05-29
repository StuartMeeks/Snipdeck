using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class ParameterEditorRowViewModel : ObservableObject
    {
        public ParameterEditorRowViewModel(Parameter parameter)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            Name = parameter.Name;
            Type = parameter.Type;
            OptionsText = string.Join(", ", parameter.Options);
            Default = parameter.Default ?? string.Empty;
        }

        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ParameterType Type { get; set; } = ParameterType.Text;

        public int TypeIndex
        {
            get => Type == ParameterType.Choice ? 1 : 0;
            set
            {
                Type = value == 1 ? ParameterType.Choice : ParameterType.Text;
                OnPropertyChanged(nameof(IsChoice));
                OnPropertyChanged(nameof(IsText));
            }
        }

        public bool IsChoice => Type == ParameterType.Choice;

        public bool IsText => Type == ParameterType.Text;

        [ObservableProperty]
        public partial string OptionsText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Default { get; set; } = string.Empty;

        public Parameter BuildParameter()
        {
            return new Parameter
            {
                Name = Name.Trim(),
                Type = Type,
                Options = ParseList(OptionsText),
                Default = string.IsNullOrWhiteSpace(Default) ? null : Default.Trim(),
            };
        }

        private static List<string> ParseList(string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? []
                : [.. text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }
    }
}
