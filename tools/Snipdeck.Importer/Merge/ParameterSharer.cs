using Snipdeck.Core.Models;

namespace Snipdeck.Importer.Merge
{
    /// <summary>
    /// De-duplicates the parameters of the snips imported into a single CLI and promotes the
    /// shared ones up to CLI-scoped parameters (<see cref="Cli.Parameters"/>), so the resolver
    /// inherits them by token name. Only parameters used by two or more snips are promoted;
    /// single-use parameters stay local.
    /// <para>
    /// Matching rules:
    /// <list type="bullet">
    /// <item><b>Choice</b> parameters match when their option <i>sets</i> are equal (order-independent);
    /// the name is not part of the match, and the most common name wins. Snips that used a different
    /// name have their template token rewritten to the winning name.</item>
    /// <item><b>Text</b> parameters match by name; the most common default value wins (including an
    /// empty/absent default).</item>
    /// </list>
    /// Ties are broken by first appearance. Analysis is pure; <see cref="Apply"/> performs the mutation.
    /// </para>
    /// </summary>
    public static class ParameterSharer
    {
        private const char _separator = '\u0001';

        /// <summary>
        /// Unifies choice-parameter names across the given snips: for each option set used by two
        /// or more snips, the most common name wins and each snip's template token and local
        /// parameter name are rewritten to it. This is normalisation only — it does not promote or
        /// strip anything — so that duplicate detection and later promotion agree on token names.
        /// Mutates the snips. Run it before de-duplication.
        /// </summary>
        public static void NormalizeChoiceNames(IReadOnlyList<Snip> snips)
        {
            ArgumentNullException.ThrowIfNull(snips);

            var occurrences = new List<Occurrence>();
            var order = 0;
            foreach (var snip in snips)
            {
                foreach (var parameter in snip.Parameters)
                {
                    occurrences.Add(new Occurrence(snip, parameter, order++));
                }
            }

            foreach (var group in occurrences
                .Where(o => o.Parameter.Type == ParameterType.Choice)
                .GroupBy(o => OptionSetKey(o.Parameter.Options)))
            {
                var members = group.ToList();
                if (DistinctSnipCount(members) < 2)
                {
                    continue;
                }

                var winningName = MostCommonName(members);

                // At most one occurrence per snip, preferring one already named the winner; never
                // rename onto a token the snip already uses (would merge two distinct arguments).
                foreach (var bySnip in members.GroupBy(m => m.Snip))
                {
                    var target = bySnip
                        .OrderByDescending(m => string.Equals(m.Parameter.Name, winningName, StringComparison.Ordinal))
                        .ThenBy(m => m.Order)
                        .First();

                    if (string.Equals(target.Parameter.Name, winningName, StringComparison.Ordinal)
                        || TemplateContainsToken(target.Snip.CommandTemplate, winningName))
                    {
                        continue;
                    }

                    target.Snip.CommandTemplate = target.Snip.CommandTemplate.Replace(
                        "{" + target.Parameter.Name + "}", "{" + winningName + "}", StringComparison.Ordinal);
                    target.Parameter.Name = winningName;
                }
            }
        }

        public static CliSharePlan Analyze(IReadOnlyList<Parameter> existingCliParameters, IReadOnlyList<Snip> snips)
        {
            ArgumentNullException.ThrowIfNull(existingCliParameters);
            ArgumentNullException.ThrowIfNull(snips);

            // Record every (snip, parameter) occurrence with a stable order for deterministic ties.
            var occurrences = new List<Occurrence>();
            var order = 0;
            foreach (var snip in snips)
            {
                foreach (var parameter in snip.Parameters)
                {
                    occurrences.Add(new Occurrence(snip, parameter, order++));
                }
            }

            var existingByName = new Dictionary<string, Parameter>(StringComparer.Ordinal);
            foreach (var p in existingCliParameters)
            {
                _ = existingByName.TryAdd(p.Name, p);
            }

            var sharedToAdd = new List<Parameter>();
            // Names already promoted this run, so a second group (e.g. a Text param sharing a
            // name with a promoted Choice) can't add a duplicate CLI-scoped name.
            var claimed = new HashSet<string>(StringComparer.Ordinal);
            // snip -> (local names to remove, token renames)
            var edits = new Dictionary<Snip, (List<string> Remove, Dictionary<string, string> Renames)>();

            void RecordEdit(Snip snip, string removeName, string? renameTo)
            {
                if (!edits.TryGetValue(snip, out var edit))
                {
                    edit = ([], new Dictionary<string, string>(StringComparer.Ordinal));
                    edits[snip] = edit;
                }

                edit.Remove.Add(removeName);
                if (renameTo is not null && !string.Equals(removeName, renameTo, StringComparison.Ordinal))
                {
                    edit.Renames[removeName] = renameTo;
                }
            }

            // --- Choice parameters: grouped by option-set (order-independent) ---
            foreach (var group in occurrences
                .Where(o => o.Parameter.Type == ParameterType.Choice)
                .GroupBy(o => OptionSetKey(o.Parameter.Options)))
            {
                var members = group.ToList();
                if (DistinctSnipCount(members) < 2)
                {
                    continue;
                }

                var winningName = MostCommonName(members);
                if (claimed.Contains(winningName))
                {
                    // Name already promoted by an earlier group — can't share it twice; leave local.
                    continue;
                }

                var options = MostCommonOptions(members);
                var chosenDefault = MostCommonDefault(members);

                // Choice matching is name-independent: if the CLI already shares a choice with the
                // same option set (under ANY name), this parameter already exists there — don't add a
                // duplicate. Reuse it only when fully compatible (same name AND default); otherwise,
                // or if the winning name is already taken by another param, leave the imported snips'
                // parameters local rather than duplicating or silently changing them.
                var existingEquivalent = existingCliParameters.FirstOrDefault(p =>
                    p.Type == ParameterType.Choice && SameOptionSet(p.Options, options));

                if (existingEquivalent is not null || existingByName.ContainsKey(winningName))
                {
                    var compatible = existingEquivalent is not null
                        && string.Equals(existingEquivalent.Name, winningName, StringComparison.Ordinal)
                        && string.Equals(existingEquivalent.Default, chosenDefault, StringComparison.Ordinal);
                    if (!compatible)
                    {
                        continue;
                    }
                }
                else
                {
                    sharedToAdd.Add(new Parameter
                    {
                        Name = winningName,
                        Type = ParameterType.Choice,
                        Options = [.. options],
                        Default = chosenDefault,
                    });
                }

                _ = claimed.Add(winningName);

                // Merge at most one parameter per snip — two distinct same-option choices in one
                // snip (e.g. {source} and {dest}) are different arguments and must not collapse
                // into a single token. Prefer the occurrence already named the winning name; skip
                // a rename that would collide with another token already in the snip's template.
                foreach (var bySnip in members.GroupBy(m => m.Snip))
                {
                    var target = bySnip
                        .OrderByDescending(m => string.Equals(m.Parameter.Name, winningName, StringComparison.Ordinal))
                        .ThenBy(m => m.Order)
                        .First();

                    if (!string.Equals(target.Parameter.Name, winningName, StringComparison.Ordinal)
                        && TemplateContainsToken(target.Snip.CommandTemplate, winningName))
                    {
                        continue;
                    }

                    RecordEdit(target.Snip, target.Parameter.Name, winningName);
                }
            }

            // --- Text parameters: grouped by name ---
            foreach (var group in occurrences
                .Where(o => o.Parameter.Type == ParameterType.Text)
                .GroupBy(o => o.Parameter.Name, StringComparer.Ordinal))
            {
                var members = group.ToList();
                if (DistinctSnipCount(members) < 2)
                {
                    continue;
                }

                var name = group.Key;
                if (claimed.Contains(name))
                {
                    // Name already promoted (e.g. as a Choice) — don't add a clashing CLI param.
                    continue;
                }

                var winningDefault = MostCommonDefault(members);

                if (existingByName.TryGetValue(name, out var existing))
                {
                    // Reuse only when the existing shared default matches; otherwise leave these
                    // local so imported snips keep their own default rather than silently adopting it.
                    if (existing.Type != ParameterType.Text
                        || !string.Equals(existing.Default, winningDefault, StringComparison.Ordinal))
                    {
                        continue;
                    }
                }
                else
                {
                    sharedToAdd.Add(new Parameter
                    {
                        Name = name,
                        Type = ParameterType.Text,
                        Default = winningDefault,
                    });
                }

                _ = claimed.Add(name);

                foreach (var member in members)
                {
                    RecordEdit(member.Snip, name, renameTo: null);
                }
            }

            var snipEdits = edits
                .Select(kvp => new SnipParameterEdit(kvp.Key, kvp.Value.Remove, kvp.Value.Renames))
                .ToList();
            return new CliSharePlan(sharedToAdd, snipEdits);
        }

        public static void Apply(Cli cli, CliSharePlan plan)
        {
            ArgumentNullException.ThrowIfNull(cli);
            ArgumentNullException.ThrowIfNull(plan);

            cli.Parameters.AddRange(plan.SharedToAdd);

            foreach (var edit in plan.SnipEdits)
            {
                foreach (var (oldName, newName) in edit.TokenRenames)
                {
                    edit.Snip.CommandTemplate = edit.Snip.CommandTemplate.Replace(
                        "{" + oldName + "}", "{" + newName + "}", StringComparison.Ordinal);
                }

                var remove = new HashSet<string>(edit.RemoveLocalNames, StringComparer.Ordinal);
                _ = edit.Snip.Parameters.RemoveAll(p => remove.Contains(p.Name));
            }
        }

        private static int DistinctSnipCount(IEnumerable<Occurrence> members)
        {
            return members.Select(m => m.Snip).Distinct().Count();
        }

        private static string OptionSetKey(IEnumerable<string> options)
        {
            return string.Join(_separator, options.Distinct(StringComparer.Ordinal).OrderBy(o => o, StringComparer.Ordinal));
        }

        private static bool SameOptionSet(IEnumerable<string> a, IEnumerable<string> b)
        {
            return new HashSet<string>(a, StringComparer.Ordinal).SetEquals(b);
        }

        private static string MostCommonName(IEnumerable<Occurrence> members)
        {
            return members
                .GroupBy(m => m.Parameter.Name, StringComparer.Ordinal)
                .Select(g => (g.Key, Count: g.Count(), First: g.Min(m => m.Order)))
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.First)
                .First().Key;
        }

        private static string? MostCommonDefault(IEnumerable<Occurrence> members)
        {
            return members
                .GroupBy(m => m.Parameter.Default)
                .Select(g => (g.Key, Count: g.Count(), First: g.Min(m => m.Order)))
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.First)
                .First().Key;
        }

        /// <summary>The most common option ordering among a choice group; ties broken by first appearance.</summary>
        private static List<string> MostCommonOptions(IEnumerable<Occurrence> members)
        {
            return members
                .GroupBy(m => string.Join(_separator, m.Parameter.Options))
                .Select(g =>
                {
                    var picked = g.OrderBy(m => m.Order).First();
                    return (Count: g.Count(), picked.Order, picked.Parameter.Options);
                })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Order)
                .First().Options;
        }

        private static bool TemplateContainsToken(string template, string name)
        {
            return template.Contains("{" + name + "}", StringComparison.Ordinal);
        }

        private readonly record struct Occurrence(Snip Snip, Parameter Parameter, int Order);
    }

    /// <summary>Parameters to add to a CLI plus the per-snip edits that promotion requires.</summary>
    public sealed record CliSharePlan(IReadOnlyList<Parameter> SharedToAdd, IReadOnlyList<SnipParameterEdit> SnipEdits)
    {
        public bool IsEmpty => SharedToAdd.Count == 0 && SnipEdits.Count == 0;
    }

    /// <summary>The local parameters to drop from a snip and the token renames its template needs.</summary>
    public sealed record SnipParameterEdit(
        Snip Snip,
        IReadOnlyList<string> RemoveLocalNames,
        IReadOnlyDictionary<string, string> TokenRenames);
}
