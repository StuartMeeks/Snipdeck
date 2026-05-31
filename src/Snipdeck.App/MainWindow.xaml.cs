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
        public MainWindow(AppConfig config, ShellPage shellPage)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(shellPage);

            Shell = shellPage.ViewModel;

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

        private void OnTitleBarControlsChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTitleBarPassthrough();
        }

        // Mark the centred search/switcher group as a passthrough region so its
        // controls receive pointer input instead of the title-bar drag handler.
        private void UpdateTitleBarPassthrough()
        {
            if (TitleBarControls.XamlRoot is null || TitleBarControls.ActualWidth <= 0)
            {
                return;
            }

            var scale = TitleBarControls.XamlRoot.RasterizationScale;
            var bounds = TitleBarControls
                .TransformToVisual(Content)
                .TransformBounds(new Windows.Foundation.Rect(0, 0, TitleBarControls.ActualWidth, TitleBarControls.ActualHeight));

            var rect = new Windows.Graphics.RectInt32(
                (int)Math.Round(bounds.X * scale),
                (int)Math.Round(bounds.Y * scale),
                (int)Math.Round(bounds.Width * scale),
                (int)Math.Round(bounds.Height * scale));

            InputNonClientPointerSource
                .GetForWindowId(AppWindow.Id)
                .SetRegionRects(NonClientRegionKind.Passthrough, [rect]);
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
