using Snipdeck.Core.Engine;
using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

namespace Snipdeck.Core.Tests.Services
{

    public class ExamplesSeedTests
    {
        [Fact]
        public void IsEmpty_is_true_for_default_document()
        {
            Assert.True(ExamplesSeed.IsEmpty(new SnipStoreDocument()));
        }

        [Fact]
        public void IsEmpty_is_false_after_Build()
        {
            Assert.False(ExamplesSeed.IsEmpty(ExamplesSeed.Build()));
        }

        [Fact]
        public void Build_produces_a_single_cli_named_Examples()
        {
            var doc = ExamplesSeed.Build();

            var cli = Assert.Single(doc.Clis);
            Assert.Equal(ExamplesSeed.CliName, cli.Name);
            Assert.NotEqual(Guid.Empty, cli.Id);
        }

        [Fact]
        public void Build_produces_multiple_snips_all_belonging_to_the_examples_cli()
        {
            var doc = ExamplesSeed.Build();
            var cliId = doc.Clis.Single().Id;

            Assert.NotEmpty(doc.Snips);
            Assert.All(doc.Snips, snip => Assert.Equal(cliId, snip.CliId));
        }

        [Fact]
        public void Every_seed_snip_has_a_unique_id()
        {
            var doc = ExamplesSeed.Build();
            var ids = doc.Snips.Select(s => s.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void Every_template_token_is_backed_by_a_parameter_definition()
        {
            var doc = ExamplesSeed.Build();

            foreach (var snip in doc.Snips)
            {
                var defined = snip.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
                var referenced = SubstitutionEngine.ExtractTokens(snip.CommandTemplate);
                foreach (var token in referenced)
                {
                    Assert.Contains(token, defined);
                }
            }
        }

        [Fact]
        public void Seed_contains_at_least_one_choice_parameter_and_one_text_parameter()
        {
            var doc = ExamplesSeed.Build();
            var allParams = doc.Snips.SelectMany(s => s.Parameters).ToList();

            Assert.Contains(allParams, p => p.Type == ParameterType.Choice);
            Assert.Contains(allParams, p => p.Type == ParameterType.Text);
        }

        [Fact]
        public void Seed_contains_at_least_one_favourite_snip()
        {
            var doc = ExamplesSeed.Build();
            Assert.Contains(doc.Snips, s => s.IsFavourite);
        }

        [Fact]
        public async Task Seed_round_trips_through_the_json_store()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "snipdeck-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var store = new JsonSnipStore(Path.Combine(tempDir, "store.json"));
                var original = ExamplesSeed.Build();

                await store.SaveAsync(original);
                var loaded = await store.LoadAsync();

                Assert.Equal(original.Clis.Count, loaded.Clis.Count);
                Assert.Equal(original.Snips.Count, loaded.Snips.Count);
                Assert.Equal(original.Clis[0].Name, loaded.Clis[0].Name);
                for (var i = 0; i < original.Snips.Count; i++)
                {
                    Assert.Equal(original.Snips[i].Title, loaded.Snips[i].Title);
                    Assert.Equal(original.Snips[i].CommandTemplate, loaded.Snips[i].CommandTemplate);
                    Assert.Equal(original.Snips[i].Parameters.Count, loaded.Snips[i].Parameters.Count);
                }
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
