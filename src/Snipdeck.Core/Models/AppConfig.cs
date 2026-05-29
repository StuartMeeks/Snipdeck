namespace Snipdeck.Core.Models
{
    public sealed class AppConfig
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public string? StoragePath { get; set; }

        public string? BackupDirectory { get; set; }

        public ThemePreference Theme { get; set; } = ThemePreference.System;

        public HotkeyBinding Hotkey { get; set; } = HotkeyBinding.Default;

        public CloseBehaviour CloseBehaviour { get; set; } = CloseBehaviour.HideToTray;
    }
}
