namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Restarts the running application. Used to apply changes that are only
    /// read at startup (e.g. the storage location), so the app reloads cleanly
    /// rather than continuing against stale, immutable startup state.
    /// </summary>
    public interface IAppRestartService
    {
        /// <summary>
        /// Requests an application restart. On success the process is terminated
        /// and this does not return; it returns <c>false</c> if the restart
        /// could not be initiated, so the caller can fall back (e.g. ask the
        /// user to restart manually).
        /// </summary>
        bool Restart();
    }
}
