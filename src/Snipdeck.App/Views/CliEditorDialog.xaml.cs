using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Views
{
    public sealed partial class CliEditorDialog : ContentDialog
    {
        private readonly IIconNormaliser _iconNormaliser;
        private readonly IFilePickerService _filePicker;

        public CliEditorDialog(
            CliEditorViewModel viewModel,
            IIconNormaliser iconNormaliser,
            IFilePickerService filePicker)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ArgumentNullException.ThrowIfNull(iconNormaliser);
            ArgumentNullException.ThrowIfNull(filePicker);
            ViewModel = viewModel;
            _iconNormaliser = iconNormaliser;
            _filePicker = filePicker;
            InitializeComponent();
            UpdatePrimaryButtonEnabled();
            viewModel.PropertyChanged += (_, _) => UpdatePrimaryButtonEnabled();
        }

        public CliEditorViewModel ViewModel { get; }

        private void UpdatePrimaryButtonEnabled()
        {
            IsPrimaryButtonEnabled = ViewModel.CanSave;
        }

        private void OnAddParameterClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.AddParameter();
        }

        private void OnRemoveParameterClicked(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is ParameterEditorRowViewModel row)
            {
                ViewModel.RemoveParameter(row);
            }
        }

        private async void OnPickIconClicked(object sender, RoutedEventArgs e)
        {
            var picked = await _filePicker.PickImageAsync();
            if (picked is null)
            {
                return;
            }
            var normalised = await _iconNormaliser.NormaliseAsync(picked.Bytes);
            ViewModel.PickedIconBytes = normalised;
            ViewModel.PickedIconFileName = picked.FileName;
        }
    }
}
