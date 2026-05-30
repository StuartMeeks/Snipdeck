namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Restarts the running application. Used to apply changes that are only
    /// read at startup (e.g. the storage location), so the app reloads cleanly
    /// rather than continuing against stale, immutable startup state.
    /// </summary>
    public interface IAppRestartService
    {
        void Restart();
    }
}
