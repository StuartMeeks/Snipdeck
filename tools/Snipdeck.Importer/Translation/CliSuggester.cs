namespace Snipdeck.Importer.Translation
{
    /// <summary>
    /// Suggests the owning CLI for a command by taking its first whitespace-delimited token,
    /// peeling off well-known process wrappers (<c>sudo</c>, <c>npx</c>, …) so the suggestion is
    /// the real tool rather than the launcher.
    /// <para>
    /// Language runtimes such as <c>pip</c>, <c>python</c> and <c>dotnet</c> are deliberately
    /// <b>not</b> peeled: in practice those are the CLI the user organises around.
    /// </para>
    /// </summary>
    public static class CliSuggester
    {
        // Pure launchers: the interesting command is whatever they invoke.
        private static readonly HashSet<string> _wrappers = new(StringComparer.OrdinalIgnoreCase)
        {
            "sudo", "doas", "env", "npx", "pnpx", "bunx", "time", "nice", "nohup", "command", "exec",
        };

        public static CliSuggestion Suggest(string? commandTemplate)
        {
            if (string.IsNullOrWhiteSpace(commandTemplate))
            {
                return new CliSuggestion(string.Empty, Confident: false);
            }

            var tokens = commandTemplate.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var index = 0;

            // Peel any leading wrappers, plus their leading -flags (e.g. `env FOO=bar tool`).
            while (index < tokens.Length && IsWrapperOrFlag(tokens[index]))
            {
                index++;
            }

            if (index >= tokens.Length)
            {
                return new CliSuggestion(string.Empty, Confident: false);
            }

            var candidate = tokens[index];

            // A leading placeholder (the command itself is parameterised) can't be a confident name.
            return candidate.Contains('{')
                ? new CliSuggestion(string.Empty, Confident: false)
                : new CliSuggestion(candidate, Confident: true);
        }

        private static bool IsWrapperOrFlag(string token)
        {
            // Option flags and env-style assignments (FOO=bar) belong to the wrapper, not the tool.
            return token.StartsWith('-') || token.Contains('=') || _wrappers.Contains(token);
        }
    }

    /// <summary>A suggested CLI name and whether the suggestion is trustworthy.</summary>
    public sealed record CliSuggestion(string Name, bool Confident);
}
