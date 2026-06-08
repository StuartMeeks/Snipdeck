using System.Globalization;
using System.Text;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Snipdeck.Core.Abstractions;
using Snipdeck.Execution.Abstractions;
using Snipdeck.Execution.Engine;
using Snipdeck.Execution.Models;

namespace Snipdeck.Execution.ViewModels
{
    /// <summary>
    /// Drives one command run and, in replay mode, a past run. Coordinates lifecycle,
    /// status and the history write; it never references WinUI. Output bytes are pushed
    /// to the view (which feeds them to the terminal) via <see cref="OutputReceived"/>,
    /// and keyboard input flows back through <see cref="SendInput"/>.
    /// </summary>
    public sealed partial class CommandRunViewModel : ObservableObject, IDisposable
    {
        private readonly IClipboardService _clipboard;

        // Live-run dependencies (null in replay mode).
        private readonly CommandSpec? _spec;
        private readonly ICommandRunner? _runner;
        private readonly ICommandHistoryStore? _historyStore;
        private readonly IDispatcher? _dispatcher;
        private readonly IClock? _clock;
        private readonly int _retentionPerSnip;
        private readonly long _maxCapturedBytes;
        private readonly CancellationTokenSource _cts = new();

        private DateTimeOffset _startedAt;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRunning))]
        [NotifyPropertyChangedFor(nameof(IsFinished))]
        [NotifyPropertyChangedFor(nameof(CanCancel))]
        [NotifyPropertyChangedFor(nameof(CanRunAgain))]
        [NotifyPropertyChangedFor(nameof(StatusSummary))]
        public partial RunStatus Status { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasExitCode))]
        [NotifyPropertyChangedFor(nameof(IsSuccess))]
        [NotifyPropertyChangedFor(nameof(StatusSummary))]
        public partial int? ExitCode { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusSummary))]
        public partial int? DurationMs { get; set; }

        /// <summary>Live-run constructor. The run begins when <see cref="StartAsync"/> is called.</summary>
        public CommandRunViewModel(
            string snipTitle,
            Guid snipId,
            Guid cliId,
            CommandSpec spec,
            ICommandRunner runner,
            ICommandHistoryStore historyStore,
            IDispatcher dispatcher,
            IClock clock,
            IClipboardService clipboard,
            int retentionPerSnip,
            long maxCapturedBytes)
        {
            ArgumentNullException.ThrowIfNull(spec);
            Title = snipTitle;
            SnipId = snipId;
            CliId = cliId;
            _spec = spec;
            ResolvedCommand = spec.ResolvedCommand;
            ShellDisplay = spec.ShellDisplay;
            WorkingDirectory = spec.WorkingDirectory;
            _runner = runner;
            _historyStore = historyStore;
            _dispatcher = dispatcher;
            _clock = clock;
            _clipboard = clipboard;
            _retentionPerSnip = retentionPerSnip;
            _maxCapturedBytes = maxCapturedBytes;
            Status = RunStatus.Pending;
        }

        /// <summary>Replay constructor for a stored run; no process is spawned.</summary>
        public CommandRunViewModel(string snipTitle, CommandHistoryEntry entry, IClipboardService clipboard)
        {
            ArgumentNullException.ThrowIfNull(entry);
            _clipboard = clipboard;
            Title = snipTitle;
            SnipId = entry.SnipId;
            CliId = entry.CliId;
            ResolvedCommand = entry.ResolvedCommand;
            ShellDisplay = string.Empty;
            WorkingDirectory = string.Empty;
            ExitCode = entry.Cancelled ? null : entry.ExitCode;
            DurationMs = entry.DurationMs;
            OutputTruncated = entry.OutputTruncated;
            ReplayOutput = entry.RawStreamGz is { Length: > 0 }
                ? GzipUtil.Decompress(entry.RawStreamGz)
                : Encoding.UTF8.GetBytes(entry.CleanedOutput);
            Status = entry.Cancelled ? RunStatus.Cancelled : RunStatus.Finished;
        }

        /// <summary>Raised on the UI thread for each chunk of live output; the view feeds it to the terminal.</summary>
        public event Action<byte[]>? OutputReceived;

        /// <summary>Raised when the user asks to re-run; the argument is the Snip to run again.</summary>
        public event EventHandler<Guid>? RunAgainRequested;

        /// <summary>The Snip's title, shown in the run header.</summary>
        public string Title { get; }

        /// <summary>The Snip this run belongs to.</summary>
        public Guid SnipId { get; }

        /// <summary>The CLI this run belongs to.</summary>
        public Guid CliId { get; }

        /// <summary>The exact command line that was (or will be) executed.</summary>
        public string ResolvedCommand { get; }

        /// <summary>Human-readable shell description (empty in replay mode).</summary>
        public string ShellDisplay { get; }

        /// <summary>Working directory for the run (empty in replay mode).</summary>
        public string WorkingDirectory { get; }

        /// <summary>True when the stored output was truncated at the capture cap.</summary>
        public bool OutputTruncated { get; private set; }

        /// <summary>In replay mode, the raw bytes to write into a fresh terminal on load.</summary>
        public byte[]? ReplayOutput { get; }

        /// <summary>True while the process is running.</summary>
        public bool IsRunning => Status == RunStatus.Running;

        /// <summary>True once the run has finished or been cancelled.</summary>
        public bool IsFinished => Status is RunStatus.Finished or RunStatus.Cancelled;

        /// <summary>Whether the Cancel action is available.</summary>
        public bool CanCancel => Status == RunStatus.Running;

        /// <summary>Whether the Run-again action is available.</summary>
        public bool CanRunAgain => IsFinished && SnipId != Guid.Empty;

        /// <summary>Whether an exit code is known (false while running or when cancelled).</summary>
        public bool HasExitCode => ExitCode.HasValue;

        /// <summary>True when the run exited with code 0.</summary>
        public bool IsSuccess => ExitCode == 0;

        /// <summary>A one-line status for the run header (running, exit code + duration, or cancelled).</summary>
        public string StatusSummary => Status switch
        {
            RunStatus.Pending => "Starting…",
            RunStatus.Running => "Running…",
            RunStatus.Cancelled => "Cancelled",
            RunStatus.Finished => DurationMs is { } ms
                ? string.Create(CultureInfo.InvariantCulture, $"Exit {ExitCode} · {FormatDuration(ms)}")
                : string.Create(CultureInfo.InvariantCulture, $"Exit {ExitCode}"),
            _ => string.Empty,
        };

        /// <summary>Runs the command, streaming output and persisting the run when it ends.</summary>
        public async Task StartAsync()
        {
            if (_spec is null || _runner is null || _historyStore is null || _dispatcher is null || _clock is null)
            {
                throw new InvalidOperationException("This view model was created for replay and cannot start a run.");
            }

            if (Status != RunStatus.Pending)
            {
                return;
            }

            Status = RunStatus.Running;
            _startedAt = _clock.UtcNow;

            var captured = new List<byte>();
            var truncated = false;
            var cancelled = false;

            try
            {
                await foreach (var chunk in _runner.RunAsync(_spec, _cts.Token).ConfigureAwait(false))
                {
                    Capture(captured, chunk, ref truncated);
                    var forward = chunk;
                    _dispatcher.Enqueue(() => OutputReceived?.Invoke(forward));
                }
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            cancelled = cancelled || _cts.IsCancellationRequested;

            var rawArray = captured.ToArray();
            var cleaned = VtTextProcessor.Process(rawArray);
            if (truncated)
            {
                cleaned += "\n\n[output truncated at the capture limit]";
            }

            var finishedAt = _clock.UtcNow;
            var duration = (int)Math.Max(0, (finishedAt - _startedAt).TotalMilliseconds);
            var exit = _runner.LastExitCode;

            var entry = new CommandHistoryEntry
            {
                CliId = CliId,
                SnipId = SnipId,
                ResolvedCommand = ResolvedCommand,
                ExecutedAt = _startedAt,
                DurationMs = duration,
                ExitCode = cancelled ? -1 : exit,
                Cancelled = cancelled,
                CleanedOutput = cleaned,
                RawStreamGz = rawArray.Length > 0 ? GzipUtil.Compress(rawArray) : null,
                OutputTruncated = truncated,
            };

            await _historyStore.AddAsync(entry, _retentionPerSnip, CancellationToken.None).ConfigureAwait(false);

            _dispatcher.Enqueue(() =>
            {
                OutputTruncated = truncated;
                DurationMs = duration;
                ExitCode = cancelled ? null : exit;
                Status = cancelled ? RunStatus.Cancelled : RunStatus.Finished;
            });
        }

        /// <summary>Forwards terminal keystrokes to the running process's input.</summary>
        public void SendInput(byte[] data)
        {
            if (_runner is null || data is null || data.Length == 0)
            {
                return;
            }

            _ = _runner.WriteInputAsync(data, _cts.Token).AsTask();
        }

        /// <summary>Resizes the pseudo-terminal to match the rendered terminal.</summary>
        public void ResizeTerminal(int columns, int rows) => _runner?.Resize(columns, rows);

        private static string FormatDuration(int ms) =>
            ms < 1000
                ? string.Create(CultureInfo.InvariantCulture, $"{ms} ms")
                : string.Create(CultureInfo.InvariantCulture, $"{ms / 1000.0:0.0} s");

        private void Capture(List<byte> captured, byte[] chunk, ref bool truncated)
        {
            if (truncated)
            {
                return;
            }

            if (captured.Count + chunk.Length <= _maxCapturedBytes)
            {
                captured.AddRange(chunk);
                return;
            }

            var room = (int)Math.Max(0, _maxCapturedBytes - captured.Count);
            if (room > 0)
            {
                captured.AddRange(chunk[..room]);
            }

            truncated = true;
        }

        [RelayCommand]
        private void Cancel()
        {
            if (Status == RunStatus.Running)
            {
                _cts.Cancel();
            }
        }

        [RelayCommand]
        private void RunAgain()
        {
            if (CanRunAgain)
            {
                RunAgainRequested?.Invoke(this, SnipId);
            }
        }

        [RelayCommand]
        private async Task CopyCommandAsync()
        {
            await _clipboard.SetTextAsync(ResolvedCommand).ConfigureAwait(false);
        }

        /// <summary>Cancels any in-flight run and releases the cancellation source.</summary>
        public void Dispose()
        {
            if (!_cts.IsCancellationRequested && Status == RunStatus.Running)
            {
                _cts.Cancel();
            }

            _cts.Dispose();
        }
    }
}
