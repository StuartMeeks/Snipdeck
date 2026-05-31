using Snipdeck.Core.Models;

namespace Snipdeck.Core.Engine
{
    /// <summary>
    /// Computes the effective parameter set to present when filling a Snip,
    /// resolving each by name with precedence: the Snip's own (local) parameters
    /// override CLI-scoped definitions, which override global definitions.
    ///
    /// The result is additive to the pre-shared-parameters behaviour: it always
    /// includes the Snip's local parameters, then adds a shared definition for
    /// any <c>{token}</c> in the template that the Snip doesn't define locally.
    /// Tokens defined in no scope are left out (the engine copies them verbatim),
    /// preserving the "bare token copies as-is" behaviour.
    /// </summary>
    public static class ParameterResolver
    {
        public static IReadOnlyList<Parameter> Resolve(
            Snip snip,
            Cli? cli,
            IReadOnlyList<Parameter>? globalParameters = null)
        {
            ArgumentNullException.ThrowIfNull(snip);

            var result = new List<Parameter>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // 1. Local parameters — the override layer; presented as-is.
            foreach (var local in snip.Parameters)
            {
                if (seen.Add(local.Name))
                {
                    result.Add(local);
                }
            }

            // 2. Tokens used by the template but not defined locally — inherit
            //    from the CLI scope first, then global. First match wins.
            foreach (var token in SubstitutionEngine.ExtractTokens(snip.CommandTemplate))
            {
                if (seen.Contains(token))
                {
                    continue;
                }

                var inherited = FindByName(cli?.Parameters, token) ?? FindByName(globalParameters, token);
                if (inherited is not null)
                {
                    _ = seen.Add(token);
                    result.Add(inherited);
                }
            }

            return result;
        }

        private static Parameter? FindByName(IReadOnlyList<Parameter>? parameters, string name)
        {
            if (parameters is null)
            {
                return null;
            }
            foreach (var parameter in parameters)
            {
                if (string.Equals(parameter.Name, name, StringComparison.Ordinal))
                {
                    return parameter;
                }
            }
            return null;
        }
    }
}
