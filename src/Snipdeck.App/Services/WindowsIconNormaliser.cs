using Snipdeck.Core.Abstractions;

using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Snipdeck.App.Services
{
    /// <summary>
    /// Centre-crops, resizes to <c>maxEdgePixels</c>, and re-encodes the
    /// source image as PNG. Implemented with <c>Windows.Graphics.Imaging</c>
    /// so we don't pull in heavier image libraries.
    /// </summary>
    internal sealed class WindowsIconNormaliser : IIconNormaliser
    {
        public async Task<byte[]> NormaliseAsync(byte[] sourceBytes, int maxEdgePixels = 256)
        {
            ArgumentNullException.ThrowIfNull(sourceBytes);
            if (maxEdgePixels < 16)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEdgePixels), maxEdgePixels, "Edge size must be at least 16 pixels.");
            }

            using var sourceStream = new InMemoryRandomAccessStream();
            var writer = new DataWriter(sourceStream);
            writer.WriteBytes(sourceBytes);
            _ = await writer.StoreAsync();
            _ = writer.DetachStream();
            sourceStream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(sourceStream);

            var sourceWidth = (uint)decoder.PixelWidth;
            var sourceHeight = (uint)decoder.PixelHeight;
            var edge = Math.Min(sourceWidth, sourceHeight);
            var offsetX = (sourceWidth - edge) / 2u;
            var offsetY = (sourceHeight - edge) / 2u;
            var targetEdge = (uint)Math.Min((uint)maxEdgePixels, edge);

            var transform = new BitmapTransform
            {
                Bounds = new BitmapBounds
                {
                    X = offsetX,
                    Y = offsetY,
                    Width = edge,
                    Height = edge,
                },
                ScaledWidth = targetEdge,
                ScaledHeight = targetEdge,
                InterpolationMode = BitmapInterpolationMode.Fant,
            };

            var pixelData = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage);

            using var outputStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outputStream);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                targetEdge,
                targetEdge,
                decoder.DpiX,
                decoder.DpiY,
                pixelData.DetachPixelData());
            await encoder.FlushAsync();

            outputStream.Seek(0);
            var reader = new DataReader(outputStream.GetInputStreamAt(0));
            var length = (uint)outputStream.Size;
            _ = await reader.LoadAsync(length);
            var bytes = new byte[length];
            reader.ReadBytes(bytes);
            return bytes;
        }
    }
}
