using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Views
{
    public sealed partial class ShellPage : UserControl
    {
        public ShellPage(ShellViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ViewModel = viewModel;
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public ShellViewModel ViewModel { get; }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadAsync();
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenSettings();
        }

        private void OnNavigationSelectionChanged(
            NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is string tag)
            {
                ViewModel.SelectedTag = tag;
            }
        }
    }
}
