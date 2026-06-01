using Snipdeck.Core.Models;
using Snipdeck.Importer.Sources;

namespace Snipdeck.Importer.Merge
{
    /// <summary>
    /// Plans and applies a merge of imported snippet candidates into an existing store document.
    /// <para>
    /// <see cref="Plan"/> is pure — it decides, per candidate, the target CLI and whether it is a
    /// duplicate to skip, and lists the CLIs that would be created. <see cref="Apply"/> mutates the
    /// document: it creates missing CLIs, mints fresh identifiers, and appends the imported snips.
    /// </para>
    /// </summary>
    public static class StoreMerger
    {
        public static MergePlan Plan(
            SnipStoreDocument target,
            IReadOnlyList<SnippetCandidate> candidates,
            MergeOptions options)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(options);

            // De-duplication is scoped to the CLI a snip lands in: CLI is Snipdeck's organising
            // axis, so the same (Title, CommandTemplate) under two different CLIs is legitimate.
            var cliNameById = target.Clis.ToDictionary(c => c.Id, c => c.Name);
            var existing = new HashSet<(string, string, string)>();
            foreach (var snip in target.Snips)
            {
                if (!snip.IsTrash)
                {
                    var owningCli = cliNameById.TryGetValue(snip.CliId, out var name) ? name : string.Empty;
                    _ = existing.Add(DedupeKey(owningCli, snip.Title, snip.CommandTemplate));
                }
            }

            var existingCliNames = new HashSet<string>(
                target.Clis.Select(c => c.Name),
                StringComparer.OrdinalIgnoreCase);

            var items = new List<MergePlanItem>(candidates.Count);
            var clisToCreate = new List<string>();
            var seenNewClis = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var plannedKeys = new HashSet<(string, string, string)>();

            foreach (var candidate in candidates)
            {
                var cliName = ResolveCliName(candidate, options);
                var key = DedupeKey(cliName, candidate.Snip.Title, candidate.Snip.CommandTemplate);

                var isDuplicate = !options.AllowDuplicates
                    && (existing.Contains(key) || plannedKeys.Contains(key));

                items.Add(new MergePlanItem(candidate, cliName, isDuplicate));

                if (isDuplicate)
                {
                    continue;
                }

                _ = plannedKeys.Add(key);

                if (!existingCliNames.Contains(cliName) && seenNewClis.Add(cliName))
                {
                    clisToCreate.Add(cliName);
                }
            }

            var sharePlans = options.ShareParameters
                ? BuildSharePlans(target, items)
                : new Dictionary<string, CliSharePlan>(StringComparer.OrdinalIgnoreCase);

            return new MergePlan(items, clisToCreate, sharePlans);
        }

        private static Dictionary<string, CliSharePlan> BuildSharePlans(
            SnipStoreDocument target,
            IReadOnlyList<MergePlanItem> items)
        {
            var existingClisByName = new Dictionary<string, Cli>(StringComparer.OrdinalIgnoreCase);
            foreach (var cli in target.Clis)
            {
                _ = existingClisByName.TryAdd(cli.Name, cli);
            }

            var plans = new Dictionary<string, CliSharePlan>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in items
                .Where(i => !i.IsDuplicateSkip)
                .GroupBy(i => i.TargetCliName, StringComparer.OrdinalIgnoreCase))
            {
                var existingParameters = existingClisByName.TryGetValue(group.Key, out var existingCli)
                    ? existingCli.Parameters
                    : (IReadOnlyList<Parameter>)[];
                var snips = group.Select(i => i.Candidate.Snip).ToList();

                var plan = ParameterSharer.Analyze(existingParameters, snips);
                if (!plan.IsEmpty)
                {
                    plans[group.Key] = plan;
                }
            }

            return plans;
        }

        public static void Apply(SnipStoreDocument target, MergePlan plan)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(plan);

            // Case-insensitive lookup of CLI name -> Cli, seeded with what's already there.
            var clisByName = new Dictionary<string, Cli>(StringComparer.OrdinalIgnoreCase);
            foreach (var cli in target.Clis)
            {
                _ = clisByName.TryAdd(cli.Name, cli);
            }

            foreach (var item in plan.Items)
            {
                if (item.IsDuplicateSkip)
                {
                    continue;
                }

                if (!clisByName.TryGetValue(item.TargetCliName, out var cli))
                {
                    cli = new Cli { Name = item.TargetCliName };
                    target.Clis.Add(cli);
                    clisByName[item.TargetCliName] = cli;
                }

                // The candidate already carries a fresh GUID (the source never reuses SnipCommand
                // ids); just point it at the resolved CLI and append.
                var snip = item.Candidate.Snip;
                snip.CliId = cli.Id;
                target.Snips.Add(snip);
            }

            // Promote duplicated parameters to CLI-shared parameters (after snips are attached,
            // so the shared definitions and any template-token rewrites land on the real objects).
            foreach (var (cliName, sharePlan) in plan.SharePlansByCli)
            {
                if (clisByName.TryGetValue(cliName, out var cli))
                {
                    ParameterSharer.Apply(cli, sharePlan);
                }
            }
        }

        private static (string, string, string) DedupeKey(string cliName, string title, string commandTemplate)
        {
            // CLI name matches case-insensitively (Cli reuse is case-insensitive); title/command are exact.
            return (cliName.ToLowerInvariant(), title, commandTemplate);
        }

        private static string ResolveCliName(SnippetCandidate candidate, MergeOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.ForceCli))
            {
                return options.ForceCli.Trim();
            }

            var confidentName = candidate.CliConfident && !string.IsNullOrWhiteSpace(candidate.SuggestedCliName)
                ? candidate.SuggestedCliName
                : null;
            var into = string.IsNullOrWhiteSpace(options.Into) ? null : options.Into.Trim();
            var bestEffort = string.IsNullOrWhiteSpace(candidate.SuggestedCliName) ? null : candidate.SuggestedCliName;

            // Precedence: a confident auto-suggestion, then the --into fallback, then any suggestion,
            // and finally a generic bucket so nothing is ever left without a home.
            return confidentName ?? into ?? bestEffort ?? "imported";
        }
    }

    /// <summary>Knobs that shape a merge, mapped from the CLI options.</summary>
    public sealed record MergeOptions(string? ForceCli, string? Into, bool AllowDuplicates, bool ShareParameters = false);

    /// <summary>One candidate's planned outcome.</summary>
    public sealed record MergePlanItem(SnippetCandidate Candidate, string TargetCliName, bool IsDuplicateSkip);

    /// <summary>
    /// The full plan: per-candidate decisions, the CLIs that would be created, and the
    /// parameter-sharing plan per target CLI (keyed by CLI name; empty when sharing is off).
    /// </summary>
    public sealed record MergePlan(
        IReadOnlyList<MergePlanItem> Items,
        IReadOnlyList<string> ClisToCreate,
        IReadOnlyDictionary<string, CliSharePlan> SharePlansByCli)
    {
        public int ImportCount => Items.Count(i => !i.IsDuplicateSkip);

        public int SkipCount => Items.Count(i => i.IsDuplicateSkip);

        public int SharedParameterCount => SharePlansByCli.Values.Sum(p => p.SharedToAdd.Count);
    }
}
