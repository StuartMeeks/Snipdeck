using Snipdeck.Core.Models;
using Snipdeck.Importer.Sources;

namespace Snipdeck.Importer.Tests
{
    public class SnipCommandSourceTests
    {
        private static readonly string _fixturePath =
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "snipcommand-sample.db");

        [Fact]
        public void Reads_the_sample_export_skipping_trashed_entries()
        {
            var candidates = new SnipCommandSource().Read(_fixturePath);

            // 5 entries in the fixture, one of which is trashed.
            Assert.Equal(4, candidates.Count);
            Assert.DoesNotContain(candidates, c => c.Snip.Title == "An old trashed snip");
        }

        [Fact]
        public void Suggests_the_cli_from_the_first_token()
        {
            var candidates = new SnipCommandSource().Read(_fixturePath);

            var pip = candidates.Single(c => c.Snip.Title == "Get latest mpt cli");
            Assert.Equal("pip", pip.SuggestedCliName);
            Assert.True(pip.CliConfident);

            var mpt = candidates.Single(c => c.Snip.Title == "Delete marketplace orders");
            Assert.Equal("mpt-app", mpt.SuggestedCliName);
        }

        [Fact]
        public void Carries_tags_favourite_usage_and_last_used()
        {
            var candidates = new SnipCommandSource().Read(_fixturePath);
            var mpt = candidates.Single(c => c.Snip.Title == "Delete marketplace orders");

            Assert.True(mpt.Snip.IsFavourite);
            Assert.Equal(["mpt", "orders"], mpt.Snip.Tags);
            Assert.Equal(4, mpt.Snip.UsageCount);
            Assert.Equal(new DateTimeOffset(2025, 9, 12, 10, 15, 30, TimeSpan.Zero), mpt.Snip.LastUsedAt);
        }

        [Fact]
        public void Empty_description_and_tags_become_null_and_empty()
        {
            var candidates = new SnipCommandSource().Read(_fixturePath);
            var pip = candidates.Single(c => c.Snip.Title == "Get latest mpt cli");

            Assert.Null(pip.Snip.Description);
            Assert.Equal(["billing", "adobe"], pip.Snip.Tags);
        }

        [Fact]
        public void Translates_markup_into_tokens_and_parameters()
        {
            var candidates = new SnipCommandSource().Read(_fixturePath);

            var export = candidates.Single(c => c.Snip.Title == "Export subscriptions");
            Assert.Equal("mpt-app adobe subscriptions export {authId}", export.Snip.CommandTemplate);
            var choice = Assert.Single(export.Snip.Parameters);
            Assert.Equal(ParameterType.Choice, choice.Type);
            Assert.Equal(3, choice.Options.Count);

            var threeYc = candidates.Single(c => c.Snip.Title == "Request 3YC");
            Assert.Equal("mpt-app adobe agreements request-3yc {AgreementID} {authId}", threeYc.Snip.CommandTemplate);
            Assert.Equal(2, threeYc.Snip.Parameters.Count);
        }

        [Fact]
        public void Non_json_content_is_rejected_with_a_friendly_message()
        {
            var ex = Assert.Throws<InvalidDataException>(() => SnipCommandSource.ReadFromText("SQLite format 3"));
            Assert.Contains("SnipCommand", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Malformed_json_is_rejected_with_a_friendly_message()
        {
            Assert.Throws<InvalidDataException>(() => SnipCommandSource.ReadFromText("{ \"commands\": [ "));
        }

        [Fact]
        public void Missing_file_throws_file_not_found()
        {
            Assert.Throws<FileNotFoundException>(() => new SnipCommandSource().Read("/no/such/file.db"));
        }
    }
}
