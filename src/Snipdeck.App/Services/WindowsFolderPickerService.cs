using Snipdeck.Core.Abstractions;

using Windows.Storage.Pickers;

namespace Snipdeck.App.Services
{
    internal sealed class WindowsFolderPickerService : IFolderPickerService
    {
        private readonly IServiceProvider _services;

        public WindowsFolderPickerService(IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);
            _services = services;
        }

        public async Task<string?> PickFolderAsync()
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeFilter.Add("*");

            var mainWindow = (MainWindow)_services.GetService(typeof(MainWindow))!;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
    }
}
