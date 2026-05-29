using H.NotifyIcon;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Snipdeck.Core.Abstractions;

namespace Snipdeck.App.Services
{
    internal sealed class HNotifyIconTrayService : ITrayService
    {
        private TaskbarIcon? _icon;
        private bool _disposed;

        public event EventHandler? ShowRequested;

        public event EventHandler? ExitRequested;

        public void Initialise()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_icon is not null)
            {
                return;
            }

            _icon = new TaskbarIcon
            {
                ToolTipText = "Snipdeck",
                IconSource = new FontIconSource
                {
                    Glyph = "",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                },
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
