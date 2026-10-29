using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

internal sealed class SqliteMigrationFenceException(string code, string message, Exception cause = null) : InvalidOperationException(message, cause)
{ internal string Code { get; } = code; }

public sealed class SqliteMigrationFenceReconciliationException(Guid migrationId, Exception cause)
    : Exception("Read the exact original native migration fence outcome before another action.", cause), INativeMutationReconciliationRequired
{
    public Guid OperationId { get; } = migrationId;
    public string OriginalTransactionId => string.Empty;
}

internal static class SqliteHolonMigrationFence
{
    internal enum Action { Acquire, Read, Cancel, BeginMutation }
    private sealed record Stored(HolonMigrationFenceRequest Request, Guid Token, HolonMigrationFenceState State, string MutationDigest);

    internal static void AssertUnfenced(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<Guid> ids)
    {
        var selected = ids.ToHashSet();
        foreach (var fence in Held(connection, transaction))
            if (fence.Request.HolonIds.Any(selected.Contains))
                throw new SqliteMigrationFenceException("COSMIC_MIGRATION_RECORD_FENCED", "An affected record is held by a durable migration barrier; no write or new delivery claim is admitted.");
    }

    private static List<Stored> Held(SqliteConnection connection, SqliteTransaction transaction)
    {
        var rows = new List<(string id, string json, string fingerprint, string token, string state, string mutation)>();
        using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText = "SELECT MigrationId,RequestJson,RequestHash,OwnerToken,FenceState,MutationDigest FROM CosmicMigrationFence WHERE FenceState='Held' LIMIT 4097";
            using var reader = query.ExecuteReader();
            while (reader.Read()) rows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5)));
        }
        if (rows.Count > 4096) throw new InvalidDataException("The explicit native held-fence inspection bound is exceeded.");
        if (rows.Count == 0) return new();
        var actual = SqliteStorageIdentity.Read(connection, transaction);
        return rows.Select(r => Validate(r.id, r.json, r.fingerprint, r.token, r.state, actual, r.mutation)).ToList();
    }

    private static Stored Validate(string id, string json, string hash, string token, string state, CosmicStorageIdentity actual, string mutationDigest)
    {
        var request = HolonMigrationFenceRequest.ReadCanonical(json, actual);
        if (id != request.MigrationId.ToString("D") || hash != request.Fingerprint
            || !Guid.TryParseExact(token, "D", out var owner) || owner == Guid.Empty
            || state is not ("Held" or "Cancelled")) throw new InvalidDataException("Native migration fence integrity is invalid.");
        var phase = state == "Cancelled" ? HolonMigrationFenceState.Cancelled : mutationDigest == null ? HolonMigrationFenceState.Held : HolonMigrationFenceState.MutationStarted;
        _ = new HolonMigrationFenceObservation(request, owner, phase, mutationDigest);
        return new(request, owner, phase, mutationDigest);
    }

    internal static OASISResult<HolonMigrationFenceObservation> Execute(string connectionString, HolonMigrationFenceRequest request,
        Action action, Guid token, CancellationToken cancellationToken, string mutationDigest = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool commitAttempted = false, acknowledged = false;
        OASISResult<HolonMigrationFenceObservation> known = null;
        OASISResult<HolonMigrationFenceObservation> Known(Guid owner, HolonMigrationFenceState state, bool saved = false, string digest = null)
            => known = new(new(request, owner, state, digest)) { IsLoaded = !saved, IsSaved = saved };
        try
        {
            if (request == null || request.ExpectedStorage.Authority != ProviderType.SQLLiteDBOASIS
                || action is Action.Cancel or Action.BeginMutation && token == Guid.Empty)
                throw new SqliteMigrationFenceException("COSMIC_MIGRATION_FENCE_OPTIONS_INVALID", "An exact native fence request and cancellation owner are required.");
            if (action == Action.BeginMutation) _ = new HolonMigrationMutationStartRequest(request, token, mutationDigest);
            var settings = new SqliteConnectionStringBuilder(connectionString);
            if (settings.Mode == SqliteOpenMode.Memory || settings.DataSource == ":memory:") throw new InvalidOperationException("A durable existing file is required.");
            settings.Mode = action == Action.Read ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite; settings.Pooling = false;
            using var connection = new SqliteConnection(settings.ToString()); connection.Open();
            using var transaction = connection.BeginTransaction(deferred: action == Action.Read);
            var actual = SqliteStorageIdentity.Read(connection, transaction);
            if (actual.BindingFingerprint != request.ExpectedStorage.BindingFingerprint)
                throw new SqliteMigrationFenceException("COSMIC_MIGRATION_FENCE_STORAGE_MISMATCH", "The native store differs from the reviewed physical binding.");
            Stored stored = null;
            using (var query = connection.CreateCommand())
            {
                query.Transaction = transaction;
                query.CommandText = "SELECT RequestJson,RequestHash,OwnerToken,FenceState,MutationDigest FROM CosmicMigrationFence WHERE MigrationId=$id LIMIT 2";
                query.Parameters.AddWithValue("$id", request.MigrationId.ToString("D"));
                using var reader = query.ExecuteReader();
                if (reader.Read())
                {
                    stored = Validate(request.MigrationId.ToString("D"), reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), actual, reader.IsDBNull(4) ? null : reader.GetString(4));
                    if (reader.Read()) throw new InvalidDataException("Native migration fence identity is not unique.");
                }
            }
            if (stored != null && stored.Request.Fingerprint != request.Fingerprint)
                throw new SqliteMigrationFenceException("COSMIC_MIGRATION_FENCE_SCOPE_MISMATCH", "The migration identity is already bound to another operator, preview or record scope.");
            if (action == Action.Read)
                return Known(stored?.Token ?? Guid.Empty, stored?.State ?? HolonMigrationFenceState.Absent, digest: stored?.MutationDigest);
            if (action == Action.Acquire)
            {
                if (stored?.State == HolonMigrationFenceState.Cancelled)
                    throw new SqliteMigrationFenceException("COSMIC_MIGRATION_FENCE_TERMINAL", "A cancelled fence cannot be reacquired under the same migration identity.");
                if (stored != null) return Known(stored.Token, stored.State, digest: stored.MutationDigest);
                var selected = request.HolonIds.ToHashSet();
                if (Held(connection, transaction).Any(f => f.Request.HolonIds.Any(selected.Contains)))
                    throw new SqliteMigrationFenceException("COSMIC_MIGRATION_FENCE_CONFLICT", "Another migration already holds an overlapping record scope.");
                token = Guid.NewGuid();
                using var insert = connection.CreateCommand(); insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO CosmicMigrationFence(MigrationId,RequestJson,RequestHash,OwnerToken,FenceState) VALUES($id,$json,$hash,$token,'Held')";
                insert.Parameters.AddWithValue("$id", request.MigrationId.ToString("D")); insert.Parameters.AddWithValue("$json", request.CanonicalJson);
                insert.Parameters.AddWithValue("$hash", request.Fingerprint); insert.Parameters.AddWithValue("$token", token.ToString("D"));
                if (insert.ExecuteNonQuery() != 1) throw new InvalidDataException("Native fence acknowledgement is incomplete.");
            }
            else if (action == Action.BeginMutation)
            {
                if (stored == null || stored.Token != token)
                    throw new SqliteMigrationFenceException("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", "Only the exact held fence owner may record mutation start.");
                if (stored.State == HolonMigrationFenceState.Cancelled)
                    throw new SqliteMigrationFenceException("COSMIC_MIGRATION_FENCE_TERMINAL", "A cancelled identity cannot begin mutation.");
                if (stored.State == HolonMigrationFenceState.MutationStarted)
                {
                    if (stored.MutationDigest != mutationDigest) throw new SqliteMigrationFenceException("COSMIC_MIGRATION_MUTATION_INTENT_REUSED", "The durable started intent cannot be replaced.");
                    return Known(token, stored.State, digest: stored.MutationDigest);
                }
                using var start = connection.CreateCommand(); start.Transaction = transaction;
                start.CommandText = "UPDATE CosmicMigrationFence SET MutationDigest=$digest WHERE MigrationId=$id AND OwnerToken=$token AND RequestHash=$scope AND FenceState='Held' AND MutationDigest IS NULL";
                start.Parameters.AddWithValue("$digest", mutationDigest); start.Parameters.AddWithValue("$id", request.MigrationId.ToString("D"));
                start.Parameters.AddWithValue("$token", token.ToString("D")); start.Parameters.AddWithValue("$scope", request.Fingerprint);
                if (start.ExecuteNonQuery() != 1) throw new InvalidDataException("Fence start compare-and-swap did not acknowledge the exact original owner.");
            }
            else
            {
                if (stored == null || stored.Token != token)
                    throw new SqliteMigrationFenceException("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", "Only the exact fence owner may cancel a pre-mutation barrier.");
                if (stored.State == HolonMigrationFenceState.Cancelled) return Known(token, stored.State);
                if (stored.State == HolonMigrationFenceState.MutationStarted)
                    throw new SqliteMigrationFenceException("COSMIC_MIGRATION_MUTATION_ALREADY_STARTED", "Started or uncertain mutation cannot use pre-mutation cancellation.");
                using var cancel = connection.CreateCommand(); cancel.Transaction = transaction;
                cancel.CommandText = "UPDATE CosmicMigrationFence SET FenceState='Cancelled' WHERE MigrationId=$id AND OwnerToken=$token AND FenceState='Held' AND MutationDigest IS NULL";
                cancel.Parameters.AddWithValue("$id", request.MigrationId.ToString("D")); cancel.Parameters.AddWithValue("$token", token.ToString("D"));
                if (cancel.ExecuteNonQuery() != 1) throw new InvalidDataException("Fence ownership changed during cancellation.");
            }
            cancellationToken.ThrowIfCancellationRequested(); commitAttempted = true; transaction.Commit(); acknowledged = true;
            return Known(token, action == Action.Acquire ? HolonMigrationFenceState.Held : action == Action.BeginMutation ? HolonMigrationFenceState.MutationStarted : HolonMigrationFenceState.Cancelled, true, action == Action.BeginMutation ? mutationDigest : null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested && !commitAttempted) { throw; }
        catch (Exception cause)
        {
            if (known != null) { known.IsWarning = true; known.WarningCount = 1; known.Exception = cause;
                known.Message = "Native fence outcome is known, but scope cleanup failed."; return known; }
            var uncertain = commitAttempted && !acknowledged;
            return new() { IsError = true, ErrorCount = 1,
                ErrorCode = uncertain ? "COSMIC_MIGRATION_FENCE_RECONCILIATION_REQUIRED" : cause is SqliteMigrationFenceException fence ? fence.Code : "COSMIC_MIGRATION_FENCE_UNVERIFIED",
                Message = "Native migration fence outcome could not be admitted; read its exact original outcome before proceeding.",
                Exception = uncertain ? new SqliteMigrationFenceReconciliationException(request.MigrationId, cause) : cause };
        }
    }
}
