using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

namespace Snipdeck.Core.Tests.Services
{
    public class SnipFilterTests
    {
        private static Snip Snip(string title, string template = "echo", string[]? tags = null, bool isTrash = false)
        {
            return new Snip
            {
                Title = title,
                CommandTemplate = template,
                IsTrash = isTrash,
                Tags = tags is null ? [] : [.. tags],
            };
        }

        [Fact]
        public void Empty_search_and_tag_returns_all_non_trash_snips()
        {
            var snips = new[]
            {
                Snip("a"),
                Snip("b"),
                Snip("c", isTrash: true),
            };

            var result = SnipFilter.Apply(snips, searchText: null, selectedTag: null).ToList();

            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, s => s.Title == "c");
        }

        [Fact]
        public void Trash_can_be_included_when_explicitly_requested()
        {
            var snips = new[]
            {
                Snip("a"),
                Snip("b", isTrash: true),
            };

            var result = SnipFilter.Apply(snips, null, null, includeTrash: true).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Search_matches_title_substring_case_insensitively()
        {
            var snips = new[]
            {
                Snip("List Organisations"),
                Snip("Deploy production"),
            };

            var result = SnipFilter.Apply(snips, "ORG", null).ToList();

            var single = Assert.Single(result);
            Assert.Equal("List Organisations", single.Title);
        }

        [Fact]
        public void Search_matches_command_template()
        {
            var snips = new[]
            {
                Snip("a", template: "pl-app orgs list"),
                Snip("b", template: "mpt-app users list"),
            };

            var result = SnipFilter.Apply(snips, "users", null).ToList();

            Assert.Single(result);
            Assert.Equal("b", result[0].Title);
        }

        [Fact]
        public void Search_matches_tag()
        {
            var snips = new[]
            {
                Snip("a", tags: ["deploy", "prod"]),
                Snip("b", tags: ["read"]),
            };

            var result = SnipFilter.Apply(snips, "deploy", null).ToList();

            Assert.Single(result);
            Assert.Equal("a", result[0].Title);
        }

        [Fact]
        public void Selected_tag_restricts_to_snips_carrying_that_tag()
        {
            var snips = new[]
            {
                Snip("a", tags: ["deploy"]),
                Snip("b", tags: ["read"]),
                Snip("c", tags: ["DEPLOY"]),
            };

            var result = SnipFilter.Apply(snips, null, "deploy").ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.Title == "a");
            Assert.Contains(result, s => s.Title == "c");
        }

        [Fact]
        public void Search_and_tag_apply_together_as_an_AND_filter()
        {
            var snips = new[]
            {
                Snip("List orgs", tags: ["read"]),
                Snip("Deploy prod", tags: ["deploy"]),
                Snip("Read logs", tags: ["read"]),
            };

            var result = SnipFilter.Apply(snips, "logs", "read").ToList();

            Assert.Single(result);
            Assert.Equal("Read logs", result[0].Title);
        }

        [Fact]
        public void Whitespace_only_search_is_ignored()
        {
            var snips = new[] { Snip("a"), Snip("b") };

            var result = SnipFilter.Apply(snips, "   ", null).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void DistinctTagsFor_returns_unique_tags_excluding_trash()
        {
            var snips = new[]
            {
                Snip("a", tags: ["deploy", "prod"]),
                Snip("b", tags: ["prod", "read"]),
                Snip("c", tags: ["secret"], isTrash: true),
            };

            var result = SnipFilter.DistinctTagsFor(snips).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains("deploy", result);
            Assert.Contains("prod", result);
            Assert.Contains("read", result);
            Assert.DoesNotContain("secret", result);
        }
    }
}
