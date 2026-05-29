# Snipdeck — CLAUDE.md

Read this file first, every session. It is the source of truth for what Snipdeck is,
how it is built, and the decisions already made. If you are about to contradict
something here, stop and raise it rather than quietly diverging.

## What this is

Snipdeck is a native Windows desktop app for managing parameterised CLI command
snippets ("Snips"), organised by the CLI they belong to. You browse a CLI, pick a
Snip, fill its arguments, and copy the resolved command to the clipboard.

It is conceptually inspired by SnipCommand, but with one defining difference:
**the CLI is the top-level organising axis.** SnipCommand has a flat command list;
Snipdeck groups every Snip under exactly one CLI (e.g. `pl-app`, `mpt-app`, `inv-app`).

## Stack & hard constraints

- .NET 10, C#.
- WinUI 3 (Windows App SDK) for the UI.
- **Unpackaged** distribution. This is load-bearing — do not reintroduce MSIX /
  packaged assumptions. Unpackaged gives normal Win32 file access (needed for the
  user-chosen storage/backup directories) and is what the Velopack updater expects.
- Windows 11 is the real target. Mica requires Win11; on Win10 it falls back to a
  solid colour. Do not depend on Mica rendering on Win10.
- MVVM via `CommunityToolkit.Mvvm`. DI via `Microsoft.Extensions.DependencyInjection`.
- Persistence is a single JSON document via `System.Text.Json`. **Not** SQLite, not
  LiteDB — the data is small, and JSON is human-readable and sync-friendly.
- Install + self-update via Velopack, releasing off GitHub releases.

## Architecture

Two projects. The split is deliberate: it keeps the logic testable, keeps the slow
WinUI build off the domain code, and makes UI changes cheap.

- **`Snipdeck.Core`** — targets `net10.0` (**not** `net10.0-windows`). Completely
  UI-free. Contains: domain models, the parameter substitution engine, the JSON
  store, backup, settings, icon resolution, the view models, and **all service
  interfaces**. This is the testable heart of the app.
- **`Snipdeck.App`** — targets `net10.0-windows`. The WinUI 3 head: XAML views,
  app startup, DI wiring, and the Windows/Win32 **implementations** of Core's
  service interfaces.

**Dependency direction is one-way: `App` → `Core`. Never the reverse.**

The pattern is dependency inversion. Every platform-bound capability (clipboard,
global hotkey, tray, file picker, updates, navigation, dispatcher) is an interface
defined in Core and implemented in App, injected via DI. **View models reference
only Core interfaces — never WinUI types** (`Frame`, `NavigationView`,
`DispatcherQueue`, etc.). This keeps Core portable and unit-testable, and it is the
single discipline that preserves the option of a future cross-platform (Avalonia)
head. If you are tempted to reach for a WinUI type inside a view model, that is the
signal to route it through an abstraction instead.

Use folders inside Core as seams, and only promote a folder to its own project when
it genuinely earns it: `Models/`, `Engine/`, `Services/`, `ViewModels/`,
`Abstractions/`.

## Domain model

- **Cli** — immutable `Id` (GUID, minted at creation), `Name`, optional `IconRef`.
  The top-level organising entity.
- **Snip** — `Id`, `CliId` (belongs to exactly one CLI), `Title`,
  `CommandTemplate` (text with `{placeholder}` tokens), `Description` (markdown),
  `Parameters[]`, `Tags[]`, `IsFavourite`, `IsTrash` (soft delete), `UsageCount`,
  `LastUsedAt`.
- **Parameter** — `Name`, `Type` (`Text` | `Choice`), `Options[]` (choice only),
  `Default`.
- **Tag** — scoped within a CLI. Filtering is one tag at a time.

Conventions:

- Templates store simple `{placeholder}` tokens. Parameter **definitions are
  structured and held separately** on the Snip — do **not** use SnipCommand's
  inline markup (`[sc_choice …]` / `[sc_variable …]`).
- The substitution engine resolves `{token}` to the user-supplied value at copy time.
- Icon resolution: if `IconRef` is present, use the user's image; otherwise generate
  an identicon (Jdenticon-net). **Seed the identicon off the immutable `Cli.Id`,
  never the display name** — renaming a CLI must not change its icon, since the whole
  point is recognisability.
- Uploaded icons are **copied into the data folder and normalised** (cap ~256²,
  square crop). Never store a path reference to the user's original file — it breaks
  the moment the data folder moves machines.

## Persistence

- One JSON document (the "store") via `System.Text.Json`.
- The storage directory and the backups directory are both user-configurable.
- **First run** — if the store does not yet exist when the app launches, the app
  seeds it with a single demo CLI called "Examples", containing a handful of
  representative Snips (mix of Text and Choice parameters, tags, a favourite).
  The user can delete it once they're oriented. Do **not** seed the store again
  on subsequent launches.
- **App config is stored separately from the store**, in `LocalAppData`. The store's
  own location cannot live inside the store (chicken-and-egg). The storage path,
  backup settings and theme choice all live in app config.
- **Writes must be crash-safe: write to a temp file, then atomically rename.** Never
  overwrite the store in place — a crash mid-write would corrupt it.
- When the storage path changes at runtime, decide deliberately: move the existing
  store, adopt an existing store already at the new path, or warn on conflict.
- Backup policy: timestamped snapshot of the store on **every successful write**
  and immediately before every Velopack update; retain the last 20, pruning the
  oldest. Backups live in the user-configurable backup directory.

## Shell & UX

- `NavigationView` shell, Mica backdrop, custom title bar
  (`ExtendsContentIntoTitleBar`).
- **PaneHeader**: search box + the CLI switcher (a dropdown, with `All / Home` as the
  top entry).
- **Pane body**: the tag list, single-select, scoped to the selected CLI (shows all
  tags when on Home). Changing the CLI reloads this list.
- **Pane footer**: Settings. About is the **last** `SettingsExpander` inside Settings.
- The content area is state-driven by the switcher:
  - **Home** (`All / Home` selected): the CLI card launcher + most-used Snips.
  - **CLI selected**: the Snip list for that CLI, filtered by the selected tag.
  - Home is not a separate layout — it is the same frame with a different content state.
- **Snip card**: title, monospace template preview, tag chips, favourite star, a Copy
  action, and an overflow menu (edit / delete).
- **Use-path**: Copy opens a parameter-fill flyout — `Text` params render as
  textboxes, `Choice` params as dropdowns — with a live preview of the resolved
  command and a Copy button.
- Settings UI uses the CommunityToolkit `SettingsCard` / `SettingsExpander` controls,
  on a single scrollable page.
- Theme: Light / Dark / **System**, defaulting to System. Applied via `RequestedTheme`
  on the root content element, persisted in app config. Mica follows the theme.
- Prefer `x:Bind` (compiled) over `Binding`. The Snip card is a `DataTemplate`. Keep
  visual tokens in `ResourceDictionaries` so re-skinning is centralised.

## Platform services (interfaces in Core, implementations in App)

- `ISnipStore`, `IBackupService`, `ISettingsStore`
- `IClipboardService`
- `IHotkeyService` — global hotkey, via Win32 `RegisterHotKey` (P/Invoke)
- `ITrayService` — system tray. WinUI has no built-in `NotifyIcon`; use the
  `H.NotifyIcon` library.
- `IFilePickerService`
- `IUpdateService` — a thin Velopack wrapper
- `INavigationService`, `IDispatcher` — so view models stay WinUI-free

## App lifecycle

- **Single instance only** (and therefore a single tray icon). Use the Windows App
  SDK `AppInstance` keyed registration + activation redirection — **not** a named
  mutex.
- The global hotkey summons / foregrounds the single running instance. Default
  binding is **Ctrl+Alt+S**, user-rebindable from Settings.
- **Close-button behaviour** is configurable, defaulting to **hide-to-tray** (the
  process keeps running so the hotkey stays live; explicit Exit lives on the tray
  menu). The opt-out flips it to a hard exit on close.
- **Boot order is strict**: Velopack hook (`VelopackApp.Build().Run()`) → single-instance
  check / redirect → DI container + UI. Velopack must run first so it can intercept
  install/update/uninstall invocations and handle the post-update relaunch.
- Pair Velopack with `Nerdbank.GitVersioning` so the version string maintains itself
  from git tags; surface it on the About page alongside a manual "Check for updates".

## Conventions

- British English spelling in all user-facing copy and documentation.
- Always use curly braces in C#, even for single-line `if` / `else` / loops.
- Enterprise-professional tone in product copy. No emojis in UI copy.
- Licensed under **Apache 2.0** (`LICENSE` at the repo root). New source files do
  not need per-file licence headers; the root licence covers the project.
- When adding or updating a NuGet reference, pull the **latest stable** version
  unless there is a documented reason not to. Don't quietly pin to an older
  patch.
- Every user-visible change — new feature, behaviour change, bug fix, removed
  capability — gets a line under `## [Unreleased]` in `CHANGELOG.md` (Keep a
  Changelog format). `README.md` is the public-facing intro and stays in sync
  with what the app actually does today, not aspirational.

## Out of scope for v1 — do not build speculatively

- **Command-palette quick picker.** In v1 the hotkey simply foregrounds the main
  window. The palette is a strong v2 candidate, not a v1 deliverable.
- **macOS / cross-platform head.** Preserve the *option* via the view-model purity
  discipline above, but do **not** add Avalonia scaffolding now. The existing split
  already buys ~90% of the future-proofing for free.
- **Secret / masked parameters.** Parked. Revisit only if storing credentials in
  Snips becomes a real, stated use case.
- **SnipCommand import** (future nicety): a SnipCommand JSON could be imported, and
  the CLI auto-suggested from the first token of each command string.

## Working agreement

- Keep the `Core` / `App` boundary and the one-way dependency intact.
- Keep the substitution engine and the JSON store covered by unit tests in a Core
  test project — these are the bits where a silent bug hurts most.
- When a product decision is genuinely ambiguous, ask rather than guess.
