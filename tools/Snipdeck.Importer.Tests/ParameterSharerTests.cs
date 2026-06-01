using Snipdeck.Core.Models;
using Snipdeck.Importer.Merge;

namespace Snipdeck.Importer.Tests
{
    public class ParameterSharerTests
    {
        private static Snip TextSnip(string template, params (string name, string? def)[] ps)
        {
            return new Snip
            {
                Title = template,
                CommandTemplate = template,
                Parameters = [.. ps.Select(p => new Parameter { Name = p.name, Type = ParameterType.Text, Default = p.def })],
            };
        }

        private static Parameter Choice(string name, string? def, params string[] options)
        {
            return new Parameter { Name = name, Type = ParameterType.Choice, Options = [.. options], Default = def };
        }

        private static void AnalyzeAndApply(Cli cli, params Snip[] snips)
        {
            var plan = ParameterSharer.Analyze(cli.Parameters, snips);
            ParameterSharer.Apply(cli, plan);
        }

        [Fact]
        public void A_text_parameter_used_by_two_snips_is_promoted_and_stripped_from_the_snips()
        {
            var cli = new Cli { Name = "mpt-app" };
            var a = TextSnip("mpt-app x {id}", ("id", "1005"));
            var b = TextSnip("mpt-app y {id}", ("id", "1005"));

            AnalyzeAndApply(cli, a, b);

            var shared = Assert.Single(cli.Parameters);
            Assert.Equal("id", shared.Name);
            Assert.Equal(ParameterType.Text, shared.Type);
            Assert.Equal("1005", shared.Default);
            Assert.Empty(a.Parameters);
            Assert.Empty(b.Parameters);
            // Templates are unchanged for text (the token already matches the shared name).
            Assert.Equal("mpt-app x {id}", a.CommandTemplate);
        }

        [Fact]
        public void A_single_use_parameter_is_left_local()
        {
            var cli = new Cli { Name = "mpt-app" };
            var a = TextSnip("mpt-app x {only}", ("only", "v"));

            AnalyzeAndApply(cli, a);

            Assert.Empty(cli.Parameters);
            Assert.Single(a.Parameters);
        }

        [Fact]
        public void Text_promotion_uses_the_most_common_default_even_when_empty()
        {
            var cli = new Cli { Name = "mpt-app" };
            // Two snips with empty (null) default, one with "X" -> empty wins.
            var a = TextSnip("a {agreement}", ("agreement", null));
            var b = TextSnip("b {agreement}", ("agreement", null));
            var c = TextSnip("c {agreement}", ("agreement", "X"));

            AnalyzeAndApply(cli, a, b, c);

            var shared = Assert.Single(cli.Parameters);
            Assert.Equal("agreement", shared.Name);
            Assert.Null(shared.Default);
        }

        [Fact]
        public void Choice_parameters_match_by_option_set_regardless_of_order()
        {
            var cli = new Cli { Name = "mpt-app" };
            var a = new Snip { Title = "a", CommandTemplate = "a {authId}", Parameters = [Choice("authId", "x", "x", "y", "z")] };
            var b = new Snip { Title = "b", CommandTemplate = "b {authId}", Parameters = [Choice("authId", "z", "z", "y", "x")] };

            AnalyzeAndApply(cli, a, b);

            var shared = Assert.Single(cli.Parameters);
            Assert.Equal("authId", shared.Name);
            Assert.Equal(ParameterType.Choice, shared.Type);
            Assert.Equal(["x", "y", "z"], shared.Options.OrderBy(o => o, StringComparer.Ordinal).ToList());
            Assert.Empty(a.Parameters);
            Assert.Empty(b.Parameters);
        }

        [Fact]
        public void Choice_promotion_picks_the_most_common_name_and_rewrites_other_template_tokens()
        {
            var cli = new Cli { Name = "mpt-app" };
            // Same option set, two snips name it "authId", one names it "auth" -> "authId" wins.
            var a = new Snip { Title = "a", CommandTemplate = "a {authId}", Parameters = [Choice("authId", "p", "p", "q")] };
            var b = new Snip { Title = "b", CommandTemplate = "b {authId}", Parameters = [Choice("authId", "p", "p", "q")] };
            var c = new Snip { Title = "c", CommandTemplate = "c {auth}", Parameters = [Choice("auth", "q", "q", "p")] };

            AnalyzeAndApply(cli, a, b, c);

            var shared = Assert.Single(cli.Parameters);
            Assert.Equal("authId", shared.Name);
            // The odd-named snip's template token was rewritten to the winning name.
            Assert.Equal("c {authId}", c.CommandTemplate);
            Assert.Empty(c.Parameters);
        }

        [Fact]
        public void Existing_compatible_shared_choice_is_reused_not_duplicated()
        {
            var cli = new Cli { Name = "mpt-app", Parameters = [Choice("authId", "x", "x", "y")] };
            var a = new Snip { Title = "a", CommandTemplate = "a {authId}", Parameters = [Choice("authId", "y", "y", "x")] };
            var b = new Snip { Title = "b", CommandTemplate = "b {authId}", Parameters = [Choice("authId", "x", "x", "y")] };

            AnalyzeAndApply(cli, a, b);

            // No new shared param added; the locals are stripped to inherit the existing one.
            Assert.Single(cli.Parameters);
            Assert.Empty(a.Parameters);
            Assert.Empty(b.Parameters);
        }

        [Fact]
        public void Existing_incompatible_name_leaves_parameters_local()
        {
            // CLI already has a Text "authId"; imported choices named "authId" must not be merged into it.
            var cli = new Cli { Name = "mpt-app", Parameters = [new Parameter { Name = "authId", Type = ParameterType.Text }] };
            var a = new Snip { Title = "a", CommandTemplate = "a {authId}", Parameters = [Choice("authId", "x", "x", "y")] };
            var b = new Snip { Title = "b", CommandTemplate = "b {authId}", Parameters = [Choice("authId", "x", "x", "y")] };

            AnalyzeAndApply(cli, a, b);

            Assert.Single(cli.Parameters); // unchanged
            Assert.Single(a.Parameters);   // left local
            Assert.Single(b.Parameters);
        }

        [Fact]
        public void Two_distinct_same_option_choices_in_one_snip_are_not_collapsed()
        {
            // {source} and {dest} happen to share an option set but are different arguments.
            var cli = new Cli { Name = "cp" };
            var a = new Snip { Title = "a", CommandTemplate = "cp {source} {dest}", Parameters = [Choice("source", "x", "x", "y"), Choice("dest", "x", "x", "y")] };
            var b = new Snip { Title = "b", CommandTemplate = "cp {source} {dest}", Parameters = [Choice("source", "x", "x", "y"), Choice("dest", "x", "x", "y")] };

            AnalyzeAndApply(cli, a, b);

            // The two tokens must remain distinct, not merged into "{source} {source}".
            Assert.Equal("cp {source} {dest}", a.CommandTemplate);
            Assert.Equal("cp {source} {dest}", b.CommandTemplate);
            // "source" is promoted; "dest" stays local on each snip.
            Assert.Single(cli.Parameters);
            Assert.Equal("source", cli.Parameters[0].Name);
            Assert.Equal("dest", Assert.Single(a.Parameters).Name);
            Assert.Equal("dest", Assert.Single(b.Parameters).Name);
        }

        [Fact]
        public void A_choice_and_a_text_param_with_the_same_name_do_not_both_promote()
        {
            var cli = new Cli { Name = "tool" };
            // Choice "fmt" used by 2 snips; Text "fmt" used by 2 other snips.
            var c1 = new Snip { Title = "c1", CommandTemplate = "c1 {fmt}", Parameters = [Choice("fmt", "json", "json", "yaml")] };
            var c2 = new Snip { Title = "c2", CommandTemplate = "c2 {fmt}", Parameters = [Choice("fmt", "json", "json", "yaml")] };
            var t1 = TextSnip("t1 {fmt}", ("fmt", "raw"));
            var t2 = TextSnip("t2 {fmt}", ("fmt", "raw"));

            AnalyzeAndApply(cli, c1, c2, t1, t2);

            // Exactly one CLI param named "fmt" (the Choice, promoted first); no duplicate name.
            var shared = Assert.Single(cli.Parameters);
            Assert.Equal("fmt", shared.Name);
            Assert.Equal(ParameterType.Choice, shared.Type);
            // The text snips keep their local "fmt" rather than clashing with the choice.
            Assert.Single(t1.Parameters);
            Assert.Single(t2.Parameters);
        }

        [Fact]
        public void Analyze_does_not_mutate_until_apply()
        {
            var cli = new Cli { Name = "mpt-app" };
            var a = TextSnip("a {id}", ("id", "1"));
            var b = TextSnip("b {id}", ("id", "1"));

            var plan = ParameterSharer.Analyze(cli.Parameters, [a, b]);

            // Pure analysis: nothing changed yet.
            Assert.Empty(cli.Parameters);
            Assert.Single(a.Parameters);
            Assert.False(plan.IsEmpty);
        }
    }
}
