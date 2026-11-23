using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public sealed partial class MongoHolonReplicationOutbox
{
    /// <summary>Failure codes whose cause is fixed in host code or configuration,
    /// not in the replica: once fixed, the exact held entry may be delivered again.
    /// Lineage conflicts are excluded; they need a reviewed baseline repair.</summary>
    public static bool IsRearmableHeldCode(string code) => code is "COSMIC_REPLICA_AUTHORIZATION_DENIED"
        or "COSMIC_REPLICA_AUTHORIZATION_FAILED" or "COSMIC_REPLICA_STORAGE_BINDING_INVALID";

    /// <summary>Trusted operator capability, not an HTTP grant. Returns one exact
    /// held route head to Pending so the worker retries it. It changes nothing when
    /// the row is not that reviewed head, requires native reconciliation
    /// evidence, or an earlier unfinished entry exists on the same route.</summary>
    public Task<OASISResult<bool>> RearmHeldHeadAsync(Guid operatorId, long sequence, Guid operationId,
        string payloadDigest, string targetId, string errorCode, CancellationToken cancellationToken = default)
        => Mutate(async session =>
        {
            var row = await _rows.Find(session, new BsonDocument("Sequence", sequence))
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (row == null) throw new InvalidDataException("The reviewed held obligation no longer exists.");
            var observed = ValidateRow(row);
            ValidatePayload(row, observed.Operation);
            if (observed.State != NextGenSoftware.OASIS.API.Core.Interfaces.HolonDeliveryState.ReconciliationRequired
                || observed.Operation != operationId || observed.Evidence?.RequiresNativeReconciliation == true
                || row["TargetId"].AsString != targetId || row["PayloadDigest"].AsString != payloadDigest
                || NullableString(row, "LastErrorCode") != errorCode)
                throw new InvalidDataException("The held obligation differs from the reviewed re-arm request.");
            var earlier = await _rows.CountDocumentsAsync(session, new BsonDocument
            {
                { "TargetId", targetId }, { "Sequence", new BsonDocument("$lt", sequence) },
                { "DeliveryState", new BsonDocument("$ne", "Completed") }
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (earlier != 0) throw new InvalidDataException("Only the route head may be re-armed.");
            var filter = new BsonDocument
            {
                { "Sequence", sequence }, { "OperationId", operationId.ToString("D") }, { "TargetId", targetId },
                { "DeliveryState", "ReconciliationRequired" }, { "PayloadDigest", payloadDigest }, { "LastErrorCode", errorCode }
            };
            var update = new BsonDocument
            {
                { "DeliveryState", "Pending" }, { "NextAttemptUtcTicks", 0L },
                { "Rearm", new BsonDocument("$literal", new BsonDocument
                    {
                        { "Format", 1 }, { "OperatorId", operatorId.ToString("D") }, { "PreviousErrorCode", errorCode },
                        { "AttemptCount", observed.Attempts }, { "PreviousErrorMessage", NullableString(row, "LastErrorMessage") ?? "" }
                    }) },
                { "RearmedAtUtcTicks", NowTicks() }
            };
            var changed = await _rows.UpdateOneAsync(session, filter, Pipeline(update), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!changed.IsAcknowledged || changed.MatchedCount != 1)
                throw new InvalidDataException("The held obligation changed during re-arm.");
            return (new OASISResult<bool>(true), true);
        }, cancellationToken, () =>
        {
            if (operatorId == Guid.Empty || sequence <= 0 || operationId == Guid.Empty
                || payloadDigest?.Length != 64 || !IsRearmableHeldCode(errorCode))
                throw new ArgumentException("A complete reviewed re-arm request is required.");
            Identity(targetId);
        });
}
