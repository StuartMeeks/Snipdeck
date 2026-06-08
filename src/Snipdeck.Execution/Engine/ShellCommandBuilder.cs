using Snipdeck.Core.Models;

namespace Snipdeck.Execution.Engine
{
    /// <summary>
    /// Pure mapping from a <see cref="ShellKind"/> and a resolved command string to the
    /// concrete <see cref="ShellLaunch"/> (executable + arguments) that runs it, plus a
    /// human-readable description for the dry-run preview. No I/O, no process spawning —
    /// the single most-tested seam between the domain and the runner.
    /// </summary>
    public static class ShellCommandBuilder
    {
        /// <summary>The token replaced by the resolved command in a custom argument template.</summary>
        public const string CommandToken = "{command}";

        /// <summary>
        /// Builds the executable + argument list for <paramref name="resolvedCommand"/> under
        /// <paramref name="shell"/>. For <see cref="ShellKind.Custom"/>, uses
        /// <paramref name="customShellPath"/> and substitutes the command into
        /// <paramref name="customShellArgsTemplate"/> at the <see cref="CommandToken"/>.
        /// </summary>
        public static ShellLaunch Build(
            ShellKind shell,
            string? customShellPath,
            string? customShellArgsTemplate,
            string resolvedCommand)
        {
            ArgumentNullException.ThrowIfNull(resolvedCommand);

            return shell switch
            {
                ShellKind.Cmd => new ShellLaunch("cmd.exe", ["/c", resolvedCommand]),
                ShellKind.PowerShell => new ShellLaunch(
                    "powershell.exe",
                    ["-NoLogo", "-NoProfile", "-Command", resolvedCommand]),
                ShellKind.PwshCore => new ShellLaunch(
                    "pwsh",
                    ["-NoLogo", "-NoProfile", "-Command", resolvedCommand]),
                ShellKind.Bash => new ShellLaunch("bash", ["-lc", resolvedCommand]),
                ShellKind.Custom => BuildCustom(customShellPath, customShellArgsTemplate, resolvedCommand),
                _ => throw new ArgumentOutOfRangeException(nameof(shell), shell, "Unknown shell kind."),
            };
        }

        /// <summary>
        /// A short, human-readable description of the shell + launcher, shown in the
        /// dry-run preview so the user sees exactly what will execute the command.
        /// </summary>
        public static string DescribeShell(
            ShellKind shell,
            string? customShellPath,
            string? customShellArgsTemplate)
        {
            return shell switch
            {
                ShellKind.Cmd => "Command Prompt (cmd /c)",
                ShellKind.PowerShell => "Windows PowerShell (powershell -Command)",
                ShellKind.PwshCore => "PowerShell (pwsh -Command)",
                ShellKind.Bash => "Bash (bash -lc)",
                ShellKind.Custom => DescribeCustom(customShellPath, customShellArgsTemplate),
                _ => shell.ToString(),
            };
        }

        private static ShellLaunch BuildCustom(
            string? customShellPath,
            string? customShellArgsTemplate,
            string resolvedCommand)
        {
            if (string.IsNullOrWhiteSpace(customShellPath))
            {
                throw new InvalidOperationException(
                    "A custom shell requires a shell path. Set it on the CLI before running.");
            }

            var template = customShellArgsTemplate ?? string.Empty;
            var arguments = new List<string>();

            // Whitespace-separated tokens; a token of exactly "{command}" becomes the
            // resolved command as a single argument, while a token that merely contains
            // the placeholder has it substituted in place. Tokens with no placeholder
            // pass through verbatim. (Quoting inside the template is out of scope for v1.)
            foreach (var token in template.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (token == CommandToken)
                {
                    arguments.Add(resolvedCommand);
                }
                else if (token.Contains(CommandToken, StringComparison.Ordinal))
                {
                    arguments.Add(token.Replace(CommandToken, resolvedCommand, StringComparison.Ordinal));
                }
                else
                {
                    arguments.Add(token);
                }
            }

            // No placeholder anywhere: append the command as a trailing argument so it
            // still runs rather than being silently dropped.
            if (!template.Contains(CommandToken, StringComparison.Ordinal))
            {
                arguments.Add(resolvedCommand);
            }

            return new ShellLaunch(customShellPath, arguments);
        }

        private static string DescribeCustom(string? customShellPath, string? customShellArgsTemplate)
        {
            var path = string.IsNullOrWhiteSpace(customShellPath) ? "(unset shell path)" : customShellPath;
            return string.IsNullOrWhiteSpace(customShellArgsTemplate)
                ? path
                : $"{path} {customShellArgsTemplate}";
        }
    }
}
