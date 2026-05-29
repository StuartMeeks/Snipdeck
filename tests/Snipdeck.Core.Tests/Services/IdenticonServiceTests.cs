using Snipdeck.Core.Services;

namespace Snipdeck.Core.Tests.Services
{
    public class IdenticonServiceTests
    {
        [Fact]
        public void GeneratePng_returns_a_valid_png_signature()
        {
            var bytes = IdenticonService.GeneratePng(Guid.NewGuid());

            // PNG signature: 89 50 4E 47 0D 0A 1A 0A
            Assert.True(bytes.Length > 8);
            Assert.Equal(0x89, bytes[0]);
            Assert.Equal((byte)'P', bytes[1]);
            Assert.Equal((byte)'N', bytes[2]);
            Assert.Equal((byte)'G', bytes[3]);
        }

        [Fact]
        public void GeneratePng_is_deterministic_for_a_given_seed()
        {
            var seed = Guid.Parse("a4d1c0e1-0000-0000-0000-000000000000");

            var first = IdenticonService.GeneratePng(seed);
            var second = IdenticonService.GeneratePng(seed);

            Assert.Equal(first, second);
        }

        [Fact]
        public void GeneratePng_differs_between_seeds()
        {
            var a = IdenticonService.GeneratePng(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            var b = IdenticonService.GeneratePng(Guid.Parse("22222222-2222-2222-2222-222222222222"));

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void GeneratePng_throws_on_too_small_size()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => IdenticonService.GeneratePng(Guid.NewGuid(), size: 8));
        }
    }
}
