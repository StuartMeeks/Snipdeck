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
            string cancelButtonText = "Cancel",
            bool destructive = false);

        Task NotifyAsync(
            string title,
            string message,
            string buttonText = "OK");

        Task<SnipEditResult?> EditSnipAsync(Snip snip, IReadOnlyList<Cli> availableClis);

        Task<CliEditResult?> EditCliAsync(Cli cli);

        /// <summary>
        /// Opens the single-parameter edit modal. Pass <paramref name="existing"/> to
        /// edit it, or <c>null</c> to add a new one. Returns the edited parameter, or
        /// <c>null</c> if the user cancelled.
        /// </summary>
        Task<Parameter?> EditParameterAsync(string title, Parameter? existing);

        Task<ParameterFillResult?> FillParametersAsync(Snip snip, IReadOnlyList<Parameter> parameters);

        /// <summary>
        /// The Run variant of the parameter-fill flow: same live preview, but the
        /// primary button reads "Run", the resolved command is shown alongside the
        /// <paramref name="shellDisplay"/> and <paramref name="workingDirectoryDisplay"/>
        /// it will execute under (the dry-run safety gate), and the dialog is shown
        /// even when the Snip has no parameters. Returns the resolved command, or
        /// <c>null</c> if the user cancelled.
        /// </summary>
        Task<ParameterFillResult?> FillParametersForRunAsync(
            Snip snip,
            IReadOnlyList<Parameter> parameters,
            string shellDisplay,
            string workingDirectoryDisplay);

        /// <summary>
        /// Opens the glyph picker so the user can browse and choose an icon.
        /// Pass <paramref name="currentGlyph"/> to pre-select the glyph in effect.
        /// Returns the chosen glyph character, or <c>null</c> if the user cancelled.
        /// </summary>
        Task<string?> PickGlyphAsync(string? currentGlyph);
    }

    public sealed record SnipEditResult(Snip Snip);

    public sealed record CliEditResult(Cli Cli, byte[]? RawIconBytes);

    public sealed record ParameterFillResult(string ResolvedCommand);
}
