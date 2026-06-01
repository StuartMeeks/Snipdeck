# TODO

A backlog of ideas worth building but not yet scheduled. Not a commitment — the
canonical list of *parked* v1 features is the "Out of scope for v1" section in
[CLAUDE.md](CLAUDE.md).

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

## Icon picker — follow-ups beyond the tags first cut

The visual glyph picker shipped for **tag** icons (PR #40): a searchable
`GridView` of `FontIcon`s, a "Choose…" button per tag row, the free-text field
kept as an escape hatch, and a curated catalogue in the App's `appsettings.json`
(re-read on each open) grounded against the official Segoe Fluent Icons list.
What's left for a later pass:

- **A glyph option for CLI icons.** CLIs are image-upload + identicon-fallback
  today; let them optionally pick a glyph too, reusing the same picker and
  catalogue. This was explicitly out of the first cut.
- **Catalogue size / search depth.** The shipped set is 50 curated glyphs with
  substring search over name, keywords and code point — plenty at this size. If
  it grows toward the full ~1.5k font, revisit fuzzy search and lean harder on
  virtualisation. The catalogue is just data in `appsettings.json`, so growing
  it needs no code change.
- **Durable catalogue location.** `appsettings.json` sits in the install
  directory, so a Velopack update overwrites user edits. If customising the
  catalogue becomes a real use case, move it (or an override) to `LocalAppData`
  alongside the rest of app config.
