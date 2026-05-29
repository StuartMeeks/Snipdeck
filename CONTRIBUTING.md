# Contributing to Snipdeck

Thanks for your interest. This guide covers the working agreement, the
non-negotiables, and how to get a change to "green".

## Prerequisites

- **.NET 10 SDK** (`dotnet --list-sdks` should show a `10.0.x` SDK).
- **Windows 11** to build and run the WinUI 3 head (`Snipdeck.App`). The
  `Snipdeck.Core` project and its tests are pure `net10.0` and build/run on any
  platform the SDK supports.
- For releases: Visual Studio 2022 17.x with the Windows App SDK workload, or
  its CLI equivalents.

## Build and test

```bash
# Restore + build everything
dotnet build

# Build Core only (works on Linux / macOS)
dotnet build src/Snipdeck.Core

# Run Core tests
dotnet test tests/Snipdeck.Core.Tests
```

On a non-Windows machine, restoring the `Snipdeck.App` project requires
`EnableWindowsTargeting=true`:

```bash
EnableWindowsTargeting=true dotnet restore
```

You will not be able to *build* the App project off Windows; the XAML compiler
is a Windows-only binary. CI (`.github/workflows/ci.yml`) builds Core on Ubuntu
and the full solution on Windows.

## Non-negotiables

These are checked into project configuration and will fail the build, not just
politely warn:

- **`TreatWarningsAsErrors=true`** — every compiler warning, analyser warning,
  and IDE-style violation is a build error. Fix the underlying issue rather than
  adding `#pragma warning disable`. If you genuinely need to suppress something,
  do it narrowly (a single line, with a comment explaining why) — not
  project-wide.
- **`.editorconfig`** — committed at the repo root, taken verbatim from
  `StuartMeeks/NextIteration.SpectreConsole.SelfUpdate`. Format your code to
  match (`dotnet format` is your friend). Brace style, `using` ordering, naming
  (private fields are `_camelCase`), and analyser severities are all set there.
- **Curly braces, always.** Even single-line `if` / `else` / `for` / `foreach` /
  `while` / `using` bodies use braces. The editorconfig enforces this
  (`csharp_prefer_braces = true:warning`), but treat it as a rule of thumb you
  follow without needing the analyser to tell you.
- **British English** in all user-facing copy, documentation, and
  `// these comments`. (Code identifiers follow .NET API conventions, which are
  American — leave `Colour` out of API surfaces and use `Color` if it's an SDK
  type.)
- **Enterprise-professional tone** in product copy. No emojis in UI strings.

## Architecture rules

These come from `CLAUDE.md` — read it before writing code that crosses the
project boundary.

- `Snipdeck.Core` is UI-free (`net10.0`, not `net10.0-windows`). It contains the
  domain, the substitution engine, JSON store, settings, backup, view models,
  and every service interface.
- `Snipdeck.App` is the WinUI 3 head and the only project allowed to depend on
  WinUI / Windows / Win32 / Windows App SDK types.
- The dependency direction is one-way: **`App → Core`**. Never the reverse.
- View models reference only Core abstractions — no `Frame`, `NavigationView`,
  `DispatcherQueue`, etc. If you find yourself reaching for a WinUI type inside
  a view model, route it through an interface in `Snipdeck.Core/Abstractions/`
  and add the implementation in `Snipdeck.App`.

## Packages

- Versions are centralised in `Directory.Packages.props` (Central Package
  Management). Add new packages with `<PackageVersion>` there and reference them
  in the consuming csproj with `<PackageReference Include="..." />` (no
  `Version` attribute).
- Use the **latest stable** version unless there is a documented reason
  otherwise. Don't quietly pin to an older patch.

## Tests

- Tests live in `tests/Snipdeck.Core.Tests/` and use xUnit. The naming
  convention is `Method_or_subject_then_expected_behaviour()` — underscores are
  fine, the test-only `NoWarn` block in the csproj accepts it.
- Cover the substitution engine and the JSON store heavily; these are the bits
  where a silent bug hurts most.
- Use `FakeClock` for any service that depends on `IClock`.

## Commits and pull requests

- One logical change per commit. The body explains *why*, not *what*.
- Every user-visible change (feature, behaviour change, bug fix, removed
  capability) gets a line under `## [Unreleased]` in `CHANGELOG.md`
  (Keep-a-Changelog format).
- Keep `README.md` in sync with what the app actually does today — not what we
  intend to ship later.
- PR titles are short, present-tense imperative
  (e.g. `Add tray icon implementation`).

## Releases

Releases are driven by git tags:

- `v1.2.3` — stable release.
- `v1.2.3-rc.1`, `v1.2.3-beta.4`, etc. — pre-release. The hyphen in the tag
  marks the GitHub release as a pre-release and feeds Velopack's channel name.

Pushing a matching tag triggers `.github/workflows/release.yml`, which builds
`Snipdeck.App`, packs it with Velopack, and attaches the artefacts to a new
GitHub Release. Don't push tags from feature branches — release from `master`.

## Asking for direction

When a product decision is genuinely ambiguous, ask rather than guess. Open an
issue or raise it in the PR description — it's cheaper than building the wrong
thing.
