# TODO

A backlog of ideas worth building but not yet scheduled. Not a commitment — the
canonical list of *parked* v1 features is the "Out of scope for v1" section in
[CLAUDE.md](CLAUDE.md).

---

## Shared parameter definitions (global and CLI-scoped)

**Problem.** Today every `Snip` carries its own `Parameter` definitions inline.
If twenty Snips all need an `env` Choice dropdown with options
`dev` / `staging` / `prod`, that definition is duplicated twenty times and the
user has to keep them in sync by hand. Same story for any other recurring
token (`region`, `org_id`, `tenant`, etc.).

**Idea.** Let parameter definitions live above the Snip and be *referenced* by
name from individual Snips, with the option to still define a parameter
locally on a Snip when something one-off is needed.

**Sketch.**
- Add a collection of shared `Parameter` definitions, probably at two scopes:
  - **CLI-scoped** — most common case, e.g. an `env` defined once on the
    `pl-app` CLI and used by every `pl-app` Snip.
  - **Global** — for the rare cross-CLI definition (`yes_no`, common flags).
  - Both scopes should be supported; CLI-scoped takes precedence over global
    when names collide.
- A Snip can either:
  - **Reference** a shared parameter by name — it picks up the type, options,
    and default automatically.
  - **Define** a parameter locally — overrides any shared definition with the
    same name *for that Snip only*.
- The substitution engine doesn't change: tokens still resolve from a
  `name → value` dictionary. Only the *definition* moves; resolution is the
  same.

**Open questions** to settle when this gets scheduled:
- Reference by **name** or by **ID**? Names match the `{token}` model and read
  well; IDs survive renames. Probably name-based with a uniqueness constraint
  per scope, plus a rename flow that propagates.
- Where does the UI to manage shared parameters live? A Settings page? A
  flyout from each CLI in the pane header? Probably the latter for CLI-scoped
  and a Settings entry for global.
- Schema migration: additive. New collections on `SnipStoreDocument` (global)
  and `Cli` (CLI-scoped). Existing Snips with local `Parameter` entries keep
  working unchanged.
- Should a Snip's `Parameters` list contain a discriminated union (local
  definition vs. reference) or two parallel lists? A small object with
  `Name` + optional inline definition feels cleanest — absent inline ⇒ resolve
  from shared scope.

This is the single biggest content-quality-of-life feature after v1 ships.

---

## Execute Snips, not just construct them (with per-CLI shell + execution history)

**Problem.** Today Snipdeck is a sophisticated clipboard — it builds the command
and you paste it into your own terminal. That round-trip discards the most
valuable artefact of running a CLI: the *output*. There's no way to ask "what
did this Snip return last time I ran it against staging?", to diff two runs,
or to even tell whether the previous invocation succeeded.

**Idea.** Let Snipdeck *execute* a Snip in its configured shell, stream the
output into a Snipdeck panel as it runs, and persist each execution against
the Snip so the user accumulates a searchable run history.

**Sketch.**

- **Per-CLI shell declaration.** `Cli` gains a `Shell` field (enum:
  `Cmd` / `PowerShell` / `PwshCore` / `Bash` / `Custom`) and an optional
  `CustomShellPath` + `CustomShellArgsTemplate` for the escape hatch.
  Default for new CLIs is whatever the platform considers canonical
  (`PowerShell` on Windows). A Snip can override its CLI's shell when needed.
- **Per-CLI executable path and working directory.** `Cli` gains optional
  `ExecutablePath` and `WorkingDirectory` fields. The CLI editor dialog
  gains a **Browse…** for the executable (re-using `IFilePickerService`)
  and another for the working directory. Both are **optional** — Snipdeck
  must remain useful as a place to author Snips for a CLI that isn't
  installed yet on this machine, or one the user will install later. So:
  - No validation at save time. An empty `ExecutablePath` is the default
    and is fine.
  - When the field *is* set, validation happens **only at Run time**: if
    the path is missing, the Run action shows a friendly "couldn't find
    `<path>` — has it been installed? edit the CLI to fix the path" rather
    than throwing. Copy still works unconditionally.
  - `WorkingDirectory` defaults at runtime to the executable's parent
    directory when `ExecutablePath` is set, otherwise the user's home
    directory. A Snip can override the CLI's working directory.
  - The executable path is not currently used by Copy — but it's worth
    storing now because the user might reasonably expect "show me where
    `pl-app` lives" as a small affordance even before Run lands.
- **Per-CLI environment variables.** `Cli` gains an `EnvironmentVariables`
  collection of `(Name, Value, IsSecret)` entries; a Snip can add or
  override entries on top of its CLI's set. The child process inherits the
  OS environment and gets these merged on top — they're additions /
  overrides, not a replacement.
  - Non-secret values are stored verbatim in the JSON store and shown in
    the editor in cleartext.
  - Secret values flip the parked "secret / masked parameters" item from
    `CLAUDE.md` from "park" to "needed". Store them via Windows DPAPI
    (`ProtectedData.Protect`, current-user scope) so the ciphertext is
    bound to the local Windows account. The editor masks the value behind
    a "Show" toggle and never logs it.
  - Trade-off: DPAPI-protected values won't survive a cross-machine sync
    of the data folder — the user would need to re-enter secrets on each
    machine. That's the right shape; the alternative (plaintext secrets
    in a synced JSON document) is the wrong one.
- **Labelled executions.** Each run can carry a free-form list of `Labels`
  — short strings like `INC-4567`, `staging-rollback`, `pr-1234-debug`.
  Labels are surfaced as chips on the history list and are fully searchable
  (`label:INC-4567` filter, or just free-text). The Run dialog has a small
  "labels" input next to the resolved-command preview; the history view
  has an inline editor so the user can label a past run after the fact.
  - **Sticky labels.** A small affordance in the shell (probably a chip in
    the title bar) lets the user *pin* one or more labels for the current
    investigation. While pinned, every Run pre-fills with those labels.
    Clearing the pin reverts to the empty default. This is the
    productivity multiplier — during an incident, the user pins
    `INC-4567` once and every subsequent Run is automatically tagged.
  - Labels are *not* tags on the Snip itself — they belong to the
    execution record. A single Snip will accumulate runs labelled with
    many different incident / ticket / deployment identifiers over time.
- **A Run action alongside Copy.** Card overflow gains **Run**. Clicking it
  walks the same parameter-fill flow as Copy, but on submit we spawn the
  configured shell with the resolved command instead of copying to the
  clipboard. The parameter-fill dialog is replaced or extended with a
  **dry-run preview** of the exact command line plus a final "Run" button —
  arbitrary execution is a footgun and the user must see the resolved
  string once before it runs.
- **Live output panel.** Output streams into a dedicated content pane —
  probably a new content state alongside `HomeViewModel` / `CliViewModel` —
  with stdout and stderr distinguished (colour, or two lanes), an exit-code
  badge once the process completes, an elapsed timer, and a **Cancel** button
  that kills the process group. Lines arrive via `IDispatcher` so we don't
  mutate UI state off-thread (this fix's lesson, applied early).
- **Execution history per Snip.** Each run captures: `SnipId`,
  `ResolvedCommand`, `StartedAt`, `FinishedAt`, `ExitCode`,
  `Cancelled`, `Stdout`, `Stderr` (or an interleaved stream with timestamps),
  the parameter values that were used, and the `Labels` the run was
  tagged with. New entries append to a per-Snip history log.
- **Searchable history.** A new "History" view (probably a pane footer entry,
  next to Settings) lists executions across all Snips, newest-first, with
  full-text search over the captured output and command line. Clicking an
  entry opens the run in the same output panel as a live run — just with the
  Cancel button disabled and a "Run again" button enabled.

**Storage — the awkward bit.**

The current JSON-store rule (`CLAUDE.md`: "not SQLite, not LiteDB — the data
is small") was made on the assumption of snippet definitions. **Execution
output is not small.** A single `kubectl describe pod` can be tens of KB, and
a power user might rack up thousands of runs.

Options to weigh when this is scheduled:

- **Per-Snip history file** (e.g. `<data>/history/<snip-id>.jsonl`,
  append-only JSONL, one line per run). Keeps the main snip store small and
  fast to load. Pruning is per-file. Cross-Snip search needs to walk all
  files — feasible up to maybe ~10k files; degrades after.
- **Per-CLI history file**. Compromise; smaller fan-out, still naturally
  partitioned, but a single hot CLI's history can grow unbounded.
- **Bring in SQLite specifically for executions** (NOT for the snip store).
  Best query/search story, opens the door to `LIKE` / `MATCH` over output.
  Departs from the "one JSON document" principle — but the principle was
  scoped to *definitions*, not arbitrary observation data. Worth a real
  conversation when this gets picked up; I (Claude) lean toward this option
  for any non-trivial history feature.

Retention defaults probably want to be "last N runs per Snip" (configurable),
matching the existing backup-retention shape.

**Open questions** to settle when scheduled:

- **Safety / confirmation.** First-run confirmation per Snip? An allow-list?
  A "this Snip has been edited since you last ran it" warning? Worth getting
  right — Snipdeck running an unreviewed `rm -rf` is a category of incident
  we don't want to ship.
- **Cancellation semantics.** `Process.Kill(entireProcessTree: true)` covers
  most shells. PowerShell can be sticky; document and test.
- **Cross-platform shells.** `cmd` and `PowerShell` are Windows-only;
  `pwsh` is cross-platform; `bash` on Windows means WSL. The enum needs to
  encode both the shell *and* its launcher.
- **Large outputs.** Cap captured-per-run size (configurable; default ~5 MB?)
  and truncate with a marker. The UI panel can spool to disk for the live
  view if needed.
- **ANSI / colour.** `IShell` should pass output through verbatim; the panel
  needs an ANSI escape parser to render colours and clear-line sequences.
  Avalonia / WinUI both lack a built-in terminal control — either pull a
  community one or render to a `RichTextBlock` with the ANSI stripped /
  interpreted.
- **Streaming-to-history vs. capture-then-write.** Stream-to-history (write
  each chunk as it arrives) means a crashed Snipdeck still leaves a useful
  partial log. Capture-then-write is simpler. Probably stream.

This is a *significant* expansion of Snipdeck's surface area — it crosses
from "snippet manager" into "lightweight runbook executor". Worth doing,
worth doing carefully, and worth a design conversation before the first PR.

---

## Importer: bring snips in from SnipCommand and friends

**Problem.** `CLAUDE.md` already parks SnipCommand import as a "future nicety":
read a SnipCommand JSON, auto-suggest the CLI from the first token of each
command. Worth doing — SnipCommand is the obvious migration path. And there
are other shapes worth ingesting later (VS Code snippets, espanso, Alfred,
TextExpander, ad-hoc shell-history dumps).

**Decision: this is a separate CLI tool, not a feature inside the WinUI app.**

Reasons:

- Import is a rare / one-off operation. It doesn't deserve UI surface area
  in the main app, where it'd compete for visual real estate with everyday
  workflows.
- A CLI tool is cross-platform — useful for users who want to bulk-prep an
  import on a Linux box or in CI before bringing the JSON over to a Windows
  Snipdeck install.
- Adding a new source format (espanso, TextExpander, …) is then a new
  subcommand in the importer, not a new dialog in the main app.
- Distribution is independent: shipped as a `dotnet tool` for trivial
  install (`dotnet tool install -g snipdeck-importer`) and versioned
  separately from the desktop app.
- Spectre.Console.Cli is the established pattern in the wider
  StuartMeeks toolbox — fits naturally.

**Sketch.**

- New project at `tools/Snipdeck.Importer/` in this solution, targeting
  `net10.0` (NOT `-windows`), depending on `Snipdeck.Core`. Re-uses
  `SnipStoreDocument`, `Cli`, `Snip`, `Parameter`, `JsonSnipStore`,
  `BackupService` — exactly the surface Core was carved off for.
- Packaged as a .NET tool (`<PackAsTool>true</PackAsTool>`,
  `ToolCommandName=snipdeck-importer`).
- Subcommand per source: `snipdeck-importer snipcommand <path>`,
  later `snipdeck-importer vscode <path>`, etc. Each source adapter
  implements an `ISnippetSource` that yields `Snip` candidates plus
  a suggested `Cli` name.
- Defaults to **dry-run**: parses the source, prints the planned
  additions (Snips grouped by suggested CLI, parameter summary), exits
  without touching the store. `--write` actually merges into the store.
- `--store <path>` lets the user point at an arbitrary store file;
  defaults to the same path the desktop app uses
  (`IPathProvider`'s default, factored down into Core).
- `--cli <name>` forces every imported Snip into a named CLI, overriding
  the per-command auto-suggestion.
- `--into <cli-name>` is the per-command equivalent — apply only to
  Snips that didn't get a confident auto-suggestion.

**SnipCommand specifics.**

- SnipCommand stores a flat list — Snipdeck groups by CLI. Auto-suggest
  the CLI from the first whitespace-separated token of each command
  (`pl-app orgs list` → `pl-app`). When the leading token is `sudo`,
  `npx`, or similar, peel it off and use the next token.
- SnipCommand uses inline markup: `[sc_choice ...]`, `[sc_variable ...]`.
  Snipdeck uses `{token}` placeholders plus a structured `Parameter[]`
  list. The importer:
  1. Parses the markup, mints a placeholder name (the variable / choice
     name, with collisions disambiguated).
  2. Replaces the inline markup with `{name}` in the command template.
  3. Emits a `Parameter` entry of the right `Type` (Choice with options,
     or Text) into the Snip.
- Description / tags carry across one-for-one where present.

**Merge semantics.**

- Always write a backup of the existing store before modifying it
  (call `BackupService` so the desktop app's retention policy stays in
  charge).
- New Snips always mint fresh GUIDs — SnipCommand IDs aren't reused.
- De-duplication: if a Snip with the same `(Title, CommandTemplate)`
  already exists, skip by default. `--allow-duplicates` opts in to
  importing them anyway.
- New CLIs are created on demand when an imported Snip's suggested CLI
  doesn't already exist in the target store.

**Open questions** to settle when scheduled:

- **Where the docs live.** The importer's README probably belongs in
  the tool's project folder, with a pointer from the desktop app's
  README so users discover it.
- **Velopack interaction.** The desktop app should never be running
  against a store the importer is concurrently rewriting. The atomic
  temp-file-then-rename write the JsonSnipStore already does avoids
  corruption, but a running desktop instance won't *see* the changes
  until it next reloads. Worth surfacing in `--write` output:
  "Snipdeck is running — restart it to see the imported Snips."
- **Adversarial sources.** A SnipCommand JSON crafted to overflow a
  parameter name, embed escape sequences in the command preview, or
  similar — be defensive when parsing. Treat the input as untrusted.
- **Future sources.** VS Code snippets (`*.code-snippets`) and espanso
  (`*.yml`) are the obvious next two. Each has its own placeholder
  syntax to translate. Add them when there's actual demand, not
  speculatively.

---

## Tighten the iteration loop (build/CI feedback)

**Problem.** During the phase build-out and post-release fixes, the
build-and-debug cycle relied heavily on PRs as the feedback loop:
local Linux can't build the `Snipdeck.App` project (the WinUI XAML
compiler is Windows-only), so analyser errors / build breaks only
surface in CI. Each round-trip is a PR, which generates churn and
sometimes ends with main broken (PR #13 was merged with red CI).

**Idea.** Three independent improvements; each is small, all three
together would make the iteration loop tight.

**Sketch.**

- **`EnableWindowsTargeting=true` for local builds.** Adding this to
  `Snipdeck.App.csproj` (or passing as `-p:EnableWindowsTargeting=true`
  on Linux) lets the restore + compile step run on non-Windows hosts.
  The WinUI XAML pass still requires Windows, but most analyser /
  C# compiler rules fire under plain `dotnet build` and would catch
  editorconfig violations (IDE0058, IDE0330, IDE0370, IDE0005 — all
  hit in this session) before the push.
- **Branch protection: require status checks to pass.** Add a rule to
  the existing branch ruleset on `main` that requires the
  `App build (windows)` and `Core build + tests (ubuntu)` checks to be
  green before the Merge button activates. Mechanical guardrail
  against the merged-red scenario.
- **Draft PRs with force-push fixups during iteration.** Convention,
  not config: open PRs as **Draft** while iterating, and amend +
  force-push fixup commits into the original commit instead of stacking
  "fix lint" follow-ups. The final merged history shows one clean
  commit per change, which is what the project's commit log wants.
  Auto-mode currently blocks `git push --force-with-lease` — would
  need an explicit settings.json permission or a one-off approval
  to enable. Force-push to `main` itself stays blocked.

**Sequencing.** Do `EnableWindowsTargeting` first (cheapest, biggest
quality-of-life win for me); then branch protection (one-off setup,
done forever); then adopt the draft-PR convention.

---

## Final UI polish pass

A deliberate sweep of visual / interaction rough edges, done **at the end**
once the feature surface has settled — batching the nitpicks avoids
re-polishing the same screens after every feature lands. Known items so far:

- **Snip card Copy button is too wide.** It currently stretches further than
  it should; size it to its content (or a sensible fixed width) so the card
  action row reads cleanly.
- **"Delete CLI" button should be styled as a danger action.** It's a
  destructive, hard-to-reverse operation — give it the red/danger accent
  (e.g. a danger `Button` style / `Foreground` from the theme palette)
  rather than the neutral default, so it visually distinguishes itself from
  benign actions.
- **Inconsistent button corner radii.** Cancel buttons render with square
  corners while Save / Copy buttons are rounded. Standardise on rounded
  corners for *all* buttons (the dialog `CloseButton` is the likely culprit —
  align it with the themed `CornerRadius` the primary buttons pick up).
- **Consider making the whole Snip card the Copy target.** Rather than a
  dedicated Copy button on the card, let a click anywhere on the card trigger
  the copy / parameter-fill flow. Weigh the trade-offs before committing:
  discoverability and a cleaner card vs. losing an explicit affordance and
  the risk of accidental copies / conflicts with the overflow menu and
  favourite star hit-targets. Decide, then either remove the button or keep
  it.

Add to this list as other cosmetic / interaction snags turn up during
feature work, then knock them out in one pass before a stable cut.

---

## Carried over from the phase stack

These were trimmed out of Phase 4–6 to keep the PRs reviewable. None are
load-bearing for the v1 demo, but they're the obvious next-pulls.

- **Hotkey rebinding UI.** The setting is editable in `AppConfig` already;
  what's missing is a key-capture control on the Settings page and the call
  to `IHotkeyService.TryRegister` after the change. Tooling: a small custom
  `Control` that listens for a single key chord then displays it formatted.
- **Storage path: move / adopt / warn-on-conflict.** Per `CLAUDE.md`, when
  the user changes the storage path we need three flows: move the existing
  store to the new path; adopt a store already at the new path; warn when
  both exist. UI: a "Change…" button next to the read-only path display.
- **Re-enable `PublishTrimmed` once JSON serialisation is trim-safe.**
  Disabled in `Snipdeck.App.csproj` to unblock the first release. To
  turn it back on:
  1. Move `JsonSnipStore` / `JsonSettingsStore` onto
     `JsonSerializerContext` source generation
     (`[JsonSerializable(typeof(SnipStoreDocument))]` etc.) so the
     untyped `Serialize/Deserialize` calls disappear. Removes IL2026.
  2. Audit Jdenticon-net, Microsoft.Windows.SDK.NET and WinRT.Runtime
     trim warnings (IL2104); either suppress per-assembly with
     `<TrimmerRootAssembly>` entries / `[DynamicallyAccessedMembers]`
     attributes, or accept them via targeted
     `<TrimmerSingleWarn>false</TrimmerSingleWarn>` carve-outs.
  3. Flip `PublishTrimmed` back to `True` for Release.

  Payoff is a meaningfully smaller self-contained Velopack package
  (probably ~80 MB instead of ~150–200 MB). Not urgent for alpha but
  worth doing before a stable cut.
