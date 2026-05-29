using System.Text.RegularExpressions;

namespace Snipdeck.Core.Engine
{
    public static partial class SubstitutionEngine
    {
        [GeneratedRegex(@"\{([A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.CultureInvariant)]
        private static partial Regex TokenRegex();

        public static SubstitutionResult Substitute(
            string template,
            IReadOnlyDictionary<string, string?> values)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(values);

            if (template.Length == 0)
            {
                return new SubstitutionResult(string.Empty, []);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unresolved = new List<string>();

            var resolved = TokenRegex().Replace(template, match =>
            {
                var name = match.Groups[1].Value;
                if (values.TryGetValue(name, out var value) && value is not null)
                {
                    return value;
                }
                if (seen.Add(name))
                {
                    unresolved.Add(name);
                }
                return match.Value;
            });

            return new SubstitutionResult(resolved, unresolved);
        }

        public static IReadOnlyList<string> ExtractTokens(string template)
        {
            ArgumentNullException.ThrowIfNull(template);

            if (template.Length == 0)
            {
                return [];
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var tokens = new List<string>();
            foreach (Match match in TokenRegex().Matches(template))
            {
                var name = match.Groups[1].Value;
                if (seen.Add(name))
                {
                    tokens.Add(name);
                }
            }
            return tokens;
        }
    }
}
