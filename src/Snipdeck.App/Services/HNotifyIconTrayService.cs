using H.NotifyIcon;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Snipdeck.Core.Abstractions;

namespace Snipdeck.App.Services
{
    internal sealed partial class HNotifyIconTrayService : ITrayService
    {
        private TaskbarIcon? _icon;
        private bool _disposed;

        public event EventHandler? ShowRequested;

        public event EventHandler? ExitRequested;

        public Task InitialiseAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_icon is not null)
            {
                return Task.CompletedTask;
            }

            // H.NotifyIcon resolves IconSource by reading BitmapImage.UriSource
            // and then hands the file bytes to System.Drawing.Icon — which only
            // accepts ICO format. The shipped app icon is already a multi-size
            // ICO, so point straight at it.
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Snipdeck.ico");
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
            return Task.CompletedTask;
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
            // In an unpackaged app H.NotifyIcon's default context menu is a native
            // Win32 PopupMenu, built from this MenuFlyout by invoking each item's
            // Command — it does NOT raise the WinUI routed Click event. So the menu
            // items must use Command (like LeftClickCommand does), not Click, or
            // they silently do nothing.
            return new MenuFlyout
            {
                Items =
                {
                    new MenuFlyoutItem
                    {
                        Text = "Show Snipdeck",
                        Command = new RelayCommand(RaiseShowRequested),
                    },
                    new MenuFlyoutSeparator(),
                    new MenuFlyoutItem
                    {
                        Text = "Exit",
                        Command = new RelayCommand(RaiseExitRequested),
                    },
                },
            };
        }

        private void RaiseShowRequested()
        {
            ShowRequested?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseExitRequested()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
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
