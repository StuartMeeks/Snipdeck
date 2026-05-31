using Snipdeck.Core.Abstractions;

namespace Snipdeck.App.Services
{
    internal sealed class WindowsExternalLinkService : IExternalLinkService
    {
        public async Task OpenAsync(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                _ = await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }
    }
}
