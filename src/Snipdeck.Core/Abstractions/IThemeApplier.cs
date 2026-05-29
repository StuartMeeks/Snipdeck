using Snipdeck.Core.Models;

namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Applies a <see cref="ThemePreference"/> to the live UI. Implemented in
    /// the App project so view models in Core stay WinUI-free.
    /// </summary>
    public interface IThemeApplier
    {
        void Apply(ThemePreference theme);
    }
}
