using Snipdeck.Core.Engine;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Services
{
    public static class ExamplesSeed
    {
        public const string CliName = "Examples";
        public const string ImporterCliName = "snipdeck-importer";

        public static bool IsEmpty(SnipStoreDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);
            return document.Clis.Count == 0 && document.Snips.Count == 0;
        }

        public static SnipStoreDocument Build()
        {
            var document = new SnipStoreDocument();
            AddExamplesCli(document);
            AddImporterCli(document);

            ValidateInternalConsistency(document);
            return document;
        }

        private static void AddExamplesCli(SnipStoreDocument document)
        {
            var cli = new Cli
            {
                Name = CliName,
                Description = "A starter CLI with a few representative snips. Delete it once you're oriented.",
            };
            document.Clis.Add(cli);

            document.Snips.Add(new Snip
            {
                CliId = cli.Id,
                Title = "Echo a greeting",
                CommandTemplate = "echo Hello, {name}!",
                Description = "Prints a greeting to the console. A minimal one-parameter example.",
                Tags = { "demo" },
                Parameters =
                {
                    new Parameter
                    {
                        Name = "name",
                        Type = ParameterType.Text,
                        Default = "world",
                    },
                },
            });

            document.Snips.Add(new Snip
            {
                CliId = cli.Id,
                Title = "Deploy to an environment",
                CommandTemplate = "myapp deploy --env {env}",
                Description = "Triggers a deployment in the named environment. Shows a Choice parameter.",
                Tags = { "demo", "deploy" },
                IsFavourite = true,
                Parameters =
                {
                    new Parameter
                    {
                        Name = "env",
                        Type = ParameterType.Choice,
                        Options = { "dev", "staging", "prod" },
                        Default = "dev",
                    },
                },
            });

            document.Snips.Add(new Snip
            {
                CliId = cli.Id,
                Title = "Create an annotated git tag",
                CommandTemplate = "git tag -a {tag} -m \"{message}\"",
                Description = "Tags the current HEAD with a name and a message. Shows two parameters in one template.",
                Tags = { "demo", "git" },
                Parameters =
                {
                    new Parameter
                    {
                        Name = "tag",
                        Type = ParameterType.Text,
                        Default = "v1.0.0",
                    },
                    new Parameter
                    {
                        Name = "message",
                        Type = ParameterType.Text,
                        Default = "Release",
                    },
                },
            });
        }

        private static void AddImporterCli(SnipStoreDocument document)
        {
            var cli = new Cli
            {
                Name = ImporterCliName,
                Description = "The Snipdeck importer command-line tool. Bring snips in from SnipCommand and other sources.",
            };
            document.Clis.Add(cli);

            document.Snips.Add(new Snip
            {
                CliId = cli.Id,
                Title = "Preview a SnipCommand import",
                CommandTemplate = "snipdeck-importer snipcommand {path}",
                Description = "Parses a SnipCommand export and prints the planned additions without touching the store. The safe default — nothing is written until you add --write.",
                Tags = { "import", "snipcommand" },
                Parameters =
                {
                    new Parameter
                    {
                        Name = "path",
                        Type = ParameterType.Text,
                        Default = "snipcommand.db",
                    },
                },
            });

            document.Snips.Add(new Snip
            {
                CliId = cli.Id,
                Title = "Import from SnipCommand",
                CommandTemplate = "snipdeck-importer snipcommand {path} --write",
                Description = "Merges the SnipCommand export into the Snipdeck store. Backs the store up first, mints fresh identifiers, and skips snips that already exist.",
                Tags = { "import", "snipcommand" },
                IsFavourite = true,
                Parameters =
                {
                    new Parameter
                    {
                        Name = "path",
                        Type = ParameterType.Text,
                        Default = "snipcommand.db",
                    },
                },
            });

            document.Snips.Add(new Snip
            {
                CliId = cli.Id,
                Title = "Import into a specific store and CLI",
                CommandTemplate = "snipdeck-importer snipcommand {path} --store {store} --cli {cli}",
                Description = "Imports into an explicit store file and forces every snip into one named CLI, overriding the per-command auto-suggestion.",
                Tags = { "import", "snipcommand" },
                Parameters =
                {
                    new Parameter
                    {
                        Name = "path",
                        Type = ParameterType.Text,
                        Default = "snipcommand.db",
                    },
                    new Parameter
                    {
                        Name = "store",
                        Type = ParameterType.Text,
                        Default = "store.json",
                    },
                    new Parameter
                    {
                        Name = "cli",
                        Type = ParameterType.Text,
                        Default = "imported",
                    },
                },
            });
        }

        private static void ValidateInternalConsistency(SnipStoreDocument document)
        {
            foreach (var snip in document.Snips)
            {
                var definedNames = new HashSet<string>(
                    snip.Parameters.Select(p => p.Name),
                    StringComparer.Ordinal);

                foreach (var token in SubstitutionEngine.ExtractTokens(snip.CommandTemplate))
                {
                    if (!definedNames.Contains(token))
                    {
                        throw new InvalidOperationException(
                            $"Seed Snip '{snip.Title}' references token '{{{token}}}' with no matching parameter definition.");
                    }
                }
            }
        }
    }
}
