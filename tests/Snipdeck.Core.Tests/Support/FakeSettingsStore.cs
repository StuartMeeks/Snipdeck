using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakeSettingsStore : ISettingsStore
    {
        public AppConfig Current { get; private set; } = new();

        public int SaveCount { get; private set; }

        public string FilePath => "in-memory-settings";

        public Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Current);

        public Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
        {
            Current = config;
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
