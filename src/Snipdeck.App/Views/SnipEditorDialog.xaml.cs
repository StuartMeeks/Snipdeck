using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Views
{
    public sealed partial class SnipEditorDialog : ContentDialog
    {
        public SnipEditorDialog(SnipEditorViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ViewModel = viewModel;
            InitializeComponent();
            UpdatePrimaryButtonEnabled();
            viewModel.PropertyChanged += (_, _) => UpdatePrimaryButtonEnabled();
        }

        public SnipEditorViewModel ViewModel { get; }

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
    }
}
