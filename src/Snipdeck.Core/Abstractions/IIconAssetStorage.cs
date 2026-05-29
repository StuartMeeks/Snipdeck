namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Persists CLI icons inside the data folder. Returns a relative path
    /// (e.g. <c>icons/&lt;cli-id&gt;.png</c>) that's safe to store on
    /// <c>Cli.IconRef</c> — relative so the folder can move between machines.
    /// </summary>
    public interface IIconAssetStorage
    {
        Task<string> SaveIconAsync(Guid cliId, byte[] bytes);

        Task DeleteIconAsync(string relativePath);

        string? ResolveAbsolutePath(string? relativePath);
    }
}
