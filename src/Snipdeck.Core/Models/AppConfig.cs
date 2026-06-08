namespace Snipdeck.Core.Models
{
    public sealed class AppConfig
    {
        // v2 adds execution-history settings (HistoryRetentionPerSnip,
        // MaxCapturedOutputBytes) for the command-execution feature.
        public const int CurrentSchemaVersion = 2;

        public const int DefaultBackupRetention = 20;

        public const int DefaultHistoryRetentionPerSnip = 50;

        public const long DefaultMaxCapturedOutputBytes = 5 * 1024 * 1024;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public string? StoragePath { get; set; }

        public string? BackupDirectory { get; set; }

        public int BackupRetention { get; set; } = DefaultBackupRetention;

        public ThemePreference Theme { get; set; } = ThemePreference.System;

        public HotkeyBinding Hotkey { get; set; } = HotkeyBinding.Default;

        public CloseBehaviour CloseBehaviour { get; set; } = CloseBehaviour.HideToTray;

        /// <summary>
        /// How many execution-history runs to keep per Snip before the oldest are
        /// pruned. Mirrors <see cref="BackupRetention"/>.
        /// </summary>
        public int HistoryRetentionPerSnip { get; set; } = DefaultHistoryRetentionPerSnip;

        /// <summary>
        /// Maximum bytes of output captured per run; beyond this the stream is
        /// truncated with a marker and the run is flagged truncated.
        /// </summary>
        public long MaxCapturedOutputBytes { get; set; } = DefaultMaxCapturedOutputBytes;
    }
}
