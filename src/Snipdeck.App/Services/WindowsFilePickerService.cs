using Snipdeck.Core.Abstractions;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace Snipdeck.App.Services
{
    internal sealed class WindowsFilePickerService : IFilePickerService
    {
        private readonly IServiceProvider _services;

        public WindowsFilePickerService(IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);
            _services = services;
        }

        public async Task<PickedFile?> PickImageAsync()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                ViewMode = PickerViewMode.Thumbnail,
            };
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".webp");

            var hwnd = GetMainWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return null;
            }

            var bytes = await ReadAllBytesAsync(file);
            return new PickedFile(file.Name, bytes);
        }

        public async Task<string?> PickExecutablePathAsync()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List,
            };
            // Common Windows executable/script types, plus a catch-all so anything
            // on disk can be chosen.
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add(".cmd");
            picker.FileTypeFilter.Add(".bat");
            picker.FileTypeFilter.Add(".ps1");
            picker.FileTypeFilter.Add("*");

            var hwnd = GetMainWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }

        private IntPtr GetMainWindowHandle()
        {
            var mainWindow = (MainWindow)_services.GetService(typeof(MainWindow))!;
            return WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
        }

        private static async Task<byte[]> ReadAllBytesAsync(StorageFile file)
        {
            var buffer = await FileIO.ReadBufferAsync(file);
            var bytes = new byte[buffer.Length];
            var reader = DataReader.FromBuffer(buffer);
            reader.ReadBytes(bytes);
            return bytes;
        }
    }
}
