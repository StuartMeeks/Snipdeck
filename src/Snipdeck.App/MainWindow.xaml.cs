using Microsoft.UI.Xaml;

using Snipdeck.App.Views;
using Snipdeck.Core.Models;

namespace Snipdeck.App
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow(AppConfig config, ShellPage shellPage)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(shellPage);

            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            ShellHost.Content = shellPage;

            ApplyTheme(config.Theme);
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
