namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Normalises a user-chosen image into a small square icon that can be
    /// stored alongside the Snip store. Implementations should cap at the
    /// requested edge size (default 256), centre-square-crop, and re-encode
    /// to a known format (PNG).
    /// </summary>
    public interface IIconNormaliser
    {
        Task<byte[]> NormaliseAsync(byte[] sourceBytes, int maxEdgePixels = 256);
    }
}
