using Snipdeck.Core.Models;

namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Presents shell-level UI (dialogs, confirmations, parameter fill) from
    /// Core view models. Implementations live in the App project and use
    /// WinUI ContentDialogs; Core stays UI-free.
    /// </summary>
    public interface IShellInteractions
    {
        Task<bool> ConfirmAsync(
            string title,
            string message,
            string confirmButtonText = "Yes",
            string cancelButtonText = "Cancel");

        Task NotifyAsync(
            string title,
            string message,
            string buttonText = "OK");

        Task<SnipEditResult?> EditSnipAsync(Snip snip, IReadOnlyList<Cli> availableClis);

        Task<CliEditResult?> EditCliAsync(Cli cli);

        Task<ParameterFillResult?> FillParametersAsync(Snip snip, IReadOnlyList<Parameter> parameters);
    }

    public sealed record SnipEditResult(Snip Snip);

    public sealed record CliEditResult(Cli Cli, byte[]? RawIconBytes);

    public sealed record ParameterFillResult(string ResolvedCommand);
}
