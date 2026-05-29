namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Abstracts marshalling work back to the UI thread so view models in Core
    /// can post updates without referencing WinUI types directly.
    /// </summary>
    public interface IDispatcher
    {
        bool HasUiThreadAccess { get; }

        void Enqueue(Action action);
    }
}
