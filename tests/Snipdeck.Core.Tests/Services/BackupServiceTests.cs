using Snipdeck.Core.Services;
using Snipdeck.Core.Tests.Support;

namespace Snipdeck.Core.Tests.Services
{

    public sealed class BackupServiceTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly string _sourcePath;
        private readonly string _backupDirectory;

        public BackupServiceTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "snipdeck-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            _sourcePath = Path.Combine(_tempDirectory, "store.json");
            _backupDirectory = Path.Combine(_tempDirectory, "backups");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            GC.SuppressFinalize(this);
        }

        private BackupService BuildService(FakeClock clock, int retention = BackupService.DefaultRetention)
        {
            return new BackupService(_sourcePath, _backupDirectory, clock, retention);
        }

        private void WriteSource(string content = "{\"schemaVersion\":1,\"clis\":[],\"snips\":[]}")
        {
            File.WriteAllText(_sourcePath, content);
        }

        [Fact]
        public void Constructor_validates_arguments()
        {
            var clock = new FakeClock(DateTimeOffset.UtcNow);

            Assert.Throws<ArgumentNullException>(() => new BackupService(null!, _backupDirectory, clock));
            Assert.Throws<ArgumentNullException>(() => new BackupService(_sourcePath, null!, clock));
            Assert.Throws<ArgumentNullException>(() => new BackupService(_sourcePath, _backupDirectory, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BackupService(_sourcePath, _backupDirectory, clock, retention: 0));
        }

        [Fact]
        public async Task CreateBackupAsync_returns_null_when_source_missing()
        {
            var clock = new FakeClock(new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero));
            var service = BuildService(clock);

            var info = await service.CreateBackupAsync();

            Assert.Null(info);
            Assert.False(Directory.Exists(_backupDirectory) && Directory.EnumerateFiles(_backupDirectory).Any());
        }

        [Fact]
        public async Task CreateBackupAsync_copies_source_to_timestamped_destination()
        {
            WriteSource("hello");
            var clock = new FakeClock(new DateTimeOffset(2026, 5, 29, 12, 34, 56, 789, TimeSpan.Zero));
            var service = BuildService(clock);

            var info = await service.CreateBackupAsync();

            Assert.NotNull(info);
            Assert.True(File.Exists(info!.FilePath));
            Assert.EndsWith("snipstore_20260529_123456789.json", info.FilePath);
            Assert.Equal("hello", await File.ReadAllTextAsync(info.FilePath));
            Assert.Equal(5, info.SizeBytes);
            Assert.Equal(clock.UtcNow, info.CreatedAtUtc);
        }

        [Fact]
        public async Task CreateBackupAsync_creates_missing_backup_directory()
        {
            WriteSource();
            Assert.False(Directory.Exists(_backupDirectory));

            var clock = new FakeClock(new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero));
            await BuildService(clock).CreateBackupAsync();

            Assert.True(Directory.Exists(_backupDirectory));
        }

        [Fact]
        public async Task CreateBackupAsync_appends_collision_suffix_when_clock_repeats()
        {
            WriteSource();
            var fixedTime = new DateTimeOffset(2026, 5, 29, 12, 0, 0, 0, TimeSpan.Zero);
            var clock = new FakeClock(fixedTime);
            var service = BuildService(clock);

            var first = await service.CreateBackupAsync();
            var second = await service.CreateBackupAsync();
            var third = await service.CreateBackupAsync();

            Assert.NotEqual(first!.FilePath, second!.FilePath);
            Assert.NotEqual(second.FilePath, third!.FilePath);
            Assert.EndsWith("snipstore_20260529_120000000.json", first.FilePath);
            Assert.EndsWith("snipstore_20260529_120000000-1.json", second.FilePath);
            Assert.EndsWith("snipstore_20260529_120000000-2.json", third.FilePath);
        }

        [Fact]
        public async Task CreateBackupAsync_prunes_backups_beyond_retention()
        {
            WriteSource();
            var clock = new FakeClock(new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero));
            var service = BuildService(clock, retention: 3);

            for (var i = 0; i < 5; i++)
            {
                await service.CreateBackupAsync();
                clock.Advance(TimeSpan.FromSeconds(1));
            }

            var remaining = Directory.GetFiles(_backupDirectory, "snipstore_*.json")
                .OrderBy(f => f, StringComparer.Ordinal)
                .Select(Path.GetFileName)
                .ToArray();

            Assert.Equal(3, remaining.Length);
            Assert.Equal("snipstore_20260529_120002000.json", remaining[0]);
            Assert.Equal("snipstore_20260529_120003000.json", remaining[1]);
            Assert.Equal("snipstore_20260529_120004000.json", remaining[2]);
        }

        [Fact]
        public async Task PruneStep_does_not_touch_unrelated_files_in_backup_directory()
        {
            WriteSource();
            Directory.CreateDirectory(_backupDirectory);
            var sibling = Path.Combine(_backupDirectory, "not-a-backup.txt");
            await File.WriteAllTextAsync(sibling, "untouched");

            var clock = new FakeClock(new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero));
            var service = BuildService(clock, retention: 1);

            for (var i = 0; i < 5; i++)
            {
                await service.CreateBackupAsync();
                clock.Advance(TimeSpan.FromSeconds(1));
            }

            Assert.True(File.Exists(sibling));
            Assert.Equal("untouched", await File.ReadAllTextAsync(sibling));
        }

        [Fact]
        public async Task ListBackupsAsync_returns_newest_first_with_parsed_timestamps()
        {
            WriteSource();
            var clock = new FakeClock(new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero));
            var service = BuildService(clock);

            await service.CreateBackupAsync();
            clock.Advance(TimeSpan.FromSeconds(5));
            await service.CreateBackupAsync();
            clock.Advance(TimeSpan.FromSeconds(5));
            await service.CreateBackupAsync();

            var list = await service.ListBackupsAsync();

            Assert.Equal(3, list.Count);
            Assert.True(list[0].CreatedAtUtc > list[1].CreatedAtUtc);
            Assert.True(list[1].CreatedAtUtc > list[2].CreatedAtUtc);
        }

        [Fact]
        public async Task ListBackupsAsync_returns_empty_when_directory_missing()
        {
            var clock = new FakeClock(DateTimeOffset.UtcNow);
            var service = BuildService(clock);

            var list = await service.ListBackupsAsync();

            Assert.Empty(list);
        }
    }
}
