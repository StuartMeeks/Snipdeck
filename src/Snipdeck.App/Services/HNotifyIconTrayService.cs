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
