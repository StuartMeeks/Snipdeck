# snipdeck-importer

A small cross-platform console tool that imports command snippets from other tools
into a [Snipdeck](../../README.md) store. The first supported source is
**SnipCommand**; the design leaves room for more sources as subcommands later.

It is shipped and versioned independently of the desktop app (it is a .NET tool),
and targets `net10.0`, so it builds and runs on Windows, Linux and macOS — handy
for bulk-prepping an import on a non-Windows box before opening Snipdeck.

## Install

```bash
dotnet tool install -g Snipdeck.Importer
```

Or run it straight from the repo without installing:

```bash
dotnet run --project tools/Snipdeck.Importer -- snipcommand <path>
```

## Usage

```
snipdeck-importer snipcommand <path> [options]
```

`<path>` is a SnipCommand export. Despite SnipCommand's `.db` extension, the export
is JSON — point the importer straight at it.

| Option | Default | Description |
| --- | --- | --- |
| `--store <path>` | the desktop app's store | Target Snipdeck store file. |
| `--write` | off | Apply the changes. Without it, the importer only previews (dry-run). |
| `--cli <name>` | — | Force every imported snip into this CLI, overriding auto-detection. |
| `--into <name>` | — | Fallback CLI for snips whose CLI could not be confidently auto-detected. |
| `--allow-duplicates` | off | Import snips even if one with the same title and command already exists. |

### Dry-run first

By default the importer parses the source, prints the planned additions grouped by
the CLI each snip would land in, and exits **without touching the store**:

```bash
snipdeck-importer snipcommand snipcommand.db
```

Add `--write` once the preview looks right:

```bash
snipdeck-importer snipcommand snipcommand.db --write
```

## What it does

- **Groups by CLI.** SnipCommand stores a flat list; Snipdeck organises snips under
  a CLI. The CLI is auto-suggested from the first whitespace-delimited token of each
  command (`mpt-app orders list` → `mpt-app`), peeling off launcher wrappers such as
  `sudo` / `npx`. Use `--cli` to force one CLI for everything, or `--into` as a
  fallback for commands the importer couldn't classify confidently.
- **Translates parameters.** SnipCommand's inline `[sc_choice …]` and
  `[sc_variable …]` markup becomes Snipdeck `{token}` placeholders plus a structured
  parameter list (Choice with options, or Text), with a sensible default carried
  across. Variable names containing spaces or punctuation are slugified into legal
  token names.
- **Carries metadata.** Title, description, tags and favourite flag come across;
  usage counts and last-used timestamps are preserved when present. Trashed entries
  are skipped.
- **Merges safely.** With `--write` the importer backs the existing store up first
  (honouring the desktop app's retention policy), mints fresh identifiers, creates
  any missing CLIs on demand, and skips snips that already exist by
  `(title, command)` unless `--allow-duplicates` is given. The atomic store write
  means a running desktop instance can't be corrupted — but it won't *see* the
  changes until it next reloads, so **restart Snipdeck after importing**.

## Building and testing

```bash
dotnet build tools/Snipdeck.Importer
dotnet test  tools/Snipdeck.Importer.Tests
```
