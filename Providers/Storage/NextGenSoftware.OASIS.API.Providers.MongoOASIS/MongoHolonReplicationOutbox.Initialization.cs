using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public sealed partial class MongoHolonReplicationOutbox
{
    /// <summary>
    /// Trusted operator capability, not an HTTP grant. The application must reauthorize
    /// the reviewed intent and obtain outcome directly from the configured target's
    /// immutable initialization receipt. A caller-supplied digest is not authentication.
    /// Completes only that exact held head; preserves its original failure evidence.
    /// </summary>
    public Task<OASISResult<bool>> CompleteInitializedBaselineAsync(
        HolonReplicaInitializationIntent intent,
        HolonReplicaInitializationOutcome outcome,
        CancellationToken cancellationToken = default)
        => Mutate(async session =>
        {
            var row = await _rows.Find(session, new BsonDocument("Sequence", intent.Sequence))
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (row == null) throw new InvalidDataException("The reviewed source obligation no longer exists.");

            var observed = ValidateRow(row);
            ValidatePayload(row, observed.Operation);
            var original = CommittedHolonReplicaPayload.Read(row["PayloadJson"].AsString);
            var baseline = new CommittedHolonReplicaRequest(intent.InitializationOperationId,
                original.SourceId, original.SourceAuthority, original.ActorId, original.Operation,
                original.Roots, original.SaveChildren, original.Recursive, original.MaxChildDepth);
            var plan = CommittedHolonReplicaPlan.CreateInitialBaseline(baseline, outcome.TargetAuthority);
            if (observed.Operation != intent.SourceOperationId || observed.Attempts != intent.AttemptCount
                || row["PayloadDigest"].AsString != intent.SourcePayloadDigest
                || plan.Fingerprint != intent.BaselineFingerprint)
                throw new InvalidDataException("The source obligation differs from the reviewed baseline intent.");

            var evidence = new BsonDocument
            {
                { "Format", 1 }, { "IntentFingerprint", intent.Fingerprint },
                { "OperatorId", intent.OperatorId.ToString("D") },
                { "InitializationOperationId", outcome.OperationId.ToString("D") },
                { "TargetAuthority", outcome.TargetAuthority.ToString() },
                { "BaselineFingerprint", outcome.BaselineFingerprint },
                { "ReceiptDigest", outcome.ReceiptDigest }
            };

            // Exact replay observes the existing acknowledgement, without rewriting
            // its timestamp or changing a later queue entry.
            if (observed.State == HolonDeliveryState.Completed)
            {
                if (!row.TryGetValue("InitializationCompletion", out var recorded) || recorded != evidence)
                    throw new InvalidDataException("Completion does not acknowledge this exact initialization.");
                return (new OASISResult<bool>(true), false);
            }

            if (observed.State != HolonDeliveryState.ReconciliationRequired
                || NullableString(row, "LastErrorCode") != "COSMIC_REPLICA_LINEAGE_CONFLICT"
                || observed.Evidence == null || observed.Evidence.RequiresNativeReconciliation)
                throw new InvalidDataException("Only the reviewed lineage conflict without unresolved execution can complete.");
            var head = await _rows.Find(session, new BsonDocument
                { { "TargetId", row["TargetId"] }, { "DeliveryState", new BsonDocument("$ne", "Completed") } })
                .Sort(new BsonDocument("Sequence", 1)).Limit(1)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (head == null || head["_id"] != row["_id"])
                throw new InvalidDataException("The reviewed obligation is not the current held target head.");

            // The native transaction owns this comparison and its completion together.
            // No reset, claim-token fabrication, payload rewrite or delivery retry.
            var filter = new BsonDocument
            {
                { "_id", row["_id"] }, { "DeliveryState", "ReconciliationRequired" },
                { "AttemptCount", intent.AttemptCount }, { "PayloadDigest", intent.SourcePayloadDigest },
                { "OperationId", intent.SourceOperationId.ToString("D") }
            };
            var changed = await _rows.UpdateOneAsync(session, filter, Pipeline(new BsonDocument
            {
                { "DeliveryState", "Completed" }, { "CompletedAtUtcTicks", NowTicks() },
                { "InitializationCompletion", new BsonDocument("$literal", evidence) }
            }), cancellationToken: cancellationToken).ConfigureAwait(false);
            return (OwnedResult(changed), changed.MatchedCount == 1);
        }, cancellationToken, () =>
        {
            ArgumentNullException.ThrowIfNull(intent);
            ArgumentNullException.ThrowIfNull(outcome);
            if (outcome.ReceiptState != ConditionalHolonReceiptState.Committed
                || outcome.OperationId != intent.InitializationOperationId
                || outcome.BaselineFingerprint != intent.BaselineFingerprint)
                throw new ArgumentException("An exact committed target initialization receipt is required.");
        });
}
