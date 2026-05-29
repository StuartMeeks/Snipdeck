using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Services;

namespace Snipdeck.App
{
    public partial class App : Application
    {
        private Window? _mainWindow;

        public App()
        {
            InitializeComponent();
            Services = Bootstrap.Build();

            // Warm up the dispatcher on the UI thread so the activation-redirect
            // handler can safely marshal back here from a worker thread.
            var dispatcher = Services.GetRequiredService<IDispatcher>();
            _ = dispatcher.HasUiThreadAccess;

            AppInstance.GetCurrent().Activated += OnInstanceActivated;
        }

        public static IServiceProvider Services { get; private set; } = null!;

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            await SeedFirstRunIfEmptyAsync();

            _mainWindow = Services.GetRequiredService<MainWindow>();
            _mainWindow.Activate();
        }

        private static async Task SeedFirstRunIfEmptyAsync()
        {
            var store = Services.GetRequiredService<ISnipStore>();
            var document = await store.LoadAsync();
            if (ExamplesSeed.IsEmpty(document))
            {
                await store.SaveAsync(ExamplesSeed.Build());
            }
        }

        private void OnInstanceActivated(object? sender, AppActivationArguments e)
        {
            var dispatcher = Services.GetRequiredService<IDispatcher>();
            dispatcher.Enqueue(() =>
            {
                _mainWindow?.Activate();
            });
        }
    }
}
