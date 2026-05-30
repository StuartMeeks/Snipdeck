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
  and the parameter values that were used. New entries append to a per-Snip
  history log.
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

- **Environment variables.** Some CLIs need env (auth tokens, region pins).
  Probably a per-CLI key/value list, masked in the UI (touches the parked
  "secret parameters" decision).
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
- **Backup retention configurable.** Plumbing: `BackupService` takes
  retention at construction time today; either re-create it on the relevant
  config change or have it read from `AppConfig` lazily.
- **CLI delete.** Settle cascade semantics — must-be-empty vs trash-all-child-snips —
  before wiring the UI.
- **Nerdbank.GitVersioning.** Right now `InformationalVersion` falls back to
  the assembly's compile-time version. NBGV would give us a real git-tag-derived
  string at build time (`v1.2.3+gabcdef0`).
- **Markdown rendering for Snip descriptions.** Stored as plain text right
  now; render via a markdown control on the parameter-fill / detail view.
- **Trash UI.** Soft-deleted Snips currently just vanish from the views.
  Need a "Trash" entry in the pane footer that lists trashed Snips with a
  restore action and a hard-delete option.
