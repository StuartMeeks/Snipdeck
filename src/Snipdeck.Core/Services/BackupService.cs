using System.Globalization;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Services
{
    public sealed class BackupService : IBackupService, IDisposable
    {
        public const int DefaultRetention = AppConfig.DefaultBackupRetention;
        private const string _filenamePrefix = "snipstore_";
        private const string _filenameSuffix = ".json";
        private const string _timestampFormat = "yyyyMMdd_HHmmssfff";

        private readonly string _sourceFilePath;
        private readonly IClock _clock;
        private readonly Func<int> _retentionProvider;
        private readonly SemaphoreSlim _gate = new(1, 1);

        /// <summary>
        /// Constructs the service with a fixed retention count. Validated eagerly.
        /// </summary>
        public BackupService(string sourceFilePath, string backupDirectory, IClock clock, int retention = DefaultRetention)
            : this(sourceFilePath, backupDirectory, clock, ValidateFixedRetention(retention))
        {
        }

        /// <summary>
        /// Constructs the service with a retention <paramref name="retentionProvider"/>
        /// read lazily on each prune, so a change to the backing configuration takes
        /// effect on the next backup without re-creating the service. Provided values
        /// are clamped to at least 1 at use time — a bad configuration value must never
        /// crash a backup.
        /// </summary>
        public BackupService(string sourceFilePath, string backupDirectory, IClock clock, Func<int> retentionProvider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(backupDirectory);
            ArgumentNullException.ThrowIfNull(clock);
            ArgumentNullException.ThrowIfNull(retentionProvider);

            _sourceFilePath = sourceFilePath;
            BackupDirectory = backupDirectory;
            _clock = clock;
            _retentionProvider = retentionProvider;
        }

        private static Func<int> ValidateFixedRetention(int retention)
        {
            return retention < 1
                ? throw new ArgumentOutOfRangeException(nameof(retention), retention, "Retention must be at least 1.")
                : () => retention;
        }

        public string BackupDirectory { get; }

        public async Task<BackupInfo?> CreateBackupAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!File.Exists(_sourceFilePath))
                {
                    return null;
                }

                _ = Directory.CreateDirectory(BackupDirectory);

                var now = _clock.UtcNow;
                var timestamp = now.UtcDateTime.ToString(_timestampFormat, CultureInfo.InvariantCulture);

                var destinationPath = Path.Combine(BackupDirectory, _filenamePrefix + timestamp + _filenameSuffix);
                var collisionIndex = 1;
                while (File.Exists(destinationPath))
                {
                    destinationPath = Path.Combine(
                        BackupDirectory,
                        $"{_filenamePrefix}{timestamp}-{collisionIndex}{_filenameSuffix}");
                    collisionIndex++;
                }

                File.Copy(_sourceFilePath, destinationPath);

                PruneStaleBackups();

                var size = new FileInfo(destinationPath).Length;
                return new BackupInfo(destinationPath, now, size);
            }
            finally
            {
                _ = _gate.Release();
            }
        }

        public Task<IReadOnlyList<BackupInfo>> ListBackupsAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(BackupDirectory))
            {
                return Task.FromResult<IReadOnlyList<BackupInfo>>([]);
            }

            var infos = EnumerateBackupFiles()
                .OrderByDescending(name => name, StringComparer.Ordinal)
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    var createdAt = TryParseTimestamp(info.Name, out var ts)
                        ? ts
                        : new DateTimeOffset(info.CreationTimeUtc, TimeSpan.Zero);
                    return new BackupInfo(path, createdAt, info.Length);
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<BackupInfo>>(infos);
        }

        public void Dispose()
        {
            _gate.Dispose();
        }

        private IEnumerable<string> EnumerateBackupFiles()
        {
            return Directory.EnumerateFiles(BackupDirectory, _filenamePrefix + "*" + _filenameSuffix);
        }

        private void PruneStaleBackups()
        {
            var retention = Math.Max(1, _retentionProvider());

            var ordered = EnumerateBackupFiles()
                .OrderByDescending(name => name, StringComparer.Ordinal)
                .ToList();

            for (var i = retention; i < ordered.Count; i++)
            {
                try
                {
                    File.Delete(ordered[i]);
                }
                catch (IOException)
                {
                    // Best-effort: a backup file may be in use. Skip and retry next time.
                }
            }
        }

        private static bool TryParseTimestamp(string fileName, out DateTimeOffset result)
        {
            result = default;
            if (!fileName.StartsWith(_filenamePrefix, StringComparison.Ordinal)
                || !fileName.EndsWith(_filenameSuffix, StringComparison.Ordinal))
            {
                return false;
            }

            var core = fileName.Substring(
                _filenamePrefix.Length,
                fileName.Length - _filenamePrefix.Length - _filenameSuffix.Length);

            var dashIndex = core.IndexOf('-', _timestampFormat.Length - 1);
            if (dashIndex > 0)
            {
                core = core[..dashIndex];
            }

            if (DateTime.TryParseExact(
                core,
                _timestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            {
                result = new DateTimeOffset(parsed, TimeSpan.Zero);
                return true;
            }

            return false;
        }
    }
}
