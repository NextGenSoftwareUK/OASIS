using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DuckDBOASIS;

/// <summary>Native DuckDB record transport. Record policy and queries live in the shared base.
/// Native per-key conditions do not imply transactions across a whole OASIS graph.</summary>
internal sealed class DuckDbRecordBackend(string connectionString) : IConditionalKeyValueBackend
{
    private const string Table = "oasis_record_store";

    private async Task<DuckDBConnection> Open(CancellationToken token)
    {
        var connection = new DuckDBConnection(connectionString);
        try { await connection.OpenAsync(token); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static void Bind(DuckDBCommand command, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public async Task VerifyAsync(CancellationToken token = default)
    {
        await using var connection = await Open(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM information_schema.tables WHERE table_schema='main' AND table_name IN ('oasis_avatars','oasis_avatar_details','oasis_holons')";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(token)) != 0)
            throw new InvalidOperationException("Legacy DuckDB record tables require an explicit migration; they will not be hidden by a new record store.");
        command.CommandText = $"CREATE TABLE IF NOT EXISTS {Table} (record_key VARCHAR PRIMARY KEY, payload VARCHAR NOT NULL, revision UUID NOT NULL DEFAULT uuid())";
        await command.ExecuteNonQueryAsync(token);
    }

    public async Task<KeyValueObservation?> ReadObservedAsync(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await using var connection = await Open(token);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT payload, CAST(revision AS VARCHAR) FROM {Table} WHERE record_key=$key";
        Bind(command, "key", key);
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) return null;
        var result = new KeyValueObservation(reader.GetString(0), reader.GetString(1));
        if (await reader.ReadAsync(token)) throw new InvalidDataException("DuckDB returned duplicate record identities.");
        return result;
    }

    public async Task<string> GetAsync(string key, CancellationToken token = default)
        => (await ReadObservedAsync(key, token))?.Value!;

    public Task PutAsync(string key, string value, CancellationToken token = default)
        => Write(key, value, null, false, token);
    public Task CreateOnlyAsync(string key, string value, CancellationToken token = default)
        => Write(key, value, null, true, token);
    public Task ReplaceUnchangedAsync(string key, string value, string revision, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        return Write(key, value, revision, false, token);
    }

    private async Task Write(string key, string value, string? revision, bool create, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key); ArgumentNullException.ThrowIfNull(value);
        await using var connection = await Open(token);
        await using var command = connection.CreateCommand();
        command.CommandText = revision != null
            ? $"INSERT INTO {Table}(record_key,payload,revision) SELECT record_key,$value,uuid() FROM {Table} WHERE record_key=$key AND revision=CAST($revision AS UUID) "
                + $"ON CONFLICT(record_key) DO UPDATE SET payload=EXCLUDED.payload, revision=EXCLUDED.revision WHERE {Table}.revision=CAST($revision AS UUID) RETURNING record_key"
            : $"INSERT INTO {Table}(record_key,payload) VALUES($key,$value) ON CONFLICT(record_key) DO "
                + (create ? "NOTHING" : "UPDATE SET payload=EXCLUDED.payload, revision=uuid()") + " RETURNING record_key";
        Bind(command, "key", key); Bind(command, "value", value);
        if (revision != null) Bind(command, "revision", revision);
        var acknowledged = await command.ExecuteScalarAsync(token);
        if (acknowledged == null || acknowledged is DBNull)
        {
            if (create || revision != null) throw new StorageLifecycleConflictException("DuckDB native record condition failed.");
            throw new InvalidDataException("DuckDB write omitted its native acknowledgement.");
        }
        if (!string.Equals(acknowledged.ToString(), key, StringComparison.Ordinal))
            throw new InvalidDataException("DuckDB acknowledged another record identity.");
    }

    public Task DeleteAsync(string key, CancellationToken token = default) => Delete(key, null, token);
    public Task DeleteUnchangedAsync(string key, string revision, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        return Delete(key, revision, token);
    }
    private async Task Delete(string key, string? revision, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await using var connection = await Open(token);
        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {Table} WHERE record_key=$key" + (revision == null ? "" : " AND revision=CAST($revision AS UUID)") + " RETURNING record_key";
        Bind(command, "key", key); if (revision != null) Bind(command, "revision", revision);
        var acknowledged = await command.ExecuteScalarAsync(token);
        if (acknowledged == null || acknowledged is DBNull)
        {
            if (revision != null) throw new StorageLifecycleConflictException("DuckDB native deletion condition failed.");
        }
        else if (!string.Equals(acknowledged.ToString(), key, StringComparison.Ordinal))
            throw new InvalidDataException("DuckDB acknowledged another deleted record.");
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        await using var connection = await Open(token);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT record_key FROM {Table} WHERE starts_with(record_key,$prefix) ORDER BY record_key";
        Bind(command, "prefix", prefix);
        var keys = new List<string>(); await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) keys.Add(reader.GetString(0));
        return keys;
    }
}
