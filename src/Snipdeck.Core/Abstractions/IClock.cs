namespace Snipdeck.Core.Abstractions
{
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
