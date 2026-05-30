using System.Text.Json.Serialization;

namespace Snipdeck.Core.Models
{
    [Flags]
    public enum HotkeyModifiers
    {
        [JsonStringEnumMemberName("none")]
        None = 0,

        [JsonStringEnumMemberName("alt")]
        Alt = 1,

        [JsonStringEnumMemberName("control")]
        Control = 2,

        [JsonStringEnumMemberName("shift")]
        Shift = 4,

        [JsonStringEnumMemberName("windows")]
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
