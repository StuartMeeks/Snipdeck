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

        public static HotkeyBinding Default => new()
        {
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt,
            Key = "S",
        };
    }
}
