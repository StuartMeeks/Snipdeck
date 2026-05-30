using Microsoft.Extensions.DependencyInjection;
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
            var settings = App.Services.GetRequiredService<SettingsViewModel>();
            ViewModel.OpenSettings(settings);
        }

        private void OnTrashClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenTrash();
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

        private void OnNewCliClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.NewCliCommand.CanExecute(null))
            {
                ViewModel.NewCliCommand.Execute(null);
            }
        }

        private void OnNewSnipClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.NewSnipCommand.CanExecute(null))
            {
                ViewModel.NewSnipCommand.Execute(null);
            }
        }

        private void OnEditCliClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.EditCurrentCliCommand.CanExecute(null))
            {
                ViewModel.EditCurrentCliCommand.Execute(null);
            }
        }

        private void OnDeleteCliClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.DeleteCurrentCliCommand.CanExecute(null))
            {
                ViewModel.DeleteCurrentCliCommand.Execute(null);
            }
        }
    }
}
