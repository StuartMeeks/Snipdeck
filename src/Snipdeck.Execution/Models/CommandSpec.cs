using Snipdeck.Execution.Engine;

namespace Snipdeck.Execution.Models
{
    /// <summary>
    /// Everything the command runner needs to spawn one run, plus the metadata the
    /// run view and history record display. <see cref="Launch"/> and
    /// <see cref="WorkingDirectory"/> drive the process; <see cref="ResolvedCommand"/>
    /// and <see cref="ShellDisplay"/> are shown to the user (dry-run preview, history).
    /// </summary>
    /// <param name="Launch">The executable + arguments to spawn (from <see cref="ShellCommandBuilder"/>).</param>
    /// <param name="WorkingDirectory">The resolved absolute working directory for the process.</param>
    /// <param name="ResolvedCommand">The user-facing command after token substitution.</param>
    /// <param name="ShellDisplay">A human-readable description of the shell + launcher.</param>
    public sealed record CommandSpec(
        ShellLaunch Launch,
        string WorkingDirectory,
        string ResolvedCommand,
        string ShellDisplay);
}
