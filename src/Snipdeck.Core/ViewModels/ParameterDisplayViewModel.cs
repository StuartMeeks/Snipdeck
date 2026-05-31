using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>Read-only display of a shared parameter, shown as a card.</summary>
    public sealed class ParameterDisplayViewModel
    {
        public ParameterDisplayViewModel(Parameter parameter)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            Name = parameter.Name;
            IsChoice = parameter.Type == ParameterType.Choice;
            TypeDisplay = IsChoice ? "Choice" : "Text";
            Default = parameter.Default ?? string.Empty;
            OptionsDisplay = string.Join(", ", parameter.Options);
        }

        public string Name { get; }

        public string TypeDisplay { get; }

        public bool IsChoice { get; }

        public string Default { get; }

        public bool HasDefault => !string.IsNullOrEmpty(Default);

        public string OptionsDisplay { get; }

        public bool HasOptions => IsChoice && !string.IsNullOrEmpty(OptionsDisplay);
    }
}
