using Microsoft.UI.Xaml;

using Snipdeck.Core.Models;

namespace Snipdeck.App
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow(AppConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

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
