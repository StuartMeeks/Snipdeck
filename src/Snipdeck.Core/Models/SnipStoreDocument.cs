namespace Snipdeck.Core.Models
{
    public sealed class SnipStoreDocument
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public List<Cli> Clis { get; set; } = [];

        public List<Snip> Snips { get; set; } = [];
    }
}
