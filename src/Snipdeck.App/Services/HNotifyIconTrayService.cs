using H.NotifyIcon;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Services;

using Windows.Storage.Streams;

namespace Snipdeck.App.Services
{
    internal sealed class HNotifyIconTrayService : ITrayService
    {
        // Stable seed so the tray icon never changes between runs of Snipdeck.
        private static readonly Guid _iconSeed = Guid.Parse("5147DEC0-0000-0000-0000-000000000001");

        private TaskbarIcon? _icon;
        private bool _disposed;

        public event EventHandler? ShowRequested;

        public event EventHandler? ExitRequested;

        public async Task InitialiseAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_icon is not null)
            {
                return;
            }

            var image = await BuildTrayBitmapAsync();

            _icon = new TaskbarIcon
            {
                ToolTipText = "Snipdeck",
                IconSource = image,
                ContextFlyout = BuildContextMenu(),
                NoLeftClickDelay = true,
            };
            _icon.LeftClickCommand = new RelayCommand(() => ShowRequested?.Invoke(this, EventArgs.Empty));
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
            var menu = new MenuFlyout();

            var showItem = new MenuFlyoutItem { Text = "Show Snipdeck" };
            showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(showItem);

            menu.Items.Add(new MenuFlyoutSeparator());

            var exitItem = new MenuFlyoutItem { Text = "Exit" };
            exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(exitItem);

            return menu;
        }

        private static async Task<BitmapImage> BuildTrayBitmapAsync()
        {
            var bytes = IdenticonService.GeneratePng(_iconSeed, size: 32);
            var image = new BitmapImage();
            using var stream = new InMemoryRandomAccessStream();
            var writer = new DataWriter(stream);
            writer.WriteBytes(bytes);
            _ = await writer.StoreAsync();
            _ = writer.DetachStream();
            stream.Seek(0);
            await image.SetSourceAsync(stream);
            return image;
        }

        private sealed class RelayCommand : System.Windows.Input.ICommand
        {
            private readonly Action _execute;

            public RelayCommand(Action execute)
            {
                _execute = execute;
            }

#pragma warning disable CS0067 // 'CanExecuteChanged' is never used — relay never changes.
            public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => _execute();
        }
    }
}
