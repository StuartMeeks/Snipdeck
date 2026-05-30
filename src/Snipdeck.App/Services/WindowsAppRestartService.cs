using Snipdeck.Core.Abstractions;

using Microsoft.Windows.AppLifecycle;

namespace Snipdeck.App.Services
{
    internal sealed class WindowsAppRestartService : IAppRestartService
    {
        public bool Restart()
        {
            // On success Windows App SDK terminates the process and this never
            // returns. If it returns, it failed (it yields a failure reason
            // rather than throwing), so report that to the caller.
            _ = AppInstance.Restart(string.Empty);
            return false;
        }
    }
}
