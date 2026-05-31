using System.Text.Json;
using System.Text.Json.Serialization;

using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

namespace Snipdeck.Core.Tests.Services
{
    /// <summary>
    /// Proves the source-generated <see cref="StoreJsonContext"/> serializes
    /// byte-for-byte identically to the previous reflection-based serializer, so
    /// existing store/settings files on disk keep loading after the switch. Runs
    /// entirely on Linux, which de-risks the format change that the trimmed
    /// publish (PublishTrimmed) depends on.
    /// </summary>
    public class StoreJsonCompatibilityTests
    {
        // Mirrors the options the JSON stores used before source-gen.
        private static readonly JsonSerializerOptions _legacyOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        private static SnipStoreDocument SampleDocument()
        {
            var cli = new Cli { Name = "pl-app", IconRef = "icons/pl.png" };
            return new SnipStoreDocument
            {
                Clis = [cli],
                Snips =
                [
                    new Snip
                    {
                        CliId = cli.Id,
                        Title = "Deploy",
                        CommandTemplate = "pl-app deploy --env {env}",
                        Description = "Deploys to **{env}**.",
                        Parameters =
                        [
                            new Parameter
                            {
                                Name = "env",
                                Type = ParameterType.Choice,
                                Options = ["dev", "prod"],
                                Default = "dev",
                            },
                        ],
                        Tags = ["deploy", "ops"],
                        IsFavourite = true,
                        IsTrash = false,
                        UsageCount = 3,
                        LastUsedAt = new DateTimeOffset(2026, 5, 30, 9, 0, 0, TimeSpan.Zero),
                    },
                ],
            };
        }

        private static AppConfig SampleConfig() => new()
        {
            StoragePath = @"C:\data\store.json",
            BackupRetention = 25,
            Theme = ThemePreference.Dark,
            CloseBehaviour = CloseBehaviour.HideToTray,
            Hotkey = HotkeyBinding.Default,
        };

        [Fact]
        public void Document_source_gen_output_is_byte_identical_to_legacy()
        {
            var doc = SampleDocument();
            var legacy = JsonSerializer.Serialize(doc, _legacyOptions);
            var generated = JsonSerializer.Serialize(doc, StoreJsonContext.Default.SnipStoreDocument);
            Assert.Equal(legacy, generated);
        }

        [Fact]
        public void Config_source_gen_output_is_byte_identical_to_legacy()
        {
            var config = SampleConfig();
            var legacy = JsonSerializer.Serialize(config, _legacyOptions);
            var generated = JsonSerializer.Serialize(config, StoreJsonContext.Default.AppConfig);
            Assert.Equal(legacy, generated);
        }

        [Fact]
        public void Legacy_json_round_trips_through_the_source_gen_context()
        {
            var doc = SampleDocument();
            var legacyJson = JsonSerializer.Serialize(doc, _legacyOptions);

            var restored = JsonSerializer.Deserialize(legacyJson, StoreJsonContext.Default.SnipStoreDocument);

            Assert.NotNull(restored);
            Assert.Equal("pl-app", restored!.Clis[0].Name);
            Assert.Equal(ParameterType.Choice, restored.Snips[0].Parameters[0].Type);
            Assert.True(restored.Snips[0].IsFavourite);
            Assert.Equal(doc.Snips[0].LastUsedAt, restored.Snips[0].LastUsedAt);
        }
    }
}
