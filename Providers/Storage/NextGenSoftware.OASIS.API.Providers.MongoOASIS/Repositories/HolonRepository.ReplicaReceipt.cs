using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NativeHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<OASISResult<CommittedHolonReplicaReceiptEvidence>> ReadCommittedHolonReplicaReceiptAsync(
        CommittedHolonReplicaReceiptReadRequest request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != ProviderType.MongoDBOASIS)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_OPTIONS_INVALID",
                Message = "An exact Mongo physical-store binding and bounded read intent are required." };
        IClientSessionHandle session = null;
        OASISResult<CommittedHolonReplicaReceiptEvidence> result = null;
        Exception failure = null; string code = "COSMIC_REPLICA_RECEIPT_UNVERIFIED"; bool cancelled = false;
        try
        {
            var plan = CommittedHolonReplicaReceiptProjection.Prepare(request);
            var database = _dbContext.MongoDB ?? throw new InvalidOperationException("The native Mongo repository is closed.");
            session = await database.Client.StartSessionAsync(cancellationToken: token).ConfigureAwait(false);
            session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority));
            // UUID assertion acquires the actual Holon namespace in this transaction;
            // a separate listCollections preflight would race collection replacement.
            await ValidateReplicaStorageBindingAsync(database, session, request.ExpectedStorage, token,
                "COSMIC_REPLICA_RECEIPT_STORAGE").ConfigureAwait(false);
            var receipts = await database.GetCollection<BsonDocument>("CosmicWriteReceipt")
                .Find(session, new BsonDocument("_id", plan.OperationId.ToString("D"))).Limit(2)
                .ToListAsync(token).ConfigureAwait(false);
            if (receipts.Count > 1) throw new InvalidDataException("Historical receipt identity is not unique.");
            string fingerprint = null, acknowledgement = null;
            if (receipts.Count == 1)
            {
                var receipt = receipts[0];
                if (!receipt.TryGetValue("Fingerprint", out var f) || !f.IsString
                    || !receipt.TryGetValue("ResultJson", out var json) || !json.IsString)
                    throw new InvalidDataException("Historical receipt is incomplete.");
                fingerprint = f.AsString; acknowledgement = json.AsString;
            }
            var evidence = CommittedHolonReplicaReceiptProjection.Project(request, plan, fingerprint, acknowledgement,
                HolonReplicaHeadReadConsistency.NativeSnapshot);
            token.ThrowIfCancellationRequested();
            result = new(evidence) { IsLoaded = true };
        }
        catch (Exception cause)
        {
            failure = cause; cancelled = cause is OperationCanceledException && token.IsCancellationRequested;
            if (cause is MongoReplicaStorageBindingException binding) code = binding.Code;
        }
        // Read-only transaction cleanup never issues a commit or repeats a native read.
        // Once observed, a cleanup failure is a warning on that known observation.
        if (session != null)
        {
            try { if (session.IsInTransaction) await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false); }
            catch (Exception cause) { failure = failure == null ? cause : new AggregateException(failure, cause); }
            try { session.Dispose(); }
            catch (Exception cause) { failure = failure == null ? cause : new AggregateException(failure, cause); }
        }
        if (failure == null) return result;
        if (result != null)
        {
            result.IsWarning = true; result.WarningCount = 1; result.Exception = failure;
            result.Message = "Historical receipt evidence was observed, but read-only session cleanup failed.";
            return result;
        }
        if (cancelled) throw new OperationCanceledException("Historical receipt read was cancelled; cleanup evidence is retained.", failure, token);
        return new() { IsError = true, ErrorCount = 1, ErrorCode = code,
            Message = "Historical delivery could not be verified; no mutation was attempted.", Exception = failure };
    }
}
