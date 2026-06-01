namespace Snipdeck.Core.Abstractions
{
    public interface IPathProvider
    {
        string AppDataDirectory { get; }

        string SettingsFilePath { get; }

        string IconCatalogueFilePath { get; }

        string DefaultStorageDirectory { get; }

        string DefaultBackupDirectory { get; }

        string LogsDirectory { get; }
    }
}
