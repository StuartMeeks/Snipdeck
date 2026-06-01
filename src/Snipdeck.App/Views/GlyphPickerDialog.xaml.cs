using System.ComponentModel;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Views
{
    /// <summary>
    /// A searchable grid of the curated glyph catalogue. The user filters and
    /// picks an icon; <see cref="ChosenGlyph"/> holds the result (the resolved
    /// glyph character) or stays null when cancelled. Double-tapping a glyph
    /// confirms the same as the Choose button.
    /// </summary>
    public sealed partial class GlyphPickerDialog : ContentDialog
    {
        public GlyphPickerDialog(GlyphPickerViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ViewModel = viewModel;
            InitializeComponent();

            // Choose is meaningless without a selection; keep it disabled until
            // one exists, and track changes as the search clears the selection.
            IsPrimaryButtonEnabled = viewModel.SelectedEntry is not null;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public GlyphPickerViewModel ViewModel { get; }

        /// <summary>The glyph the user chose, or null if they cancelled.</summary>
        public string? ChosenGlyph { get; private set; }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlyphPickerViewModel.SelectedEntry))
            {
                IsPrimaryButtonEnabled = ViewModel.SelectedEntry is not null;
            }
        }

        private void OnChooseClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (ViewModel.SelectedEntry is null)
            {
                // Nothing selected: keep the dialog open rather than returning blank.
                args.Cancel = true;
                return;
            }

            ChosenGlyph = ViewModel.SelectedGlyph;
        }

        private void OnGlyphDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (ViewModel.SelectedEntry is null)
            {
                return;
            }

            ChosenGlyph = ViewModel.SelectedGlyph;
            Hide();
        }
    }
}
