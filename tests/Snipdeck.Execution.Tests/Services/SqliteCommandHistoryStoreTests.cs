using Microsoft.Data.Sqlite;

using Snipdeck.Execution.Models;
using Snipdeck.Execution.Services;

namespace Snipdeck.Execution.Tests.Services
{
    public sealed class SqliteCommandHistoryStoreTests : IDisposable
    {
        private readonly string _directory;
        private readonly SqliteCommandHistoryStore _store;
        private readonly DateTimeOffset _baseTime = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

        public SqliteCommandHistoryStoreTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "snipdeck-history-tests", Guid.NewGuid().ToString("N"));
            _store = new SqliteCommandHistoryStore(Path.Combine(_directory, "history.db"));
        }

        private CommandHistoryEntry Entry(
            Guid snipId,
            string command = "pl-app deploy",
            string output = "done",
            int minutesAfterBase = 0,
            bool cancelled = false,
            bool truncated = false,
            byte[]? rawGz = null)
        {
            return new CommandHistoryEntry
            {
                Id = Guid.NewGuid(),
                CliId = Guid.NewGuid(),
                SnipId = snipId,
                ResolvedCommand = command,
                ParameterValuesJson = "{\"env\":\"prod\"}",
                ExecutedAt = _baseTime.AddMinutes(minutesAfterBase),
                DurationMs = 1234,
                ExitCode = cancelled ? -1 : 0,
                Cancelled = cancelled,
                CleanedOutput = output,
                RawStreamGz = rawGz,
                OutputTruncated = truncated,
            };
        }

        [Fact]
        public async Task Added_run_round_trips_through_query_and_get()
        {
            var snip = Guid.NewGuid();
            var entry = Entry(snip, rawGz: [1, 2, 3, 4], truncated: true);

            await _store.AddAsync(entry, retentionPerSnip: 50, cancellationToken: TestContext.Current.CancellationToken);

            var all = await _store.QueryAsync(cancellationToken: TestContext.Current.CancellationToken);
            var stored = Assert.Single(all);
            Assert.Equal(entry.Id, stored.Id);
            Assert.Equal(entry.CliId, stored.CliId);
            Assert.Equal(snip, stored.SnipId);
            Assert.Equal("pl-app deploy", stored.ResolvedCommand);
            Assert.Equal("{\"env\":\"prod\"}", stored.ParameterValuesJson);
            Assert.Equal(entry.ExecutedAt, stored.ExecutedAt);
            Assert.Equal(1234, stored.DurationMs);
            Assert.Equal(0, stored.ExitCode);
            Assert.False(stored.Cancelled);
            Assert.Equal("done", stored.CleanedOutput);
            Assert.True(stored.OutputTruncated);
            Assert.Equal([1, 2, 3, 4], stored.RawStreamGz);

            var fetched = await _store.GetAsync(entry.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(fetched);
            Assert.Equal(entry.Id, fetched!.Id);
        }

        [Fact]
        public async Task Get_returns_null_for_unknown_id()
        {
            Assert.Null(await _store.GetAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Query_returns_newest_first()
        {
            var snip = Guid.NewGuid();
            await _store.AddAsync(Entry(snip, command: "old", minutesAfterBase: 0), 50, TestContext.Current.CancellationToken);
            await _store.AddAsync(Entry(snip, command: "middle", minutesAfterBase: 5), 50, TestContext.Current.CancellationToken);
            await _store.AddAsync(Entry(snip, command: "newest", minutesAfterBase: 10), 50, TestContext.Current.CancellationToken);

            var all = await _store.QueryAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(["newest", "middle", "old"], all.Select(e => e.ResolvedCommand));
        }

        [Fact]
        public async Task Query_filters_by_snip()
        {
            var snipA = Guid.NewGuid();
            var snipB = Guid.NewGuid();
            await _store.AddAsync(Entry(snipA, command: "a-cmd"), 50, TestContext.Current.CancellationToken);
            await _store.AddAsync(Entry(snipB, command: "b-cmd"), 50, TestContext.Current.CancellationToken);

            var onlyA = await _store.QueryAsync(snipId: snipA, cancellationToken: TestContext.Current.CancellationToken);
            var stored = Assert.Single(onlyA);
            Assert.Equal("a-cmd", stored.ResolvedCommand);
        }

        [Fact]
        public async Task Query_search_matches_command_or_output_case_insensitively()
        {
            var snip = Guid.NewGuid();
            await _store.AddAsync(Entry(snip, command: "kubectl describe pod", output: "Running", minutesAfterBase: 0), 50, TestContext.Current.CancellationToken);
            await _store.AddAsync(Entry(snip, command: "git status", output: "clean tree", minutesAfterBase: 1), 50, TestContext.Current.CancellationToken);

            var byCommand = await _store.QueryAsync(search: "KUBECTL", cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("kubectl describe pod", Assert.Single(byCommand).ResolvedCommand);

            var byOutput = await _store.QueryAsync(search: "CLEAN", cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("git status", Assert.Single(byOutput).ResolvedCommand);
        }

        [Fact]
        public async Task Add_prunes_oldest_runs_beyond_retention_per_snip()
        {
            var snip = Guid.NewGuid();
            for (var i = 0; i < 5; i++)
            {
                await _store.AddAsync(Entry(snip, command: $"run-{i}", minutesAfterBase: i), retentionPerSnip: 3, cancellationToken: TestContext.Current.CancellationToken);
            }

            var all = await _store.QueryAsync(snipId: snip, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(["run-4", "run-3", "run-2"], all.Select(e => e.ResolvedCommand));
        }

        [Fact]
        public async Task Retention_is_scoped_per_snip()
        {
            var snipA = Guid.NewGuid();
            var snipB = Guid.NewGuid();
            for (var i = 0; i < 4; i++)
            {
                await _store.AddAsync(Entry(snipA, command: $"a-{i}", minutesAfterBase: i), retentionPerSnip: 2, cancellationToken: TestContext.Current.CancellationToken);
                await _store.AddAsync(Entry(snipB, command: $"b-{i}", minutesAfterBase: i), retentionPerSnip: 2, cancellationToken: TestContext.Current.CancellationToken);
            }

            Assert.Equal(2, (await _store.QueryAsync(snipId: snipA, cancellationToken: TestContext.Current.CancellationToken)).Count);
            Assert.Equal(2, (await _store.QueryAsync(snipId: snipB, cancellationToken: TestContext.Current.CancellationToken)).Count);
        }

        [Fact]
        public async Task Retention_of_zero_or_less_skips_pruning()
        {
            var snip = Guid.NewGuid();
            for (var i = 0; i < 4; i++)
            {
                await _store.AddAsync(Entry(snip, command: $"run-{i}", minutesAfterBase: i), retentionPerSnip: 0, cancellationToken: TestContext.Current.CancellationToken);
            }

            Assert.Equal(4, (await _store.QueryAsync(snipId: snip, cancellationToken: TestContext.Current.CancellationToken)).Count);
        }

        [Fact]
        public async Task Delete_removes_a_single_run()
        {
            var snip = Guid.NewGuid();
            var keep = Entry(snip, command: "keep", minutesAfterBase: 1);
            var drop = Entry(snip, command: "drop", minutesAfterBase: 0);
            await _store.AddAsync(keep, 50, TestContext.Current.CancellationToken);
            await _store.AddAsync(drop, 50, TestContext.Current.CancellationToken);

            await _store.DeleteAsync(drop.Id, TestContext.Current.CancellationToken);

            var all = await _store.QueryAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("keep", Assert.Single(all).ResolvedCommand);
        }

        [Fact]
        public async Task Clear_removes_all_runs()
        {
            var snip = Guid.NewGuid();
            await _store.AddAsync(Entry(snip), 50, TestContext.Current.CancellationToken);
            await _store.AddAsync(Entry(snip, minutesAfterBase: 1), 50, TestContext.Current.CancellationToken);

            await _store.ClearAsync(TestContext.Current.CancellationToken);

            Assert.Empty(await _store.QueryAsync(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Null_raw_stream_round_trips_as_null()
        {
            var snip = Guid.NewGuid();
            var entry = Entry(snip, rawGz: null);
            await _store.AddAsync(entry, 50, TestContext.Current.CancellationToken);

            var stored = await _store.GetAsync(entry.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Null(stored!.RawStreamGz);
        }

        public void Dispose()
        {
            _store.Dispose();
            SqliteConnection.ClearAllPools();
            try
            {
                if (Directory.Exists(_directory))
                {
                    Directory.Delete(_directory, recursive: true);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup of the temp database.
            }
        }
    }
}
