using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Controls
{
    public sealed partial class CliCard : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(CliCardViewModel),
                typeof(CliCard),
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
    }
}
