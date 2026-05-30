using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Services
{
    /// <summary>
    /// Filesystem implementation of <see cref="IStorageRelocationService"/>.
    /// Relocates the store file plus the icons subdirectory and leaves the
    /// settings/backups alone (those have their own locations).
    /// </summary>
    public sealed class StorageRelocationService : IStorageRelocationService
    {
        private const string _iconsSubdirectory = "icons";

        public StorageRelocationService(string storeFileName = "store.json")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storeFileName);
            StoreFileName = storeFileName;
        }

        public string StoreFileName { get; }

        public StorageChangeOutcome Inspect(string currentDirectory, string targetDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);

            return PathsEqual(currentDirectory, targetDirectory) ? StorageChangeOutcome.NoChange
                : File.Exists(StorePath(targetDirectory)) ? StorageChangeOutcome.AdoptTarget
                : File.Exists(StorePath(currentDirectory)) ? StorageChangeOutcome.MoveToTarget
                : StorageChangeOutcome.SetEmptyTarget;
        }

        public void MoveStore(string currentDirectory, string targetDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);

            _ = Directory.CreateDirectory(targetDirectory);

            var sourceStore = StorePath(currentDirectory);
            if (File.Exists(sourceStore))
            {
                File.Copy(sourceStore, StorePath(targetDirectory), overwrite: true);
            }

            var sourceIcons = Path.Combine(currentDirectory, _iconsSubdirectory);
            var targetIcons = Path.Combine(targetDirectory, _iconsSubdirectory);
            if (Directory.Exists(sourceIcons))
            {
                CopyDirectory(sourceIcons, targetIcons);
            }

            // Only remove the originals once everything is safely copied across.
            if (File.Exists(sourceStore))
            {
                File.Delete(sourceStore);
            }
            if (Directory.Exists(sourceIcons))
            {
                Directory.Delete(sourceIcons, recursive: true);
            }
        }

        private string StorePath(string directory) => Path.Combine(directory, StoreFileName);

        private static bool PathsEqual(string a, string b) =>
            string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
                StringComparison.OrdinalIgnoreCase);

        private static void CopyDirectory(string source, string destination)
        {
            _ = Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
            }
            foreach (var directory in Directory.EnumerateDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }
    }
}
