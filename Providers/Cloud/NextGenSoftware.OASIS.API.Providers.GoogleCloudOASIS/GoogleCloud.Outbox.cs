using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Protobuf;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Native = Google.Cloud.Firestore.V1;
using QueryParts = Google.Cloud.Firestore.V1.StructuredQuery.Types;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    private sealed partial class FirestoreOutbox(GoogleCloudOASIS provider, CosmicStorageIdentity expectedSource)
    {
        private async Task<OASISResult<T>> ExecuteAsync<T>(
            Func<FirestoreDb, ByteString, Task<(T Result, IReadOnlyList<Native.Write> Writes)>> operation,
            CancellationToken cancellationToken)
        {
            try
            {
                var database = provider._firestoreDb;
                if (!provider.IsProviderActivated || !provider._enableFirestore || database == null)
                    throw new InvalidOperationException("Activate the enrolled Firestore database before opening the outbox.");

                var identity = FirestoreStorageIdentityDocument(database);
                var result = await RunFirestoreIdentityTransactionAsync<T>(database, identity.Path, async transaction =>
                {
                    var stored = await ReadFirestoreTransactionDocumentAsync(identity, transaction,
                        new[] { "storeId" }, cancellationToken).ConfigureAwait(false);
                    if (stored == null || ParseFirestoreStorageIdentity(database, ReadReplicaReceiptField(stored, "storeId"))
                        .BindingFingerprint != expectedSource.BindingFingerprint)
                        throw new FirestoreReplicaConflict("COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH",
                            "The physical Firestore database differs from the enrolled outbox source.");

                    return await operation(database, transaction).ConfigureAwait(false);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
                return new(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception cause)
            {
                return new() { IsError = true, ErrorCount = 1, Exception = cause,
                    ErrorCode = cause is FirestoreReplicaConflict conflict ? conflict.Code : "COSMIC_FIRESTORE_OUTBOX_FAILED",
                    Message = cause.Message };
            }
        }

        private static readonly string[] DeliveryFields =
        {
            "sequence", "operationId", "targetId", "payload", "digest", "state", "attemptCount",
            "worker", "lease", "leaseUntil", "retryAt", "error", "message", "recovery", "recoveryDigest"
        };

        public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence, CancellationToken cancellationToken = default)
        {
            if (sequence <= 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));

            return ExecuteAsync<HolonDeliveryInspection>(async (database, transaction) =>
            {
                var reference = FirestoreOutboxDelivery(database, sequence);
                var document = await ReadFirestoreTransactionDocumentAsync(reference, transaction, DeliveryFields,
                    cancellationToken).ConfigureAwait(false);
                var inspection = document == null ? null : Inspect(database, document);
                return (inspection, Array.Empty<Native.Write>());
            }, cancellationToken);
        }

        public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string targetId,
            CancellationToken cancellationToken = default)
        {
            ValidateTargetId(targetId);
            return ExecuteAsync<HolonDeliveryInspection>(async (database, transaction) =>
            {
                var head = await ReadHeadAsync(database, transaction, targetId, cancellationToken).ConfigureAwait(false);
                return (head?.Inspection, Array.Empty<Native.Write>());
            }, cancellationToken);
        }

        public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string targetId, string workerId,
            TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            ValidateTargetId(targetId);
            ValidateLeaseDuration(leaseDuration);
            if (string.IsNullOrWhiteSpace(workerId) || workerId.Length > 256)
                throw new ArgumentException("A bounded worker identity is required.", nameof(workerId));

            return ExecuteAsync<HolonDeliveryClaim>(async (database, transaction) =>
            {
                var head = await ReadHeadAsync(database, transaction, targetId, cancellationToken).ConfigureAwait(false);
                if (head == null || head.Value.Inspection.State == HolonDeliveryState.ReconciliationRequired)
                    return (null, Array.Empty<Native.Write>());

                var (inspection, document) = head.Value;
                var fields = DecodeFirestoreIdentityFields(document);
                var now = DateTime.UtcNow;
                if (ReadDeliveryTicks(fields, "leaseUntil") > now.Ticks || ReadDeliveryTicks(fields, "retryAt") > now.Ticks)
                    return (null, Array.Empty<Native.Write>());

                var lease = Guid.NewGuid();
                var until = now.Add(leaseDuration);
                var attempt = checked(inspection.AttemptCount + 1);
                fields["state"] = nameof(HolonDeliveryState.InFlight);
                fields["worker"] = workerId;
                fields["lease"] = lease.ToString("D");
                fields["leaseUntil"] = until.Ticks;
                fields["retryAt"] = 0L;
                fields["attemptCount"] = (long)attempt;
                var claim = new HolonDeliveryClaim(inspection.Sequence, inspection.OperationId, targetId,
                    workerId, lease, (string)fields["payload"], attempt, until);
                var write = FirestoreReplicaDocumentWrite(FirestoreOutboxDelivery(database, inspection.Sequence), fields, document);
                return (claim, new[] { write });
            }, cancellationToken);
        }

        public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim, TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
        {
            ValidateLeaseDuration(leaseDuration);
            return UpdateOwnedAsync(claim, (fields, now) =>
                fields["leaseUntil"] = Math.Max(ReadDeliveryTicks(fields, "leaseUntil"), now.Add(leaseDuration).Ticks),
                cancellationToken);
        }

        public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            string errorCode = null, string message = null, DateTime? nextAttemptUtc = null,
            CancellationToken cancellationToken = default)
            => FinishCoreAsync(claim, outcome, null, errorCode, message, nextAttemptUtc, cancellationToken);

        public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            HolonDeliveryRecoveryEvidence recoveryEvidence, string errorCode = null, string message = null,
            DateTime? nextAttemptUtc = null, CancellationToken cancellationToken = default)
            => FinishCoreAsync(claim, outcome, recoveryEvidence ?? throw new ArgumentNullException(nameof(recoveryEvidence)),
                errorCode, message, nextAttemptUtc, cancellationToken);

        private Task<OASISResult<bool>> FinishCoreAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            HolonDeliveryRecoveryEvidence evidence, string errorCode, string message, DateTime? nextAttemptUtc,
            CancellationToken cancellationToken)
        {
            var state = outcome switch
            {
                HolonDeliveryOutcome.Completed => HolonDeliveryState.Completed,
                HolonDeliveryOutcome.Retry => HolonDeliveryState.Pending,
                HolonDeliveryOutcome.ReconciliationRequired => HolonDeliveryState.ReconciliationRequired,
                _ => throw new ArgumentOutOfRangeException(nameof(outcome))
            };
            if ((outcome == HolonDeliveryOutcome.Completed && (errorCode != null || message != null))
                || (outcome != HolonDeliveryOutcome.Completed && (string.IsNullOrWhiteSpace(errorCode)
                    || errorCode.Length > 128 || message?.Length > 4096))
                || (outcome == HolonDeliveryOutcome.Retry ? nextAttemptUtc?.Kind != DateTimeKind.Utc : nextAttemptUtc != null)
                || (evidence?.RequiresNativeReconciliation == true && state != HolonDeliveryState.ReconciliationRequired))
                throw new ArgumentException("Delivery diagnostics and retry time must match the outcome.");

            var recovery = evidence == null ? null : HolonRecoveryEvidenceJson.Write(evidence);
            return UpdateOwnedAsync(claim, (fields, now) =>
            {
                if (nextAttemptUtc <= now)
                    throw new ArgumentException("The next delivery attempt must be in the future.", nameof(nextAttemptUtc));
                fields["state"] = state.ToString();
                fields.Remove("worker");
                fields.Remove("lease");
                fields.Remove("leaseUntil");
                fields["retryAt"] = nextAttemptUtc?.Ticks ?? 0L;
                fields["error"] = errorCode;
                fields["message"] = message;
                fields["recovery"] = recovery;
                fields["recoveryDigest"] = recovery == null ? null : HolonRecoveryEvidenceJson.Digest(recovery);
            }, cancellationToken);
        }

        private Task<OASISResult<bool>> UpdateOwnedAsync(HolonDeliveryClaim claim,
            Action<Dictionary<string, object>, DateTime> update, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(claim);
            if (claim.Sequence <= 0 || claim.OperationId == Guid.Empty || claim.LeaseToken == Guid.Empty
                || string.IsNullOrWhiteSpace(claim.WorkerId) || claim.WorkerId.Length > 256)
                throw new ArgumentException("A valid Firestore delivery claim is required.", nameof(claim));
            ValidateTargetId(claim.TargetId);

            return ExecuteAsync<bool>(async (database, transaction) =>
            {
                var reference = FirestoreOutboxDelivery(database, claim.Sequence);
                var document = await ReadFirestoreTransactionDocumentAsync(reference, transaction, DeliveryFields,
                    cancellationToken).ConfigureAwait(false);
                if (document == null)
                    return (false, Array.Empty<Native.Write>());

                var inspection = Inspect(database, document);
                var fields = DecodeFirestoreIdentityFields(document);
                var now = DateTime.UtcNow;
                if (inspection.State != HolonDeliveryState.InFlight || inspection.OperationId != claim.OperationId
                    || inspection.TargetId != claim.TargetId || ReadDeliveryTicks(fields, "leaseUntil") <= now.Ticks
                    || !Equals(fields.GetValueOrDefault("worker"), claim.WorkerId)
                    || !Equals(fields.GetValueOrDefault("lease"), claim.LeaseToken.ToString("D")))
                    return (false, Array.Empty<Native.Write>());

                update(fields, now);
                return (true, new[] { FirestoreReplicaDocumentWrite(reference, fields, document) });
            }, cancellationToken);
        }

        private async Task<(HolonDeliveryInspection Inspection, Native.Document Document)?> ReadHeadAsync(
            FirestoreDb database, ByteString transaction, string targetId, CancellationToken cancellationToken)
        {
            var query = new Native.StructuredQuery
            {
                Where = new QueryParts.Filter
                {
                    FieldFilter = new QueryParts.FieldFilter
                    {
                        Field = new QueryParts.FieldReference { FieldPath = "targetId" },
                        Op = QueryParts.FieldFilter.Types.Operator.Equal,
                        Value = new Native.Value { StringValue = targetId }
                    }
                }
            };
            query.From.Add(new QueryParts.CollectionSelector { CollectionId = FirestoreOutboxCollection });
            query.OrderBy.Add(new QueryParts.Order
            {
                Field = new QueryParts.FieldReference { FieldPath = "__name__" },
                Direction = QueryParts.Direction.Ascending
            });

            using var call = database.Client.RunQuery(new Native.RunQueryRequest
            {
                Parent = $"projects/{database.ProjectId}/databases/{database.DatabaseId}/documents",
                Transaction = transaction,
                StructuredQuery = query
            });
            var stream = call.GetResponseStream();
            while (await stream.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            {
                var document = stream.Current.Document;
                if (document == null)
                    continue;
                if (ReadReplicaReceiptField(document, "targetId") != targetId)
                    throw new InvalidDataException("Firestore returned a delivery for another target.");
                if (ReadReplicaReceiptField(document, "state") != nameof(HolonDeliveryState.Completed))
                    return (Inspect(database, document), document);
            }

            return null;
        }

        private static long ReadDeliveryTicks(Dictionary<string, object> fields, string name)
        {
            if (!fields.TryGetValue(name, out var value))
                return 0;
            if (value is not long ticks || ticks < 0 || ticks > DateTime.MaxValue.Ticks)
                throw new InvalidDataException($"The Firestore delivery has an invalid {name} timestamp.");
            return ticks;
        }

        private static void ValidateTargetId(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId) || targetId != targetId.Trim() || targetId.Length > 256)
                throw new ArgumentException("A bounded target identity is required.", nameof(targetId));
        }

        private static void ValidateLeaseDuration(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero || duration > TimeSpan.FromHours(1))
                throw new ArgumentOutOfRangeException(nameof(duration));
        }

        private HolonDeliveryInspection Inspect(FirestoreDb database, Native.Document document)
        {
            var fields = DecodeFirestoreIdentityFields(document);
            var sequence = (long)fields["sequence"];
            if (sequence <= 0 || document.Name != FirestoreOutboxDelivery(database, sequence).Path)
                throw new InvalidDataException("The Firestore delivery sequence differs from its document identity.");

            var operationText = ReadReplicaReceiptField(document, "operationId");
            if (!Guid.TryParseExact(operationText, "D", out var operationId) || operationId == Guid.Empty
                || operationId.ToString("D") != operationText)
                throw new InvalidDataException("The Firestore delivery operation identity is invalid.");
            var target = ReadReplicaReceiptField(document, "targetId");
            var payload = ReadReplicaReceiptField(document, "payload");
            if (HolonRecoveryEvidenceJson.Digest(payload) != ReadReplicaReceiptField(document, "digest"))
                throw new InvalidDataException("The Firestore delivery payload checksum differs.");

            var request = CommittedHolonReplicaPayload.ReadForTarget(payload, target);
            var route = request.NativeRoutes.Single(binding => binding.TargetId == target);
            if (request.OperationId != operationId || request.SourceAuthority != FirestoreReplicaAuthority
                || route.SourceStorage.BindingFingerprint != expectedSource.BindingFingerprint)
                throw new InvalidDataException("The Firestore delivery payload differs from the enrolled route.");

            var stateText = ReadReplicaReceiptField(document, "state");
            var attempts = checked((int)(long)fields["attemptCount"]);
            if (!Enum.TryParse<HolonDeliveryState>(stateText, out var state) || state.ToString() != stateText
                || !Enum.IsDefined(state) || attempts < 0)
                throw new InvalidDataException("The Firestore delivery state or attempt count is invalid.");

            return new(sequence, operationId, target, state, attempts,
                (string)fields.GetValueOrDefault("error"), (string)fields.GetValueOrDefault("message"),
                HolonRecoveryEvidenceJson.ReadStored((string)fields.GetValueOrDefault("recovery"),
                    (string)fields.GetValueOrDefault("recoveryDigest"), state), route);
        }
    }
}
