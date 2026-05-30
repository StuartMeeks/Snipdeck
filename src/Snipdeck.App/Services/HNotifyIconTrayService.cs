using System.Buffers.Binary;

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
        private const string _trayIconFileName = "tray-icon.ico";
        private const int _trayIconPixels = 32;

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

            // H.NotifyIcon resolves IconSource by reading BitmapImage.UriSource
            // and then hands the file bytes to System.Drawing.Icon — which only
            // accepts ICO format. Wrap the identicon PNG in a minimal ICO
            // container before writing it out.
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
            var pngBytes = IdenticonService.GeneratePng(_iconSeed, size: _trayIconPixels);
            var icoBytes = WrapPngAsIco(pngBytes, _trayIconPixels, _trayIconPixels);
            _ = Directory.CreateDirectory(_paths.AppDataDirectory);
            var path = Path.Combine(_paths.AppDataDirectory, _trayIconFileName);
            await File.WriteAllBytesAsync(path, icoBytes);
            return path;
        }

        // Modern Windows accepts a PNG embedded inside an ICO container —
        // just a 6-byte ICONDIR header plus a 16-byte ICONDIRENTRY pointing
        // at the PNG bytes. Format ref: ICONDIR / ICONDIRENTRY on MSDN.
        private static byte[] WrapPngAsIco(byte[] png, int width, int height)
        {
            const int headerSize = 6;
            const int entrySize = 16;
            const int dataOffset = headerSize + entrySize;

            var ico = new byte[dataOffset + png.Length];
            var span = ico.AsSpan();

            // ICONDIR: reserved(0), type(1 = icon), count(1)
            BinaryPrimitives.WriteUInt16LittleEndian(span[2..4], 1);
            BinaryPrimitives.WriteUInt16LittleEndian(span[4..6], 1);

            // ICONDIRENTRY: width/height bytes are 0 for >=256, otherwise the literal size.
            ico[6] = width >= 256 ? (byte)0 : (byte)width;
            ico[7] = height >= 256 ? (byte)0 : (byte)height;
            // ico[8] colorCount = 0 (>= 8bpp), ico[9] reserved = 0 — already zero.
            BinaryPrimitives.WriteUInt16LittleEndian(span[10..12], 1);   // planes
            BinaryPrimitives.WriteUInt16LittleEndian(span[12..14], 32);  // bits per pixel
            BinaryPrimitives.WriteUInt32LittleEndian(span[14..18], (uint)png.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(span[18..22], dataOffset);

            Buffer.BlockCopy(png, 0, ico, dataOffset, png.Length);
            return ico;
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
