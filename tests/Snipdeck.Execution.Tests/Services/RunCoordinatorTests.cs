using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Execution.Services;
using Snipdeck.Execution.Tests.Support;
using Snipdeck.Execution.ViewModels;

namespace Snipdeck.Execution.Tests.Services
{
    public class RunCoordinatorTests
    {
        private static RunCoordinator Build(FakeShellInteractions interactions)
        {
            return new RunCoordinator(
                interactions,
                () => new FakeCommandRunner([]),
                new FakeCommandHistoryStore(),
                new InlineDispatcher(),
                new FakeClock(),
                new FakeClipboardService(),
                new AppConfig());
        }

        [Fact]
        public async Task CreateRunAsync_builds_a_run_view_model_on_confirmation()
        {
            var interactions = new FakeShellInteractions
            {
                NextRunFillResult = new ParameterFillResult("pl-app deploy --env prod"),
            };
            var coordinator = Build(interactions);
            var cli = new Cli { Name = "pl-app", Shell = ShellKind.PwshCore };
            var snip = new Snip { CliId = cli.Id, Title = "Deploy", CommandTemplate = "pl-app deploy --env {env}" };

            var result = await coordinator.CreateRunAsync(snip, cli, []);

            var vm = Assert.IsType<CommandRunViewModel>(result);
            Assert.Equal("pl-app deploy --env prod", vm.ResolvedCommand);
            Assert.Equal("PowerShell (pwsh -Command)", vm.ShellDisplay);
            Assert.Equal("PowerShell (pwsh -Command)", interactions.LastRunShellDisplay);
        }

        [Fact]
        public async Task CreateRunAsync_returns_null_when_preview_is_cancelled()
        {
            var interactions = new FakeShellInteractions { NextRunFillResult = null };
            var coordinator = Build(interactions);
            var snip = new Snip { Title = "Deploy", CommandTemplate = "x" };

            var result = await coordinator.CreateRunAsync(snip, new Cli(), []);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateRunAsync_notifies_and_returns_null_for_a_custom_shell_with_no_path()
        {
            var interactions = new FakeShellInteractions
            {
                NextRunFillResult = new ParameterFillResult("x"),
            };
            var coordinator = Build(interactions);
            var cli = new Cli { Shell = ShellKind.Custom, CustomShellPath = null };
            var snip = new Snip { CliId = cli.Id, Title = "Deploy", CommandTemplate = "x" };

            var result = await coordinator.CreateRunAsync(snip, cli, []);

            Assert.Null(result);
            Assert.Equal(1, interactions.NotifyCount);
        }

        [Fact]
        public async Task CreateRunAsync_honours_the_snip_shell_override()
        {
            var interactions = new FakeShellInteractions
            {
                NextRunFillResult = new ParameterFillResult("ls -la"),
            };
            var coordinator = Build(interactions);
            var cli = new Cli { Shell = ShellKind.PowerShell };
            var snip = new Snip { CliId = cli.Id, Title = "List", CommandTemplate = "ls -la", ShellOverride = ShellKind.Bash };

            var result = await coordinator.CreateRunAsync(snip, cli, []);

            var vm = Assert.IsType<CommandRunViewModel>(result);
            Assert.Equal("Bash (bash -lc)", vm.ShellDisplay);
        }
    }
}
