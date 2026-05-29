# Changelog

All notable changes to Snipdeck are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added — Phase 2: App lifecycle skeleton
- Explicit `Program.cs` entry point that runs the Velopack hook → initialises
  WinRT COM wrappers → checks single-instance via Windows App SDK
  `AppInstance.FindOrRegisterForKey("snipdeck")` and redirects activation to
  the primary instance → starts WinUI with a `DispatcherQueueSynchronizationContext`.
- `Bootstrap.cs` builds the DI container (`Microsoft.Extensions.DependencyInjection`):
  loads `AppConfig` from the settings store synchronously, resolves the
  snip-store and backup directories (config value or default), and registers
  every Core service plus `MainWindow`.
- App-side platform services: `WindowsPathProvider` (paths rooted at
  `%LOCALAPPDATA%\Snipdeck`), `WinUiDispatcher` (lazy-captures the UI thread's
  `DispatcherQueue` on first use).
- Core abstractions: `IPathProvider`, `IDispatcher`.
- `MainWindow` shell: Mica backdrop, `ExtendsContentIntoTitleBar` with a custom
  draggable title bar showing the app name, theme application from `AppConfig`
  (Light / Dark / System).
- First-run seed: if the snip store is empty when the app launches, the
  Examples CLI is written via `ISnipStore` before the window appears.
- Activation redirect from a secondary instance brings the primary instance's
  window back to the foreground.
- `Microsoft.Extensions.DependencyInjection` (10.0.8) added to centralised
  package versions.

### Added
- Repository scaffold: `Snipdeck.Core` (net10.0, UI-free), `Snipdeck.App` (WinUI 3,
  net10.0-windows), `Snipdeck.Core.Tests` (xUnit).
- Apache 2.0 licence.
- README and changelog.
- Core domain models: `Cli`, `Snip`, `Parameter` (Text / Choice), `Tag` as a
  string list, root `SnipStoreDocument` with `SchemaVersion`.
- `SubstitutionEngine` — replaces `{placeholder}` tokens against a value
  dictionary and returns both the resolved text and the list of unresolved tokens
  in first-appearance order. Only `[A-Za-z_][A-Za-z0-9_]*` is treated as a token,
  so JSON braces and other literal braces pass through.
- `ISnipStore` / `JsonSnipStore` — `System.Text.Json`-backed store with
  temp-file-then-rename atomic writes, schema-version guarding, and a single
  semaphore protecting concurrent access.
- `ISettingsStore` / `JsonSettingsStore` — application settings stored separately
  from the snip store, with defaults (`Theme = System`, `CloseBehaviour =
  HideToTray`, hotkey = Ctrl+Alt+S) applied when the file is missing.
- `IBackupService` / `BackupService` — copies the snip store to a timestamped
  filename on demand, prunes to the configured retention (default 20), and
  exposes a newest-first listing.
- `IClock` / `SystemClock` for testable time.
- `ExamplesSeed` — first-run seed producing one "Examples" CLI with a handful of
  representative Snips (Text + Choice parameters, tags, a favourite).
- GitHub Actions CI workflow: builds Core on Ubuntu, builds the full solution on
  Windows, runs Core tests on both.
- GitHub Actions release workflow: tag-triggered (`v*.*.*` stable, `v*.*.*-*`
  pre-release), publishes the app, packs with Velopack, and attaches the
  artefacts to a GitHub Release.
- `.editorconfig` taken verbatim from
  `StuartMeeks/NextIteration.SpectreConsole.SelfUpdate`. Enforces brace style,
  `using` ordering, naming conventions, and analyser severities.
- `Directory.Build.props` at the repo root: shared `Authors` ("Stuart Meeks"),
  `Company` ("Next Iteration"), copyright, nullability, implicit usings, latest
  `LangVersion`, and **`TreatWarningsAsErrors=true`** + `EnforceCodeStyleInBuild`.
- `Directory.Packages.props` at the repo root for Central Package Management
  (`ManagePackageVersionsCentrally=true`,
  `CentralPackageTransitivePinningEnabled=true`).
- `CONTRIBUTING.md` covering prerequisites, the non-negotiables, the
  Core / App boundary discipline, the release process, and how to ask for
  direction when something's genuinely ambiguous.

### Changed
- All Core source converted to block-scoped namespaces, collection expressions
  (`[]`), and a `GeneratedRegex`-backed token regex to comply with the
  editorconfig under `TreatWarningsAsErrors`.
- `JsonSnipStore`, `JsonSettingsStore`, and `BackupService` now implement
  `IDisposable` to release their internal semaphores (CA1001).
- `Snipdeck.App.csproj` no longer carries `<Nullable>` (now inherited from
  `Directory.Build.props`).
- Bumped test-project dependencies to latest stable: `coverlet.collector` 6.0.4 →
  10.0.1, `Microsoft.NET.Test.Sdk` 17.14.1 → 18.6.0, `xunit.runner.visualstudio`
  3.1.4 → 3.1.5.
