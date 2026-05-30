using H.NotifyIcon;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Services;

namespace Snipdeck.App.Services
{
    internal sealed partial class HNotifyIconTrayService : ITrayService
    {
        // Stable seed so the tray icon never changes between runs of Snipdeck.
        private static readonly Guid _iconSeed = Guid.Parse("5147DEC0-0000-0000-0000-000000000001");
        private const string _trayIconFileName = "tray-icon.png";

        private readonly IPathProvider _paths;
        private TaskbarIcon? _icon;
        private bool _disposed;

        public HNotifyIconTrayService(IPathProvider paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            _paths = paths;
        }

        public event EventHandler? ShowRequested;

        public event EventHandler? ExitRequested;

        public async Task InitialiseAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_icon is not null)
            {
                return;
            }

            // H.NotifyIcon resolves IconSource by reading BitmapImage.UriSource,
            // not the pixel data — a stream-loaded BitmapImage NREs deep inside
            // the library. Persist the identicon to a stable file path and load
            // the BitmapImage from that URI instead.
            var iconPath = await WriteTrayIconFileAsync();
            var image = new BitmapImage(new Uri(iconPath, UriKind.Absolute));

            _icon = new TaskbarIcon
            {
                ToolTipText = "Snipdeck",
                IconSource = image,
                ContextFlyout = BuildContextMenu(),
                NoLeftClickDelay = true,
                LeftClickCommand = new RelayCommand(RaiseShowRequested),
            };
            _icon.ForceCreate();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _icon?.Dispose();
            _icon = null;
        }

        private MenuFlyout BuildContextMenu()
        {
            var showItem = new MenuFlyoutItem { Text = "Show Snipdeck" };
            showItem.Click += OnShowItemClick;

            var exitItem = new MenuFlyoutItem { Text = "Exit" };
            exitItem.Click += OnExitItemClick;

            return new MenuFlyout
            {
                Items =
                {
                    showItem,
                    new MenuFlyoutSeparator(),
                    exitItem,
                },
            };
        }

        private void OnShowItemClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            RaiseShowRequested();
        }

        private void OnExitItemClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseShowRequested()
        {
            ShowRequested?.Invoke(this, EventArgs.Empty);
        }

        private async Task<string> WriteTrayIconFileAsync()
        {
            var bytes = IdenticonService.GeneratePng(_iconSeed, size: 32);
            _ = Directory.CreateDirectory(_paths.AppDataDirectory);
            var path = Path.Combine(_paths.AppDataDirectory, _trayIconFileName);
            await File.WriteAllBytesAsync(path, bytes);
            return path;
        }

        private sealed partial class RelayCommand(Action execute) : System.Windows.Input.ICommand
        {
#pragma warning disable CS0067 // 'CanExecuteChanged' is never used — relay never changes.
            public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => execute();
        }
    }
}
