namespace Snipdeck.Core.Abstractions
{
    public interface ITrayService : IDisposable
    {
        event EventHandler? ShowRequested;

        event EventHandler? ExitRequested;

        Task InitialiseAsync();
    }
}
