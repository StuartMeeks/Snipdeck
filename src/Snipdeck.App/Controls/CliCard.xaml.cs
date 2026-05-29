using System.Windows.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Controls
{
    public sealed partial class CliCard : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(CliCardViewModel), typeof(CliCard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty NavigateCommandProperty =
            DependencyProperty.Register(nameof(NavigateCommand), typeof(ICommand), typeof(CliCard),
                new PropertyMetadata(null));

        public CliCard()
        {
            InitializeComponent();
        }

        public CliCardViewModel? ViewModel
        {
            get => (CliCardViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public ICommand? NavigateCommand
        {
            get => (ICommand?)GetValue(NavigateCommandProperty);
            set => SetValue(NavigateCommandProperty, value);
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (NavigateCommand?.CanExecute(ViewModel) == true)
            {
                NavigateCommand.Execute(ViewModel);
            }
        }
    }
}
