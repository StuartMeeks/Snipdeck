using System.Globalization;

using Microsoft.Data.Sqlite;

using Snipdeck.Execution.Abstractions;
using Snipdeck.Execution.Models;

namespace Snipdeck.Execution.Services
{
    /// <summary>
    /// SQLite-backed <see cref="ICommandHistoryStore"/>. Execution output is not
    /// "small" — a single command can emit tens of KB and a power user accumulates
    /// thousands of runs — so history lives in its own database, separate from the
    /// JSON snip store, with indexed per-Snip queries and substring search.
    /// </summary>
    public sealed class SqliteCommandHistoryStore : ICommandHistoryStore, IDisposable
    {
        private readonly string _connectionString;
        private readonly SemaphoreSlim _initGate = new(1, 1);
        private bool _initialised;

        /// <summary>Creates a store backed by the SQLite database at <paramref name="databasePath"/>.</summary>
        public SqliteCommandHistoryStore(string databasePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
            DatabasePath = databasePath;
            _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        }

        /// <summary>The path to the SQLite history database file.</summary>
        public string DatabasePath { get; }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CommandHistoryEntry>> QueryAsync(
            string? search = null,
            Guid? snipId = null,
            CancellationToken cancellationToken = default)
        {
            await EnsureInitialisedAsync(cancellationToken).ConfigureAwait(false);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            var sql = "SELECT " + _columns + " FROM runs";
            var clauses = new List<string>();

            if (snipId.HasValue)
            {
                clauses.Add("snip_id = $snip");
                _ = command.Parameters.AddWithValue("$snip", snipId.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                clauses.Add("(resolved_command LIKE $search OR cleaned_output LIKE $search)");
                _ = command.Parameters.AddWithValue("$search", "%" + search.Trim() + "%");
            }

            if (clauses.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", clauses);
            }

            sql += " ORDER BY executed_at DESC";
            command.CommandText = sql;

            var results = new List<CommandHistoryEntry>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                results.Add(Map(reader));
            }

            return results;
        }

        /// <inheritdoc/>
        public async Task<CommandHistoryEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await EnsureInitialisedAsync(cancellationToken).ConfigureAwait(false);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT " + _columns + " FROM runs WHERE id = $id";
            _ = command.Parameters.AddWithValue("$id", id.ToString());

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Map(reader) : null;
        }

        /// <inheritdoc/>
        public async Task AddAsync(CommandHistoryEntry entry, int retentionPerSnip, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entry);
            await EnsureInitialisedAsync(cancellationToken).ConfigureAwait(false);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (var insert = connection.CreateCommand())
            {
                insert.Transaction = (SqliteTransaction)transaction;
                insert.CommandText =
                    "INSERT INTO runs (id, cli_id, snip_id, resolved_command, parameter_values_json, " +
                    "executed_at, duration_ms, exit_code, cancelled, cleaned_output, raw_stream_gz, output_truncated) " +
                    "VALUES ($id, $cli, $snip, $cmd, $params, $at, $dur, $exit, $cancelled, $clean, $raw, $truncated)";
                _ = insert.Parameters.AddWithValue("$id", entry.Id.ToString());
                _ = insert.Parameters.AddWithValue("$cli", entry.CliId.ToString());
                _ = insert.Parameters.AddWithValue("$snip", entry.SnipId.ToString());
                _ = insert.Parameters.AddWithValue("$cmd", entry.ResolvedCommand);
                _ = insert.Parameters.AddWithValue("$params", (object?)entry.ParameterValuesJson ?? DBNull.Value);
                _ = insert.Parameters.AddWithValue("$at", entry.ExecutedAt.ToString("O", CultureInfo.InvariantCulture));
                _ = insert.Parameters.AddWithValue("$dur", entry.DurationMs);
                _ = insert.Parameters.AddWithValue("$exit", entry.ExitCode);
                _ = insert.Parameters.AddWithValue("$cancelled", entry.Cancelled ? 1 : 0);
                _ = insert.Parameters.AddWithValue("$clean", entry.CleanedOutput);
                _ = insert.Parameters.AddWithValue("$raw", (object?)entry.RawStreamGz ?? DBNull.Value);
                _ = insert.Parameters.AddWithValue("$truncated", entry.OutputTruncated ? 1 : 0);
                _ = await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            if (retentionPerSnip > 0)
            {
                await using var prune = connection.CreateCommand();
                prune.Transaction = (SqliteTransaction)transaction;
                // Keep the newest N runs for this Snip; delete the rest.
                prune.CommandText =
                    "DELETE FROM runs WHERE snip_id = $snip AND id NOT IN (" +
                    "SELECT id FROM runs WHERE snip_id = $snip ORDER BY executed_at DESC LIMIT $keep)";
                _ = prune.Parameters.AddWithValue("$snip", entry.SnipId.ToString());
                _ = prune.Parameters.AddWithValue("$keep", retentionPerSnip);
                _ = await prune.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await EnsureInitialisedAsync(cancellationToken).ConfigureAwait(false);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM runs WHERE id = $id";
            _ = command.Parameters.AddWithValue("$id", id.ToString());
            _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await EnsureInitialisedAsync(cancellationToken).ConfigureAwait(false);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM runs";
            _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Releases the initialisation gate.</summary>
        public void Dispose() => _initGate.Dispose();

        private const string _columns =
            "id, cli_id, snip_id, resolved_command, parameter_values_json, executed_at, " +
            "duration_ms, exit_code, cancelled, cleaned_output, raw_stream_gz, output_truncated";

        private async Task EnsureInitialisedAsync(CancellationToken cancellationToken)
        {
            if (_initialised)
            {
                return;
            }

            await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_initialised)
                {
                    return;
                }

                var directory = Path.GetDirectoryName(DatabasePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    _ = Directory.CreateDirectory(directory);
                }

                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText =
                    "CREATE TABLE IF NOT EXISTS runs (" +
                    "id TEXT PRIMARY KEY, " +
                    "cli_id TEXT NOT NULL, " +
                    "snip_id TEXT NOT NULL, " +
                    "resolved_command TEXT NOT NULL, " +
                    "parameter_values_json TEXT NULL, " +
                    "executed_at TEXT NOT NULL, " +
                    "duration_ms INTEGER NOT NULL, " +
                    "exit_code INTEGER NOT NULL, " +
                    "cancelled INTEGER NOT NULL, " +
                    "cleaned_output TEXT NOT NULL, " +
                    "raw_stream_gz BLOB NULL, " +
                    "output_truncated INTEGER NOT NULL);" +
                    "CREATE INDEX IF NOT EXISTS ix_runs_snip ON runs (snip_id, executed_at);";
                _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                _initialised = true;
            }
            finally
            {
                _ = _initGate.Release();
            }
        }

        private static CommandHistoryEntry Map(SqliteDataReader reader)
        {
            return new CommandHistoryEntry
            {
                Id = Guid.Parse(reader.GetString(0)),
                CliId = Guid.Parse(reader.GetString(1)),
                SnipId = Guid.Parse(reader.GetString(2)),
                ResolvedCommand = reader.GetString(3),
                ParameterValuesJson = reader.IsDBNull(4) ? null : reader.GetString(4),
                ExecutedAt = DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                DurationMs = reader.GetInt32(6),
                ExitCode = reader.GetInt32(7),
                Cancelled = reader.GetInt32(8) != 0,
                CleanedOutput = reader.GetString(9),
                RawStreamGz = reader.IsDBNull(10) ? null : reader.GetFieldValue<byte[]>(10),
                OutputTruncated = reader.GetInt32(11) != 0,
            };
        }
    }
}
