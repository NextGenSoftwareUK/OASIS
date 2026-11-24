using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

/// <summary>The parent receipt state a unit of work observed inside its transaction.</summary>
internal enum MongoUnitOfWorkReceipt { Absent, Replay }

public partial class HolonRepository
{
    /// <summary>
    /// Runs both unit-of-work halves in ONE snapshot transaction, then records the parent receipt.
    /// A replay (parent receipt present) re-reads the part receipts and commits nothing.
    /// </summary>
    internal async Task<(bool Committed, string ErrorCode, Exception Cause)> ExecuteUnitOfWorkAsync(ConditionalHolonUnitOfWorkRequest request,
        Func<IClientSessionHandle, MongoUnitOfWorkReceipt, Task<bool>> parts, CancellationToken token)
    {
        var database = _dbContext.MongoDB;
        var receipts = database.GetCollection<BsonDocument>("CosmicWriteReceipt");
        using var session = await database.Client.StartSessionAsync(cancellationToken: token).ConfigureAwait(false);
        session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority.With(journal: true)));
        var commitAttempted = false;
        try
        {
            var parentId = request.Write.OperationId.ToString("D");
            var parent = await receipts.Find(session, new BsonDocument("_id", parentId)).SingleOrDefaultAsync(token).ConfigureAwait(false);
            if (parent != null && parent.GetValue("Fingerprint", BsonNull.Value) != request.ParentFingerprint)
            { await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false); return (false, "COSMIC_OPERATION_ID_REUSED", null); }
            if (parent != null)
            {
                var partCount = await receipts.CountDocumentsAsync(session, Builders<BsonDocument>.Filter.In("_id",
                    new[] { request.SaveOperationId.ToString("D"), request.DeleteOperationId.ToString("D") }), cancellationToken: token).ConfigureAwait(false);
                var (savePart, deletePart) = request.CreatePartRequests();
                if (partCount != (savePart == null ? 0 : 1) + (deletePart == null ? 0 : 1))
                { await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false); return (false, "COSMIC_RECEIPT_INVALID", null); }
            }
            if (!await parts(session, parent == null ? MongoUnitOfWorkReceipt.Absent : MongoUnitOfWorkReceipt.Replay).ConfigureAwait(false))
            {
                if (session.IsInTransaction) await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                return (false, null, null);
            }
            if (parent != null) { await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false); return (true, null, null); }
            if (!await ApplyUniqueKeyClaimsAsync(database, session, request, token).ConfigureAwait(false))
            { await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false); return (false, "COSMIC_UNIQUE_KEY_CONFLICT", null); }
            await receipts.InsertOneAsync(session, new BsonDocument { {"_id", parentId}, {"Fingerprint", request.ParentFingerprint},
                {"ResultJson", new BsonDocument { {"kind", ConditionalHolonUnitOfWorkRequest.ReceiptKind},
                    {"save", request.SaveOperationId.ToString("D")}, {"delete", request.DeleteOperationId.ToString("D")} }.ToJson()},
                {"CommittedAtUtc", DateTime.UtcNow} }, cancellationToken: token).ConfigureAwait(false);
            commitAttempted = true;
            await session.CommitTransactionAsync(token).ConfigureAwait(false);
            return (true, null, null);
        }
        catch (Exception cause)
        {
            if (!commitAttempted && session.IsInTransaction)
                try { await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false); } catch (Exception abort) { cause = new AggregateException(cause, abort); }
            return (false, commitAttempted ? "COSMIC_WRITE_RECONCILIATION_REQUIRED"
                : MongoTransactionFailure.IsWriteConflict(cause) ? "COSMIC_CONCURRENT_WRITE"
                : cause is OperationCanceledException ? "COSMIC_CONDITIONAL_WRITE_CANCELLED" : "COSMIC_CONDITIONAL_WRITE_FAILED", cause);
        }
    
    }

    /// <summary>M10 claims inside the snapshot transaction. _id is the native key, so a concurrent
    /// claim of the same key is a write conflict or duplicate key and aborts one transaction.</summary>
    private static async Task<bool> ApplyUniqueKeyClaimsAsync(IMongoDatabase database, IClientSessionHandle session,
        ConditionalHolonUnitOfWorkRequest request, CancellationToken token)
    {
        if (request.UniqueKeys.Count == 0 && request.Deletes.Count == 0) return true;
        var keys = database.GetCollection<BsonDocument>("CosmicUniqueKey");
        foreach (var deleted in request.Deletes)
            await keys.DeleteManyAsync(session, new BsonDocument("HolonId", deleted.Id.ToString("D")), cancellationToken: token).ConfigureAwait(false);
        foreach (var claim in request.UniqueKeys)
        {
            var holon = claim.HolonId.ToString("D");
            var stale = new BsonDocument { {"HolonId", holon}, {"Scope", claim.Scope} };
            if (claim.Value != null) stale.Add("_id", new BsonDocument("$ne", claim.NativeKey));
            await keys.DeleteManyAsync(session, stale, cancellationToken: token).ConfigureAwait(false);
            if (claim.Value == null) continue;
            var held = await keys.Find(session, new BsonDocument("_id", claim.NativeKey)).SingleOrDefaultAsync(token).ConfigureAwait(false);
            if (held != null && held["HolonId"].AsString != holon) return false;
            if (held == null)
                await keys.InsertOneAsync(session, new BsonDocument { {"_id", claim.NativeKey}, {"Scope", claim.Scope}, {"HolonId", holon} },
                    cancellationToken: token).ConfigureAwait(false);
        }
        return true;
    }
}
