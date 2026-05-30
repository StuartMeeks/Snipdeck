using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakePathProvider : IPathProvider
    {
        public string AppDataDirectory { get; init; } = "/data";

        public string SettingsFilePath { get; init; } = "/data/settings.json";

        public string DefaultStorageDirectory { get; init; } = "/data/store";

        public string DefaultBackupDirectory { get; init; } = "/data/backups";

        public string LogsDirectory { get; init; } = "/data/logs";
    }
}
