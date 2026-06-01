using Snipdeck.Importer.Commands;

using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    _ = config.SetApplicationName("snipdeck-importer");

    _ = config.AddCommand<SnipCommandImportCommand>("snipcommand")
        .WithDescription("Import command snippets from a SnipCommand export.")
        .WithExample("snipcommand", "snipcommand.db")
        .WithExample("snipcommand", "snipcommand.db", "--write")
        .WithExample("snipcommand", "snipcommand.db", "--store", "store.json", "--cli", "mpt-app");
});

return await app.RunAsync(args);
