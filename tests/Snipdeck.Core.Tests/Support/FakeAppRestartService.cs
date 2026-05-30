using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakeAppRestartService : IAppRestartService
    {
        public int RestartCount { get; private set; }

        /// <summary>Simulates whether the restart could be initiated.</summary>
        public bool NextRestartResult { get; set; } = true;

        public bool Restart()
        {
            RestartCount++;
            return NextRestartResult;
        }
    }
}
