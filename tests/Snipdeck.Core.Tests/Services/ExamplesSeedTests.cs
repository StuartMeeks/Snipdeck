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
        public void Build_produces_the_examples_and_importer_clis()
        {
            var doc = ExamplesSeed.Build();

            Assert.Equal(2, doc.Clis.Count);
            Assert.Contains(doc.Clis, c => c.Name == ExamplesSeed.CliName);
            Assert.Contains(doc.Clis, c => c.Name == ExamplesSeed.ImporterCliName);
            Assert.All(doc.Clis, c => Assert.NotEqual(Guid.Empty, c.Id));
            Assert.Equal(doc.Clis.Count, doc.Clis.Select(c => c.Id).Distinct().Count());
        }

        [Fact]
        public void Build_produces_snips_that_each_belong_to_a_seeded_cli()
        {
            var doc = ExamplesSeed.Build();
            var cliIds = doc.Clis.Select(c => c.Id).ToHashSet();

            Assert.NotEmpty(doc.Snips);
            Assert.All(doc.Snips, snip => Assert.Contains(snip.CliId, cliIds));

            // Both seeded CLIs carry at least one snip.
            Assert.All(doc.Clis, cli => Assert.Contains(doc.Snips, s => s.CliId == cli.Id));
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

                await store.SaveAsync(original, TestContext.Current.CancellationToken);
                var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

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
