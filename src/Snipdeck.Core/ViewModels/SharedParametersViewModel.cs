using System.Collections.ObjectModel;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// Read-only "Shared parameters" content view: lists the definitions as cards.
    /// Editing happens in a modal (the shell's EditSharedParameters command).
    /// Used for both the global set (<see cref="IsGlobal"/>) and a single CLI's set.
    /// </summary>
    public sealed class SharedParametersViewModel
    {
        public SharedParametersViewModel(string title, string description, bool isGlobal, IReadOnlyList<Parameter> parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);

            Title = title;
            Description = description;
            IsGlobal = isGlobal;
            Parameters = new ObservableCollection<ParameterDisplayViewModel>(
                parameters.Select(p => new ParameterDisplayViewModel(p)));
        }

        public string Title { get; }

        public string Description { get; }

        /// <summary>True for the cross-CLI global set; false for a single CLI's set.</summary>
        public bool IsGlobal { get; }

        public ObservableCollection<ParameterDisplayViewModel> Parameters { get; }

        public bool IsEmpty => Parameters.Count == 0;
    }
}
