using Microsoft.UI.Xaml;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.App.Services
{
    internal sealed class WindowsThemeApplier : IThemeApplier
    {
        private readonly IServiceProvider _services;

        public WindowsThemeApplier(IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);
            _services = services;
        }

        public void Apply(ThemePreference theme)
        {
            var mainWindow = (MainWindow?)_services.GetService(typeof(MainWindow));
            if (mainWindow?.Content is FrameworkElement root)
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
