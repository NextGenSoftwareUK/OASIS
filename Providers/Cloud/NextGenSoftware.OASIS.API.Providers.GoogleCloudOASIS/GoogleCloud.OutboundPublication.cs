using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Protobuf;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Native = Google.Cloud.Firestore.V1;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    private const string FirestoreOutboxCollection = "cosmicOutboundDeliveries";
    private const string FirestoreOutboxSequenceCollection = "cosmicOutboundState";

    internal async Task<OASISResult<IEnumerable<IHolon>>> ExecuteFirestoreEnrolledWriteAsync(
        ConditionalHolonWriteRequest request, HolonReplicationEnrollment enrollment, CancellationToken cancellationToken)
    {
        try
        {
            var plan = ConditionalHolonPlan.CreateReplicating(request, FirestoreReplicaAuthority, enrollment);
            if (enrollment.SourceStorageIdentity == null || enrollment.NativeRoutes.Count == 0)
                throw new ArgumentException("Firestore replication requires a physical source and native target routes.");

            var database = _firestoreDb;
            if (!IsProviderActivated || !_enableFirestore || database == null)
                throw new InvalidOperationException("Activate the enrolled Firestore database before writing.");
            var receipt = database.Collection(ConditionalReceiptCollection).Document(ConditionalOperationKey(plan.OperationId));
            var roots = await RunFirestoreIdentityTransactionAsync<IReadOnlyList<IHolon>>(database, receipt.Path,
                async transaction =>
                {
                    var identity = await ReadFirestoreTransactionDocumentAsync(FirestoreStorageIdentityDocument(database),
                        transaction, new[] { "storeId" }, cancellationToken).ConfigureAwait(false);
                    if (identity == null || ParseFirestoreStorageIdentity(database, ReadReplicaReceiptField(identity, "storeId"))
                        .BindingFingerprint != enrollment.SourceStorageIdentity.BindingFingerprint)
                        throw new FirestoreReplicaConflict("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH",
                            "The physical Firestore database differs from the enrolled source.");

                    var stored = await ReadFirestoreTransactionDocumentAsync(receipt, transaction,
                        new[] { "operationId", "requestHash", "resultJson" }, cancellationToken).ConfigureAwait(false);
                    if (stored != null)
                        return (ReadFirestoreConditionalReceiptFields(DecodeFirestoreIdentityFields(stored), plan),
                            Array.Empty<Native.Write>());

                    var fence = database.Collection(ConditionalFenceCollection).Document(receipt.Id);
                    var fenceSnapshot = await ReadFirestoreTransactionDocumentAsync(fence, transaction,
                        new[] { "operationId", "revision" }, cancellationToken).ConfigureAwait(false);
                    if (fenceSnapshot != null)
                        ValidateOperationFenceFields(DecodeFirestoreIdentityFields(fenceSnapshot), plan.OperationId);

                    var writes = new List<Native.Write>();
                    var acknowledged = new Dictionary<Guid, IHolon>();
                    var conflicts = new List<CosmicConcurrencyConflict>();
                    foreach (var node in plan.Nodes)
                    {
                        var id = node.Snapshot.Id;
                        var target = database.Collection("holons").Document(id.ToString("D"));
                        var current = await ReadFirestoreIdentityDocumentAsync(target, transaction, cancellationToken).ConfigureAwait(false);
                        var fields = current == null ? null : DecodeFirestoreIdentityFields(current);
                        var old = fields == null ? null : await ReadFirestoreHistoryAsync<Holon>(fields, target.Id, 0).ConfigureAwait(false);
                        var actual = old == null ? (Guid?)null : CosmicConcurrency.GetProviderRevisionId(old, FirestoreReplicaAuthority) ?? Guid.Empty;
                        if (actual != plan.ExpectedRevisions[id])
                        {
                            conflicts.Add(new CosmicConcurrencyConflict { HolonId = id, ExpectedRevisionId = plan.ExpectedRevisions[id],
                                ActualRevisionId = actual, ActualVersionId = old?.VersionId, ActualVersion = old?.Version });
                            continue;
                        }

                        var proposal = PrepareFirestoreConditionalHolon(plan, node, old);
                        var staged = await StageFirestoreDocumentAsync(proposal, fields, target.Id, IsVersionControlEnabled).ConfigureAwait(false);
                        var json = staged.ToString(Newtonsoft.Json.Formatting.None);
                        var committed = OasisJson.DeserializeStored<Holon>(json);
                        acknowledged.Add(id, committed);
                        writes.Add(plan.Operation == ConditionalHolonOperation.HardDelete
                            ? new Native.Write { Delete = target.Path, CurrentDocument = ReplicaPrecondition(current) }
                            : FirestoreReplicaDocumentWrite(target, CreateFirestoreHolonFields(committed, json), current));
                    }

                    if (conflicts.Count != 0)
                        throw new CosmicConcurrencyException(conflicts);
                    foreach (var edge in plan.ChildIds)
                        acknowledged[edge.Key].Children = edge.Value.Select(id => acknowledged[id]).ToList();

                    var receiptJson = ConditionalHolonReceipt.Write(plan.RootIds.Select(id => acknowledged[id]).ToArray(), plan);
                    var validated = ValidateFirestoreConditionalReceipt(receiptJson, plan);
                    writes.Add(FirestoreReplicaDocumentWrite(receipt, new()
                    {
                        ["operationId"] = plan.OperationId.ToString("D"), ["requestHash"] = plan.Fingerprint, ["resultJson"] = receiptJson
                    }, null));
                    writes.Add(FirestoreReplicaDocumentWrite(fence, new()
                    {
                        ["operationId"] = plan.OperationId.ToString("D"), ["revision"] = Guid.NewGuid().ToString("D")
                    }, fenceSnapshot));
                    await StageFirestoreOutboundDeliveriesAsync(database, transaction, writes, plan, validated,
                        cancellationToken).ConfigureAwait(false);
                    return (validated, writes);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
            return FirestoreGraphAcknowledgement(roots, plan.Operation);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (FirestoreReplicaConflict cause) { return FirestoreGraphFailure(cause.Code, cause.Message, cause); }
        catch (CosmicConcurrencyException cause) { return FirestoreGraphFailure(CosmicConcurrencyException.Code, cause.Message, cause); }
        catch (StorageLifecycleConflictException cause) { return FirestoreGraphFailure(StorageLifecycleConflictException.ErrorCode, cause.Message, cause); }
        catch (FirestoreOperationReuseException cause) { return FirestoreGraphFailure("COSMIC_OPERATION_ID_REUSED", cause.Message, cause); }
        catch (GoogleFirestoreMutationReconciliationException cause)
        { return FirestoreGraphFailure("COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED", cause.Message, cause); }
        catch (RpcException cause) when (cause.StatusCode is StatusCode.AlreadyExists or StatusCode.FailedPrecondition or StatusCode.NotFound or StatusCode.Aborted)
        { return FirestoreGraphFailure(CosmicConcurrencyException.Code, "Firestore graph conditions changed before commit.", cause); }
        catch (Exception cause)
        { return FirestoreGraphFailure("COSMIC_CONDITIONAL_WRITE_FAILED", "Firestore enrolled graph operation failed.", cause); }
    }

    private static DocumentReference FirestoreOutboxDelivery(FirestoreDb database, long sequence)
        => database.Collection(FirestoreOutboxCollection).Document(sequence.ToString("D19", CultureInfo.InvariantCulture));

    private static async Task StageFirestoreOutboundDeliveriesAsync(FirestoreDb database,
        ByteString transaction, List<Native.Write> writes,
        ConditionalHolonPlan plan, IEnumerable<IHolon> acknowledged, CancellationToken cancellationToken)
    {
        var enrollment = plan.ReplicationEnrollment;
        var counter = database.Collection(FirestoreOutboxSequenceCollection).Document("sequence");
        var observed = await ReadFirestoreTransactionDocumentAsync(counter, transaction, new[] { "sequence" }, cancellationToken).ConfigureAwait(false);
        long sequence = 0;
        if (observed != null && (!observed.Fields.TryGetValue("sequence", out var value)
            || value.ValueTypeCase != Native.Value.ValueTypeOneofCase.IntegerValue
            || (sequence = value.IntegerValue) <= 0))
            throw new InvalidDataException("The Firestore outbox sequence is invalid.");

        var payload = CommittedHolonReplicaPayload.Capture(new CommittedHolonReplicaRequest(
            plan.OperationId, enrollment.SourceId, plan.Authority, plan.ActorId, plan.Operation,
            acknowledged, plan.SaveChildren, plan.Recursive, plan.MaxChildDepth, enrollment.NativeRoutes));
        var digest = HolonRecoveryEvidenceJson.Digest(payload);

        foreach (var route in enrollment.NativeRoutes)
        {
            sequence = checked(sequence + 1);
            var reference = FirestoreOutboxDelivery(database, sequence);
            var fields = new Dictionary<string, object>
            {
                ["sequence"] = sequence,
                ["operationId"] = plan.OperationId.ToString("D"),
                ["targetId"] = route.TargetId,
                ["payload"] = payload,
                ["digest"] = digest,
                ["state"] = nameof(HolonDeliveryState.Pending),
                ["attemptCount"] = 0L
            };
            writes.Add(FirestoreReplicaDocumentWrite(reference, fields, null));
        }

        writes.Add(FirestoreReplicaDocumentWrite(counter, new() { ["sequence"] = sequence }, observed));
    }
}
