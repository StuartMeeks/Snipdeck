using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakeIconAssetStorage : IIconAssetStorage
    {
        public Dictionary<Guid, byte[]> Saved { get; } = [];

        public List<string> Deleted { get; } = [];

        public Task<string> SaveIconAsync(Guid cliId, byte[] bytes)
        {
            Saved[cliId] = bytes;
            return Task.FromResult($"icons/{cliId:N}.png");
        }

        public Task DeleteIconAsync(string relativePath)
        {
            Deleted.Add(relativePath);
            return Task.CompletedTask;
        }

        public string? ResolveAbsolutePath(string? relativePath) => relativePath;
    }
}
