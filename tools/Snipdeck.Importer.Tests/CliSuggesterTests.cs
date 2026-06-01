using Snipdeck.Importer.Translation;

namespace Snipdeck.Importer.Tests
{
    public class CliSuggesterTests
    {
        [Theory]
        [InlineData("mpt-app orders delete \"x.txt\"", "mpt-app")]
        [InlineData("inv-app validate invoices", "inv-app")]
        [InlineData("pip install --index-url https://x mpt-cli", "pip")]
        public void First_token_is_the_suggested_cli(string command, string expected)
        {
            var result = CliSuggester.Suggest(command);
            Assert.True(result.Confident);
            Assert.Equal(expected, result.Name);
        }

        [Theory]
        [InlineData("sudo systemctl restart x", "systemctl")]
        [InlineData("npx create-react-app foo", "create-react-app")]
        [InlineData("env FOO=bar mytool run", "mytool")]
        public void Leading_wrappers_are_peeled(string command, string expected)
        {
            var result = CliSuggester.Suggest(command);
            Assert.True(result.Confident);
            Assert.Equal(expected, result.Name);
        }

        [Theory]
        [InlineData("pip install x")]
        [InlineData("python -m build")]
        [InlineData("dotnet build")]
        public void Language_runtimes_are_not_peeled(string command)
        {
            var first = command.Split(' ')[0];
            var result = CliSuggester.Suggest(command);
            Assert.Equal(first, result.Name);
        }

        [Fact]
        public void Empty_command_is_not_confident()
        {
            var result = CliSuggester.Suggest("   ");
            Assert.False(result.Confident);
            Assert.Equal(string.Empty, result.Name);
        }

        [Fact]
        public void A_parameterised_leading_token_is_not_confident()
        {
            var result = CliSuggester.Suggest("{tool} run");
            Assert.False(result.Confident);
        }
    }
}
