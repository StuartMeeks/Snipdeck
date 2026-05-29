using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakeUpdateService : IUpdateService
    {
        public UpdateCheckResult NextCheckResult { get; set; } = new(false, null);

        public bool NextApplyResult { get; set; }

        public int CheckCallCount { get; private set; }

        public int ApplyCallCount { get; private set; }

        public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            CheckCallCount++;
            return Task.FromResult(NextCheckResult);
        }

        public Task<bool> ApplyUpdateAndRestartAsync(CancellationToken cancellationToken = default)
        {
            ApplyCallCount++;
            return Task.FromResult(NextApplyResult);
        }
    }
}
