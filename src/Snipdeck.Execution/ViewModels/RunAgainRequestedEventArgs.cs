namespace Snipdeck.Execution.ViewModels
{
    /// <summary>
    /// Raised by a run view's "Run again": the Snip to re-run and the parameter values
    /// the original run used (to pre-fill the Run dialog), or <c>null</c> when unknown.
    /// </summary>
    public sealed class RunAgainRequestedEventArgs(
        Guid snipId,
        IReadOnlyDictionary<string, string?>? parameterValues) : EventArgs
    {
        /// <summary>The Snip to run again.</summary>
        public Guid SnipId { get; } = snipId;

        /// <summary>The original run's parameter values keyed by name, or <c>null</c> when unknown.</summary>
        public IReadOnlyDictionary<string, string?>? ParameterValues { get; } = parameterValues;
    }
}
