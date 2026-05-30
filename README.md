# Snipdeck

A native Windows desktop app for managing parameterised CLI command snippets
("Snips"), organised by the CLI they belong to. Browse a CLI, pick a Snip, fill
its arguments, and copy the resolved command to the clipboard.

Conceptually inspired by SnipCommand, with one defining difference: **the CLI is
the top-level organising axis** — every Snip belongs to exactly one CLI (e.g.
`pl-app`, `mpt-app`, `inv-app`).

> Snipdeck is alpha software. The list below describes what's actually
> implemented today, not what's planned. See [`TODO.md`](TODO.md) for the
> backlog and [`CHANGELOG.md`](CHANGELOG.md) for what's shipped.

## Status

Alpha. **v0.1.0-alpha.1** is the first packaged release — see
[Releases](https://github.com/StuartMeeks/Snipdeck/releases). It contains
the full v1 feature set: browse CLIs and Snips, author Snips with structured
parameters, fill and copy resolved commands, global hotkey, system tray with
close-to-tray, theme switching, and Velopack-backed self-update.

## Install

Download `Snipdeck-alpha-Setup.exe` from the latest
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
  Snipdeck.Core/        net10.0       — UI-free domain, engine, store, services
  Snipdeck.App/         net10.0-windows — WinUI 3 head, platform implementations
tests/
  Snipdeck.Core.Tests/  net10.0       — xUnit coverage for Core
```

The dependency direction is one-way: `App → Core`. The view models live in Core
and never touch WinUI types directly — every platform-bound capability is an
interface defined in Core and implemented in App.

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
`Snipdeck.App` project is Windows-only.

## Licence

Licensed under the Apache Licence, Version 2.0. See [`LICENSE`](LICENSE).
