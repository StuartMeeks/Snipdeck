using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class GlyphInputTests
    {
        private static readonly string _tagGlyph = char.ConvertFromUtf32(0xE8EC);

        [Theory]
        [InlineData("E8EC")]
        [InlineData("e8ec")]
        [InlineData("U+E8EC")]
        [InlineData("0xE8EC")]
        [InlineData("\\uE8EC")]
        [InlineData("&#xE8EC;")]
        public void Resolve_converts_a_typed_code_point_to_its_character(string input)
        {
            Assert.Equal(_tagGlyph, GlyphInput.Resolve(input));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Resolve_returns_empty_for_blank_input(string? input)
        {
            Assert.Equal(string.Empty, GlyphInput.Resolve(input));
        }

        [Fact]
        public void Resolve_keeps_a_single_pasted_character_literally()
        {
            // A lone character is taken as-is — even "#" or an actual pasted glyph —
            // never reinterpreted as a code point.
            Assert.Equal("#", GlyphInput.Resolve("#"));
            Assert.Equal(_tagGlyph, GlyphInput.Resolve(_tagGlyph));
        }

        [Fact]
        public void Resolve_keeps_non_hex_text_unchanged()
        {
            Assert.Equal("tag", GlyphInput.Resolve(" tag "));
        }
    }
}
