using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Views
{
    public sealed partial class ParameterFillDialog : ContentDialog
    {
        public ParameterFillDialog(ParameterFillViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ViewModel = viewModel;
            InitializeComponent();
            if (viewModel.IsRunMode)
            {
                // Same dialog, repurposed as the dry-run safety gate before execution.
                Title = "Run snip";
                PrimaryButtonText = "Run";
            }

            UpdatePrimaryButtonEnabled();
            viewModel.PropertyChanged += (_, _) => UpdatePrimaryButtonEnabled();
        }

        public ParameterFillViewModel ViewModel { get; }

        private void UpdatePrimaryButtonEnabled()
        {
            IsPrimaryButtonEnabled = ViewModel.IsCopyEnabled;
        }
    }
}
