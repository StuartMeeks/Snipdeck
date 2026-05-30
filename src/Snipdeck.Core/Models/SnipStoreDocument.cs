namespace Snipdeck.Core.Models
{
    public sealed class SnipStoreDocument
    {
        // v2 adds shared parameter definitions (Cli.Parameters + GlobalParameters).
        // v3 adds per-tag icon glyphs (Cli.TagIcons).
        // Additive and forward-incompatible: an older build refuses a newer store
        // rather than silently dropping the new collections.
        public const int CurrentSchemaVersion = 3;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public List<Cli> Clis { get; set; } = [];

        public List<Snip> Snips { get; set; } = [];

        /// <summary>
        /// Parameter definitions available to every Snip across all CLIs. A
        /// Snip's <c>{token}</c> resolves to one of these by name when neither
        /// the Snip nor its CLI defines a parameter of that name (lowest
        /// precedence). See <c>ParameterResolver</c>.
        /// </summary>
        public List<Parameter> GlobalParameters { get; set; } = [];

        /// <summary>
        /// Icon glyph per tag name (Segoe Fluent Icons), applied wherever that
        /// tag appears in the left navigation. Tag names absent from the map use
        /// the default "#" glyph. Tags are matched by name across all CLIs.
        /// </summary>
        public Dictionary<string, string> TagIcons { get; set; } = [];
    }
}
