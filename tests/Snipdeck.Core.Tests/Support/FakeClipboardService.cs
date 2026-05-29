using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakeClipboardService : IClipboardService
    {
        public string? LastText { get; private set; }

        public int SetTextCallCount { get; private set; }

        public Task SetTextAsync(string text)
        {
            LastText = text;
            SetTextCallCount++;
            return Task.CompletedTask;
        }
    }
}
