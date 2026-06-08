namespace Snipdeck.Core.Models
{
    public sealed class Cli
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        /// <summary>Short, free-text summary shown on the CLI's Home card.</summary>
        public string Description { get; set; } = string.Empty;

        public string? IconRef { get; set; }

        /// <summary>
        /// Parameter definitions shared by every Snip under this CLI. A Snip's
        /// <c>{token}</c> resolves to one of these by name when the Snip has no
        /// local parameter of that name. See <c>ParameterResolver</c>.
        /// </summary>
        public List<Parameter> Parameters { get; set; } = [];

        // --- Execution configuration (schema v5) ---
        // All optional. None are validated at save time — a CLI is useful as a
        // place to author Snips long before the tool is installed. The executable
        // path is checked only when a Snip is actually run.

        /// <summary>The shell this CLI's Snips run in. See <see cref="ShellKind"/>.</summary>
        public ShellKind Shell { get; set; } = ShellKind.PowerShell;

        /// <summary>Executable path for <see cref="ShellKind.Custom"/>.</summary>
        public string? CustomShellPath { get; set; }

        /// <summary>
        /// Argument template for <see cref="ShellKind.Custom"/>; the resolved command
        /// substitutes for the <c>{command}</c> token (e.g. <c>-NoLogo -Command {command}</c>).
        /// </summary>
        public string? CustomShellArgsTemplate { get; set; }

        /// <summary>
        /// Optional path to the CLI's own executable, surfaced when running. Not used
        /// by Copy. Validated only at Run time; an empty value is the default.
        /// </summary>
        public string? ExecutablePath { get; set; }

        /// <summary>
        /// Optional working directory for runs. Defaults at runtime to the executable's
        /// parent directory when <see cref="ExecutablePath"/> is set, otherwise the
        /// user's home directory. A Snip may override this.
        /// </summary>
        public string? WorkingDirectory { get; set; }
    }
}
