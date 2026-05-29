using System.Windows.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Controls
{
    public sealed partial class SnipCard : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(SnipCardViewModel), typeof(SnipCard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CopyCommandProperty =
            DependencyProperty.Register(nameof(CopyCommand), typeof(ICommand), typeof(SnipCard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty EditCommandProperty =
            DependencyProperty.Register(nameof(EditCommand), typeof(ICommand), typeof(SnipCard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(SnipCard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty FavouriteCommandProperty =
            DependencyProperty.Register(nameof(FavouriteCommand), typeof(ICommand), typeof(SnipCard),
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

        public ICommand? CopyCommand
        {
            get => (ICommand?)GetValue(CopyCommandProperty);
            set => SetValue(CopyCommandProperty, value);
        }

        public ICommand? EditCommand
        {
            get => (ICommand?)GetValue(EditCommandProperty);
            set => SetValue(EditCommandProperty, value);
        }

        public ICommand? DeleteCommand
        {
            get => (ICommand?)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        public ICommand? FavouriteCommand
        {
            get => (ICommand?)GetValue(FavouriteCommandProperty);
            set => SetValue(FavouriteCommandProperty, value);
        }

        public static string FavouriteGlyph(bool isFavourite) => isFavourite ? "\uE735" : "\uE734";
    }
}
