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
