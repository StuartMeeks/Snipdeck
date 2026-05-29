using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

namespace Snipdeck.Core.Tests.Services
{

    public sealed class JsonSettingsStoreTests : IDisposable
    {
        private readonly string _tempDirectory;

        public JsonSettingsStoreTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "snipdeck-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            GC.SuppressFinalize(this);
        }

        private string PathIn(string name) => Path.Combine(_tempDirectory, name);

        [Fact]
        public async Task LoadAsync_returns_defaults_when_file_missing()
        {
            var store = new JsonSettingsStore(PathIn("settings.json"));

            var config = await store.LoadAsync();

            Assert.Equal(AppConfig.CurrentSchemaVersion, config.SchemaVersion);
            Assert.Null(config.StoragePath);
            Assert.Null(config.BackupDirectory);
            Assert.Equal(ThemePreference.System, config.Theme);
            Assert.Equal(CloseBehaviour.HideToTray, config.CloseBehaviour);
            Assert.NotNull(config.Hotkey);
            Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, config.Hotkey.Modifiers);
            Assert.Equal("S", config.Hotkey.Key);
        }

        [Fact]
        public async Task SaveAsync_then_LoadAsync_round_trips_all_fields()
        {
            var store = new JsonSettingsStore(PathIn("settings.json"));

            await store.SaveAsync(new AppConfig
            {
                StoragePath = "/data/store",
                BackupDirectory = "/data/backups",
                Theme = ThemePreference.Dark,
                CloseBehaviour = CloseBehaviour.Exit,
                Hotkey = new HotkeyBinding
                {
                    Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift,
                    Key = "Space",
                },
            });

            var loaded = await store.LoadAsync();

            Assert.Equal("/data/store", loaded.StoragePath);
            Assert.Equal("/data/backups", loaded.BackupDirectory);
            Assert.Equal(ThemePreference.Dark, loaded.Theme);
            Assert.Equal(CloseBehaviour.Exit, loaded.CloseBehaviour);
            Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, loaded.Hotkey.Modifiers);
            Assert.Equal("Space", loaded.Hotkey.Key);
        }

        [Fact]
        public async Task SaveAsync_creates_missing_parent_directory()
        {
            var nested = PathIn("a/b/settings.json");
            var store = new JsonSettingsStore(nested);

            await store.SaveAsync(new AppConfig());

            Assert.True(File.Exists(nested));
        }

        [Fact]
        public async Task SaveAsync_does_not_leave_tmp_file_behind_on_success()
        {
            var path = PathIn("settings.json");
            var store = new JsonSettingsStore(path);

            await store.SaveAsync(new AppConfig());

            Assert.False(File.Exists(path + ".tmp"));
            Assert.True(File.Exists(path));
        }

        [Fact]
        public async Task LoadAsync_throws_when_schema_version_is_newer_than_supported()
        {
            var path = PathIn("settings.json");
            var futureJson = $$"""
                { "schemaVersion": {{AppConfig.CurrentSchemaVersion + 1}} }
                """;
            await File.WriteAllTextAsync(path, futureJson);

            var store = new JsonSettingsStore(path);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync());
        }

        [Fact]
        public async Task LoadAsync_repairs_a_missing_hotkey_with_the_default()
        {
            var path = PathIn("settings.json");
            await File.WriteAllTextAsync(path, "{}");

            var store = new JsonSettingsStore(path);
            var loaded = await store.LoadAsync();

            Assert.NotNull(loaded.Hotkey);
            Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, loaded.Hotkey.Modifiers);
            Assert.Equal("S", loaded.Hotkey.Key);
        }

        [Fact]
        public async Task SaveAsync_throws_on_null_config()
        {
            var store = new JsonSettingsStore(PathIn("settings.json"));

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(null!));
        }
    }
}
