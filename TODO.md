# TODO

A backlog of ideas worth building but not yet scheduled. Not a commitment — the
canonical list of *parked* features is the "Not building yet" section in
[CLAUDE.md](CLAUDE.md).

> **Command execution** — run a Snip in its configured shell, watch it live with full
> terminal fidelity (colours, spinners, progress bars, interactive prompts), and keep a
> clean, searchable run history — shipped in **v1.0.0**. The items below are the
> follow-ons that were deliberately deferred from that first cut.

---

## Per-CLI / per-Snip environment variables (including secrets)

Command execution shipped with per-CLI shell, executable path and working directory,
but **not** environment variables. Add an `EnvironmentVariables` collection of
`(Name, Value, IsSecret)` entries on `Cli`, with a Snip able to add or override entries
on top. The child process inherits the OS environment and gets these merged on top
(additions / overrides, not a replacement) — the runner already builds the child
environment, so this slots in there.

- Non-secret values: stored verbatim in the JSON store, shown in the CLI editor in
  cleartext.
- Secret values: un-parks the "secret / masked parameters" item in `CLAUDE.md`. Store
  via Windows DPAPI (`ProtectedData.Protect`, current-user scope) so the ciphertext is
  bound to the local Windows account; the editor masks the value behind a "Show" toggle
  and never logs it.
- Trade-off (the right shape): DPAPI-protected values don't survive a cross-machine sync
  of the data folder, so secrets are re-entered per machine. The alternative — plaintext
  secrets in a synced JSON document — is the wrong one.

## Labelled executions + sticky labels

Each run can carry a free-form list of `Labels` — short strings like `INC-4567`,
`staging-rollback`, `pr-1234-debug`. Labels belong to the **execution record**, not the
Snip (a Snip accumulates runs labelled with many different incident / ticket / deployment
identifiers over time).

- Surface them as chips on the History list and make them searchable (`label:INC-4567`
  filter, or just free text — the SQLite history store already does substring search).
- The Run dialog gains a small labels input next to the resolved-command preview; the
  History view gets an inline editor to label a past run after the fact.
- **Sticky labels** (the productivity multiplier): a chip in the shell lets the user
  *pin* one or more labels for the current investigation. While pinned, every Run
  pre-fills with them — pin `INC-4567` once during an incident and every run is tagged.

## Per-Snip shell / working-directory override editor

The model (`Snip.ShellOverride`, `Snip.WorkingDirectoryOverride`) and the runner already
support per-Snip overrides of the CLI's shell and working directory, but there's no
editor UI yet — only CLI-level configuration is exposed. Add an "advanced" section to the
Snip editor so a Snip can override its CLI's shell / working directory.

## Run safety hardening (optional)

The dry-run preview (resolved command + shell + working directory, shown before every
run) is the current safety gate. Worth considering if usage warrants it: a per-Snip
first-run confirmation, an allow-list, or a "this Snip has been edited since you last ran
it" warning — Snipdeck running an unreviewed `rm -rf` is a category of incident worth
guarding against.
