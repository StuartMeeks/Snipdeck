using System.Text;
using System.Text.RegularExpressions;

using Snipdeck.Core.Models;

namespace Snipdeck.Importer.Translation
{
    /// <summary>
    /// Translates SnipCommand inline markup into Snipdeck's <c>{token}</c> placeholders plus a
    /// structured <see cref="Parameter"/> list.
    /// <para>
    /// Two self-closing forms are recognised:
    /// <c>[sc_choice name="x" value="a,b,c" /]</c> (CSV options, first is the default) and
    /// <c>[sc_variable name="x" value="default" /]</c> (an empty value means no default).
    /// </para>
    /// <para>
    /// The translator is pure and defensive: input is treated as untrusted, names are slugified
    /// into legal token identifiers (with collisions disambiguated), control/escape characters are
    /// stripped from values, lengths are capped, and any markup it cannot parse is left verbatim so
    /// the command never gets corrupted.
    /// </para>
    /// </summary>
    public static partial class ScMarkupTranslator
    {
        private const int _maxNameLength = 100;
        private const int _maxValueLength = 4000;
        private const int _maxOptions = 100;

        // A self-closing [sc_choice ...] / [sc_variable ...] tag. Attribute values are
        // double-quoted and may contain anything except a quote (so backslashes, spaces,
        // commas and ']' inside a value are fine). Non-self-closing or malformed tags simply
        // don't match and are left untouched.
        [GeneratedRegex(
            """\[sc_(?<kind>choice|variable)(?<attrs>(?:\s+[A-Za-z][A-Za-z0-9]*\s*=\s*"[^"]*")*)\s*/\]""",
            RegexOptions.CultureInvariant)]
        private static partial Regex TagRegex();

        [GeneratedRegex(
            "(?<key>[A-Za-z][A-Za-z0-9]*)\\s*=\\s*\"(?<val>[^\"]*)\"",
            RegexOptions.CultureInvariant)]
        private static partial Regex AttrRegex();

        public static ScTranslation Translate(string? command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return new ScTranslation(command ?? string.Empty, []);
            }

            // name (as written in the markup) -> minted token; lets a name repeated within one
            // command map to a single token and a single Parameter.
            var nameToToken = new Dictionary<string, string>(StringComparer.Ordinal);
            var usedTokens = new HashSet<string>(StringComparer.Ordinal);
            var parameters = new List<Parameter>();

            var rewritten = TagRegex().Replace(command, match =>
            {
                var attributes = ParseAttributes(match.Groups["attrs"].Value);
                if (!attributes.TryGetValue("name", out var rawName) || string.IsNullOrWhiteSpace(rawName))
                {
                    // No usable name — leave the markup verbatim rather than guess.
                    return match.Value;
                }

                var isChoice = string.Equals(match.Groups["kind"].Value, "choice", StringComparison.Ordinal);
                _ = attributes.TryGetValue("value", out var rawValue);
                rawValue ??= string.Empty;

                if (!nameToToken.TryGetValue(rawName, out var token))
                {
                    token = MintToken(rawName, usedTokens);
                    nameToToken[rawName] = token;
                    _ = usedTokens.Add(token);
                    parameters.Add(BuildParameter(token, isChoice, rawValue));
                }

                return "{" + token + "}";
            });

            return new ScTranslation(rewritten, parameters);
        }

        private static Parameter BuildParameter(string token, bool isChoice, string rawValue)
        {
            var value = Sanitise(rawValue);
            if (isChoice)
            {
                var options = value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Take(_maxOptions)
                    .ToList();
                return new Parameter
                {
                    Name = token,
                    Type = ParameterType.Choice,
                    Options = options,
                    Default = options.Count > 0 ? options[0] : null,
                };
            }

            return new Parameter
            {
                Name = token,
                Type = ParameterType.Text,
                Default = value.Length == 0 ? null : value,
            };
        }

        private static Dictionary<string, string> ParseAttributes(string attrs)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match attr in AttrRegex().Matches(attrs))
            {
                // First occurrence wins; ignore duplicate keys.
                _ = result.TryAdd(attr.Groups["key"].Value, attr.Groups["val"].Value);
            }

            return result;
        }

        /// <summary>
        /// Turns a markup name into a legal Snipdeck token matching <c>[A-Za-z_][A-Za-z0-9_]*</c>,
        /// disambiguating against tokens already used in the same command.
        /// </summary>
        private static string MintToken(string rawName, HashSet<string> usedTokens)
        {
            var slug = Slugify(rawName);
            if (!usedTokens.Contains(slug))
            {
                return slug;
            }

            for (var suffix = 2; ; suffix++)
            {
                var candidate = slug + "_" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!usedTokens.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        private static string Slugify(string rawName)
        {
            var name = Sanitise(rawName);
            if (name.Length > _maxNameLength)
            {
                name = name[.._maxNameLength];
            }

            // Already a legal identifier? Keep it as-is so "authId" / "customerId" survive verbatim.
            if (IdentifierRegex().IsMatch(name))
            {
                return name;
            }

            // Otherwise build PascalCase-ish from alphanumeric runs: "Agreement ID" -> "AgreementID",
            // "Order ID / Agreement ID" -> "OrderIDAgreementID".
            var parts = AlphanumericRunRegex().Matches(name);
            if (parts.Count == 0)
            {
                return "param";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i].Value;
                if (i == 0)
                {
                    _ = builder.Append(part);
                }
                else
                {
                    _ = builder.Append(char.ToUpperInvariant(part[0]));
                    if (part.Length > 1)
                    {
                        _ = builder.Append(part[1..]);
                    }
                }
            }

            // parts is non-empty here and every run contributes at least one character, so the
            // slug is never empty. A leading digit is illegal for a token; prefix an underscore.
            var slug = builder.ToString();
            return char.IsDigit(slug[0]) ? "_" + slug : slug;
        }

        /// <summary>Strips control and ANSI escape characters and caps length.</summary>
        private static string Sanitise(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (!char.IsControl(ch))
                {
                    _ = builder.Append(ch);
                }

                if (builder.Length >= _maxValueLength)
                {
                    break;
                }
            }

            return builder.ToString().Trim();
        }

        [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
        private static partial Regex IdentifierRegex();

        [GeneratedRegex("[A-Za-z0-9]+", RegexOptions.CultureInvariant)]
        private static partial Regex AlphanumericRunRegex();
    }

    /// <summary>The rewritten command template plus the parameters extracted from its markup.</summary>
    public sealed record ScTranslation(string CommandTemplate, IReadOnlyList<Parameter> Parameters);
}
