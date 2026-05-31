using Snipdeck.Core.Engine;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Tests.Engine
{
    public class ParameterResolverTests
    {
        private static Parameter P(string name, ParameterType type = ParameterType.Text, string? def = null) =>
            new() { Name = name, Type = type, Default = def };

        [Fact]
        public void Local_parameters_are_returned_as_is()
        {
            var snip = new Snip { CommandTemplate = "x {a}", Parameters = [P("a")] };

            var result = ParameterResolver.Resolve(snip, cli: null, globalParameters: null);

            var p = Assert.Single(result);
            Assert.Equal("a", p.Name);
        }

        [Fact]
        public void Template_token_inherits_from_the_cli_scope_when_not_defined_locally()
        {
            var snip = new Snip { CommandTemplate = "deploy {env}" };
            var cli = new Cli { Parameters = [P("env", ParameterType.Choice, "dev")] };

            var result = ParameterResolver.Resolve(snip, cli, globalParameters: null);

            var p = Assert.Single(result);
            Assert.Equal("env", p.Name);
            Assert.Equal(ParameterType.Choice, p.Type);
            Assert.Equal("dev", p.Default);
        }

        [Fact]
        public void Local_definition_overrides_a_cli_definition_of_the_same_name()
        {
            var snip = new Snip
            {
                CommandTemplate = "deploy {env}",
                Parameters = [P("env", ParameterType.Text, "local-default")],
            };
            var cli = new Cli { Parameters = [P("env", ParameterType.Choice, "dev")] };

            var result = ParameterResolver.Resolve(snip, cli, globalParameters: null);

            var p = Assert.Single(result);
            Assert.Equal(ParameterType.Text, p.Type);
            Assert.Equal("local-default", p.Default);
        }

        [Fact]
        public void Cli_definition_takes_precedence_over_global()
        {
            var snip = new Snip { CommandTemplate = "x {region}" };
            var cli = new Cli { Parameters = [P("region", def: "cli")] };
            var global = new[] { P("region", def: "global") };

            var result = ParameterResolver.Resolve(snip, cli, global);

            Assert.Equal("cli", Assert.Single(result).Default);
        }

        [Fact]
        public void Global_is_used_when_neither_local_nor_cli_define_the_token()
        {
            var snip = new Snip { CommandTemplate = "x {region}" };
            var global = new[] { P("region", def: "global") };

            var result = ParameterResolver.Resolve(snip, cli: new Cli(), globalParameters: global);

            Assert.Equal("global", Assert.Single(result).Default);
        }

        [Fact]
        public void Tokens_defined_in_no_scope_are_not_returned()
        {
            // Preserves the "bare token copies verbatim" behaviour — no input is
            // surfaced for an undefined token.
            var snip = new Snip { CommandTemplate = "git commit -m {message}" };

            var result = ParameterResolver.Resolve(snip, cli: new Cli(), globalParameters: []);

            Assert.Empty(result);
        }

        [Fact]
        public void Local_params_not_in_the_template_are_still_included()
        {
            var snip = new Snip { CommandTemplate = "static command", Parameters = [P("unused")] };

            var result = ParameterResolver.Resolve(snip, cli: null, globalParameters: null);

            Assert.Equal("unused", Assert.Single(result).Name);
        }

        [Fact]
        public void Local_first_then_inherited_in_template_order()
        {
            var snip = new Snip
            {
                CommandTemplate = "run {a} {b} {c}",
                Parameters = [P("a")], // local
            };
            var cli = new Cli { Parameters = [P("b")] };
            var global = new[] { P("c") };

            var result = ParameterResolver.Resolve(snip, cli, global);

            Assert.Equal(["a", "b", "c"], result.Select(p => p.Name));
        }
    }
}
