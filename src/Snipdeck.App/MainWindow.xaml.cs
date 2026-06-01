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

            // While on System theme, the caption buttons must follow an OS
            // light/dark flip too — RequestedTheme stays Default, so only
            // ActualTheme changes.
            if (Content is FrameworkElement themedRoot)
            {
                themedRoot.ActualThemeChanged += (sender, _) => UpdateCaptionButtonColours(sender.ActualTheme);
            }
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

        /// <summary>
        /// Applies the chosen theme to the app content and the system caption
        /// buttons. Public so the runtime theme applier routes through here,
        /// keeping the window content and the min/max/close glyphs in step.
        /// </summary>
        public void ApplyTheme(ThemePreference theme)
        {
            if (Content is not FrameworkElement root)
            {
                return;
            }

            root.RequestedTheme = theme switch
            {
                ThemePreference.Light => ElementTheme.Light,
                ThemePreference.Dark => ElementTheme.Dark,
                ThemePreference.System => ElementTheme.Default,
                _ => ElementTheme.Default,
            };

            // ActualTheme resolves Default to the OS light/dark choice, so the
            // caption glyphs get a concrete theme to contrast against.
            UpdateCaptionButtonColours(root.ActualTheme);
        }

        // The caption buttons are system-drawn chrome, not styled by the app's
        // theme resources — so without this they keep their default (white)
        // glyphs and vanish on a light background. Backgrounds stay transparent
        // so the Mica backdrop shows through; only the glyph colours change.
        private void UpdateCaptionButtonColours(ElementTheme actualTheme)
        {
            var dark = actualTheme == ElementTheme.Dark;

            var foreground = dark
                ? Microsoft.UI.Colors.White
                : Windows.UI.Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A);
            var inactive = dark
                ? Windows.UI.Color.FromArgb(0xFF, 0x80, 0x80, 0x80)
                : Windows.UI.Color.FromArgb(0xFF, 0x8A, 0x8A, 0x8A);
            var hoverBackground = dark
                ? Windows.UI.Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)
                : Windows.UI.Color.FromArgb(0x14, 0x00, 0x00, 0x00);
            var pressedBackground = dark
                ? Windows.UI.Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF)
                : Windows.UI.Color.FromArgb(0x0A, 0x00, 0x00, 0x00);

            var titleBar = AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonForegroundColor = foreground;
            titleBar.ButtonHoverForegroundColor = foreground;
            titleBar.ButtonHoverBackgroundColor = hoverBackground;
            titleBar.ButtonPressedForegroundColor = foreground;
            titleBar.ButtonPressedBackgroundColor = pressedBackground;
            titleBar.ButtonInactiveForegroundColor = inactive;
        }
    }
}
