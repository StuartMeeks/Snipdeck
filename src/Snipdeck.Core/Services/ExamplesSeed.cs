using Snipdeck.Core.Engine;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Services
{
    public static class ExamplesSeed
    {
        public const string CliName = "Examples";

        public static bool IsEmpty(SnipStoreDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);
            return document.Clis.Count == 0 && document.Snips.Count == 0;
        }

        public static SnipStoreDocument Build()
        {
            var cli = new Cli { Name = CliName };
            var document = new SnipStoreDocument();
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

            ValidateInternalConsistency(document);
            return document;
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
