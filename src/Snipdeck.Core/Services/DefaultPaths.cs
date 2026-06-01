namespace Snipdeck.Core.Services
{
    /// <summary>
    /// Resolves Snipdeck's default data paths under <c>%LOCALAPPDATA%\Snipdeck</c> (Windows)
    /// or the platform equivalent of <see cref="Environment.SpecialFolder.LocalApplicationData"/>.
    /// <para>
    /// This lives in Core so that both the WinUI head and the cross-platform importer tool
    /// resolve the same locations without duplicating the layout. The desktop app's
    /// <c>WindowsPathProvider</c> delegates here, so its behaviour is unchanged.
    /// </para>
    /// </summary>
    public static class DefaultPaths
    {
        public const string AppFolderName = "Snipdeck";
        public const string SettingsFileName = "settings.json";
        public const string StoreDirectoryName = "store";
        public const string StoreFileName = "store.json";
        public const string BackupsDirectoryName = "backups";
        public const string LogsDirectoryName = "logs";

        /// <summary>The Snipdeck data root, e.g. <c>%LOCALAPPDATA%\Snipdeck</c>.</summary>
        public static string AppDataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName);

        /// <summary>The app-config file, stored separately from the snip store.</summary>
        public static string SettingsFilePath { get; } = Path.Combine(AppDataDirectory, SettingsFileName);

        /// <summary>The default directory that holds the snip store document.</summary>
        public static string DefaultStorageDirectory { get; } = Path.Combine(AppDataDirectory, StoreDirectoryName);

        /// <summary>The default directory that holds timestamped store backups.</summary>
        public static string DefaultBackupDirectory { get; } = Path.Combine(AppDataDirectory, BackupsDirectoryName);

        /// <summary>The default directory that holds application logs.</summary>
        public static string LogsDirectory { get; } = Path.Combine(AppDataDirectory, LogsDirectoryName);

        /// <summary>
        /// Composes the full default store-document path (<c>&lt;storage&gt;/store.json</c>) from an
        /// optional configured storage directory, falling back to <see cref="DefaultStorageDirectory"/>.
        /// </summary>
        public static string ResolveStoreFilePath(string? configuredStorageDirectory)
        {
            var storageDirectory = string.IsNullOrWhiteSpace(configuredStorageDirectory)
                ? DefaultStorageDirectory
                : configuredStorageDirectory;
            return Path.Combine(storageDirectory, StoreFileName);
        }
    }
}
