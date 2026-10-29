using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed class MongoMigrationFenceException(string code, string message) : InvalidOperationException(message)
{ internal string Code { get; } = code; }

internal static class MongoHolonMigrationFence
{
    internal enum Action { Acquire, Read, Cancel }
    internal const string Gates = "CosmicMigrationRecordGate", Receipts = "CosmicMigrationFence";
    private const string SchemaId = "cosmic-migration-record-gate-schema-v1";
    private static void ValidateSchema(BsonDocument marker)
    {
        if (marker == null || marker.ElementCount != 2 || marker.GetValue("_id", BsonNull.Value) != SchemaId
            || marker.GetValue("Format", BsonNull.Value) != 1) throw new InvalidDataException("Native migration admission schema is missing or invalid; it was not repaired.");
    }

    internal static void EnsureSchema(IMongoDatabase database) => EnsureSchemaAsync(database).GetAwaiter().GetResult();
    internal static async Task EnsureSchemaAsync(IMongoDatabase database)
    {
        var gates = database.GetCollection<BsonDocument>(Gates);
        var marker = await gates.Find(new BsonDocument("_id", SchemaId)).SingleOrDefaultAsync().ConfigureAwait(false);
        if (marker == null)
        {
            if (await database.GetCollection<BsonDocument>(Receipts).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, new CountOptions { Limit = 1 }).ConfigureAwait(false) != 0)
                throw new InvalidDataException("Retained migration history has lost its admission schema; explicit reconciliation is required.");
            marker = await gates.FindOneAndUpdateAsync(new BsonDocument("_id", SchemaId), new BsonDocument("$setOnInsert", new BsonDocument("Format", 1)),
                new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After }).ConfigureAwait(false);
        }
        ValidateSchema(marker);
        await database.GetCollection<BsonDocument>(Gates).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("FenceId"),
            new CreateIndexOptions { Name = "cosmic_migration_gate_owner_v1" })).ConfigureAwait(false);
        await database.GetCollection<BsonDocument>(Receipts).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("FenceState"),
            new CreateIndexOptions { Name = "cosmic_migration_fence_state_v1" })).ConfigureAwait(false);
    }

    private static async Task RequireSchemaAsync(IMongoDatabase database, IClientSessionHandle session, CancellationToken token)
        => ValidateSchema(await database.GetCollection<BsonDocument>(Gates).Find(session, new BsonDocument("_id", SchemaId))
            .SingleOrDefaultAsync(token).ConfigureAwait(false));

    private static async Task<BsonDocument> TouchAsync(IMongoDatabase database, IClientSessionHandle session, Guid id, CancellationToken token)
    {
        var gate = await database.GetCollection<BsonDocument>(Gates).FindOneAndUpdateAsync(session,
            new BsonDocument("_id", id.ToString("D")), new BsonDocument {
                { "$setOnInsert", new BsonDocument { { "FenceId", "" }, { "FenceHash", "" } } },
                { "$inc", new BsonDocument("Epoch", 1L) } },
            new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After }, token).ConfigureAwait(false);
        if (gate == null || gate["_id"] != id.ToString("D") || !gate.TryGetValue("Epoch", out var epoch) || !epoch.IsInt64 || epoch.AsInt64 < 1
            || !gate.TryGetValue("FenceId", out var fence) || !fence.IsString || !gate.TryGetValue("FenceHash", out var hash) || !hash.IsString
            || (fence.AsString.Length == 0) != (hash.AsString.Length == 0))
            throw new InvalidDataException("Native migration record admission metadata is invalid.");
        return gate;
    }

    /// <summary>Every V2 writer and claim touches the same per-record gate as
    /// acquisition. A snapshot-only fence read would permit write skew.</summary>
    internal static async Task AssertUnfencedAsync(IMongoDatabase database, IClientSessionHandle session,
        IEnumerable<Guid> ids, CancellationToken token)
    {
        await RequireSchemaAsync(database, session, token).ConfigureAwait(false);
        foreach (var id in ids.Distinct().OrderBy(id => id.ToString("D"), StringComparer.Ordinal))
            if ((await TouchAsync(database, session, id, token).ConfigureAwait(false))["FenceId"].AsString.Length != 0)
                throw new MongoMigrationFenceException("COSMIC_MIGRATION_RECORD_FENCED", "An affected record is held by a durable migration barrier.");
    }

    private static HolonMigrationFenceObservation Read(BsonDocument row, HolonMigrationFenceRequest expected)
    {
        if (row == null) return new(expected, Guid.Empty, HolonMigrationFenceState.Absent);
        if (!row.TryGetValue("RequestJson", out var json) || !json.IsString || !row.TryGetValue("RequestHash", out var hash) || !hash.IsString
            || !row.TryGetValue("OwnerToken", out var owner) || !owner.IsString || !Guid.TryParseExact(owner.AsString, "D", out var token) || token == Guid.Empty
            || !row.TryGetValue("FenceState", out var state) || !state.IsString || state.AsString is not ("Held" or "Cancelled"))
            throw new InvalidDataException("Native migration fence receipt is invalid.");
        var original = HolonMigrationFenceRequest.ReadCanonical(json.AsString, expected.ExpectedStorage);
        if (row["_id"] != original.MigrationId.ToString("D") || original.Fingerprint != hash.AsString)
            throw new InvalidDataException("Native migration fence receipt integrity differs.");
        if (original.Fingerprint != expected.Fingerprint)
            throw new MongoMigrationFenceException("COSMIC_MIGRATION_FENCE_SCOPE_MISMATCH", "The migration identity belongs to another exact operator or review scope.");
        return new(expected, token, state.AsString == "Held" ? HolonMigrationFenceState.Held : HolonMigrationFenceState.Cancelled);
    }

    internal static async Task<OASISResult<HolonMigrationFenceObservation>> ExecuteAsync(IMongoDatabase database,
        HolonMigrationFenceRequest request, Action action, Guid owner, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        IClientSessionHandle session = null; OASISResult<HolonMigrationFenceObservation> result = null;
        bool known = false, commitEntered = false, closeEntered = false, cancelled = false; Exception failure = null;
        string sessionId = null; long transaction = 0;
        try
        {
            if (database == null || request == null || request.ExpectedStorage.Authority != Core.Enums.ProviderType.MongoDBOASIS
                || action == Action.Cancel && owner == Guid.Empty)
                throw new MongoMigrationFenceException("COSMIC_MIGRATION_FENCE_OPTIONS_INVALID", "An exact activated Mongo fence scope and owner are required.");
            session = await database.Client.StartSessionAsync(cancellationToken: token).ConfigureAwait(false);
            session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority.With(journal: true)));
            await HolonRepository.ValidateReplicaStorageBindingAsync(database, session, request.ExpectedStorage, token,
                "COSMIC_MIGRATION_FENCE_STORAGE").ConfigureAwait(false);
            await RequireSchemaAsync(database, session, token).ConfigureAwait(false);
            var receipts = database.GetCollection<BsonDocument>(Receipts);
            var stored = Read(await receipts.Find(session, new BsonDocument("_id", request.MigrationId.ToString("D")))
                .SingleOrDefaultAsync(token).ConfigureAwait(false), request);
            if (stored.State == HolonMigrationFenceState.Absent && await database.GetCollection<BsonDocument>(Gates)
                .Find(session, new BsonDocument("FenceId", request.MigrationId.ToString("D"))).Limit(1).AnyAsync(token).ConfigureAwait(false))
                throw new InvalidDataException("Native record ownership exists without its fence receipt.");
            bool changed = false;
            if (action == Action.Read) result = new(stored) { IsLoaded = true };
            else if (action == Action.Acquire && stored.State == HolonMigrationFenceState.Held) result = new(stored) { IsLoaded = true };
            else if (action == Action.Acquire)
            {
                if (stored.State == HolonMigrationFenceState.Cancelled)
                    throw new MongoMigrationFenceException("COSMIC_MIGRATION_FENCE_TERMINAL", "A cancelled migration fence cannot be reacquired.");
                owner = Guid.NewGuid();
                foreach (var id in request.HolonIds)
                {
                    var gate = await TouchAsync(database, session, id, token).ConfigureAwait(false);
                    if (gate["FenceId"].AsString.Length != 0)
                        throw new MongoMigrationFenceException("COSMIC_MIGRATION_FENCE_CONFLICT", "Another migration holds an overlapping record scope.");
                    var updated = await database.GetCollection<BsonDocument>(Gates).UpdateOneAsync(session,
                        new BsonDocument { { "_id", id.ToString("D") }, { "Epoch", gate["Epoch"] }, { "FenceId", "" } },
                        new BsonDocument("$set", new BsonDocument { { "FenceId", request.MigrationId.ToString("D") }, { "FenceHash", request.Fingerprint } }), cancellationToken: token).ConfigureAwait(false);
                    if (updated.MatchedCount != 1) throw new InvalidDataException("Native record admission changed during acquisition.");
                }
                await receipts.InsertOneAsync(session, new BsonDocument { { "_id", request.MigrationId.ToString("D") },
                    { "RequestJson", request.CanonicalJson }, { "RequestHash", request.Fingerprint }, { "OwnerToken", owner.ToString("D") }, { "FenceState", "Held" } }, cancellationToken: token).ConfigureAwait(false);
                result = new(new(request, owner, HolonMigrationFenceState.Held)) { IsSaved = true }; changed = true;
            }
            else
            {
                if (stored.State == HolonMigrationFenceState.Absent || stored.OwnerToken != owner)
                    throw new MongoMigrationFenceException("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", "Only the exact pre-mutation fence owner may cancel it.");
                result = new(new(request, owner, HolonMigrationFenceState.Cancelled)) { IsLoaded = stored.State == HolonMigrationFenceState.Cancelled };
                if (stored.State == HolonMigrationFenceState.Held)
                {
                    foreach (var id in request.HolonIds)
                    {
                        var updated = await database.GetCollection<BsonDocument>(Gates).UpdateOneAsync(session,
                            new BsonDocument { { "_id", id.ToString("D") }, { "FenceId", request.MigrationId.ToString("D") }, { "FenceHash", request.Fingerprint } },
                            new BsonDocument { { "$set", new BsonDocument { { "FenceId", "" }, { "FenceHash", "" } } }, { "$inc", new BsonDocument("Epoch", 1L) } }, cancellationToken: token).ConfigureAwait(false);
                        if (updated.MatchedCount != 1) throw new InvalidDataException("Native fence membership differs; cancellation was not applied.");
                    }
                    var updatedReceipt = await receipts.UpdateOneAsync(session, new BsonDocument { { "_id", request.MigrationId.ToString("D") },
                        { "OwnerToken", owner.ToString("D") }, { "FenceState", "Held" }, { "RequestHash", request.Fingerprint } },
                        new BsonDocument("$set", new BsonDocument("FenceState", "Cancelled")), cancellationToken: token).ConfigureAwait(false);
                    if (updatedReceipt.MatchedCount != 1) throw new InvalidDataException("Native fence owner changed.");
                    result.IsSaved = true; changed = true;
                }
            }
            token.ThrowIfCancellationRequested();
            if (changed)
            {
                sessionId = session.ServerSession.Id.ToJson(); transaction = session.WrappedCoreSession.CurrentTransaction.TransactionNumber;
                commitEntered = true; await session.CommitTransactionAsync(token).ConfigureAwait(false); known = true;
            }
            else { known = true; closeEntered = true; await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false); }
        }
        catch (Exception cause)
        {
            cancelled = !commitEntered && cause is OperationCanceledException && token.IsCancellationRequested;
            failure = commitEntered && !known ? new MongoTransactionReconciliationException(sessionId, transaction, cause) : cause;
            if (session != null && !commitEntered && !closeEntered)
                try { if (session.IsInTransaction) await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false); }
                catch (Exception cleanup) { failure = new AggregateException(failure, cleanup); }
        }
        if (session != null) try { session.Dispose(); } catch (Exception cleanup) { failure = failure == null ? cleanup : new AggregateException(failure, cleanup); }
        if (failure == null) return result;
        if (known) { result.IsWarning = true; result.WarningCount++; result.Exception = failure; result.Message = "Native fence outcome is known; scope cleanup failed."; return result; }
        if (cancelled) throw new OperationCanceledException("Native fence operation cancelled before commit; cleanup evidence retained.", failure, token);
        return new() { IsError = true, ErrorCount = 1, Exception = failure,
            ErrorCode = commitEntered ? "COSMIC_MIGRATION_FENCE_RECONCILIATION_REQUIRED" : failure is MongoMigrationFenceException fence ? fence.Code
                : failure is MongoReplicaStorageBindingException binding ? binding.Code
                : MongoTransactionFailure.IsWriteConflict(failure) || failure is MongoWriteException write && write.WriteError?.Category == ServerErrorCategory.DuplicateKey
                    ? "COSMIC_MIGRATION_FENCE_CONCURRENT_CHANGE" : "COSMIC_MIGRATION_FENCE_UNVERIFIED",
            Message = "Native fence admission failed; reconcile its exact original outcome before proceeding." };
    }
}
