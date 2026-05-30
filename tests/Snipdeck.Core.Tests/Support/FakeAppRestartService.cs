using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakeAppRestartService : IAppRestartService
    {
        public int RestartCount { get; private set; }

        public void Restart() => RestartCount++;
    }
}
