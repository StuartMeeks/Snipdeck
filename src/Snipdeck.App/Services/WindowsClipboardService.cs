using Snipdeck.Core.Abstractions;

using Windows.ApplicationModel.DataTransfer;

namespace Snipdeck.App.Services
{
    internal sealed class WindowsClipboardService : IClipboardService
    {
        public Task SetTextAsync(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
            return Task.CompletedTask;
        }
    }
}
