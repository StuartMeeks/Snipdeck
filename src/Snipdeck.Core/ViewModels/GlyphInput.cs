using System.Globalization;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// Normalises a user-entered icon glyph into the actual character to render.
    /// Accepts a pasted glyph character as-is, or a Unicode code point typed as
    /// hex — bare ("E8EC") or prefixed ("U+E8EC", "0xE8EC", "&amp;#xE8EC;").
    /// </summary>
    public static class GlyphInput
    {
        private static readonly string[] _prefixes =
            ["U+", "u+", "\\u", "\\U", "0x", "0X", "&#x", "&#X"];

        /// <summary>
        /// Returns the resolved glyph character, or <see cref="string.Empty"/> when
        /// the input is blank. Input that isn't a recognised code point is returned
        /// trimmed but otherwise unchanged.
        /// </summary>
        public static string Resolve(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var trimmed = input.Trim();
            var hex = trimmed;
            foreach (var prefix in _prefixes)
            {
                if (hex.StartsWith(prefix, StringComparison.Ordinal))
                {
                    hex = hex[prefix.Length..];
                    break;
                }
            }
            hex = hex.TrimEnd(';');

            // Only treat 2–6 hex digits as a code point; a single character is taken
            // literally (it's almost certainly a pasted glyph, not a typed code).
            return hex.Length is >= 2 and <= 6
                && hex.All(Uri.IsHexDigit)
                && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint)
                && codePoint > 0
                && codePoint <= 0x10FFFF
                && codePoint is < 0xD800 or > 0xDFFF
                ? char.ConvertFromUtf32(codePoint)
                : trimmed;
        }
    }
}
