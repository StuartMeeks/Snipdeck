# Changelog

All notable changes to Snipdeck are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- **JSON stores moved to System.Text.Json source generation.** `JsonSnipStore`
  and `JsonSettingsStore` now serialise via a generated `JsonSerializerContext`
  instead of the reflection-based serializer, removing the IL2026 trim warnings.
  The on-disk format is unchanged (proven byte-identical by tests, with enum
  names pinned via `[JsonStringEnumMemberName]`), so existing stores keep
  loading. `PublishTrimmed` stays off: a trimmed WinUI publish still trips
  IL2104 on the WinAppSDK/WinRT/Jdenticon assemblies, which aren't trim-safe.

### Added
- **Change the storage location.** A "Change…" button on Settings → Storage
  location lets you pick a new folder for your snips. If the folder already
  contains a Snipdeck store it's adopted (your current snips are left where
  they are); otherwise your store and icons are moved there. The choice is
  confirmed first, and Snipdeck restarts to apply it — the storage path is
  read at startup, so restarting keeps everything consistent and avoids
  writing to the old location after the switch.
- **Rebindable global hotkey.** The global hotkey is now editable from
  Settings: click the capture box and press a shortcut (at least one of
  Ctrl/Alt/Shift plus a key). The new binding registers and persists
  immediately; if the chord is already taken by another app, the previous
  binding is restored and a brief notice is shown. A "Reset" button restores
  the default (Ctrl+Alt+S). Previously the hotkey was display-only.
- **Markdown rendering for snip descriptions.** A snip's description is now
  rendered as Markdown (headings, bold/italic, inline and block code, links,
  ordered/unordered lists) in the copy flyout, instead of being hidden. The
  Markdown is parsed in Core (`MarkdownParser`, backed by Markdig) into a
  UI-free model that the WinUI head maps onto native text; the editor labels
  the field as Markdown. A snip with a description but no parameters now opens
  the flyout too, so its description is always visible before copying.
- **Git-derived version numbers (Nerdbank.GitVersioning).** Every build now
  stamps a real version (`0.1.0-alpha.<git-height>+<commit>`) into all
  assemblies from a single `version.json`, so the About page shows a meaningful
  version for dev and CI builds instead of the `1.0.0` compile-time fallback.
  Releases derive their version from NBGV rather than the tag string — see
  CONTRIBUTING.md for the `nbgv tag` release flow.
- **Trash.** A "Trash" entry in the pane footer lists soft-deleted snips from
  across every CLI. Each can be **Restored** (returned to its CLI) or **Deleted
  permanently** (after confirmation). Previously, deleting a snip moved it to
  trash but it then vanished with no way to recover or purge it.
- **Configurable backup retention.** Choose how many timestamped store backups
  to keep (default 20) from Settings → "Backups to keep". The count is honoured
  on the next write-triggered backup, with no restart required.
- **Delete a CLI.** A "Delete CLI" action on the CLI view removes an empty CLI
  after confirmation. Deletion uses must-be-empty semantics: a CLI with visible
  (non-trashed) snips can't be deleted until those snips are removed. The CLI's
  icon asset and any leftover trashed snips are cleaned up with it.

## [0.1.0-alpha.1] - 2026-05-30

First packaged release. Cuts an alpha to exercise the release pipeline
end-to-end and put the v1 feature set in front of real users.

### Added
- **Unhandled-exception logging.** Exceptions that previously vanished into
  WinUI's "Continue?" debugger dialog (and would have crashed the process
  outside the debugger) are now captured to
  `%LOCALAPPDATA%\Snipdeck\logs\unhandled.log`. The handlers cover XAML
  `Application.UnhandledException`, unobserved `Task` exceptions, and
  `AppDomain.UnhandledException` for completeness. `IPathProvider` gains
  `LogsDirectory`. The log rotates at 5 MB.

### Fixed
- Tray icon initialisation crash, take two
  (`ArgumentException: Argument 'picture' must be a picture that can be
  used as a Icon.`): once H.NotifyIcon could read the file (PR #15) it
  passed the bytes to `new System.Drawing.Icon(stream, size)`, which
  only accepts ICO-format input. The tray service now wraps the
  identicon PNG in a minimal ICO container (modern Windows accepts
  PNG-in-ICO) and writes `%LOCALAPPDATA%\Snipdeck\tray-icon.ico`.
- Tray icon initialisation crash (`NullReferenceException` inside
  `H.NotifyIcon.ImageExtensions.ToStream(Uri)`): H.NotifyIcon resolves
  `TaskbarIcon.IconSource` by reading `BitmapImage.UriSource`, but the
  tray service was loading the identicon via `BitmapImage.SetSourceAsync`
  from an in-memory stream — pixels populated, URI null. The service now
  persists the identicon to `%LOCALAPPDATA%\Snipdeck\tray-icon.ico` and
  loads the `BitmapImage` from that URI, which is the shape H.NotifyIcon
  expects.
- First-run crash on startup (`RPC_E_WRONG_THREAD` / `0x8001010E`):
  `ShellViewModel.LoadAsync` resumed on a thread-pool thread after loading
  the store and then mutated `CliChoices`, an `ObservableCollection` already
  bound to the CLI switcher. WinRT rejects cross-thread collection-changed
  marshalling. The await now stays on the UI thread.

### Added — Phase 6: Settings page + Velopack updater
- **Settings page becomes editable.** Theme switches live (System/Light/Dark
  apply immediately via `IThemeApplier` → `MainWindow`'s content tree). Close
  behaviour (Hide-to-tray vs Exit) persists to `AppConfig` as you change it.
  Global hotkey is shown read-only for now — rebinding lands later.
- **About expander** is populated from real assembly metadata: name,
  `InformationalVersion`, and copyright (from `Directory.Build.props`).
- **Manual update check** via `IUpdateService` (Velopack-backed,
  `WindowsUpdateService`). Points at the GitHub releases for this repo,
  catches dev-build / network-failure cases, and exposes a Check / Apply
  pair on the Settings page.
- `SettingsViewModel` now lives in the DI container as transient — a fresh
  instance is resolved each time the Settings entry is clicked so reading
  config state is always current.

### Added — Phase 5: Platform services
- **Global hotkey** via Win32 `RegisterHotKey`. Default Ctrl+Alt+S; pressed
  anywhere brings the existing Snipdeck window to the foreground.
  `WindowsHotkeyService` subclasses the main window's WndProc with
  `SetWindowSubclass` to catch `WM_HOTKEY`. `HotkeyModifiers` map straight
  through to `MOD_*` constants (intentional alignment from Phase 1).
- **Tray icon** via `H.NotifyIcon`. Left-click brings the window forward,
  right-click shows a context menu with **Show Snipdeck** and **Exit**.
- **Close-to-tray** behaviour: when `AppConfig.CloseBehaviour` is
  `HideToTray`, `AppWindow.Closing` is cancelled and the window hides. The
  tray's **Exit** flips an internal flag and lets the next close pass
  through cleanly.
- **`IFilePickerService`** abstraction (App-side `WindowsFilePickerService`)
  centralises file-picker setup. `CliEditorDialog` no longer pokes Win32 itself
  — it consumes the abstraction.
- **CLI cards now render uploaded icons** (when `Cli.IconRef` is set), falling
  back to identicons when not. `Identicon` control gains an `IconRef`
  dependency property and resolves the absolute path via
  `IIconAssetStorage`.

### Added — Phase 4: Authoring + parameter-fill flyout
- Snip card actions are now live: **Copy** opens a parameter-fill
  `ContentDialog` (or copies the template directly when the Snip has no
  parameters), **Edit** opens a Snip editor, **Delete** soft-trashes after
  confirmation, the **star** toggles favourite.
- Copying a Snip bumps `UsageCount` and `LastUsedAt` (drives the most-used
  list on Home).
- New Snip / New CLI buttons on Home and the CLI view. Edit CLI button on the
  CLI view header.
- `SnipEditorDialog` — title, command template (monospace, multi-line),
  description, tags (comma-separated), parameter rows with type
  (Text / Choice), default, and options. Add/remove parameters inline.
- `CliEditorDialog` — name + icon picker. Picked images are normalised to a
  256² centre-square PNG via `WindowsIconNormaliser` (`Windows.Graphics.Imaging`)
  and stored under `<data>/icons/<cli-id>.png` by `IconAssetStorage`.
- `ParameterFillDialog` — one input per parameter (TextBox for `Text`,
  ComboBox for `Choice`), with a live preview of the resolved command and
  the Copy button disabled until the template is fully resolved.
- New Core abstractions: `IClipboardService`, `IIconNormaliser`,
  `IIconAssetStorage`, `IShellInteractions`.
- New Core view models: `ParameterFillViewModel`, `ParameterInputViewModel`,
  `SnipEditorViewModel`, `ParameterEditorRowViewModel`, `CliEditorViewModel`.
- `ShellViewModel` gains `CopySnipCommand`, `EditSnipCommand`,
  `DeleteSnipCommand`, `ToggleFavouriteCommand`, `NewSnipCommand`,
  `NewCliCommand`, `EditCurrentCliCommand`, `SelectCliCommand`.
- App-side implementations: `WindowsClipboardService`,
  `WindowsIconNormaliser`, `WindowsShellInteractions`.
- Clicking a CLI card on Home navigates into that CLI (was: switcher-only).
- 18 new Core unit tests cover the new view models and the command flow
  (clipboard write, usage bumping, soft-delete, favourite toggle,
  new-CLI-with-icon).

### Added — Phase 3: Shell + read-only browse
- `ShellViewModel` owns the cross-cutting shell state: CLI switcher choices,
  current search text, selected tag (with an "All" sentinel for clean
  filter-off semantics), and the active content view model.
- `HomeViewModel` builds the home view's CLI cards (alphabetical, with snip
  counts) and the most-used Snips list (top 6 by `UsageCount`, then
  `LastUsedAt` desc; hidden when nothing's been used yet).
- `CliViewModel` exposes the filtered Snip list for a single CLI, favourites
  bubbled to the top.
- `SettingsViewModel` stub — populates the About expander; real settings UI
  arrives in Phase 6.
- `SnipFilter` pure helpers: case-insensitive search across title / template /
  tags, tag filter, trash exclusion, `DistinctTagsFor` for the pane tag list.
- `IdenticonService` (Jdenticon-net) — generates deterministic identicon PNG
  bytes seeded off `Cli.Id` so renaming a CLI doesn't change its icon.
- `ShellPage` (WinUI): `NavigationView` with a custom pane header
  (`AutoSuggestBox` search + CLI switcher `ComboBox`), pane body tag list,
  pane footer Settings button, content area driven by a
  `ShellContentTemplateSelector` that picks the right `DataTemplate` based on
  the current content view-model type.
- Custom user controls: `Identicon` (dependency-property-driven, lazy image
  load), `CliCard` (identicon + name + snip count), `SnipCard` (title,
  monospace template preview, tag chips, favourite star, disabled
  Copy/Edit/Delete buttons with "Phase 4/5" tooltips).
- Settings page stub uses `SettingsCard` / `SettingsExpander` with About as the
  last expander; About shows app name, copyright, and version (the version
  string falls back to the assembly's `InformationalVersion` until Phase 6
  wires Nerdbank.GitVersioning).
- `MainWindow` now hosts the `ShellPage` in its content row; the custom title
  bar and Mica backdrop carry over from Phase 2.
- Converters: `BoolToVisibilityConverter`, `CountToVisibilityConverter`.

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
