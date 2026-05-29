using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Services
{
    /// <summary>
    /// File-system-backed icon storage. The relative path returned by
    /// <see cref="SaveIconAsync"/> is always of the form
    /// <c>icons/&lt;guid&gt;.png</c>.
    /// </summary>
    public sealed class IconAssetStorage : IIconAssetStorage
    {
        private const string _iconsSubdirectory = "icons";
        private const string _iconExtension = ".png";

        private readonly string _baseDirectory;

        public IconAssetStorage(string baseDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
            _baseDirectory = baseDirectory;
        }

        public async Task<string> SaveIconAsync(Guid cliId, byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            var directory = Path.Combine(_baseDirectory, _iconsSubdirectory);
            _ = Directory.CreateDirectory(directory);

            var fileName = cliId.ToString("N") + _iconExtension;
            var absolute = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(absolute, bytes).ConfigureAwait(false);

            return Path.Combine(_iconsSubdirectory, fileName).Replace('\\', '/');
        }

        public Task DeleteIconAsync(string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
            var absolute = ResolveAbsolutePath(relativePath);
            if (absolute is not null && File.Exists(absolute))
            {
                File.Delete(absolute);
            }
            return Task.CompletedTask;
        }

        public string? ResolveAbsolutePath(string? relativePath)
        {
            return string.IsNullOrWhiteSpace(relativePath)
                ? null
                : Path.Combine(_baseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
