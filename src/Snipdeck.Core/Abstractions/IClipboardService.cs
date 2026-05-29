namespace Snipdeck.Core.Abstractions
{
    public interface IClipboardService
    {
        Task SetTextAsync(string text);
    }
}
