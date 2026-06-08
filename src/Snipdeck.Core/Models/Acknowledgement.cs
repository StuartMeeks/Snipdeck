namespace Snipdeck.Core.Models
{
    /// <summary>
    /// A third-party project Snipdeck depends on, surfaced in Settings → About and the
    /// README so its authors are credited.
    /// </summary>
    /// <param name="Name">The project's display name.</param>
    /// <param name="Purpose">What Snipdeck uses it for.</param>
    /// <param name="Url">The project's home (repository or product page).</param>
    /// <param name="Licence">The licence it's distributed under.</param>
    public sealed record Acknowledgement(string Name, string Purpose, Uri Url, string Licence);
}
