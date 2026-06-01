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
            // Route through the window so the content and the system caption
            // buttons are themed together (see MainWindow.ApplyTheme).
            var mainWindow = (MainWindow?)_services.GetService(typeof(MainWindow));
            mainWindow?.ApplyTheme(theme);
        }
    }
}
