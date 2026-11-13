using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Native = Google.Cloud.Firestore.V1;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS : ICommittedHolonReplicaReceiptStorageProvider
{
    private const string ReplicaReceiptCollection = "cosmicReplicaReceipts";

    public async Task<OASISResult<CommittedHolonReplicaReceiptEvidence>> ReadCommittedHolonReplicaReceiptAsync(
        CommittedHolonReplicaReceiptReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != Core.Enums.ProviderType.GoogleCloudOASIS)
            return FirestoreReplicaReceiptError("COSMIC_REPLICA_RECEIPT_OPTIONS_INVALID",
                "An exact Firestore receipt intent and binding are required.");

        try
        {
            var database = _firestoreDb;
            if (!IsProviderActivated || !_enableFirestore || database == null)
                throw new InvalidOperationException("Activate the enrolled Firestore database before reading receipts.");

            var plan = CommittedHolonReplicaReceiptProjection.Prepare(request);
            var identity = FirestoreStorageIdentityDocument(database);
            var receipt = database.Collection(ReplicaReceiptCollection).Document(plan.OperationId.ToString("D"));
            return await RunFirestoreIdentityTransactionAsync<OASISResult<CommittedHolonReplicaReceiptEvidence>>(
                database, receipt.Path, async transaction =>
                {
                    var store = await ReadFirestoreTransactionDocumentAsync(identity, transaction,
                        new[] { "storeId" }, cancellationToken).ConfigureAwait(false);
                    if (store == null || ParseFirestoreStorageIdentity(database, ReadReplicaReceiptField(store, "storeId"))
                        .BindingFingerprint != request.ExpectedStorage.BindingFingerprint)
                        return (FirestoreReplicaReceiptError("COSMIC_REPLICA_RECEIPT_STORAGE_MISMATCH",
                            "The physical Firestore database differs from the reviewed binding."), Array.Empty<Native.Write>());

                    var stored = await ReadFirestoreTransactionDocumentAsync(receipt, transaction,
                        new[] { "fingerprint", "acknowledgement" }, cancellationToken).ConfigureAwait(false);
                    var evidence = CommittedHolonReplicaReceiptProjection.Project(request, plan,
                        stored == null ? null : ReadReplicaReceiptField(stored, "fingerprint"),
                        stored == null ? null : ReadReplicaReceiptField(stored, "acknowledgement"),
                        HolonReplicaHeadReadConsistency.NativeSnapshot);
                    return (new OASISResult<CommittedHolonReplicaReceiptEvidence>(evidence) { IsLoaded = true },
                        Array.Empty<Native.Write>());
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception cause)
        {
            return FirestoreReplicaReceiptError("COSMIC_REPLICA_RECEIPT_UNVERIFIED",
                "The Firestore receipt could not be verified.", cause);
        }
    }

    private static string ReadReplicaReceiptField(Native.Document document, string field)
    {
        if (!document.Fields.TryGetValue(field, out var value)
            || value.ValueTypeCase != Native.Value.ValueTypeOneofCase.StringValue
            || string.IsNullOrEmpty(value.StringValue))
            throw new InvalidDataException($"The Firestore document lacks a valid {field} field.");
        return value.StringValue;
    }

    private static OASISResult<CommittedHolonReplicaReceiptEvidence> FirestoreReplicaReceiptError(
        string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
