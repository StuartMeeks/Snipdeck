using Snipdeck.Core.Abstractions;

using Microsoft.Windows.AppLifecycle;

namespace Snipdeck.App.Services
{
    internal sealed class WindowsAppRestartService : IAppRestartService
    {
        public void Restart()
        {
            // Windows App SDK gracefully terminates and relaunches the current
            // instance. Used to apply a storage-location change cleanly.
            _ = AppInstance.Restart(string.Empty);
        }
    }
}
