using Microsoft.UI.Dispatching;

using Snipdeck.Core.Abstractions;

namespace Snipdeck.App.Services
{
    /// <summary>
    /// Captures the UI thread's <see cref="DispatcherQueue"/> the first time it
    /// is resolved on the UI thread; that means the very first call must come
    /// from the main thread (the App startup path satisfies this).
    /// </summary>
    internal sealed class WinUiDispatcher : IDispatcher
    {
        private DispatcherQueue? _dispatcherQueue;

        public bool HasUiThreadAccess => GetQueue().HasThreadAccess;

        public void Enqueue(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            var queue = GetQueue();
            if (queue.HasThreadAccess)
            {
                action();
                return;
            }

            _ = queue.TryEnqueue(() => action());
        }

        private DispatcherQueue GetQueue()
        {
            return _dispatcherQueue ??= DispatcherQueue.GetForCurrentThread()
                ?? throw new InvalidOperationException(
                    "WinUiDispatcher was first resolved off the UI thread; no DispatcherQueue is available.");
        }
    }
}
