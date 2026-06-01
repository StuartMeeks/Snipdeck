using Snipdeck.Core.Engine;
using Snipdeck.Core.Models;
using Snipdeck.Importer.Translation;

namespace Snipdeck.Importer.Tests
{
    public class ScMarkupTranslatorTests
    {
        [Fact]
        public void Plain_command_with_no_markup_is_unchanged_and_has_no_parameters()
        {
            var result = ScMarkupTranslator.Translate("mpt-app orders delete \"D:\\Working\\x.txt\"");

            Assert.Equal("mpt-app orders delete \"D:\\Working\\x.txt\"", result.CommandTemplate);
            Assert.Empty(result.Parameters);
        }

        [Fact]
        public void Choice_markup_becomes_a_choice_parameter_with_options_and_first_default()
        {
            var result = ScMarkupTranslator.Translate(
                "mpt-app export [sc_choice name=\"authId\" value=\"a-01,b-02,c-03\" /]");

            Assert.Equal("mpt-app export {authId}", result.CommandTemplate);
            var p = Assert.Single(result.Parameters);
            Assert.Equal("authId", p.Name);
            Assert.Equal(ParameterType.Choice, p.Type);
            Assert.Equal(["a-01", "b-02", "c-03"], p.Options);
            Assert.Equal("a-01", p.Default);
        }

        [Fact]
        public void Variable_markup_becomes_a_text_parameter_with_its_value_as_default()
        {
            var result = ScMarkupTranslator.Translate(
                "tool [sc_variable name=\"customerId\" value=\"1005\" /]");

            Assert.Equal("tool {customerId}", result.CommandTemplate);
            var p = Assert.Single(result.Parameters);
            Assert.Equal("customerId", p.Name);
            Assert.Equal(ParameterType.Text, p.Type);
            Assert.Equal("1005", p.Default);
        }

        [Fact]
        public void Empty_variable_value_yields_a_null_default()
        {
            var result = ScMarkupTranslator.Translate(
                "tool [sc_variable name=\"Agreement ID\" value=\"\" /]");

            var p = Assert.Single(result.Parameters);
            Assert.Null(p.Default);
        }

        [Theory]
        [InlineData("Agreement ID", "AgreementID")]
        [InlineData("Order ID / Agreement ID", "OrderIDAgreementID")]
        [InlineData("License Quantity", "LicenseQuantity")]
        [InlineData("VIP Membership No", "VIPMembershipNo")]
        public void Names_with_spaces_and_punctuation_are_slugified_to_legal_tokens(string name, string expectedToken)
        {
            var result = ScMarkupTranslator.Translate(
                $"tool [sc_variable name=\"{name}\" value=\"\" /]");

            Assert.Equal($"tool {{{expectedToken}}}", result.CommandTemplate);
            Assert.Equal(expectedToken, Assert.Single(result.Parameters).Name);
        }

        [Fact]
        public void Already_legal_names_are_preserved_verbatim()
        {
            var result = ScMarkupTranslator.Translate("tool [sc_variable name=\"authId\" value=\"x\" /]");
            Assert.Equal("authId", Assert.Single(result.Parameters).Name);
        }

        [Fact]
        public void A_name_repeated_in_one_command_maps_to_a_single_token_and_parameter()
        {
            var result = ScMarkupTranslator.Translate(
                "tool [sc_variable name=\"Agreement ID\" value=\"\" /] then [sc_variable name=\"Agreement ID\" value=\"\" /]");

            Assert.Equal("tool {AgreementID} then {AgreementID}", result.CommandTemplate);
            Assert.Single(result.Parameters);
        }

        [Fact]
        public void Distinct_names_that_slug_to_the_same_token_are_disambiguated()
        {
            // "Order ID" and "Order-ID" both slugify to "OrderID".
            var result = ScMarkupTranslator.Translate(
                "tool [sc_variable name=\"Order ID\" value=\"\" /] [sc_variable name=\"Order-ID\" value=\"\" /]");

            Assert.Equal("tool {OrderID} {OrderID_2}", result.CommandTemplate);
            Assert.Equal(2, result.Parameters.Count);
        }

        [Fact]
        public void Multiple_mixed_markups_in_one_command_are_all_translated()
        {
            var result = ScMarkupTranslator.Translate(
                "mpt-app adobe agreements request-3yc [sc_variable name=\"Agreement ID\" value=\"\" /] [sc_choice name=\"authId\" value=\"a,b\" /]");

            Assert.Equal("mpt-app adobe agreements request-3yc {AgreementID} {authId}", result.CommandTemplate);
            Assert.Equal(2, result.Parameters.Count);
        }

        [Fact]
        public void Every_emitted_token_is_backed_by_a_parameter()
        {
            var result = ScMarkupTranslator.Translate(
                "mpt-app x [sc_choice name=\"authId\" value=\"a,b\" /] [sc_variable name=\"Licensee IDs\" value=\"LCE-\" /]");

            var defined = result.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var token in SubstitutionEngine.ExtractTokens(result.CommandTemplate))
            {
                Assert.Contains(token, defined);
            }
        }

        [Fact]
        public void Malformed_markup_without_a_name_is_left_verbatim()
        {
            const string command = "tool [sc_variable value=\"x\" /]";
            var result = ScMarkupTranslator.Translate(command);

            Assert.Equal(command, result.CommandTemplate);
            Assert.Empty(result.Parameters);
        }

        [Fact]
        public void Non_self_closing_markup_is_left_verbatim()
        {
            const string command = "tool [sc_variable name=\"x\" value=\"1\"]";
            var result = ScMarkupTranslator.Translate(command);

            Assert.Equal(command, result.CommandTemplate);
            Assert.Empty(result.Parameters);
        }

        [Fact]
        public void Control_and_escape_characters_are_stripped_from_values()
        {
            var result = ScMarkupTranslator.Translate(
                "tool [sc_variable name=\"x\" value=\"a[31mb\tc\" /]");

            // ESC and tab removed; visible characters preserved.
            Assert.Equal("a[31mbc", Assert.Single(result.Parameters).Default);
        }

        [Fact]
        public void Null_or_empty_command_is_handled_gracefully()
        {
            Assert.Equal(string.Empty, ScMarkupTranslator.Translate(null).CommandTemplate);
            Assert.Empty(ScMarkupTranslator.Translate("").Parameters);
        }
    }
}
