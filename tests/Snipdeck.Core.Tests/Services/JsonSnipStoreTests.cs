using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

namespace Snipdeck.Core.Tests.Services
{

    public sealed class JsonSnipStoreTests : IDisposable
    {
        private readonly string _tempDirectory;

        public JsonSnipStoreTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "snipdeck-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            GC.SuppressFinalize(this);
        }

        private string PathIn(string name) => System.IO.Path.Combine(_tempDirectory, name);

        [Fact]
        public void Throws_when_file_path_is_null_or_whitespace()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonSnipStore(null!));
            Assert.Throws<ArgumentException>(() => new JsonSnipStore(""));
            Assert.Throws<ArgumentException>(() => new JsonSnipStore("   "));
        }

        [Fact]
        public async Task LoadAsync_returns_empty_document_when_file_missing()
        {
            var store = new JsonSnipStore(PathIn("store.json"));

            var document = await store.LoadAsync();

            Assert.Equal(SnipStoreDocument.CurrentSchemaVersion, document.SchemaVersion);
            Assert.Empty(document.Clis);
            Assert.Empty(document.Snips);
        }

        [Fact]
        public async Task SaveAsync_then_LoadAsync_round_trips_full_document()
        {
            var path = PathIn("store.json");
            var store = new JsonSnipStore(path);

            var cliId = Guid.NewGuid();
            var snipId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var original = new SnipStoreDocument
            {
                Clis =
                {
                    new Cli { Id = cliId, Name = "pl-app", IconRef = "pl-app.png" },
                },
                Snips =
                {
                    new Snip
                    {
                        Id = snipId,
                        CliId = cliId,
                        Title = "List orgs",
                        CommandTemplate = "pl-app orgs list --env {env}",
                        Description = "Lists every organisation visible to the caller.",
                        Tags = { "orgs", "read" },
                        IsFavourite = true,
                        UsageCount = 3,
                        LastUsedAt = now,
                        Parameters =
                        {
                            new Parameter
                            {
                                Name = "env",
                                Type = ParameterType.Choice,
                                Options = { "dev", "prod" },
                                Default = "dev",
                            },
                        },
                    },
                },
            };

            await store.SaveAsync(original);
            var loaded = await store.LoadAsync();

            Assert.Equal(SnipStoreDocument.CurrentSchemaVersion, loaded.SchemaVersion);

            var cli = Assert.Single(loaded.Clis);
            Assert.Equal(cliId, cli.Id);
            Assert.Equal("pl-app", cli.Name);
            Assert.Equal("pl-app.png", cli.IconRef);

            var snip = Assert.Single(loaded.Snips);
            Assert.Equal(snipId, snip.Id);
            Assert.Equal(cliId, snip.CliId);
            Assert.Equal("List orgs", snip.Title);
            Assert.Equal("pl-app orgs list --env {env}", snip.CommandTemplate);
            Assert.Equal("Lists every organisation visible to the caller.", snip.Description);
            Assert.Equal(new[] { "orgs", "read" }, snip.Tags);
            Assert.True(snip.IsFavourite);
            Assert.False(snip.IsTrash);
            Assert.Equal(3, snip.UsageCount);
            Assert.Equal(now, snip.LastUsedAt);

            var param = Assert.Single(snip.Parameters);
            Assert.Equal("env", param.Name);
            Assert.Equal(ParameterType.Choice, param.Type);
            Assert.Equal(new[] { "dev", "prod" }, param.Options);
            Assert.Equal("dev", param.Default);
        }

        [Fact]
        public async Task SaveAsync_creates_missing_parent_directory()
        {
            var nested = PathIn("a/b/c/store.json");
            var store = new JsonSnipStore(nested);

            await store.SaveAsync(new SnipStoreDocument());

            Assert.True(File.Exists(nested));
        }

        [Fact]
        public async Task SaveAsync_does_not_leave_tmp_file_behind_on_success()
        {
            var path = PathIn("store.json");
            var store = new JsonSnipStore(path);

            await store.SaveAsync(new SnipStoreDocument());

            Assert.False(File.Exists(path + ".tmp"));
            Assert.True(File.Exists(path));
        }

        [Fact]
        public async Task SaveAsync_overwrites_existing_file()
        {
            var path = PathIn("store.json");
            var store = new JsonSnipStore(path);

            await store.SaveAsync(new SnipStoreDocument
            {
                Clis = { new Cli { Name = "first" } },
            });

            await store.SaveAsync(new SnipStoreDocument
            {
                Clis = { new Cli { Name = "second" } },
            });

            var loaded = await store.LoadAsync();
            var cli = Assert.Single(loaded.Clis);
            Assert.Equal("second", cli.Name);
        }

        [Fact]
        public async Task LoadAsync_throws_when_schema_version_is_newer_than_supported()
        {
            var path = PathIn("store.json");
            var futureJson = $$"""
                {
                  "schemaVersion": {{SnipStoreDocument.CurrentSchemaVersion + 1}},
                  "clis": [],
                  "snips": []
                }
                """;
            await File.WriteAllTextAsync(path, futureJson);

            var store = new JsonSnipStore(path);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync());
        }

        [Fact]
        public async Task SaveAsync_throws_on_null_document()
        {
            var store = new JsonSnipStore(PathIn("store.json"));

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(null!));
        }

        [Fact]
        public async Task Concurrent_saves_leave_a_consistent_well_formed_file()
        {
            var path = PathIn("store.json");
            var store = new JsonSnipStore(path);

            var tasks = Enumerable.Range(0, 20).Select(i => store.SaveAsync(new SnipStoreDocument
            {
                Clis = { new Cli { Name = $"cli-{i}" } },
            })).ToArray();

            await Task.WhenAll(tasks);

            var loaded = await store.LoadAsync();
            var cli = Assert.Single(loaded.Clis);
            Assert.StartsWith("cli-", cli.Name);
            Assert.False(File.Exists(path + ".tmp"));
        }

        [Fact]
        public async Task Parameter_type_is_serialised_as_camel_case_string()
        {
            var path = PathIn("store.json");
            var store = new JsonSnipStore(path);

            await store.SaveAsync(new SnipStoreDocument
            {
                Snips =
                {
                    new Snip
                    {
                        Parameters =
                        {
                            new Parameter { Name = "p", Type = ParameterType.Choice },
                        },
                    },
                },
            });

            var json = await File.ReadAllTextAsync(path);
            Assert.Contains("\"type\": \"choice\"", json);
        }
    }
}
