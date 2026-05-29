using System.Globalization;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Services
{
    public sealed class BackupService : IBackupService, IDisposable
    {
        public const int DefaultRetention = 20;
        private const string _filenamePrefix = "snipstore_";
        private const string _filenameSuffix = ".json";
        private const string _timestampFormat = "yyyyMMdd_HHmmssfff";

        private readonly string _sourceFilePath;
        private readonly IClock _clock;
        private readonly int _retention;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public BackupService(string sourceFilePath, string backupDirectory, IClock clock, int retention = DefaultRetention)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(backupDirectory);
            ArgumentNullException.ThrowIfNull(clock);
            if (retention < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(retention), retention, "Retention must be at least 1.");
            }

            _sourceFilePath = sourceFilePath;
            BackupDirectory = backupDirectory;
            _clock = clock;
            _retention = retention;
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
            var ordered = EnumerateBackupFiles()
                .OrderByDescending(name => name, StringComparer.Ordinal)
                .ToList();

            for (var i = _retention; i < ordered.Count; i++)
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
