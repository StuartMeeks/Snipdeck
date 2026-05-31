using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Views
{
    public sealed partial class ParametersEditorDialog : ContentDialog
    {
        public ParametersEditorDialog(ParametersEditorViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ViewModel = viewModel;
            InitializeComponent();
        }

        public ParametersEditorViewModel ViewModel { get; }
    }
}
