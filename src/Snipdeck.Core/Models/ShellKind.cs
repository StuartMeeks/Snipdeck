using System.Text.Json.Serialization;

namespace Snipdeck.Core.Models
{
    /// <summary>
    /// The shell a CLI's Snips are executed in. Encodes both the shell and its
    /// launcher; <see cref="Custom"/> defers to a user-supplied path and argument
    /// template. <c>Cmd</c> and <c>PowerShell</c> are Windows-only; <c>PwshCore</c>
    /// (pwsh) is cross-platform; <c>Bash</c> on Windows means a bash on PATH (e.g.
    /// WSL or Git Bash). The shell is only consulted when a Snip is run.
    /// </summary>
    public enum ShellKind
    {
        [JsonStringEnumMemberName("cmd")]
        Cmd = 0,

        [JsonStringEnumMemberName("powershell")]
        PowerShell = 1,

        [JsonStringEnumMemberName("pwshCore")]
        PwshCore = 2,

        [JsonStringEnumMemberName("bash")]
        Bash = 3,

        [JsonStringEnumMemberName("custom")]
        Custom = 4,
    }
}
