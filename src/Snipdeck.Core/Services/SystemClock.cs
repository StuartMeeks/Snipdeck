using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Services
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
