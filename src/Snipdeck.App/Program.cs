using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

using Velopack;

namespace Snipdeck.App
{
    /// <summary>
    /// Explicit entry point. The boot order is load-bearing:
    /// <list type="number">
    ///   <item>Velopack — must run first so it can intercept install / update / uninstall invocations.</item>
    ///   <item>WinRT COM wrappers init.</item>
    ///   <item>Single-instance check + activation redirect.</item>
    ///   <item>UI start (DI container + main window).</item>
    /// </list>
    /// </summary>
    public static class Program
    {
        private const string _singleInstanceKey = "snipdeck";

        [STAThread]
        public static int Main(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);

            VelopackApp.Build().Run();

            WinRT.ComWrappersSupport.InitializeComWrappers();

            if (RedirectActivationIfSecondaryInstance())
            {
                return 0;
            }

            Application.Start(p =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });

            return 0;
        }

        private static bool RedirectActivationIfSecondaryInstance()
        {
            var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
            var keyInstance = AppInstance.FindOrRegisterForKey(_singleInstanceKey);

            if (keyInstance.IsCurrent)
            {
                return false;
            }

            keyInstance.RedirectActivationToAsync(activation).AsTask().Wait();
            return true;
        }
    }
}
