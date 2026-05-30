using System.Text.Json;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Services
{
    public sealed class JsonSettingsStore : ISettingsStore, IDisposable
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        public JsonSettingsStore(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            FilePath = filePath;
        }

        public string FilePath { get; }

        public async Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!File.Exists(FilePath))
                {
                    return new AppConfig();
                }

                await using var stream = new FileStream(
                    FilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                var config = await JsonSerializer
                    .DeserializeAsync(stream, StoreJsonContext.Default.AppConfig, cancellationToken)
                    .ConfigureAwait(false);

                if (config is null)
                {
                    return new AppConfig();
                }

                if (config.SchemaVersion > AppConfig.CurrentSchemaVersion)
                {
                    throw new InvalidOperationException(
                        $"Settings schema version {config.SchemaVersion} is newer than the supported version " +
                        $"{AppConfig.CurrentSchemaVersion}. Update the application to read these settings.");
                }

                config.Hotkey ??= HotkeyBinding.Default;

                return config;
            }
            finally
            {
                _ = _gate.Release();
            }
        }

        public async Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(config);

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    _ = Directory.CreateDirectory(directory);
                }

                var tempPath = FilePath + ".tmp";

                await using (var stream = new FileStream(
                    tempPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await JsonSerializer
                        .SerializeAsync(stream, config, StoreJsonContext.Default.AppConfig, cancellationToken)
                        .ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                File.Move(tempPath, FilePath, overwrite: true);
            }
            finally
            {
                _ = _gate.Release();
            }
        }

        public void Dispose()
        {
            _gate.Dispose();
        }
    }
}
