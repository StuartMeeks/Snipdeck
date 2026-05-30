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
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            var combined = Path.Combine(_baseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var full = Path.GetFullPath(combined);

            // The store is untrusted input and this path feeds File.Delete. Reject
            // anything that escapes the managed icons directory — an absolute path
            // (Path.Combine silently discards the base for one) or `..` traversal —
            // so a malformed IconRef can never touch files outside icon storage.
            var iconsRoot = Path.GetFullPath(Path.Combine(_baseDirectory, _iconsSubdirectory));
            var prefix = iconsRoot + Path.DirectorySeparatorChar;
            return full.StartsWith(prefix, StringComparison.Ordinal) ? full : null;
        }
    }
}
