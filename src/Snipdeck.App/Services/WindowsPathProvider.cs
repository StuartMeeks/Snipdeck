using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Services;

namespace Snipdeck.App.Services
{
    /// <summary>
    /// Resolves Snipdeck's data paths under <c>%LOCALAPPDATA%\Snipdeck</c>.
    /// The layout lives in <see cref="DefaultPaths"/> (Core) so the cross-platform
    /// importer tool resolves identical locations; this provider simply delegates.
    /// </summary>
    internal sealed class WindowsPathProvider : IPathProvider
    {
        public string AppDataDirectory => DefaultPaths.AppDataDirectory;

        public string SettingsFilePath => DefaultPaths.SettingsFilePath;

        public string DefaultStorageDirectory => DefaultPaths.DefaultStorageDirectory;

        public string DefaultBackupDirectory => DefaultPaths.DefaultBackupDirectory;

        public string LogsDirectory => DefaultPaths.LogsDirectory;
    }
}
