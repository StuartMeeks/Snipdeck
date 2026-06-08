using System.IO.Compression;

namespace Snipdeck.Execution.Engine
{
    /// <summary>Gzip compression for the retained raw VT stream stored in history.</summary>
    public static class GzipUtil
    {
        /// <summary>Compresses bytes with gzip.</summary>
        public static byte[] Compress(ReadOnlySpan<byte> data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                gzip.Write(data);
            }

            return output.ToArray();
        }

        /// <summary>Decompresses gzip bytes produced by <see cref="Compress"/>.</summary>
        public static byte[] Decompress(byte[] compressed)
        {
            ArgumentNullException.ThrowIfNull(compressed);
            using var input = new MemoryStream(compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }
    }
}
