using Snipdeck.Importer.Merge;

using Spectre.Console;

namespace Snipdeck.Importer.Output
{
    /// <summary>
    /// Renders a merge plan as Spectre tables grouped by target CLI. All cell content comes from
    /// untrusted input, so every value is escaped before rendering.
    /// </summary>
    public static class DryRunRenderer
    {
        public static void Render(string sourceName, string storePath, MergePlan plan, bool willWrite)
        {
            var mode = willWrite ? "[yellow]WRITE[/]" : "[green]DRY-RUN[/]";
            AnsiConsole.MarkupLineInterpolated(
                $"Importing from {sourceName} into {storePath}");
            AnsiConsole.MarkupLine($"Mode: {mode}");
            AnsiConsole.WriteLine();

            var byCli = plan.Items
                .GroupBy(i => i.TargetCliName, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            var newClis = new HashSet<string>(plan.ClisToCreate, StringComparer.OrdinalIgnoreCase);

            foreach (var group in byCli)
            {
                var heading = newClis.Contains(group.Key)
                    ? $"{Escape(group.Key)} [grey](new CLI)[/]"
                    : Escape(group.Key);

                var table = new Table()
                    .Title(heading)
                    .Border(TableBorder.Rounded)
                    .AddColumn("Title")
                    .AddColumn("Command template")
                    .AddColumn("Params")
                    .AddColumn("Status");

                foreach (var item in group)
                {
                    var status = item.IsDuplicateSkip
                        ? "[grey]skip (duplicate)[/]"
                        : "[green]import[/]";
                    var lowConfidence = !item.Candidate.CliConfident && !item.IsDuplicateSkip
                        ? " [yellow](CLI guessed)[/]"
                        : string.Empty;

                    _ = table.AddRow(
                        Escape(item.Candidate.Snip.Title),
                        Escape(Truncate(item.Candidate.Snip.CommandTemplate, 70)),
                        item.Candidate.Snip.Parameters.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        status + lowConfidence);
                }

                AnsiConsole.Write(table);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLineInterpolated(
                $"CLIs to create: {plan.ClisToCreate.Count}    Snips to import: {plan.ImportCount}    Skipped (duplicates): {plan.SkipCount}");

            RenderSharedParameters(plan);
        }

        private static void RenderSharedParameters(MergePlan plan)
        {
            if (plan.SharePlansByCli.Count == 0)
            {
                return;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLineInterpolated(
                $"Shared parameters (scoped to their CLI): {plan.SharedParameterCount}");
            foreach (var (cliName, share) in plan.SharePlansByCli
                .Where(kvp => kvp.Value.SharedToAdd.Count > 0)
                .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                var names = string.Join(", ", share.SharedToAdd.Select(p => p.Name));
                AnsiConsole.MarkupLineInterpolated($"  {cliName}: {names}");
            }
        }

        private static string Escape(string value)
        {
            return Markup.Escape(value ?? string.Empty);
        }

        private static string Truncate(string value, int max)
        {
            return string.IsNullOrEmpty(value) || value.Length <= max
                ? value ?? string.Empty
                : value[..(max - 1)] + "…";
        }
    }
}
