using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

namespace Snipdeck.App
{
    public partial class App : Application
    {
        private MainWindow? _mainWindow;
        private IHotkeyService? _hotkey;
        private ITrayService? _tray;
        private AppConfig? _config;
        private bool _allowClose;

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

            _config = Services.GetRequiredService<AppConfig>();
            _mainWindow = Services.GetRequiredService<MainWindow>();
            _mainWindow.Activate();

            WireCloseToTray(_mainWindow, _config);
            await InitialiseTrayAsync();
            InitialiseHotkey(_config);
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
            dispatcher.Enqueue(BringToFront);
        }

        private void WireCloseToTray(MainWindow window, AppConfig config)
        {
            window.AppWindow.Closing += (_, args) =>
            {
                if (_allowClose || config.CloseBehaviour == CloseBehaviour.Exit)
                {
                    return;
                }
                args.Cancel = true;
                window.AppWindow.Hide();
            };
        }

        private async Task InitialiseTrayAsync()
        {
            _tray = Services.GetRequiredService<ITrayService>();
            _tray.ShowRequested += OnTrayShowRequested;
            _tray.ExitRequested += OnTrayExitRequested;
            await _tray.InitialiseAsync();
        }

        private void InitialiseHotkey(AppConfig config)
        {
            _hotkey = Services.GetRequiredService<IHotkeyService>();
            _hotkey.Pressed += OnHotkeyPressed;
            _ = _hotkey.TryRegister(config.Hotkey);
        }

        private void OnTrayShowRequested(object? sender, EventArgs e) => BringToFront();

        private void OnTrayExitRequested(object? sender, EventArgs e)
        {
            _allowClose = true;
            _mainWindow?.Close();
        }

        private void OnHotkeyPressed(object? sender, EventArgs e) => BringToFront();

        private void BringToFront()
        {
            if (_mainWindow is null)
            {
                return;
            }
            _mainWindow.AppWindow.Show();
            _mainWindow.Activate();
        }
    }
}
