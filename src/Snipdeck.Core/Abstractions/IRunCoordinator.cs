using Snipdeck.Core.Models;

namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Drives the "run a Snip" flow without exposing execution types to Core. The
    /// implementation (in Snipdeck.Execution) runs the parameter-fill + dry-run
    /// preview, resolves the command, builds the run, and returns a view model for
    /// the shell to display as its current content. Returns <c>null</c> if the user
    /// cancelled at the preview, or surfaces a friendly notice (e.g. a missing
    /// executable) and returns <c>null</c> rather than throwing.
    /// </summary>
    public interface IRunCoordinator
    {
        /// <summary>
        /// Prepares and (on confirmation) begins a run for <paramref name="snip"/>.
        /// The returned object is the run view model to set as the shell's current
        /// content; it is typed as <see cref="object"/> so Core stays free of any
        /// execution dependency. <c>null</c> means no run was started.
        /// </summary>
        Task<object?> CreateRunAsync(
            Snip snip,
            Cli? cli,
            IReadOnlyList<Parameter> resolvedParameters);
    }
}
