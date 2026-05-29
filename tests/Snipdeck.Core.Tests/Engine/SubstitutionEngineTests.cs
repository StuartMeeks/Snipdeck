using Snipdeck.Core.Engine;

namespace Snipdeck.Core.Tests.Engine
{

    public class SubstitutionEngineTests
    {
        [Fact]
        public void Empty_template_returns_empty_and_no_unresolved()
        {
            var result = SubstitutionEngine.Substitute(string.Empty, new Dictionary<string, string?>());

            Assert.Equal(string.Empty, result.Text);
            Assert.Empty(result.UnresolvedTokens);
            Assert.True(result.IsFullyResolved);
        }

        [Fact]
        public void Template_with_no_tokens_is_returned_verbatim()
        {
            var template = "echo hello world";

            var result = SubstitutionEngine.Substitute(template, new Dictionary<string, string?>());

            Assert.Equal(template, result.Text);
            Assert.Empty(result.UnresolvedTokens);
        }

        [Fact]
        public void Single_token_is_substituted()
        {
            var values = new Dictionary<string, string?> { ["name"] = "Stuart" };

            var result = SubstitutionEngine.Substitute("hello {name}", values);

            Assert.Equal("hello Stuart", result.Text);
            Assert.True(result.IsFullyResolved);
        }

        [Fact]
        public void Multiple_distinct_tokens_are_substituted()
        {
            var values = new Dictionary<string, string?>
            {
                ["env"] = "prod",
                ["region"] = "eu-west-1",
            };

            var result = SubstitutionEngine.Substitute("deploy --env {env} --region {region}", values);

            Assert.Equal("deploy --env prod --region eu-west-1", result.Text);
            Assert.True(result.IsFullyResolved);
        }

        [Fact]
        public void Repeated_token_is_substituted_each_occurrence()
        {
            var values = new Dictionary<string, string?> { ["x"] = "42" };

            var result = SubstitutionEngine.Substitute("{x}-{x}-{x}", values);

            Assert.Equal("42-42-42", result.Text);
            Assert.True(result.IsFullyResolved);
        }

        [Fact]
        public void Missing_token_leaves_literal_in_place_and_lists_it_as_unresolved()
        {
            var result = SubstitutionEngine.Substitute("hello {name}", new Dictionary<string, string?>());

            Assert.Equal("hello {name}", result.Text);
            Assert.Equal(new[] { "name" }, result.UnresolvedTokens);
            Assert.False(result.IsFullyResolved);
        }

        [Fact]
        public void Repeated_missing_token_is_listed_once_only()
        {
            var result = SubstitutionEngine.Substitute(
                "{missing} and {missing} again",
                new Dictionary<string, string?>());

            Assert.Equal("{missing} and {missing} again", result.Text);
            Assert.Single(result.UnresolvedTokens);
            Assert.Equal("missing", result.UnresolvedTokens[0]);
        }

        [Fact]
        public void Mix_of_resolved_and_unresolved_handles_both()
        {
            var values = new Dictionary<string, string?> { ["a"] = "1" };

            var result = SubstitutionEngine.Substitute("{a} {b} {a} {c}", values);

            Assert.Equal("1 {b} 1 {c}", result.Text);
            Assert.Equal(new[] { "b", "c" }, result.UnresolvedTokens);
        }

        [Fact]
        public void Unresolved_tokens_preserve_first_appearance_order()
        {
            var result = SubstitutionEngine.Substitute(
                "{z} {a} {m} {a} {z}",
                new Dictionary<string, string?>());

            Assert.Equal(new[] { "z", "a", "m" }, result.UnresolvedTokens);
        }

        [Fact]
        public void Empty_string_value_substitutes_to_empty_string_and_is_considered_resolved()
        {
            var values = new Dictionary<string, string?> { ["flag"] = string.Empty };

            var result = SubstitutionEngine.Substitute("cmd {flag} end", values);

            Assert.Equal("cmd  end", result.Text);
            Assert.True(result.IsFullyResolved);
        }

        [Fact]
        public void Null_value_in_dictionary_is_treated_as_unresolved()
        {
            var values = new Dictionary<string, string?> { ["flag"] = null };

            var result = SubstitutionEngine.Substitute("cmd {flag}", values);

            Assert.Equal("cmd {flag}", result.Text);
            Assert.Equal(new[] { "flag" }, result.UnresolvedTokens);
        }

        [Fact]
        public void Token_matching_is_case_sensitive()
        {
            var values = new Dictionary<string, string?> { ["Env"] = "prod" };

            var result = SubstitutionEngine.Substitute("{env} vs {Env}", values);

            Assert.Equal("{env} vs prod", result.Text);
            Assert.Equal(new[] { "env" }, result.UnresolvedTokens);
        }

        [Theory]
        [InlineData("{a-b}")]        // hyphen is not allowed in identifiers
        [InlineData("{ name }")]     // whitespace is not allowed
        [InlineData("{}")]           // empty token name
        [InlineData("{1name}")]      // leading digit
        [InlineData("{a.b}")]        // dot not allowed
        public void Invalid_token_shapes_are_left_as_literal_text(string template)
        {
            var values = new Dictionary<string, string?> { ["name"] = "x" };

            var result = SubstitutionEngine.Substitute(template, values);

            Assert.Equal(template, result.Text);
            Assert.Empty(result.UnresolvedTokens);
        }

        [Fact]
        public void Json_payload_with_one_placeholder_only_substitutes_the_placeholder()
        {
            var template = "aws ec2 describe-instances --filters '{\"Name\":\"tag:Env\",\"Values\":[\"{env}\"]}'";
            var values = new Dictionary<string, string?> { ["env"] = "prod" };

            var result = SubstitutionEngine.Substitute(template, values);

            Assert.Equal(
                "aws ec2 describe-instances --filters '{\"Name\":\"tag:Env\",\"Values\":[\"prod\"]}'",
                result.Text);
            Assert.True(result.IsFullyResolved);
        }

        [Fact]
        public void Adjacent_tokens_with_no_separator_are_substituted_independently()
        {
            var values = new Dictionary<string, string?>
            {
                ["a"] = "Foo",
                ["b"] = "Bar",
            };

            var result = SubstitutionEngine.Substitute("{a}{b}", values);

            Assert.Equal("FooBar", result.Text);
        }

        [Theory]
        [InlineData("_hidden")]
        [InlineData("snake_case")]
        [InlineData("camelCase")]
        [InlineData("PascalCase")]
        [InlineData("with9digits")]
        public void Valid_identifier_shapes_are_substituted(string name)
        {
            var values = new Dictionary<string, string?> { [name] = "ok" };

            var result = SubstitutionEngine.Substitute("[{" + name + "}]", values);

            Assert.Equal("[ok]", result.Text);
        }

        [Fact]
        public void Substitute_throws_on_null_template()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SubstitutionEngine.Substitute(null!, new Dictionary<string, string?>()));
        }

        [Fact]
        public void Substitute_throws_on_null_values_dictionary()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SubstitutionEngine.Substitute("template", null!));
        }

        [Fact]
        public void ExtractTokens_returns_unique_tokens_in_first_appearance_order()
        {
            var tokens = SubstitutionEngine.ExtractTokens("{b} {a} {b} {c} {a}");

            Assert.Equal(new[] { "b", "a", "c" }, tokens);
        }

        [Fact]
        public void ExtractTokens_on_template_with_no_tokens_returns_empty()
        {
            var tokens = SubstitutionEngine.ExtractTokens("echo hello");

            Assert.Empty(tokens);
        }
    }
}
