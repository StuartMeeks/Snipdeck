using Snipdeck.Core.Models;

namespace Snipdeck.Core.Abstractions
{
    public interface ISettingsStore
    {
        string FilePath { get; }

        Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default);
    }
}
