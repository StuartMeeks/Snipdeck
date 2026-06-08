using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Execution.Abstractions;
using Snipdeck.Execution.Engine;
using Snipdeck.Execution.Models;
using Snipdeck.Execution.ViewModels;

namespace Snipdeck.Execution.Services
{
    /// <summary>
    /// Implements the Core <see cref="IRunCoordinator"/> hook: resolves the effective
    /// shell and working directory, validates the executable, shows the dry-run preview
    /// (the safety gate), builds the <see cref="CommandSpec"/>, and returns a
    /// <see cref="CommandRunViewModel"/> for the shell to display. The view is returned
    /// not-yet-started; the run begins once the terminal is ready.
    /// </summary>
    public sealed class RunCoordinator(
        IShellInteractions interactions,
        Func<ICommandRunner> runnerFactory,
        ICommandHistoryStore historyStore,
        IDispatcher dispatcher,
        IClock clock,
        IClipboardService clipboard,
        AppConfig config) : IRunCoordinator
    {
        /// <inheritdoc/>
        public async Task<object?> CreateRunAsync(Snip snip, Cli? cli, IReadOnlyList<Parameter> resolvedParameters)
        {
            ArgumentNullException.ThrowIfNull(snip);

            var shell = snip.ShellOverride ?? cli?.Shell ?? ShellKind.PowerShell;
            var customPath = cli?.CustomShellPath;
            var customArgs = cli?.CustomShellArgsTemplate;
            var executablePath = cli?.ExecutablePath;

            // Validation happens only at Run time; Copy is always unconditional.
            if (!string.IsNullOrWhiteSpace(executablePath) && !File.Exists(executablePath))
            {
                await interactions.NotifyAsync(
                    "Couldn't run this Snip",
                    $"Couldn't find \"{executablePath}\". Has it been installed? Edit the CLI to fix the path.")
                    .ConfigureAwait(false);
                return null;
            }

            if (shell == ShellKind.Custom && string.IsNullOrWhiteSpace(customPath))
            {
                await interactions.NotifyAsync(
                    "Couldn't run this Snip",
                    "This CLI uses a custom shell but no shell path is set. Edit the CLI to set it.")
                    .ConfigureAwait(false);
                return null;
            }

            var workingDirectory = ResolveWorkingDirectory(snip, cli, executablePath);
            var shellDisplay = ShellCommandBuilder.DescribeShell(shell, customPath, customArgs);

            var fill = await interactions
                .FillParametersForRunAsync(snip, resolvedParameters, shellDisplay, workingDirectory)
                .ConfigureAwait(false);
            if (fill is null)
            {
                return null; // cancelled at the dry-run preview
            }

            var launch = ShellCommandBuilder.Build(shell, customPath, customArgs, fill.ResolvedCommand);
            var spec = new CommandSpec(launch, workingDirectory, fill.ResolvedCommand, shellDisplay);

            return new CommandRunViewModel(
                snip.Title,
                snip.Id,
                cli?.Id ?? Guid.Empty,
                spec,
                runnerFactory(),
                historyStore,
                dispatcher,
                clock,
                clipboard,
                config.HistoryRetentionPerSnip,
                config.MaxCapturedOutputBytes);
        }

        private static string ResolveWorkingDirectory(Snip snip, Cli? cli, string? executablePath)
        {
            if (!string.IsNullOrWhiteSpace(snip.WorkingDirectoryOverride))
            {
                return snip.WorkingDirectoryOverride;
            }

            if (!string.IsNullOrWhiteSpace(cli?.WorkingDirectory))
            {
                return cli.WorkingDirectory;
            }

            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                var directory = Path.GetDirectoryName(executablePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    return directory;
                }
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
    }
}
