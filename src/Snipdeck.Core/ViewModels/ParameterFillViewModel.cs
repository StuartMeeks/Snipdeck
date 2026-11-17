using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Engine;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class ParameterFillViewModel : ObservableObject
    {
        private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);
        private readonly bool _hasParameters;

        public ParameterFillViewModel(Snip snip, IReadOnlyList<Parameter> parameters)
            : this(snip, parameters, isRunMode: false, shellDisplay: string.Empty, workingDirectory: string.Empty)
        {
        }

        /// <summary>
        /// Run-mode constructor: the same live preview, but the dialog presents it as the
        /// dry-run safety gate — the resolved command alongside the <paramref name="shellDisplay"/>
        /// and <paramref name="workingDirectory"/> it will execute under.
        /// <paramref name="initialValues"/>, when supplied, pre-fill the inputs in place of
        /// the parameter defaults (names that no longer exist, and choice values that are
        /// no longer offered, are ignored so the fill falls back to the default).
        /// </summary>
        public ParameterFillViewModel(
            Snip snip,
            IReadOnlyList<Parameter> parameters,
            bool isRunMode,
            string shellDisplay,
            string workingDirectory,
            IReadOnlyDictionary<string, string?>? initialValues = null)
        {
            ArgumentNullException.ThrowIfNull(snip);
            ArgumentNullException.ThrowIfNull(parameters);

            IsRunMode = isRunMode;
            ShellDisplay = shellDisplay;
            WorkingDirectory = workingDirectory;
            Snip = snip;
            // Captured before building Inputs: a parameter with a default fires its
            // change callback (→ UpdateResolution) during construction, before the
            // Inputs collection is assigned, so UpdateResolution can't read Inputs.
            _hasParameters = parameters.Count > 0;
            Inputs = new ObservableCollection<ParameterInputViewModel>(
                parameters.Select(p => new ParameterInputViewModel(p, OnInputValueChanged)));

            foreach (var input in Inputs)
            {
                if (initialValues is not null
                    && initialValues.TryGetValue(input.Name, out var initial)
                    && IsAcceptable(input, initial))
                {
                    input.Value = initial;
                }

                _values[input.Name] = input.Value;
            }

            UpdateResolution();
        }

        public Snip Snip { get; }

        /// <summary>True when this fill is for a Run (primary button "Run", shows shell + working dir).</summary>
        public bool IsRunMode { get; }

        /// <summary>Human-readable shell that will execute the command (Run mode only).</summary>
        public string ShellDisplay { get; }

        /// <summary>Working directory the command will run in (Run mode only).</summary>
        public string WorkingDirectory { get; }

        public string? Description => Snip.Description;

        public bool HasDescription => !string.IsNullOrWhiteSpace(Snip.Description);

        public ObservableCollection<ParameterInputViewModel> Inputs { get; }

        [ObservableProperty]
        public partial string Preview { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsCopyEnabled { get; set; }

        public string ResolvedCommand => Preview;

        /// <summary>A snapshot of the current input values, keyed by parameter name.</summary>
        public IReadOnlyDictionary<string, string?> Values =>
            Inputs.ToDictionary(i => i.Name, i => i.Value, StringComparer.Ordinal);

        private static bool IsAcceptable(ParameterInputViewModel input, string? value) =>
            !input.IsChoice || (value is not null && input.Options.Contains(value, StringComparer.Ordinal));

        private void OnInputValueChanged(ParameterInputViewModel input)
        {
            _values[input.Name] = input.Value;
            UpdateResolution();
        }

        private void UpdateResolution()
        {
            var result = SubstitutionEngine.Substitute(Snip.CommandTemplate, _values);
            Preview = result.Text;
            // With no parameters to fill there's nothing to gate on: the template
            // copies verbatim (unresolved tokens are returned as-is), matching the
            // direct, no-dialog copy path. This is the case for a snip opened in
            // the flyout solely to show its description. Only gate copy on full
            // resolution when there are actually inputs to fill.
            IsCopyEnabled = !_hasParameters || result.IsFullyResolved;
        }
    }
}
