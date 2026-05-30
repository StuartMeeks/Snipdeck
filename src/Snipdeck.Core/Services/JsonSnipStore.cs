using System.Text.Json;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Services
{
    public sealed class JsonSnipStore : ISnipStore, IDisposable
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        public JsonSnipStore(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            FilePath = filePath;
        }

        public string FilePath { get; }

        public async Task<SnipStoreDocument> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!File.Exists(FilePath))
                {
                    return new SnipStoreDocument();
                }

                await using var stream = new FileStream(
                    FilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                var document = await JsonSerializer
                    .DeserializeAsync(stream, StoreJsonContext.Default.SnipStoreDocument, cancellationToken)
                    .ConfigureAwait(false)
                    ?? new SnipStoreDocument();

                return document.SchemaVersion > SnipStoreDocument.CurrentSchemaVersion
                    ? throw new InvalidOperationException(
                        $"Store schema version {document.SchemaVersion} is newer than the supported version " +
                        $"{SnipStoreDocument.CurrentSchemaVersion}. Update the application to read this store.")
                    : document;
            }
            finally
            {
                _ = _gate.Release();
            }
        }

        public async Task SaveAsync(SnipStoreDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);

            // Stamp the current schema version on every write so a store touched
            // by this build is marked v2 — an older (v1-only) build then refuses
            // it rather than silently dropping the shared-parameter collections.
            document.SchemaVersion = SnipStoreDocument.CurrentSchemaVersion;

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
                        .SerializeAsync(stream, document, StoreJsonContext.Default.SnipStoreDocument, cancellationToken)
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
