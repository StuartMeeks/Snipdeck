using Snipdeck.Core.Models;
using Snipdeck.Execution.Engine;

namespace Snipdeck.Execution.Tests.Engine
{
    public class ShellCommandBuilderTests
    {
        [Fact]
        public void Cmd_wraps_with_slash_c()
        {
            var launch = ShellCommandBuilder.Build(ShellKind.Cmd, null, null, "pl-app deploy");
            Assert.Equal("cmd.exe", launch.FileName);
            Assert.Equal(["/c", "pl-app deploy"], launch.Arguments);
        }

        [Fact]
        public void PowerShell_uses_nologo_noprofile_command()
        {
            var launch = ShellCommandBuilder.Build(ShellKind.PowerShell, null, null, "pl-app deploy");
            Assert.Equal("powershell.exe", launch.FileName);
            Assert.Equal(["-NoLogo", "-NoProfile", "-Command", "pl-app deploy"], launch.Arguments);
        }

        [Fact]
        public void PwshCore_uses_pwsh()
        {
            var launch = ShellCommandBuilder.Build(ShellKind.PwshCore, null, null, "pl-app deploy");
            Assert.Equal("pwsh", launch.FileName);
            Assert.Equal(["-NoLogo", "-NoProfile", "-Command", "pl-app deploy"], launch.Arguments);
        }

        [Fact]
        public void Bash_uses_login_command()
        {
            var launch = ShellCommandBuilder.Build(ShellKind.Bash, null, null, "ls -la");
            Assert.Equal("bash", launch.FileName);
            Assert.Equal(["-lc", "ls -la"], launch.Arguments);
        }

        [Fact]
        public void Custom_substitutes_command_token_as_a_single_argument()
        {
            var launch = ShellCommandBuilder.Build(ShellKind.Custom, @"C:\tools\nu.exe", "-c {command}", "echo hi there");
            Assert.Equal(@"C:\tools\nu.exe", launch.FileName);
            Assert.Equal(["-c", "echo hi there"], launch.Arguments);
        }

        [Fact]
        public void Custom_substitutes_token_embedded_in_an_argument()
        {
            var launch = ShellCommandBuilder.Build(ShellKind.Custom, "sh", "--run={command}", "do thing");
            Assert.Equal(["--run=do thing"], launch.Arguments);
        }

        [Fact]
        public void Custom_appends_command_when_template_has_no_token()
        {
            var launch = ShellCommandBuilder.Build(ShellKind.Custom, "sh", "-x", "do thing");
            Assert.Equal(["-x", "do thing"], launch.Arguments);
        }

        [Fact]
        public void Custom_with_empty_template_passes_command_as_sole_argument()
        {
            var launch = ShellCommandBuilder.Build(ShellKind.Custom, "sh", null, "do thing");
            Assert.Equal(["do thing"], launch.Arguments);
        }

        [Fact]
        public void Custom_without_a_shell_path_throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => ShellCommandBuilder.Build(ShellKind.Custom, null, "{command}", "x"));
        }

        [Theory]
        [InlineData(ShellKind.Cmd, "Command Prompt (cmd /c)")]
        [InlineData(ShellKind.PowerShell, "Windows PowerShell (powershell -Command)")]
        [InlineData(ShellKind.PwshCore, "PowerShell (pwsh -Command)")]
        [InlineData(ShellKind.Bash, "Bash (bash -lc)")]
        public void DescribeShell_returns_a_friendly_label(ShellKind shell, string expected)
        {
            Assert.Equal(expected, ShellCommandBuilder.DescribeShell(shell, null, null));
        }

        [Fact]
        public void DescribeShell_for_custom_includes_path_and_template()
        {
            Assert.Equal(@"C:\tools\nu.exe -c {command}",
                ShellCommandBuilder.DescribeShell(ShellKind.Custom, @"C:\tools\nu.exe", "-c {command}"));
        }
    }
}
