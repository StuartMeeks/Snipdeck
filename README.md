<p align="center">
  <img src="designs/snipdeck-tile.svg" alt="Snipdeck" width="160">
</p>

# Snipdeck

<p align="center">
  <a href="https://github.com/StuartMeeks/Snipdeck/actions/workflows/ci.yml"><img src="https://github.com/StuartMeeks/Snipdeck/actions/workflows/ci.yml/badge.svg" alt="CI status"></a>
  <a href="https://github.com/StuartMeeks/Snipdeck/releases/latest"><img src="https://img.shields.io/github/v/release/StuartMeeks/Snipdeck?sort=semver" alt="Latest release"></a>
  <a href="https://github.com/StuartMeeks/Snipdeck/releases"><img src="https://img.shields.io/github/downloads/StuartMeeks/Snipdeck/total" alt="Downloads"></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" alt=".NET 10"></a>
  <img src="https://img.shields.io/badge/platform-Windows%2011-0078D6?logo=windows11&logoColor=white" alt="Platform: Windows 11">
  <a href="LICENSE"><img src="https://img.shields.io/github/license/StuartMeeks/Snipdeck" alt="Licence: Apache-2.0"></a>
</p>

A native Windows desktop app for managing parameterised CLI command snippets
("Snips"), organised by the CLI they belong to. Browse a CLI, pick a Snip, fill
its arguments, and copy the resolved command to the clipboard — or run it in place
and watch it live, with a clean, searchable run history.

Conceptually inspired by
[SnipCommand](https://github.com/gurayyarar/SnipCommand), with one defining
difference: **the CLI is the top-level organising axis** — every Snip belongs
to exactly one CLI (e.g. `pl-app`, `mpt-app`, `inv-app`).

## Status

Snipdeck has shipped its **v1 release** and is stable — see
[Releases](https://github.com/StuartMeeks/Snipdeck/releases) for the current
build, or [`CHANGELOG.md`](CHANGELOG.md) for the full history. The installed app
self-updates from then on.

The v1 feature set:

- Browse CLIs and Snips; author Snips with structured Text and Choice parameters,
  tags, favourites, and a Markdown description.
- Fill a Snip's parameters from a flyout with a live preview, and copy the resolved
  command to the clipboard.
- **Run** a Snip in its configured shell via a real pseudo-terminal (ConPTY) — so
  colours, spinners, progress bars and interactive prompts all work — watch it live,
  and keep a clean, searchable plain-text run history you can replay. Per-CLI shell,
  executable path and working directory configure how runs launch.
- Global hotkey (default **Ctrl+Alt+S**, rebindable), system tray with
  configurable close-to-tray, Light/Dark/System theme, and per-write plus
  pre-update store backups.
- Migrate from SnipCommand with the [`snipdeck-importer`](tools/Snipdeck.Importer)
  .NET global tool.

See [`TODO.md`](TODO.md) for the backlog of deferred and v2 ideas.

## Install

Download `Snipdeck-stable-Setup.exe` from the latest
[release](https://github.com/StuartMeeks/Snipdeck/releases) and run it.
The installer is unpackaged (no Microsoft Store, no MSIX) and Velopack
handles self-update from then on.

Windows 11 is the supported target — Mica backdrop only renders there;
on Windows 10 it falls back to a solid colour.

## Stack

- .NET 10, C#
- WinUI 3 (Windows App SDK) — unpackaged
- MVVM via [`CommunityToolkit.Mvvm`](https://github.com/CommunityToolkit/dotnet)
- DI via `Microsoft.Extensions.DependencyInjection`
- JSON persistence via `System.Text.Json`
- Identicons via [`Jdenticon-net`](https://github.com/dmester/jdenticon-net)
- Tray icon via [`H.NotifyIcon`](https://github.com/HavenDV/H.NotifyIcon)
- Install + self-update via [Velopack](https://velopack.io/)
- Tests via xUnit

## Repository layout

```
src/
  Snipdeck.Core/             net10.0         — UI-free domain, engine, store, services
  Snipdeck.App/              net10.0-windows — WinUI 3 head, platform implementations
tests/
  Snipdeck.Core.Tests/       net10.0         — xUnit coverage for Core
tools/
  Snipdeck.Importer/         net10.0         — snipdeck-importer console tool (SnipCommand import)
  Snipdeck.Importer.Tests/   net10.0         — xUnit coverage for the importer
```

The dependency direction is one-way: `App → Core`. The view models live in Core
and never touch WinUI types directly — every platform-bound capability is an
interface defined in Core and implemented in App.

## Importing from SnipCommand

Migrating from SnipCommand? The cross-platform `snipdeck-importer` console tool
reads a SnipCommand export and merges its snippets into your Snipdeck store,
grouping them under a CLI per command. It defaults to a dry-run preview. Install
it as a .NET global tool:

```bash
dotnet tool install -g NextIteration.Snipdeck.Importer
```

See [`tools/Snipdeck.Importer/README.md`](tools/Snipdeck.Importer/README.md) for
the full options.

## Building

Requirements:

- Windows 11 (Windows 10 1809+ may work but is not the target)
- .NET 10 SDK
- Visual Studio 2022 17.x with the **Windows App SDK** workload, *or* the
  command-line equivalents

```powershell
# Restore + build everything
dotnet build

# Run Core unit tests
dotnet test tests/Snipdeck.Core.Tests
```

The `Snipdeck.Core` project targets `net10.0` and is fully portable, so
`dotnet build` / `dotnet test` for Core also work on Linux and macOS. The
`Snipdeck.App` project is Windows-only. The `Snipdeck.Importer` tool also targets
`net10.0` and builds, tests and runs on any platform.

## Acknowledgements

Snipdeck owes its founding idea to
[SnipCommand](https://github.com/gurayyarar/SnipCommand) by
[Güray Yarar](https://github.com/gurayyarar) — a cross-platform command
snippet manager that demonstrated how useful parameterised, fill-in-and-copy
command templates are day to day. Snipdeck reimagines that idea as a native
Windows app organised around the CLI each command belongs to. A
[SnipCommand import tool](tools/Snipdeck.Importer) ships in this repo.

Snipdeck is built on the work of these open-source projects, with thanks to
their authors and maintainers:

- [.NET Community Toolkit](https://github.com/CommunityToolkit/dotnet) — MVVM source generators and helpers (MIT)
- [Coverlet](https://github.com/coverlet-coverage/coverlet) — code-coverage collection (MIT)
- [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) — system-tray icon and menu (MIT)
- [Jdenticon](https://github.com/dmester/jdenticon-net) — identicons for CLIs without a custom icon (MIT)
- [Markdig](https://github.com/xoofx/markdig) — Markdown rendering for Snip descriptions (BSD-2-Clause)
- [Microsoft.Extensions.DependencyInjection](https://github.com/dotnet/runtime) — dependency injection (MIT)
- [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) — git-derived versioning (MIT)
- [Porta.Pty](https://github.com/tomlm/Porta.Pty) — cross-platform pseudo-terminal (ConPTY) for running commands (MIT)
- [Spectre.Console](https://github.com/spectreconsole/spectre.console) — console UI for the import tool (MIT)
- [SQLite](https://www.sqlite.org) & [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore) — execution-history storage (Public Domain / MIT)
- [Velopack](https://github.com/velopack/velopack) — installer and self-update (MIT)
- [WebView2](https://learn.microsoft.com/microsoft-edge/webview2/) — hosts the xterm.js live terminal (Microsoft)
- [Windows App SDK & WinUI 3](https://github.com/microsoft/WindowsAppSDK) — the native Windows UI framework (MIT)
- [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows) — `SettingsCard` / `SettingsExpander` controls (MIT)
- [xterm.js](https://github.com/xtermjs/xterm.js) — renders the live terminal output (MIT)
- [xUnit](https://github.com/xunit/xunit) — unit-testing framework (Apache-2.0)

## Licence

Licensed under the Apache Licence, Version 2.0. See [`LICENSE`](LICENSE).
