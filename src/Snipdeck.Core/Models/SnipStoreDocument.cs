namespace Snipdeck.Core.Models
{
    public sealed class SnipStoreDocument
    {
        // v2 adds shared parameter definitions (Cli.Parameters + GlobalParameters).
        // Additive and forward-incompatible: a v1-only build refuses a v2 store
        // rather than silently dropping shared parameters.
        public const int CurrentSchemaVersion = 2;

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
    }
}
