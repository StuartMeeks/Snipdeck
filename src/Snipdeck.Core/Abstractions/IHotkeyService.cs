using Snipdeck.Core.Models;

namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Registers a system-wide hotkey via the platform. The implementation is
    /// responsible for translating <see cref="HotkeyBinding"/> into the
    /// appropriate native constants (on Windows: MOD_* + virtual-key codes).
    /// </summary>
    public interface IHotkeyService
    {
        event EventHandler? Pressed;

        bool TryRegister(HotkeyBinding binding);

        void Unregister();
    }
}
