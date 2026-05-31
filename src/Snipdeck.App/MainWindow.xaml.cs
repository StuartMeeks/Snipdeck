using System.ComponentModel;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Snipdeck.App.Views;
using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.App
{
    public sealed partial class MainWindow : Window
    {
        private readonly ShellPage _shellPage;

        public MainWindow(AppConfig config, ShellPage shellPage)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(shellPage);

            _shellPage = shellPage;
            Shell = shellPage.ViewModel;
            Shell.PropertyChanged += OnShellPropertyChanged;

            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            // The whole bar is the drag region. WinUI does NOT auto-exclude interactive
            // children, so the centred search/switcher group is registered as a
            // passthrough region instead (recomputed when the bar or group resizes).
            SetTitleBar(AppTitleBar);
            AppTitleBar.SizeChanged += (_, _) => UpdateTitleBarPassthrough();

            ShellHost.Content = shellPage;

            ApplyTheme(config.Theme);
        }

        // The title-bar switcher and snip search bind to the shell view model.
        public ShellViewModel Shell { get; }

        // The title-bar hamburger toggles the shell's navigation pane.
        private void OnPaneToggleClicked(object sender, RoutedEventArgs e)
        {
            _shellPage.TogglePane();
        }

        // Keep the title-bar search box in sync when the view model clears the
        // search (e.g. clicking Home), since the box text is otherwise UI-only.
        private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShellViewModel.SearchText)
                && string.IsNullOrEmpty(Shell.SearchText)
                && !string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchBox.Text = string.Empty;
            }
        }

        private void OnTitleBarControlsChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTitleBarPassthrough();
        }

        // Mark the interactive title-bar elements (hamburger + the centred
        // search/switcher group) as passthrough regions so they receive pointer
        // input instead of the title-bar drag handler.
        private void UpdateTitleBarPassthrough()
        {
            if (TitleBarControls.XamlRoot is null)
            {
                return;
            }

            var scale = TitleBarControls.XamlRoot.RasterizationScale;
            var rects = new List<Windows.Graphics.RectInt32>();
            foreach (var element in new FrameworkElement[] { PaneToggleButton, TitleBarControls })
            {
                if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
                {
                    continue;
                }
                var bounds = element
                    .TransformToVisual(Content)
                    .TransformBounds(new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));
                rects.Add(new Windows.Graphics.RectInt32(
                    (int)Math.Round(bounds.X * scale),
                    (int)Math.Round(bounds.Y * scale),
                    (int)Math.Round(bounds.Width * scale),
                    (int)Math.Round(bounds.Height * scale)));
            }

            InputNonClientPointerSource
                .GetForWindowId(AppWindow.Id)
                .SetRegionRects(NonClientRegionKind.Passthrough, [.. rects]);
        }

        private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (string.IsNullOrEmpty(sender.Text))
                {
                    // Clearing the box clears any active filter (without leaving Home).
                    Shell.SearchText = string.Empty;
                }
                sender.ItemsSource = Shell.GetSearchSuggestions(sender.Text);
            }
        }

        private void OnSearchSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is SnipSearchResult result)
            {
                Shell.SelectSearchResult(result);
            }
        }

        private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is SnipSearchResult result)
            {
                Shell.SelectSearchResult(result);
            }
            else
            {
                Shell.ApplySearch(args.QueryText);
            }
        }

        private void ApplyTheme(ThemePreference theme)
        {
            if (Content is FrameworkElement root)
            {
                root.RequestedTheme = theme switch
                {
                    ThemePreference.Light => ElementTheme.Light,
                    ThemePreference.Dark => ElementTheme.Dark,
                    ThemePreference.System => ElementTheme.Default,
                    _ => ElementTheme.Default,
                };
            }
        }
    }
}
