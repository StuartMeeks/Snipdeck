using Jdenticon;

namespace Snipdeck.Core.Services
{
    /// <summary>
    /// Generates identicon PNG bytes from a <see cref="Guid"/> seed. The seed
    /// must be the immutable <see cref="Models.Cli.Id"/> so renaming a CLI
    /// doesn't change its icon — recognisability is the whole point.
    /// </summary>
    public static class IdenticonService
    {
        public const int DefaultSize = 128;

        public static byte[] GeneratePng(Guid seed, int size = DefaultSize)
        {
            if (size < 16)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "Identicon size must be at least 16 pixels.");
            }

            var identicon = Identicon.FromValue(seed.ToString("N"), size);
            using var memory = new MemoryStream();
            identicon.SaveAsPng(memory);
            return memory.ToArray();
        }
    }
}
