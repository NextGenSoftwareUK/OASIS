using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    // A missing-row snapshot read is not a native predicate lock. All conditional
    // receipt owners and first preparation claims write this same operation fence.
    private Task<UpdateResult> TouchOperationFenceAsync(IClientSessionHandle session, Guid operation, CancellationToken token)
        => _dbContext.MongoDB.GetCollection<BsonDocument>("CosmicOperationFence").UpdateOneAsync(session,
            new BsonDocument("_id", operation.ToString("D")), new BsonDocument {
                { "$inc", new BsonDocument("Generation", 1L) },
                { "$setOnInsert", new BsonDocument("CreatedAtUtc", DateTime.UtcNow) }
            }, new UpdateOptions { IsUpsert = true }, token);

    internal async Task<OASISResult<HolonWritePreparation>> ClaimHolonWritePreparationAsync(HolonWritePreparation candidate, CancellationToken token)
    {
        IClientSessionHandle session = null; var commitEntered = false; var known = false; var abortFailed = false;
        string sessionId = null; long? transaction = null;
        Exception failure = null; OASISResult<HolonWritePreparation> result = null;
        void Fail(Exception error) => failure = failure == null ? error : new AggregateException(failure, error);
        try { result = await Claim().ConfigureAwait(false); } catch (Exception error) { Fail(error); }
        if (session != null && session.IsInTransaction && !commitEntered)
            try { await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false); }
            catch (Exception error) { abortFailed = true; Fail(error); }
        try { session?.Dispose(); } catch (Exception error) { Fail(error); }
        if (failure != null)
        {
            if (known) { result.IsWarning = true; result.WarningCount++; result.Exception = failure; result.Message = "The durable preparation is known, but MongoDB cleanup failed."; }
            else
            {
                var uncertain = commitEntered || abortFailed;
                var conflict = MongoTransactionFailure.IsWriteConflict(failure) || failure is MongoWriteException write && write.WriteError?.Code == 11000;
                result = PreparationError(uncertain ? "COSMIC_PREPARATION_RECONCILIATION_REQUIRED" : conflict ? "COSMIC_PREPARATION_CONCURRENT_CLAIM"
                    : failure is OperationCanceledException ? "COSMIC_PREPARATION_CANCELLED" : "COSMIC_PREPARATION_FAILED",
                    uncertain ? "Recover the original preparation before dispatching a holon mutation."
                        : conflict ? "A concurrent operation owns this claim. Retry the same operation after it completes; do not allocate another operation ID."
                        : "MongoDB preparation did not commit.",
                    uncertain ? new MongoPreparationReconciliationException(candidate?.OperationId ?? Guid.Empty, sessionId, transaction, failure) : failure);
            }
        }
        if (result != null && candidate != null) { result.MetaData ??= new Dictionary<string, string>(); result.MetaData["COSMIC.OperationId"] = candidate.OperationId.ToString("D"); }
        return result;

        async Task<OASISResult<HolonWritePreparation>> Claim()
        {
            token.ThrowIfCancellationRequested();
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (candidate.Authority != ProviderType.MongoDBOASIS) return PreparationError("COSMIC_AUTHORITY_MISMATCH", "Preparation belongs to another provider authority.");
            var body = new BsonDocument { { "_id", candidate.OperationId.ToString("D") }, { "ActorId", candidate.ActorId.ToString("D") },
                { "Format", 1 }, { "Records", new BsonArray(candidate.Records.Select(record => new BsonDocument {
                    { "Id", record.HolonId.ToString("D") }, { "Envelope", record.Envelope } })) }, { "CreatedAtUtc", DateTime.UtcNow } };
            if (body.ToBson().Length > 16 * 1024 * 1024) return PreparationError("COSMIC_PREPARATION_TOO_LARGE", "Protected preparation exceeds MongoDB's native BSON document limit.");
            var collection = _dbContext.MongoDB.GetCollection<BsonDocument>("CosmicWritePreparation",
                new MongoCollectionSettings { ReadConcern = ReadConcern.Majority, ReadPreference = ReadPreference.Primary,
                    WriteConcern = WriteConcern.WMajority.With(journal: true) });
            var lookup = new BsonDocument("_id", candidate.OperationId.ToString("D"));
            var existing = await collection.Find(lookup).SingleOrDefaultAsync(token).ConfigureAwait(false);
            if (existing != null) { var read = ReadPreparation(candidate, existing); known = !read.IsError; return read; }
            session = await _dbContext.MongoClient.StartSessionAsync(cancellationToken: token).ConfigureAwait(false);
            sessionId = session.ServerSession.Id.ToJson();
            session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority.With(journal: true)));
            transaction = session.WrappedCoreSession.CurrentTransaction.TransactionNumber;
            await TouchOperationFenceAsync(session, candidate.OperationId, token).ConfigureAwait(false);
            existing = await collection.Find(session, lookup).SingleOrDefaultAsync(token).ConfigureAwait(false);
            if (existing != null) { var read = ReadPreparation(candidate, existing); known = !read.IsError; return read; }
            var receipts = _dbContext.MongoDB.GetCollection<BsonDocument>("CosmicWriteReceipt");
            if (await receipts.Find(session, lookup).Limit(1).AnyAsync(token).ConfigureAwait(false))
                return PreparationError("COSMIC_PREPARATION_RECONCILIATION_REQUIRED", "This operation has a write receipt but no original protected preparation. Reconcile it before another mutation.");
            await collection.InsertOneAsync(session, body, cancellationToken: token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested(); commitEntered = true;
            await session.CommitTransactionAsync(token).ConfigureAwait(false); known = true;
            return new OASISResult<HolonWritePreparation>(candidate);
        }
    }

    private static OASISResult<HolonWritePreparation> ReadPreparation(HolonWritePreparation candidate, BsonDocument body)
    {
        if (body.GetValue("ActorId", BsonNull.Value) != candidate.ActorId.ToString("D"))
            return PreparationError("COSMIC_PREPARATION_ACTOR_MISMATCH", "The operation is owned by another actor.");
        if (body.ElementCount != 5 || body.GetValue("Format", BsonNull.Value).BsonType != BsonType.Int32 || body["Format"].AsInt32 != 1
            || body.GetValue("_id", BsonNull.Value) != candidate.OperationId.ToString("D")
            || body.GetValue("Records", BsonNull.Value) is not BsonArray rows || rows.Count < 1 || rows.Count > HolonWritePreparation.MaximumRecords
            || body.GetValue("CreatedAtUtc", BsonNull.Value).BsonType != BsonType.DateTime)
            throw new InvalidDataException("Stored preparation schema is invalid.");
        var records = new List<ProtectedHolonPreparation>();
        foreach (var value in rows)
        {
            if (value is not BsonDocument row || row.ElementCount != 2 || !row.TryGetValue("Id", out var idText) || !idText.IsString
                || !Guid.TryParseExact(idText.AsString, "D", out var id) || idText.AsString != id.ToString("D")
                || !row.TryGetValue("Envelope", out var envelope) || !envelope.IsString)
                throw new InvalidDataException("Stored preparation record is invalid.");
            records.Add(new ProtectedHolonPreparation(id, envelope.AsString));
        }
        return new OASISResult<HolonWritePreparation>(new HolonWritePreparation(candidate.OperationId, candidate.ActorId, candidate.Authority, records));
    }
    private static OASISResult<HolonWritePreparation> PreparationError(string code, string message, Exception error = null)
        => new OASISResult<HolonWritePreparation> { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
}
