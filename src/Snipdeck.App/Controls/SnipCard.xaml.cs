using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Controls
{
    public sealed partial class SnipCard : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(SnipCardViewModel),
                typeof(SnipCard),
                new PropertyMetadata(null));

        public SnipCard()
        {
            InitializeComponent();
        }

        public SnipCardViewModel? ViewModel
        {
            get => (SnipCardViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
    }
}
