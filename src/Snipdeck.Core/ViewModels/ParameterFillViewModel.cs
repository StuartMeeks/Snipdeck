using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Engine;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class ParameterFillViewModel : ObservableObject
    {
        private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);

        public ParameterFillViewModel(Snip snip)
        {
            ArgumentNullException.ThrowIfNull(snip);

            Snip = snip;
            Inputs = new ObservableCollection<ParameterInputViewModel>(
                snip.Parameters.Select(p => new ParameterInputViewModel(p, OnInputValueChanged)));

            foreach (var input in Inputs)
            {
                _values[input.Name] = input.Value;
            }

            UpdateResolution();
        }

        public Snip Snip { get; }

        public string? Description => Snip.Description;

        public bool HasDescription => !string.IsNullOrWhiteSpace(Snip.Description);

        public ObservableCollection<ParameterInputViewModel> Inputs { get; }

        [ObservableProperty]
        public partial string Preview { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsCopyEnabled { get; set; }

        public string ResolvedCommand => Preview;

        private void OnInputValueChanged(ParameterInputViewModel input)
        {
            _values[input.Name] = input.Value;
            UpdateResolution();
        }

        private void UpdateResolution()
        {
            var result = SubstitutionEngine.Substitute(Snip.CommandTemplate, _values);
            Preview = result.Text;
            IsCopyEnabled = result.IsFullyResolved;
        }
    }
}
