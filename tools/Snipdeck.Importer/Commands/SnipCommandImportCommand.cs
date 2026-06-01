using Snipdeck.Core.Services;
using Snipdeck.Importer.Merge;
using Snipdeck.Importer.Output;
using Snipdeck.Importer.Sources;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Snipdeck.Importer.Commands
{
    /// <summary>
    /// <c>snipdeck-importer snipcommand &lt;path&gt;</c> — imports a SnipCommand export.
    /// Previews by default; <c>--write</c> backs the store up, merges, and saves.
    /// </summary>
    public sealed class SnipCommandImportCommand : AsyncCommand<ImportCommandSettings>
    {
        protected override async Task<int> ExecuteAsync(
            CommandContext context,
            ImportCommandSettings settings,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var source = new SnipCommandSource();
            IReadOnlyList<SnippetCandidate> candidates;
            try
            {
                candidates = source.Read(settings.Path);
            }
            catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException or UnauthorizedAccessException or IOException)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]error:[/] {ex.Message}");
                return 1;
            }

            var targets = await ResolveTargetsAsync(settings.Store).ConfigureAwait(false);

            var store = new JsonSnipStore(targets.StorePath);
            var document = await store.LoadAsync(cancellationToken).ConfigureAwait(false);

            var options = new MergeOptions(
                settings.Cli,
                settings.Into,
                settings.AllowDuplicates,
                ShareParameters: !settings.NoShareParameters);
            var plan = StoreMerger.Plan(document, candidates, options);

            DryRunRenderer.Render(source.DisplayName, targets.StorePath, plan, settings.Write);

            if (!settings.Write)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]This was a dry-run. Re-run with [bold]--write[/] to apply.[/]");
                return 0;
            }

            if (plan.ImportCount == 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("Nothing to import. The store was not modified.");
                return 0;
            }

            var backup = new BackupService(targets.StorePath, targets.BackupDirectory, new SystemClock(), targets.Retention);
            var backupInfo = await backup.CreateBackupAsync(cancellationToken).ConfigureAwait(false);

            StoreMerger.Apply(document, plan);
            await store.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLineInterpolated(
                $"[green]Imported {plan.ImportCount} snip(s)[/] into {targets.StorePath}.");
            if (backupInfo is not null)
            {
                AnsiConsole.MarkupLineInterpolated($"[grey]Backed up the previous store to {backupInfo.FilePath}[/]");
            }

            AnsiConsole.MarkupLine("[yellow]If Snipdeck is running, restart it to see the imported Snips.[/]");
            return 0;
        }

        private static async Task<ImportTargets> ResolveTargetsAsync(string? explicitStore)
        {
            // An explicit --store keeps its backups beside it and uses the default retention,
            // so importing into a throwaway store never touches the desktop app's backup folder.
            if (!string.IsNullOrWhiteSpace(explicitStore))
            {
                var storePath = Path.GetFullPath(explicitStore);
                var directory = Path.GetDirectoryName(storePath) ?? Directory.GetCurrentDirectory();
                var backupDirectory = Path.Combine(directory, DefaultPaths.BackupsDirectoryName);
                return new ImportTargets(storePath, backupDirectory, BackupService.DefaultRetention);
            }

            // Otherwise mirror exactly what the desktop app resolves from its config.
            var settingsStore = new JsonSettingsStore(DefaultPaths.SettingsFilePath);
            var config = await settingsStore.LoadAsync().ConfigureAwait(false);

            // Clamp like the desktop app's lazy retention provider does — a corrupt config
            // (e.g. BackupRetention = 0) must never crash the importer's fixed-retention BackupService.
            var retention = Math.Max(1, config.BackupRetention);
            return new ImportTargets(
                DefaultPaths.ResolveStoreFilePath(config.StoragePath),
                config.BackupDirectory ?? DefaultPaths.DefaultBackupDirectory,
                retention);
        }

        private sealed record ImportTargets(string StorePath, string BackupDirectory, int Retention);
    }
}
