using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakeClock(DateTimeOffset initial) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = initial;

        public void Advance(TimeSpan delta)
        {
            UtcNow = UtcNow.Add(delta);
        }

        public void Set(DateTimeOffset value)
        {
            UtcNow = value;
        }
    }
}
