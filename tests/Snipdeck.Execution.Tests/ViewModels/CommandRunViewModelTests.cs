using System.Text;

using Snipdeck.Execution.Engine;
using Snipdeck.Execution.Models;
using Snipdeck.Execution.Tests.Support;
using Snipdeck.Execution.ViewModels;

namespace Snipdeck.Execution.Tests.ViewModels
{
    public class CommandRunViewModelTests
    {
        private static readonly Guid _snipId = Guid.NewGuid();
        private static readonly Guid _cliId = Guid.NewGuid();

        private static CommandSpec Spec(string resolved = "echo hi") =>
            new(new ShellLaunch("sh", ["-c", resolved]), "/tmp", resolved, "Bash (bash -lc)");

        private static CommandRunViewModel LiveVm(
            FakeCommandRunner runner,
            FakeCommandHistoryStore history,
            FakeClock clock,
            FakeClipboardService clipboard,
            long maxBytes = 1_000_000,
            int retention = 10)
        {
            return new CommandRunViewModel(
                "Deploy", _snipId, _cliId, Spec(), runner, history,
                new InlineDispatcher(), clock, clipboard, retention, maxBytes);
        }

        [Fact]
        public async Task StartAsync_forwards_chunks_and_stores_clean_transcript()
        {
            var chunks = new[]
            {
                Encoding.UTF8.GetBytes("\u001b[32mhello\u001b[0m"),
                Encoding.UTF8.GetBytes(" world\r\n"),
            };
            var runner = new FakeCommandRunner(chunks, exitCode: 0);
            var history = new FakeCommandHistoryStore();
            var clipboard = new FakeClipboardService();
            var vm = LiveVm(runner, history, new FakeClock(), clipboard);

            var received = new List<byte[]>();
            vm.OutputReceived += received.Add;

            await vm.StartAsync();

            Assert.Equal(2, received.Count);
            Assert.Equal(RunStatus.Finished, vm.Status);
            Assert.Equal(0, vm.ExitCode);
            Assert.True(vm.IsSuccess);

            var entry = Assert.Single(history.Entries);
            Assert.Equal("hello world", entry.CleanedOutput);
            Assert.False(entry.Cancelled);
            Assert.Equal(10, history.LastRetentionPerSnip);
            Assert.Equal(_snipId, entry.SnipId);
            Assert.Equal(_cliId, entry.CliId);

            Assert.NotNull(entry.RawStreamGz);
            var raw = Encoding.UTF8.GetString(GzipUtil.Decompress(entry.RawStreamGz!));
            Assert.Equal("\u001b[32mhello\u001b[0m world\r\n", raw);
        }

        [Fact]
        public async Task StartAsync_truncates_capture_beyond_the_cap()
        {
            var runner = new FakeCommandRunner([Encoding.UTF8.GetBytes("abcdefghij")]);
            var history = new FakeCommandHistoryStore();
            var vm = LiveVm(runner, history, new FakeClock(), new FakeClipboardService(), maxBytes: 5);

            await vm.StartAsync();

            var entry = Assert.Single(history.Entries);
            Assert.True(entry.OutputTruncated);
            Assert.True(vm.OutputTruncated);
            Assert.StartsWith("abcde", entry.CleanedOutput);
            Assert.Contains("truncated", entry.CleanedOutput, StringComparison.Ordinal);
        }

        [Fact]
        public async Task StartAsync_marks_run_cancelled_when_the_runner_is_cancelled()
        {
            var runner = new FakeCommandRunner([Encoding.UTF8.GetBytes("partial")], throwCancellation: true);
            var history = new FakeCommandHistoryStore();
            var vm = LiveVm(runner, history, new FakeClock(), new FakeClipboardService());

            await vm.StartAsync();

            Assert.Equal(RunStatus.Cancelled, vm.Status);
            Assert.Null(vm.ExitCode);
            var entry = Assert.Single(history.Entries);
            Assert.True(entry.Cancelled);
        }

        [Fact]
        public void SendInput_forwards_bytes_to_the_runner()
        {
            var runner = new FakeCommandRunner([]);
            var vm = LiveVm(runner, new FakeCommandHistoryStore(), new FakeClock(), new FakeClipboardService());

            vm.SendInput([1, 2, 3]);

            Assert.Equal([1, 2, 3], Assert.Single(runner.WrittenInput));
        }

        [Fact]
        public void ResizeTerminal_forwards_to_the_runner()
        {
            var runner = new FakeCommandRunner([]);
            var vm = LiveVm(runner, new FakeCommandHistoryStore(), new FakeClock(), new FakeClipboardService());

            vm.ResizeTerminal(100, 40);

            Assert.Equal((100, 40), runner.LastResize);
        }

        [Fact]
        public async Task CopyCommand_copies_the_resolved_command()
        {
            var clipboard = new FakeClipboardService();
            var vm = LiveVm(new FakeCommandRunner([]), new FakeCommandHistoryStore(), new FakeClock(), clipboard);

            await vm.CopyCommandCommand.ExecuteAsync(null);

            Assert.Equal("echo hi", clipboard.LastText);
        }

        [Fact]
        public void Replay_constructor_decompresses_raw_stream_and_reflects_status()
        {
            var entry = new CommandHistoryEntry
            {
                SnipId = _snipId,
                CliId = _cliId,
                ResolvedCommand = "echo hi",
                ExitCode = 0,
                Cancelled = false,
                CleanedOutput = "raw",
                DurationMs = 1500,
                RawStreamGz = GzipUtil.Compress(Encoding.UTF8.GetBytes("\u001b[31mraw\u001b[0m")),
            };

            var vm = new CommandRunViewModel("History", entry, new FakeClipboardService());

            Assert.Equal(RunStatus.Finished, vm.Status);
            Assert.True(vm.IsFinished);
            Assert.Equal(0, vm.ExitCode);
            Assert.Equal(1500, vm.DurationMs);
            Assert.True(vm.CanRunAgain);
            Assert.False(vm.CanCancel);
            Assert.NotNull(vm.ReplayOutput);
            Assert.Equal("\u001b[31mraw\u001b[0m", Encoding.UTF8.GetString(vm.ReplayOutput!));
        }

        [Fact]
        public void Replay_constructor_falls_back_to_cleaned_output_when_no_raw_stream()
        {
            var entry = new CommandHistoryEntry
            {
                SnipId = _snipId,
                ResolvedCommand = "x",
                Cancelled = true,
                CleanedOutput = "plain transcript",
                RawStreamGz = null,
            };

            var vm = new CommandRunViewModel("History", entry, new FakeClipboardService());

            Assert.Equal(RunStatus.Cancelled, vm.Status);
            Assert.Null(vm.ExitCode);
            Assert.Equal("plain transcript", Encoding.UTF8.GetString(vm.ReplayOutput!));
        }

        [Fact]
        public void RunAgain_raises_with_the_snip_id()
        {
            var entry = new CommandHistoryEntry { SnipId = _snipId, ResolvedCommand = "x", CleanedOutput = "y" };
            var vm = new CommandRunViewModel("History", entry, new FakeClipboardService());

            Guid? raised = null;
            vm.RunAgainRequested += (_, id) => raised = id;
            vm.RunAgainCommand.Execute(null);

            Assert.Equal(_snipId, raised);
        }
    }
}
