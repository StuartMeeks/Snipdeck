using Snipdeck.Core.Services;

namespace Snipdeck.Core.Tests.Services
{
    public sealed class IconAssetStorageTests : IDisposable
    {
        private readonly string _baseDirectory;
        private readonly IconAssetStorage _storage;

        public IconAssetStorageTests()
        {
            _baseDirectory = Path.Combine(Path.GetTempPath(), "snipdeck-icons-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_baseDirectory);
            _storage = new IconAssetStorage(_baseDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_baseDirectory))
            {
                Directory.Delete(_baseDirectory, recursive: true);
            }
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task SaveIconAsync_writes_under_icons_and_returns_relative_path()
        {
            var id = Guid.NewGuid();

            var relative = await _storage.SaveIconAsync(id, [0x89, 0x50, 0x4E, 0x47]);

            Assert.Equal($"icons/{id:N}.png", relative);
            Assert.True(File.Exists(Path.Combine(_baseDirectory, "icons", $"{id:N}.png")));
        }

        [Fact]
        public async Task DeleteIconAsync_removes_a_contained_icon()
        {
            var id = Guid.NewGuid();
            var relative = await _storage.SaveIconAsync(id, [0x89, 0x50, 0x4E, 0x47]);
            var absolute = Path.Combine(_baseDirectory, "icons", $"{id:N}.png");

            await _storage.DeleteIconAsync(relative);

            Assert.False(File.Exists(absolute));
        }

        [Fact]
        public async Task DeleteIconAsync_ignores_parent_directory_traversal()
        {
            // A file outside the icons directory that a malformed IconRef tries to reach.
            var victim = Path.Combine(_baseDirectory, "important.txt");
            await File.WriteAllTextAsync(victim, "keep me");

            await _storage.DeleteIconAsync("../important.txt");

            Assert.True(File.Exists(victim));
        }

        [Fact]
        public async Task DeleteIconAsync_ignores_absolute_paths()
        {
            var victim = Path.Combine(_baseDirectory, "outside.txt");
            await File.WriteAllTextAsync(victim, "keep me");

            // Path.Combine(base, absolute) discards base — an unguarded delete would hit this.
            await _storage.DeleteIconAsync(victim);

            Assert.True(File.Exists(victim));
        }

        [Fact]
        public void ResolveAbsolutePath_returns_null_for_escaping_paths()
        {
            Assert.Null(_storage.ResolveAbsolutePath("../escape.png"));
            Assert.Null(_storage.ResolveAbsolutePath(Path.Combine(_baseDirectory, "outside.png")));
            Assert.Null(_storage.ResolveAbsolutePath(null));
        }

        [Fact]
        public void ResolveAbsolutePath_returns_full_path_for_contained_icon()
        {
            var resolved = _storage.ResolveAbsolutePath("icons/abc.png");

            Assert.NotNull(resolved);
            Assert.Equal(Path.GetFullPath(Path.Combine(_baseDirectory, "icons", "abc.png")), resolved);
        }
    }
}
