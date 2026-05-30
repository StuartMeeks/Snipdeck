using Snipdeck.Core.Abstractions;

namespace Snipdeck.App.Services
{
    /// <summary>
    /// Resolves Snipdeck's data paths under <c>%LOCALAPPDATA%\Snipdeck</c>.
    /// </summary>
    internal sealed class WindowsPathProvider : IPathProvider
    {
        private const string _appFolderName = "Snipdeck";
        private const string _settingsFileName = "settings.json";
        private const string _storeDirectoryName = "store";
        private const string _backupsDirectoryName = "backups";
        private const string _logsDirectoryName = "logs";

        public WindowsPathProvider()
        {
            AppDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _appFolderName);
            SettingsFilePath = Path.Combine(AppDataDirectory, _settingsFileName);
            DefaultStorageDirectory = Path.Combine(AppDataDirectory, _storeDirectoryName);
            DefaultBackupDirectory = Path.Combine(AppDataDirectory, _backupsDirectoryName);
            LogsDirectory = Path.Combine(AppDataDirectory, _logsDirectoryName);
        }

        public string AppDataDirectory { get; }

        public string SettingsFilePath { get; }

        public string DefaultStorageDirectory { get; }

        public string DefaultBackupDirectory { get; }

        public string LogsDirectory { get; }
    }
}
