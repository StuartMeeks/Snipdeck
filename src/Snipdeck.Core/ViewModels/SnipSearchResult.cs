namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// A snip-search autocomplete suggestion. <see cref="CliName"/> is shown as a
    /// badge so identically-named snips in different CLIs are distinguishable.
    /// </summary>
    public sealed record SnipSearchResult(string Title, string CliName, Guid CliId, Guid SnipId);
}
