using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.ViewModels;

using Windows.Storage;
using Windows.Storage.Pickers;

namespace Snipdeck.App.Views
{
    public sealed partial class CliEditorDialog : ContentDialog
    {
        private readonly IIconNormaliser _iconNormaliser;
        private readonly IntPtr _ownerWindowHandle;

        public CliEditorDialog(
            CliEditorViewModel viewModel,
            IIconNormaliser iconNormaliser,
            IntPtr ownerWindowHandle)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ArgumentNullException.ThrowIfNull(iconNormaliser);
            ViewModel = viewModel;
            _iconNormaliser = iconNormaliser;
            _ownerWindowHandle = ownerWindowHandle;
            InitializeComponent();
            UpdatePrimaryButtonEnabled();
            viewModel.PropertyChanged += (_, _) => UpdatePrimaryButtonEnabled();
        }

        public CliEditorViewModel ViewModel { get; }

        private void UpdatePrimaryButtonEnabled()
        {
            IsPrimaryButtonEnabled = ViewModel.CanSave;
        }

        private async void OnPickIconClicked(object sender, RoutedEventArgs e)
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

            WinRT.Interop.InitializeWithWindow.Initialize(picker, _ownerWindowHandle);

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            var raw = await ReadAllBytesAsync(file);
            var normalised = await _iconNormaliser.NormaliseAsync(raw);
            ViewModel.PickedIconBytes = normalised;
            ViewModel.PickedIconFileName = file.Name;
        }

        private static async Task<byte[]> ReadAllBytesAsync(StorageFile file)
        {
            var buffer = await FileIO.ReadBufferAsync(file);
            var bytes = new byte[buffer.Length];
            var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer);
            reader.ReadBytes(bytes);
            return bytes;
        }
    }
}
