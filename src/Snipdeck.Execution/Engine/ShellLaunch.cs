namespace Snipdeck.Execution.Engine
{
    /// <summary>
    /// The concrete process to spawn for a shell: the executable file name and the
    /// ordered argument list. Produced by <see cref="ShellCommandBuilder"/>; the
    /// command runner is responsible for any platform-specific command-line quoting.
    /// </summary>
    /// <param name="FileName">The shell executable (e.g. <c>powershell.exe</c>, <c>pwsh</c>).</param>
    /// <param name="Arguments">The launcher arguments, with the resolved command as a single trailing argument.</param>
    public sealed record ShellLaunch(string FileName, IReadOnlyList<string> Arguments);
}
