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
