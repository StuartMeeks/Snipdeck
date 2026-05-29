namespace Snipdeck.Core.Abstractions
{
    public sealed record UpdateCheckResult(bool UpdateAvailable, string? AvailableVersion);

    /// <summary>
    /// Thin wrapper around Velopack so the UI can request an update check
    /// without referencing the SDK directly.
    /// </summary>
    public interface IUpdateService
    {
        Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

        Task<bool> ApplyUpdateAndRestartAsync(CancellationToken cancellationToken = default);
    }
}
