using Snipdeck.Core.Abstractions;

using Velopack;
using Velopack.Sources;

namespace Snipdeck.App.Services
{
    /// <summary>
    /// Velopack-backed updater. Points at the GitHub releases for Snipdeck;
    /// silently no-ops when running from a dev tree (not installed) so the
    /// dev experience matches release-build expectations.
    /// </summary>
    internal sealed class WindowsUpdateService : IUpdateService
    {
        private const string _githubRepoUrl = "https://github.com/StuartMeeks/Snipdeck";

        private readonly UpdateManager _manager;
        private UpdateInfo? _pendingUpdate;

        public WindowsUpdateService()
        {
            _manager = new UpdateManager(new GithubSource(_githubRepoUrl, accessToken: null, prerelease: false));
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            if (!_manager.IsInstalled)
            {
                return new UpdateCheckResult(UpdateAvailable: false, AvailableVersion: null);
            }

            try
            {
                _pendingUpdate = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Network errors / missing release feed shouldn't crash the app — surface as "no update".
                return new UpdateCheckResult(UpdateAvailable: false, AvailableVersion: null);
            }

            if (_pendingUpdate is null)
            {
                return new UpdateCheckResult(UpdateAvailable: false, AvailableVersion: null);
            }

            return new UpdateCheckResult(
                UpdateAvailable: true,
                AvailableVersion: _pendingUpdate.TargetFullRelease.Version.ToString());
        }

        public async Task<bool> ApplyUpdateAndRestartAsync(CancellationToken cancellationToken = default)
        {
            if (_pendingUpdate is null || !_manager.IsInstalled)
            {
                return false;
            }

            try
            {
                await _manager.DownloadUpdatesAsync(_pendingUpdate).ConfigureAwait(false);
                _manager.ApplyUpdatesAndRestart(_pendingUpdate);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
