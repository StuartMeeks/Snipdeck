using System.ComponentModel;

using Spectre.Console.Cli;

namespace Snipdeck.Importer.Commands
{
    /// <summary>
    /// Options shared by every import subcommand. Defaults to a safe dry-run; only <c>--write</c>
    /// touches the store.
    /// </summary>
    public class ImportCommandSettings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("Path to the source export to import.")]
        public string Path { get; init; } = string.Empty;

        [CommandOption("--store <path>")]
        [Description("Target Snipdeck store file. Defaults to the desktop app's store.")]
        public string? Store { get; init; }

        [CommandOption("--write")]
        [Description("Apply the changes. Without this flag the importer only previews (dry-run).")]
        public bool Write { get; init; }

        [CommandOption("--cli <name>")]
        [Description("Force every imported snip into this CLI, overriding auto-detection.")]
        public string? Cli { get; init; }

        [CommandOption("--into <name>")]
        [Description("Fallback CLI for snips whose CLI could not be confidently auto-detected.")]
        public string? Into { get; init; }

        [CommandOption("--allow-duplicates")]
        [Description("Import snips even if one with the same title and command already exists.")]
        public bool AllowDuplicates { get; init; }
    }
}
