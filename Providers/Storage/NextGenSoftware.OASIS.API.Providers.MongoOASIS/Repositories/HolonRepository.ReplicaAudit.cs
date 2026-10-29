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
using NativeHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<OASISResult<HolonReplicaHeadReadResult>> ReadHolonReplicaHeadsAsync(
        HolonReplicaHeadReadRequest request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != ProviderType.MongoDBOASIS)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_HEAD_AUDIT_OPTIONS_INVALID",
                Message = "An exact Mongo physical-store binding and bounded read intent are required." };
        IClientSessionHandle session = null;
        OASISResult<HolonReplicaHeadReadResult> result = null;
        Exception failure = null; string code = "COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED"; bool cancelled = false;
        try
        {
            var database = _dbContext.MongoDB ?? throw new InvalidOperationException("The native Mongo repository is closed.");
            session = await database.Client.StartSessionAsync(cancellationToken: token).ConfigureAwait(false);
            session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority));
            // UUID assertion acquires the actual Holon namespace in this transaction;
            // a separate listCollections preflight would race collection replacement.
            await ValidateReplicaStorageBindingAsync(database, session, request.ExpectedStorage, token,
                "COSMIC_REPLICA_HEAD_AUDIT_STORAGE").ConfigureAwait(false);
            var ids = new BsonArray(request.HolonIds.Select(id => id.ToString("D")));
            var headFilter = new BsonDocument("_id", new BsonDocument("$in", ids));
            var heads = new Dictionary<Guid, HolonReplicaHeadEvidence>();
            // One snapshot payload per batch avoids retaining hundreds of large graphs.
            using (var cursor = await database.GetCollection<BsonDocument>("CosmicReplicaHead").FindAsync(session, headFilter,
                new FindOptions<BsonDocument> { BatchSize = 1, Limit = 257 }, token).ConfigureAwait(false))
            {
                while (await cursor.MoveNextAsync(token).ConfigureAwait(false))
                foreach (var body in cursor.Current)
                {
                    if (!body.TryGetValue("_id", out var key) || !key.IsString || !Guid.TryParseExact(key.AsString, "D", out var id)
                        || !request.HolonIds.Contains(id) || heads.ContainsKey(id) || heads.Count >= request.HolonIds.Count)
                        throw new InvalidDataException("The native replica head identity set is invalid.");
                    heads.Add(id, ReadProtectedReplicaHead(body, id).Evidence);
                }
            }
            var currentFilter = new BsonDocument("HolonId", new BsonDocument("$in", ids));
            var projection = new BsonDocument { { "HolonId", 1 }, { "Version", 1 }, { "VersionId", 1 }, { "ProviderRevisionId", 1 } };
            var current = await database.GetCollection<BsonDocument>(MongoDbContext.HolonCollectionName)
                .Find(session, currentFilter).Project<BsonDocument>(projection).Limit(257).ToListAsync(token).ConfigureAwait(false);
            var records = new Dictionary<Guid, NativeHolon>();
            foreach (var body in current)
            {
                var row = BsonSerializer.Deserialize<NativeHolon>(body);
                if (!request.HolonIds.Contains(row.HolonId) || records.ContainsKey(row.HolonId) || records.Count >= request.HolonIds.Count)
                    throw new InvalidDataException("The current native record identity set is invalid.");
                records.Add(row.HolonId, row);
            }
            var observed = new List<HolonReplicaHeadObservation>();
            foreach (var id in request.HolonIds)
            {
                heads.TryGetValue(id, out var head);
                var exists = records.TryGetValue(id, out var row);
                if (head != null && (head.HardDeleted ? exists : !exists || row.Version != head.Version
                    || row.VersionId != head.VersionId || row.ProviderRevisionId != head.ProviderRevisionId))
                    throw new InvalidDataException("The current native record and protected replica head diverge.");
                observed.Add(new(id, exists, head));
            }
            token.ThrowIfCancellationRequested();
            result = new(new(request.ExpectedStorage, observed)) { IsLoaded = true };
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
            result.Message = "Native head evidence was observed, but read-only session cleanup failed.";
            return result;
        }
        if (cancelled) throw new OperationCanceledException("Head evidence read was cancelled; cleanup evidence is retained.", failure, token);
        return new() { IsError = true, ErrorCount = 1, ErrorCode = code,
            Message = "Protected target evidence could not be verified; no partial observations are returned.", Exception = failure };
    }
}
