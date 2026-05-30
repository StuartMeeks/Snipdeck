namespace Snipdeck.Core.Models
{
    [Flags]
    public enum HotkeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
    }

    public sealed class HotkeyBinding
    {
        public HotkeyModifiers Modifiers { get; set; } = HotkeyModifiers.None;

        public string Key { get; set; } = string.Empty;

        public bool IsEmpty =>
            Modifiers == HotkeyModifiers.None && string.IsNullOrWhiteSpace(Key);

        /// <summary>
        /// A binding the platform can actually register: at least one modifier
        /// plus a key. A bare key (no modifier) is rejected — global hotkeys
        /// need a modifier so they don't swallow ordinary typing.
        /// </summary>
        public bool IsValid =>
            Modifiers != HotkeyModifiers.None && !string.IsNullOrWhiteSpace(Key);

        public static HotkeyBinding Default => new()
        {
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt,
            Key = "S",
        };

        /// <summary>Human-readable chord, e.g. "Ctrl+Alt+S" (or "(unbound)").</summary>
        public string ToDisplayString()
        {
            if (IsEmpty)
            {
                return "(unbound)";
            }
            var parts = new List<string>();
            if (Modifiers.HasFlag(HotkeyModifiers.Control))
            {
                parts.Add("Ctrl");
            }
            if (Modifiers.HasFlag(HotkeyModifiers.Alt))
            {
                parts.Add("Alt");
            }
            if (Modifiers.HasFlag(HotkeyModifiers.Shift))
            {
                parts.Add("Shift");
            }
            if (Modifiers.HasFlag(HotkeyModifiers.Windows))
            {
                parts.Add("Win");
            }
            if (!string.IsNullOrWhiteSpace(Key))
            {
                parts.Add(Key);
            }
            return string.Join('+', parts);
        }
    }
}
