using Snipdeck.Core.Models;
using Snipdeck.Importer.Merge;
using Snipdeck.Importer.Sources;

namespace Snipdeck.Importer.Tests
{
    public class StoreMergerTests
    {
        private static readonly MergeOptions _defaults = new(ForceCli: null, Into: null, AllowDuplicates: false);

        private static SnippetCandidate Candidate(string cli, string title, string template, bool confident = true)
        {
            return new SnippetCandidate(cli, confident, new Snip { Title = title, CommandTemplate = template });
        }

        [Fact]
        public void New_cli_is_created_on_demand_and_snips_are_attached_to_it()
        {
            var doc = new SnipStoreDocument();
            var plan = StoreMerger.Plan(doc, [Candidate("mpt-app", "Delete", "mpt-app orders delete")], _defaults);

            Assert.Equal(["mpt-app"], plan.ClisToCreate);
            Assert.Equal(1, plan.ImportCount);

            StoreMerger.Apply(doc, plan);

            var cli = Assert.Single(doc.Clis);
            Assert.Equal("mpt-app", cli.Name);
            Assert.NotEqual(Guid.Empty, cli.Id);
            Assert.Equal(cli.Id, Assert.Single(doc.Snips).CliId);
        }

        [Fact]
        public void Existing_cli_is_reused_case_insensitively()
        {
            var existing = new Cli { Name = "MPT-App" };
            var doc = new SnipStoreDocument { Clis = { existing } };

            var plan = StoreMerger.Plan(doc, [Candidate("mpt-app", "Delete", "mpt-app orders delete")], _defaults);
            Assert.Empty(plan.ClisToCreate);

            StoreMerger.Apply(doc, plan);

            Assert.Single(doc.Clis);
            Assert.Equal(existing.Id, Assert.Single(doc.Snips).CliId);
        }

        [Fact]
        public void Multiple_snips_for_the_same_new_cli_create_it_once()
        {
            var doc = new SnipStoreDocument();
            var plan = StoreMerger.Plan(
                doc,
                [
                    Candidate("mpt-app", "A", "mpt-app a"),
                    Candidate("mpt-app", "B", "mpt-app b"),
                ],
                _defaults);

            Assert.Equal(["mpt-app"], plan.ClisToCreate);

            StoreMerger.Apply(doc, plan);

            Assert.Single(doc.Clis);
            Assert.Equal(2, doc.Snips.Count);
            Assert.All(doc.Snips, s => Assert.Equal(doc.Clis[0].Id, s.CliId));
        }

        [Fact]
        public void Duplicate_against_existing_store_is_skipped_by_default()
        {
            var cli = new Cli { Name = "mpt-app" };
            var doc = new SnipStoreDocument
            {
                Clis = { cli },
                Snips = { new Snip { CliId = cli.Id, Title = "Delete", CommandTemplate = "mpt-app orders delete" } },
            };

            var plan = StoreMerger.Plan(doc, [Candidate("mpt-app", "Delete", "mpt-app orders delete")], _defaults);

            Assert.Equal(0, plan.ImportCount);
            Assert.Equal(1, plan.SkipCount);
            Assert.Empty(plan.ClisToCreate);

            StoreMerger.Apply(doc, plan);
            Assert.Single(doc.Snips);
        }

        [Fact]
        public void A_trashed_existing_snip_does_not_block_the_import()
        {
            var cli = new Cli { Name = "mpt-app" };
            var doc = new SnipStoreDocument
            {
                Clis = { cli },
                Snips = { new Snip { CliId = cli.Id, Title = "Delete", CommandTemplate = "mpt-app orders delete", IsTrash = true } },
            };

            var plan = StoreMerger.Plan(doc, [Candidate("mpt-app", "Delete", "mpt-app orders delete")], _defaults);
            Assert.Equal(1, plan.ImportCount);
        }

        [Fact]
        public void Same_title_and_command_under_a_different_cli_is_not_a_duplicate()
        {
            var other = new Cli { Name = "other-app" };
            var doc = new SnipStoreDocument
            {
                Clis = { other },
                Snips = { new Snip { CliId = other.Id, Title = "Delete", CommandTemplate = "mpt-app orders delete" } },
            };

            // Same title+command, but it auto-suggests CLI "mpt-app" — a different CLI from the existing snip.
            var plan = StoreMerger.Plan(doc, [Candidate("mpt-app", "Delete", "mpt-app orders delete")], _defaults);

            Assert.Equal(1, plan.ImportCount);
            Assert.Equal(["mpt-app"], plan.ClisToCreate);
        }

        [Fact]
        public void AllowDuplicates_imports_even_when_a_match_exists()
        {
            var cli = new Cli { Name = "mpt-app" };
            var doc = new SnipStoreDocument
            {
                Clis = { cli },
                Snips = { new Snip { CliId = cli.Id, Title = "Delete", CommandTemplate = "mpt-app orders delete" } },
            };

            var options = _defaults with { AllowDuplicates = true };
            var plan = StoreMerger.Plan(doc, [Candidate("mpt-app", "Delete", "mpt-app orders delete")], options);

            Assert.Equal(1, plan.ImportCount);
            StoreMerger.Apply(doc, plan);
            Assert.Equal(2, doc.Snips.Count);
        }

        [Fact]
        public void Identical_candidates_within_one_batch_are_deduped()
        {
            var doc = new SnipStoreDocument();
            var plan = StoreMerger.Plan(
                doc,
                [
                    Candidate("mpt-app", "Delete", "mpt-app orders delete"),
                    Candidate("mpt-app", "Delete", "mpt-app orders delete"),
                ],
                _defaults);

            Assert.Equal(1, plan.ImportCount);
            Assert.Equal(1, plan.SkipCount);
        }

        [Fact]
        public void ForceCli_overrides_the_suggested_cli_for_every_candidate()
        {
            var doc = new SnipStoreDocument();
            var options = _defaults with { ForceCli = "bucket" };
            var plan = StoreMerger.Plan(
                doc,
                [
                    Candidate("mpt-app", "A", "mpt-app a"),
                    Candidate("inv-app", "B", "inv-app b"),
                ],
                options);

            Assert.All(plan.Items, i => Assert.Equal("bucket", i.TargetCliName));
            Assert.Equal(["bucket"], plan.ClisToCreate);
        }

        [Fact]
        public void Into_is_used_only_for_unconfident_suggestions()
        {
            var doc = new SnipStoreDocument();
            var options = _defaults with { Into = "fallback" };
            var plan = StoreMerger.Plan(
                doc,
                [
                    Candidate("mpt-app", "A", "mpt-app a", confident: true),
                    new SnippetCandidate(string.Empty, false, new Snip { Title = "B", CommandTemplate = "{x} b" }),
                ],
                options);

            Assert.Equal("mpt-app", plan.Items[0].TargetCliName);
            Assert.Equal("fallback", plan.Items[1].TargetCliName);
        }

        [Fact]
        public void ShareParameters_promotes_duplicated_params_to_the_created_cli_and_strips_the_snips()
        {
            var doc = new SnipStoreDocument();
            var a = new Snip
            {
                Title = "A",
                CommandTemplate = "mpt-app a {authId}",
                Parameters = [new Parameter { Name = "authId", Type = ParameterType.Choice, Options = ["x", "y"], Default = "x" }],
            };
            var b = new Snip
            {
                Title = "B",
                CommandTemplate = "mpt-app b {authId}",
                Parameters = [new Parameter { Name = "authId", Type = ParameterType.Choice, Options = ["y", "x"], Default = "y" }],
            };
            var options = _defaults with { ShareParameters = true };

            var plan = StoreMerger.Plan(
                doc,
                [new SnippetCandidate("mpt-app", true, a), new SnippetCandidate("mpt-app", true, b)],
                options);

            Assert.Equal(1, plan.SharedParameterCount);

            StoreMerger.Apply(doc, plan);

            var cli = Assert.Single(doc.Clis);
            var shared = Assert.Single(cli.Parameters);
            Assert.Equal("authId", shared.Name);
            // Both snips now inherit the shared parameter instead of carrying their own.
            Assert.All(doc.Snips, s => Assert.Empty(s.Parameters));
        }

        [Fact]
        public void Swapped_same_option_choices_are_not_treated_as_duplicates()
        {
            // Two distinct choices that happen to share an option set must keep positional identity:
            // "cp {source} {dest}" and "cp {dest} {source}" are different commands, not duplicates.
            var doc = new SnipStoreDocument();
            static Snip Swapped(string a, string b)
            {
                return new Snip
                {
                    Title = "Copy",
                    CommandTemplate = $"cp {{{a}}} {{{b}}}",
                    Parameters =
                    [
                        new Parameter { Name = a, Type = ParameterType.Choice, Options = ["x", "y"], Default = "x" },
                        new Parameter { Name = b, Type = ParameterType.Choice, Options = ["x", "y"], Default = "x" },
                    ],
                };
            }
            var options = _defaults with { ShareParameters = true };

            var plan = StoreMerger.Plan(
                doc,
                [new SnippetCandidate("cp", true, Swapped("source", "dest")), new SnippetCandidate("cp", true, Swapped("dest", "source"))],
                options);

            // Both are imported — neither is wrongly collapsed into the other.
            Assert.Equal(2, plan.ImportCount);
        }

        [Fact]
        public void A_skipped_duplicate_does_not_rename_or_drop_a_genuinely_importable_snip()
        {
            // Existing store snip uses {authId} (with its choice param).
            var cli = new Cli { Name = "x" };
            var doc = new SnipStoreDocument
            {
                Clis = { cli },
                Snips =
                {
                    new Snip
                    {
                        CliId = cli.Id,
                        Title = "Existing",
                        CommandTemplate = "x run {authId}",
                        Parameters = [new Parameter { Name = "authId", Type = ParameterType.Choice, Options = ["a", "b"], Default = "a" }],
                    },
                },
            };

            // A genuinely-new snip uses {auth}; plus a re-import of the existing {authId} snip (a dup).
            var real = new SnippetCandidate("x", true, new Snip
            {
                Title = "Real",
                CommandTemplate = "x do {auth}",
                Parameters = [new Parameter { Name = "auth", Type = ParameterType.Choice, Options = ["a", "b"], Default = "a" }],
            });
            var dup = new SnippetCandidate("x", true, new Snip
            {
                Title = "Existing",
                CommandTemplate = "x run {authId}",
                Parameters = [new Parameter { Name = "authId", Type = ParameterType.Choice, Options = ["a", "b"], Default = "a" }],
            });
            var options = _defaults with { ShareParameters = true };

            var plan = StoreMerger.Plan(doc, [real, dup], options);
            StoreMerger.Apply(doc, plan);

            // The re-imported existing snip is skipped; "Real" is imported and NOT renamed to {authId}
            // (the skipped duplicate must not drive a rename), and stays local (no sharing group of 2).
            Assert.True(plan.Items.Single(i => ReferenceEquals(i.Candidate, dup)).IsDuplicateSkip);
            Assert.Equal("x do {auth}", real.Snip.CommandTemplate);
            Assert.Equal("auth", Assert.Single(real.Snip.Parameters).Name);
            Assert.Empty(cli.Parameters);
        }

        [Fact]
        public void A_duplicate_only_import_does_not_mutate_the_existing_clis_shared_parameters()
        {
            var cli = new Cli { Name = "x" };
            var doc = new SnipStoreDocument
            {
                Clis = { cli },
                Snips =
                {
                    new Snip { CliId = cli.Id, Title = "A", CommandTemplate = "x a {p}" },
                    new Snip { CliId = cli.Id, Title = "B", CommandTemplate = "x b {p}" },
                },
            };

            // Re-import the same two snips (exact duplicates), each carrying a {p} parameter.
            static SnippetCandidate Dup(string title, string template)
            {
                return new SnippetCandidate("x", true, new Snip
                {
                    Title = title,
                    CommandTemplate = template,
                    Parameters = [new Parameter { Name = "p", Type = ParameterType.Text, Default = "v" }],
                });
            }
            var options = _defaults with { ShareParameters = true };

            var plan = StoreMerger.Plan(doc, [Dup("A", "x a {p}"), Dup("B", "x b {p}")], options);

            Assert.Equal(0, plan.ImportCount);
            Assert.Equal(0, plan.SharedParameterCount);

            StoreMerger.Apply(doc, plan);

            // Nothing imported, and the existing CLI gained no shared parameters.
            Assert.Equal(2, doc.Snips.Count);
            Assert.Empty(cli.Parameters);
        }

        [Fact]
        public void Sharing_is_off_by_default_so_params_stay_local()
        {
            var doc = new SnipStoreDocument();
            var a = TextParamSnip("A", "mpt-app a {id}");
            var b = TextParamSnip("B", "mpt-app b {id}");

            var plan = StoreMerger.Plan(doc, [a, b], _defaults);
            StoreMerger.Apply(doc, plan);

            Assert.Empty(Assert.Single(doc.Clis).Parameters);
            Assert.All(doc.Snips, s => Assert.Single(s.Parameters));
        }

        private static SnippetCandidate TextParamSnip(string title, string template)
        {
            return new SnippetCandidate("mpt-app", true, new Snip
            {
                Title = title,
                CommandTemplate = template,
                Parameters = [new Parameter { Name = "id", Type = ParameterType.Text, Default = "1" }],
            });
        }

        [Fact]
        public void Unconfident_with_no_into_falls_back_to_a_generic_bucket()
        {
            var doc = new SnipStoreDocument();
            var plan = StoreMerger.Plan(
                doc,
                [new SnippetCandidate(string.Empty, false, new Snip { Title = "B", CommandTemplate = "{x} b" })],
                _defaults);

            Assert.Equal("imported", plan.Items[0].TargetCliName);
        }
    }
}
