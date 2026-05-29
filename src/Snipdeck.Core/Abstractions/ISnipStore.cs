using Snipdeck.Core.Models;

namespace Snipdeck.Core.Abstractions
{
    public interface ISnipStore
    {
        string FilePath { get; }

        Task<SnipStoreDocument> LoadAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(SnipStoreDocument document, CancellationToken cancellationToken = default);
    }
}
