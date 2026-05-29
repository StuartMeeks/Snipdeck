using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakeThemeApplier : IThemeApplier
    {
        public ThemePreference? LastAppliedTheme { get; private set; }

        public void Apply(ThemePreference theme)
        {
            LastAppliedTheme = theme;
        }
    }
}
