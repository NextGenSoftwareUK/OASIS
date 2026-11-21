using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Protobuf;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Native = Google.Cloud.Firestore.V1;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS : IBoundCommittedHolonReplicaStorageProvider, IHolonReplicaHeadAuditStorageProvider
{
    private const ProviderType FirestoreReplicaAuthority = Core.Enums.ProviderType.GoogleCloudOASIS;

    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        => ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
        => ApplyFirestoreReplicaAsync(request, null, cancellationToken);

    public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage)
        => ApplyBoundCommittedHolonReplicaAsync(request, expectedStorage).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedStorage);
        return ApplyFirestoreReplicaAsync(request, expectedStorage, cancellationToken);
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ApplyFirestoreReplicaAsync(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken)
    {
        try
        {
            CommittedHolonReplicaPlan.ValidateNativeDestination(request, expectedStorage);
            var plan = CommittedHolonReplicaPlan.Create(request, FirestoreReplicaAuthority);
            var database = _firestoreDb;
            if (!IsProviderActivated || !_enableFirestore || database == null)
                throw new InvalidOperationException("Activate Firestore before applying a replica.");

            var receipt = database.Collection(ReplicaReceiptCollection).Document(plan.OperationId.ToString("D"));
            var roots = await RunFirestoreIdentityTransactionAsync<IReadOnlyList<IHolon>>(database, receipt.Path,
                async transaction =>
                {
                    var identity = await ReadFirestoreTransactionDocumentAsync(FirestoreStorageIdentityDocument(database),
                        transaction, new[] { "storeId" }, cancellationToken).ConfigureAwait(false);
                    if (identity == null)
                        throw new FirestoreReplicaConflict("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH",
                            "The Firestore database has no persisted routing identity.");
                    var storage = ParseFirestoreStorageIdentity(database, ReadReplicaReceiptField(identity, "storeId"));
                    if (expectedStorage != null && storage.BindingFingerprint != expectedStorage.BindingFingerprint)
                        throw new FirestoreReplicaConflict("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH",
                            "The physical Firestore database differs from the enrolled replica store.");

                    var previousReceipt = await ReadFirestoreTransactionDocumentAsync(receipt, transaction,
                        new[] { "fingerprint", "acknowledgement" }, cancellationToken).ConfigureAwait(false);
                    if (previousReceipt != null)
                    {
                        if (ReadReplicaReceiptField(previousReceipt, "fingerprint") != plan.Fingerprint)
                            throw new FirestoreReplicaConflict("COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID",
                                "The Firestore receipt belongs to another committed request.");
                        return (ConditionalHolonReceipt.Read(ReadReplicaReceiptField(previousReceipt, "acknowledgement"), plan).ToArray(),
                            Array.Empty<Native.Write>());
                    }

                    var writes = new List<Native.Write>();
                    var snapshots = plan.Nodes.ToDictionary(node => node.Snapshot.Id, node => node.Snapshot);
                    foreach (var record in snapshots.Values)
                    {
                        var id = record.Id.ToString("D");
                        var target = database.Collection("holons").Document(id);
                        var head = database.Collection("cosmicReplicaHeads").Document(id);
                        var current = await ReadFirestoreIdentityDocumentAsync(target, transaction, cancellationToken).ConfigureAwait(false);
                        var prior = await ReadFirestoreReplicaHeadAsync(head, transaction, cancellationToken).ConfigureAwait(false);
                        var fields = current == null ? null : DecodeFirestoreIdentityFields(current);
                        if (prior == null)
                        {
                            if (current != null || record.Version != 1)
                                throw ReplicaLineageConflict();
                        }
                        else
                        {
                            var evidence = await ValidateFirestoreReplicaHeadAsync(record.Id, prior, fields).ConfigureAwait(false);
                            if (evidence.SourceId != plan.SourceId || evidence.SourceAuthority != plan.SourceAuthority
                                || evidence.VersionId != record.PreviousVersionId || evidence.Version != record.Version - 1)
                                throw ReplicaLineageConflict();
                        }

                        record.ProviderUniqueStorageKey ??= new();
                        record.ProviderUniqueStorageKey[FirestoreReplicaAuthority] = id;
                        record.PreviousVersionProviderUniqueStorageKey?.Remove(FirestoreReplicaAuthority);
                        record.ProviderMetaData?.Remove(FirestoreReplicaAuthority);
                        record.InstanceSavedOnProviderType = new EnumValue<ProviderType>(FirestoreReplicaAuthority);
                        var staged = await StageFirestoreDocumentAsync(record, fields, id, IsVersionControlEnabled);
                        plan.ValidateAcknowledgedRecord(record);
                        var json = staged.ToString(Newtonsoft.Json.Formatting.None);
                        var snapshot = StoredRecordJson.Serialize<IHolon>(record);
                        var hardDeleted = plan.Operation == ConditionalHolonOperation.HardDelete;
                        if (hardDeleted)
                            writes.Add(new Native.Write { Delete = target.Path, CurrentDocument = ReplicaPrecondition(current) });
                        else
                            writes.Add(FirestoreReplicaDocumentWrite(target, CreateFirestoreHolonFields(record, json), current));

                        writes.Add(FirestoreReplicaDocumentWrite(head, new Dictionary<string, object>
                        {
                            ["source"] = plan.SourceId, ["authority"] = plan.SourceAuthority.ToString(),
                            ["snapshot"] = snapshot, ["digest"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))),
                            ["revision"] = CosmicConcurrency.GetProviderRevisionId(record, FirestoreReplicaAuthority).Value.ToString("D"),
                            ["hardDeleted"] = hardDeleted
                        }, prior));
                    }

                    foreach (var record in snapshots.Values)
                        record.Children = plan.ChildIds[record.Id].Select(id => (IHolon)snapshots[id]).ToList();
                    var acknowledgement = ConditionalHolonReceipt.Write(plan.RootIds.Select(id => (IHolon)snapshots[id]).ToArray(), plan);
                    var acknowledged = ConditionalHolonReceipt.Read(acknowledgement, plan).ToArray();
                    writes.Add(FirestoreReplicaDocumentWrite(receipt, new Dictionary<string, object>
                    {
                        ["fingerprint"] = plan.Fingerprint, ["acknowledgement"] = acknowledgement
                    }, null));
                    return (acknowledged, writes);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return new(roots) { IsSaved = plan.Operation == ConditionalHolonOperation.Save,
                IsDeleted = plan.Operation != ConditionalHolonOperation.Save,
                SavedCount = plan.Operation == ConditionalHolonOperation.Save ? plan.Nodes.Count : 0,
                DeletedCount = plan.Operation != ConditionalHolonOperation.Save ? plan.Nodes.Count : 0 };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, Exception = cause, Message = cause.Message,
                ErrorCode = cause is FirestoreReplicaConflict conflict ? conflict.Code : "COSMIC_REPLICA_WRITE_FAILED" };
        }
    }

    public async Task<OASISResult<HolonReplicaHeadReadResult>> ReadHolonReplicaHeadsAsync(
        HolonReplicaHeadReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != FirestoreReplicaAuthority)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_HEAD_AUDIT_OPTIONS_INVALID",
                Message = "An exact Firestore replica-head intent and binding are required." };

        try
        {
            var database = _firestoreDb;
            if (!IsProviderActivated || !_enableFirestore || database == null)
                throw new InvalidOperationException("Activate the enrolled Firestore database before reading replica heads.");

            var identity = FirestoreStorageIdentityDocument(database);
            var result = await RunFirestoreIdentityTransactionAsync<HolonReplicaHeadReadResult>(
                database, identity.Path, async transaction =>
                {
                    var stored = await ReadFirestoreTransactionDocumentAsync(identity, transaction,
                        new[] { "storeId" }, cancellationToken).ConfigureAwait(false);
                    if (stored == null)
                        throw new FirestoreReplicaConflict("COSMIC_REPLICA_HEAD_AUDIT_STORAGE_MISMATCH",
                            "The Firestore database has no persisted routing identity.");
                    var storage = ParseFirestoreStorageIdentity(database, ReadReplicaReceiptField(stored, "storeId"));
                    if (storage.BindingFingerprint != request.ExpectedStorage.BindingFingerprint)
                        throw new FirestoreReplicaConflict("COSMIC_REPLICA_HEAD_AUDIT_STORAGE_MISMATCH",
                            "The physical Firestore database differs from the reviewed binding.");

                    var observations = new List<HolonReplicaHeadObservation>(request.HolonIds.Count);
                    foreach (var holonId in request.HolonIds)
                    {
                        var id = holonId.ToString("D");
                        var current = await ReadFirestoreIdentityDocumentAsync(database.Collection("holons").Document(id),
                            transaction, cancellationToken).ConfigureAwait(false);
                        var head = await ReadFirestoreReplicaHeadAsync(database.Collection("cosmicReplicaHeads").Document(id),
                            transaction, cancellationToken).ConfigureAwait(false);
                        var evidence = head == null ? null : await ValidateFirestoreReplicaHeadAsync(holonId, head,
                            current == null ? null : DecodeFirestoreIdentityFields(current)).ConfigureAwait(false);
                        observations.Add(new HolonReplicaHeadObservation(holonId, current != null, evidence));
                    }

                    return (new HolonReplicaHeadReadResult(storage, observations), Array.Empty<Native.Write>());
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new(result) { IsLoaded = true };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, Exception = cause,
                ErrorCode = cause is FirestoreReplicaConflict conflict ? conflict.Code : "COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED",
                Message = cause is FirestoreReplicaConflict ? cause.Message : "The Firestore replica heads could not be verified." };
        }
    }

    private static Task<Native.Document> ReadFirestoreReplicaHeadAsync(DocumentReference reference,
        ByteString transaction, CancellationToken cancellationToken)
        => ReadFirestoreTransactionDocumentAsync(reference, transaction,
            new[] { "source", "authority", "snapshot", "digest", "revision", "hardDeleted" }, cancellationToken);

    private async Task<HolonReplicaHeadEvidence> ValidateFirestoreReplicaHeadAsync(Guid id, Native.Document head,
        Dictionary<string, object> currentFields)
    {
        var metadata = DecodeFirestoreIdentityFields(head);
        var evidence = HolonReplicaHeadRecord.Read(id, FirestoreReplicaAuthority,
            (string)metadata["source"], (string)metadata["authority"], (string)metadata["snapshot"],
            (string)metadata["digest"], (string)metadata["revision"], (bool)metadata["hardDeleted"]).Evidence;
        var current = currentFields == null ? null : await ReadFirestoreHistoryAsync<Holon>(currentFields,
            id.ToString("D"), 0).ConfigureAwait(false);
        HolonReplicaHeadRecord.ValidateCurrent(id, FirestoreReplicaAuthority, current, evidence);
        return evidence;
    }

    private static Native.Precondition ReplicaPrecondition(Native.Document prior)
        => prior == null ? new() { Exists = false } : new() { UpdateTime = prior.UpdateTime };

    private static Native.Write FirestoreReplicaDocumentWrite(DocumentReference reference,
        Dictionary<string, object> fields, Native.Document prior)
    {
        _ = FirestoreDocumentSize.Require(reference, fields);
        var document = new Native.Document { Name = reference.Path };
        foreach (var field in fields)
            document.Fields.Add(field.Key, EncodeFirestoreIdentityValue(field.Value));
        return new() { Update = document, CurrentDocument = ReplicaPrecondition(prior) };
    }

    private static FirestoreReplicaConflict ReplicaLineageConflict()
        => new("COSMIC_REPLICA_LINEAGE_CONFLICT", "The Firestore replica lineage does not match the committed source.");

    private sealed class FirestoreReplicaConflict(string code, string message) : InvalidOperationException(message)
    {
        public string Code { get; } = code;
    }
}
