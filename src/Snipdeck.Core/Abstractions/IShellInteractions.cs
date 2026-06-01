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
