# Snipdeck — Command Execution & History Feature

Read `CLAUDE.md` first. This document adds a specific feature on top of that
foundation. All Core/App boundary rules, naming conventions, and working
agreements from `CLAUDE.md` apply here.

---

## Feature goal

Allow the user to execute a resolved Snip command directly from Snipdeck, watch
it run with full live terminal output (colours, spinners, progress bars), and have
a clean plain-text record stored in history when it finishes. The live view must
be faithful — the command should behave as if it is running in a real terminal.
The stored history must be clean — no ANSI escape codes, no cursor-movement
artefacts, no spinner frames stacked on top of each other.

---

## Architecture: two concurrent consumers of one byte stream

```
PTY (Porta.Pty — ConPTY)
    │
    ├──► WebView2 + xterm.js      live view  — raw VT bytes, full fidelity
    │
    └──► in-memory buffer
              │
              └──► VtOutputProcessor          on exit → clean text → history
```

The PTY makes the child process believe it is talking to a real terminal.
`Console.IsOutputRedirected` stays `false`, so tools built on Spectre.Console,
Progress, etc. render fully. The raw VT byte stream is split: every chunk is
forwarded to xterm.js for live display AND appended to a buffer. When the
process exits, `VtOutputProcessor` collapses the buffer into clean text for
storage.

**Start with the pipe path first.** Many .NET CLIs (including those built on
Spectre.Console) self-demote when `Console.IsOutputRedirected` is `true` —
they drop colour and animations automatically. Test `pl-app`, `mpt-app`, and
`inv-app` via `PipeCommandRunner` before reaching for the PTY. If they
self-demote, `VtOutputProcessor` still strips residual ANSI and the pipe path
is sufficient. Add `PtyCommandRunner` only when a specific CLI proves it
does not self-demote.

---

## New domain model

Add to `Snipdeck.Core/Models/`:

```csharp
public sealed class CommandHistoryEntry
{
    public Guid          Id            { get; init; } = Guid.NewGuid();
    public Guid          CliId         { get; init; }   // which CLI
    public Guid          SnipId        { get; init; }   // which Snip
    public string        ResolvedCommand { get; init; } = string.Empty;
    public DateTimeOffset ExecutedAt   { get; init; }
    public int           DurationMs    { get; init; }
    public int           ExitCode      { get; init; }
    public string        CleanedOutput { get; init; } = string.Empty;
    // Optional: store raw bytes (Base64) for replay fidelity — omit in v1
}
```

`CommandHistoryEntry` is persisted in the same JSON store as snips, under a
top-level `"history"` array. Use the same atomic write pattern (write to temp
file, rename).

---

## New abstractions  (Snipdeck.Core/Abstractions/)

```csharp
// Runs a command, streams output chunks, returns exit code on completion.
// Implementations must not reference WinUI types.
public interface ICommandRunner
{
    IAsyncEnumerable<byte[]> RunAsync(
        string command,
        CancellationToken cancellationToken = default);

    // Exit code available after the async enumerable completes.
    int LastExitCode { get; }
}

public interface ICommandHistoryStore
{
    Task<IReadOnlyList<CommandHistoryEntry>> GetAllAsync();
    Task AddAsync(CommandHistoryEntry entry);
    Task DeleteAsync(Guid id);
    Task ClearAllAsync();
}
```

---

## New service implementations

### PipeCommandRunner  →  `Snipdeck.Core/Services/`
- Uses `System.Diagnostics.Process` with `RedirectStandardOutput = true` and
  `RedirectStandardError = true`.
- Merges stdout and stderr into one stream (write stderr chunks too — the user
  needs to see error output in the live view).
- Targets `net10.0` — no Windows dependency. Unit-testable.
- When `Console.IsOutputRedirected` is `true`, Spectre.Console self-demotes;
  this path is sufficient for tools that respect that convention.

### PtyCommandRunner  →  `Snipdeck.App/Services/`
- Uses **Porta.Pty** (`dotnet add Snipdeck.App package Porta.Pty`).
- Wraps Windows ConPTY so the child process sees a real terminal.
  `Console.IsOutputRedirected` stays `false`; progress bars and spinners
  render fully.
- Lives in `App` because it has a Windows platform dependency.
- Implements `ICommandRunner` — the view model does not know which runner
  is in use; DI provides it.

### VtOutputProcessor  →  `Snipdeck.Core/Engine/`
- Pure static logic. No I/O, no dependencies. The most important test target
  for this whole feature.
- Input: raw VT byte string (UTF-8). Output: clean plain text.
- Algorithm:

  ```
  1. Decode bytes to string (UTF-8).
  2. Simulate a simple line buffer:
       - Split on \r and \n, tracking current-line index and column position.
       - \r  → reset column to 0 (overwrite mode — spinner frames cancel out).
       - \n  → advance to next line.
       - \x1b[2K   → clear the current line buffer.
       - \x1b[nA   → move cursor up n lines (multi-line progress bar erasure).
       - \x1b[nB   → move cursor down n lines.
       - \x1b[nD   → move cursor back n columns.
       - Any other \x1b[…  sequence → discard (don't emit to buffer).
  3. After simulation, strip remaining ANSI colour/style codes via regex:
       \x1b\[[0-9;]*[A-Za-z]
  4. Trim blank lines left by erased progress bar frames.
  5. Return joined lines.
  ```

- Commit raw VT output from your actual CLIs as test fixtures
  (`tests/Snipdeck.Core.Tests/Fixtures/`) and write xUnit tests against them.
  This is where a silent bug (mangled command stored in history) hurts most.

### CommandHistoryStore  →  `Snipdeck.Core/Services/`
- Implements `ICommandHistoryStore`.
- Reads/writes the `"history"` array in the JSON store.
- Same atomic write pattern as the snip store (write temp → rename).

---

## Live view: WebView2 + xterm.js  (Snipdeck.App)

NuGet: `Microsoft.Web.WebView2` (add to `Snipdeck.App` if not already present).

Embed an HTML page as a build asset (`Assets/terminal.html`). The page
initialises an xterm.js terminal that fills the container:

```html
<!DOCTYPE html>
<html>
<head>
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/xterm/css/xterm.css"/>
  <script src="https://cdn.jsdelivr.net/npm/xterm/lib/xterm.js"></script>
  <style>html,body,#terminal{margin:0;padding:0;background:#0d0d0d;height:100vh}</style>
</head>
<body>
  <div id="terminal"></div>
  <script>
    const term = new Terminal({ theme: { background: '#0d0d0d' }, convertEol: true });
    term.open(document.getElementById('terminal'));
    window.chrome.webview.addEventListener('message', e => {
      const bytes = Uint8Array.from(atob(e.data), c => c.charCodeAt(0));
      term.write(bytes);
    });
  </script>
</body>
</html>
```

Stream PTY bytes to it from C#:

```csharp
// In the view or code-behind that owns the WebView2 instance
private async Task ForwardChunkAsync(byte[] chunk)
{
    var b64 = Convert.ToBase64String(chunk);
    await _webView.CoreWebView2.ExecuteScriptAsync(
        $"window.chrome.webview.postMessage('{b64}')");
    // Note: direction is App → WebView2, use PostWebMessageAsString
}
```

Actually use `PostWebMessageAsString` on the C# side and receive via
`window.chrome.webview.addEventListener('message', ...)` on the JS side.
Base64-encode chunks to avoid escaping issues with raw VT bytes crossing
the interop boundary.

Initialise xterm.js after `CoreWebView2InitializationCompleted` fires — not
in the constructor.

---

## Execution flow (view model, Snipdeck.Core/ViewModels/)

```
User clicks "Run" on a Snip
    │
    ├─ Create CommandHistoryEntry (CliId, SnipId, ResolvedCommand, ExecutedAt = now)
    ├─ Start stopwatch
    ├─ foreach byte[] chunk in _commandRunner.RunAsync(resolvedCommand)
    │       ├─ Append chunk to in-memory buffer (List<byte>)
    │       └─ Raise OutputChunkReceived event → App wires to WebView2 forwarder
    │
    └─ On completion
         ├─ Stop stopwatch → DurationMs
         ├─ ExitCode = _commandRunner.LastExitCode
         ├─ CleanedOutput = VtOutputProcessor.Process(buffer)
         └─ await _historyStore.AddAsync(entry)
```

The view model raises `OutputChunkReceived` as an event (or an
`IObservable<byte[]>`). The view (App layer) subscribes and forwards to
WebView2. The view model never references WebView2 directly — it stays in
`Core`.

---

## New NuGet packages

| Package | Project |
|---|---|
| `Porta.Pty` | `Snipdeck.App` |
| `Microsoft.Web.WebView2` | `Snipdeck.App` |

---

## What NOT to do

- **Do not put `PtyCommandRunner` in `Core`** — it has a Windows dependency
  (ConPTY). It belongs in `App`.
- **Do not write VT bytes directly into `CleanedOutput`** — always run through
  `VtOutputProcessor` first.
- **Do not initialise WebView2 in the constructor** — wait for
  `CoreWebView2InitializationCompleted`.
- **Do not block the UI thread** while the command runs — `RunAsync` is
  `IAsyncEnumerable`; await it on a background context and marshal UI updates
  via `IDispatcher`.
- **Do not store raw bytes in history by default** — `CleanedOutput` is the
  contract. Raw bytes are optional and deferred to a future version.

---

## Test targets (priority order)

1. `VtOutputProcessor` — unit tests against real CLI output fixtures.
   Cover: spinner (CR overwrite), multi-line progress bar (cursor-up + erase),
   colour-only output (ANSI strip only), plain output (passthrough).
2. `CommandHistoryStore` — round-trip (add, read back, delete).
3. `PipeCommandRunner` — integration test against a known simple command.
