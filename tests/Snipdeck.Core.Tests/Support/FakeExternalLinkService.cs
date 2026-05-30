using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakeExternalLinkService : IExternalLinkService
    {
        public string? LastOpenedUrl { get; private set; }

        public int OpenCount { get; private set; }

        public Task OpenAsync(string url)
        {
            LastOpenedUrl = url;
            OpenCount++;
            return Task.CompletedTask;
        }
    }
}
