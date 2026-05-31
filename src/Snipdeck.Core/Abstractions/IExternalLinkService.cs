namespace Snipdeck.Core.Abstractions
{
    /// <summary>Opens an external URL (e.g. documentation) in the default browser.</summary>
    public interface IExternalLinkService
    {
        Task OpenAsync(string url);
    }
}
