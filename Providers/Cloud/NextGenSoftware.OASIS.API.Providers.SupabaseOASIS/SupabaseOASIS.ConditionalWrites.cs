using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;

public partial class SupabaseOASIS : IConditionalHolonStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
        => ExecuteConditionalHolonWriteAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(
        ConditionalHolonWriteRequest request, CancellationToken cancellationToken = default)
        => ExecuteSupabaseConditionalWriteAsync(request, null, cancellationToken);

    private async Task<OASISResult<IEnumerable<IHolon>>> ExecuteSupabaseConditionalWriteAsync(
        ConditionalHolonWriteRequest request, HolonReplicationEnrollment? enrollment, CancellationToken token)
    {
        var commitEntered = false;
        try
        {
            token.ThrowIfCancellationRequested();
            if (enrollment != null && (enrollment.SourceStorageIdentity == null || enrollment.NativeRoutes.Count == 0))
                throw new ArgumentException("Supabase replication requires a physically bound source and targets.", nameof(enrollment));
            var plan = enrollment == null ? ConditionalHolonPlan.Create(request, ReplicaAuthority)
                : ConditionalHolonPlan.CreateReplicating(request, ReplicaAuthority, enrollment);
            if (!IsProviderActivated)
                throw new InvalidOperationException("Activate Supabase before committing a conditional write.");
            var identity = enrollment?.SourceStorageIdentity;
            if (identity == null)
            {
                var captured = await CaptureStorageIdentityAsync(token).ConfigureAwait(false);
                if (captured.IsError)
                    return ReplicaFailure(captured.ErrorCode, captured.Message, captured.Exception);
                identity = captured.Result;
            }
            if (!Guid.TryParse(identity.StoreId, out var storeId) || storeId == Guid.Empty
                || CreateSupabaseStorageIdentity(storeId).BindingFingerprint != identity.BindingFingerprint)
                return SourceBindingMismatch();

            using var receiptDoc = await ReadReplicaRpcAsync("oasis_read_source_receipt",
                new { expected_store = storeId, requested_operation = plan.OperationId }, token).ConfigureAwait(false);
            var receipt = SingleReplicaRpcRow(receiptDoc);
            if (!receipt.GetProperty("store_matches").GetBoolean())
                return SourceBindingMismatch();
            var originalHash = receipt.GetProperty("request_hash").GetString();
            if (originalHash != null)
                return originalHash == plan.Fingerprint
                    ? AcknowledgeSource(plan, receipt.GetProperty("result_json").GetString())
                    : SourceOperationReused();

            using var state = await ReadReplicaRpcAsync("oasis_read_replica_state",
                new { expected_store = storeId, requested_ids = plan.Nodes.Select(node => node.Snapshot.Id).ToArray() },
                token).ConfigureAwait(false);
            var observations = state.RootElement.EnumerateArray().ToDictionary(row => row.GetProperty("id").GetGuid());
            if (observations.Count != plan.Nodes.Count)
                throw new InvalidDataException("Supabase returned an incomplete source state observation.");

            var previous = new Dictionary<Guid, Holon?>();
            var conflicts = new List<CosmicConcurrencyConflict>();
            var conditions = request.Write.Conditions.ToDictionary(condition => condition.HolonId);
            foreach (var node in plan.Nodes)
            {
                var id = node.Snapshot.Id;
                var observed = observations[id];
                if (!observed.GetProperty("store_matches").GetBoolean())
                    return SourceBindingMismatch();
                if (observed.GetProperty("replica_head").ValueKind != JsonValueKind.Null)
                    return ReplicaFailure("COSMIC_SOURCE_REPLICA_CONFLICT", "Supabase cannot commit a source write over a protected replica.");
                var current = ReadCurrentHolon(id, observed.GetProperty("current_row"));
                previous.Add(id, current);
                var expected = conditions[id].ExpectedRevisionId;
                var actual = current == null ? (Guid?)null
                    : CosmicConcurrency.GetProviderRevisionId(current, ReplicaAuthority) ?? Guid.Empty;
                if (expected != actual)
                    conflicts.Add(new CosmicConcurrencyConflict { HolonId = id, ExpectedRevisionId = expected,
                        ActualRevisionId = actual, ActualVersionId = current?.VersionId, ActualVersion = current?.Version });
            }
            if (conflicts.Count != 0)
                throw new CosmicConcurrencyException(conflicts);

            var now = DateTime.UtcNow;
            var records = new Dictionary<Guid, IHolon>();
            var mutations = new List<object>();
            var saving = plan.Operation == ConditionalHolonOperation.Save;
            foreach (var node in plan.Nodes)
            {
                var old = previous[node.Snapshot.Id];
                if (saving && old?.IsDeleted == true)
                    throw new StorageLifecycleConflictException("An active save cannot overwrite a Supabase tombstone.");
                var record = saving ? node.Snapshot : StoredRecordJson.Project<Holon>(old
                    ?? throw new InvalidOperationException("A conditional delete requires a current Supabase record."));
                record.ChildRoleManifestJson = HolonChildRolePersistence.Resolve(node,
                    saving ? HolonChildRoleWriteKind.SourceSave : HolonChildRoleWriteKind.SourceDelete,
                    old != null, old?.ChildRoleManifestJson);
                record.Version = old == null ? 1 : checked(old.Version + 1);
                record.PreviousVersionId = old?.VersionId ?? Guid.Empty;
                record.VersionId = Guid.NewGuid();
                record.CreatedDate = old?.CreatedDate ?? now;
                record.CreatedByAvatarId = old?.CreatedByAvatarId ?? plan.ActorId;
                record.CreatedProviderType = old == null ? new(ReplicaAuthority) : old.CreatedProviderType;
                record.ModifiedDate = old == null ? DateTime.MinValue : now;
                record.ModifiedByAvatarId = old == null ? Guid.Empty : plan.ActorId;
                record.InstanceSavedOnProviderType = new EnumValue<Core.Enums.ProviderType>(ReplicaAuthority);
                if (!saving)
                {
                    record.IsActive = false;
                    record.DeletedDate = now;
                    record.DeletedByAvatarId = plan.ActorId;
                }
                record.ProviderUniqueStorageKey ??= new();
                record.ProviderUniqueStorageKey[ReplicaAuthority] = record.Id.ToString("D");
                CosmicConcurrency.SetProviderRevisionId(record, ReplicaAuthority, Guid.NewGuid());
                records.Add(record.Id, record);
                mutations.Add(new { id = record.Id, expected_current = observations[record.Id].GetProperty("current_row"),
                    current = plan.Operation == ConditionalHolonOperation.HardDelete ? null : CreateCurrentHolonRow(record) });
            }

            foreach (var edge in plan.ChildIds)
                records[edge.Key].Children = edge.Value.Select(id => records[id]).ToList();
            var delta = HolonCommittedDelta.Capture(plan.OperationId, plan.ActorId, plan.Authority, plan.Operation,
                now, previous.Values.OfType<Holon>(), records.Values, retainStoredValues: false);
            var receiptJson = ConditionalHolonReceipt.Write(plan.RootIds.Select(id => records[id]).ToArray(), plan, delta);
            var acknowledged = ConditionalHolonReceipt.Read(receiptJson, plan, out _);
            var deliveries = new List<object>();
            if (enrollment != null)
            {
                var payload = CommittedHolonReplicaPayload.Capture(new CommittedHolonReplicaRequest(plan.OperationId,
                    enrollment.SourceId, plan.Authority, plan.ActorId, plan.Operation, acknowledged,
                    plan.SaveChildren, plan.Recursive, plan.MaxChildDepth, enrollment.NativeRoutes));
                var digest = HolonRecoveryEvidenceJson.Digest(payload);
                foreach (var route in enrollment.NativeRoutes)
                    deliveries.Add(new { target_id = route.TargetId, payload_json = payload, payload_digest = digest });
            }

            token.ThrowIfCancellationRequested();
            commitEntered = true;
            using var committed = await ReadReplicaRpcAsync("oasis_commit_source",
                new { expected_store = storeId, requested_operation = plan.OperationId, expected_hash = plan.Fingerprint,
                    receipt_json = receiptJson, mutations, deliveries }, CancellationToken.None).ConfigureAwait(false);
            var outcome = SingleReplicaRpcRow(committed);
            return outcome.GetProperty("outcome").GetString() switch
            {
                "committed" => AcknowledgeSource(plan, outcome.GetProperty("committed_json").GetString()),
                "binding_mismatch" => SourceBindingMismatch(),
                "operation_reused" => SourceOperationReused(),
                "state_conflict" => ReplicaFailure("COSMIC_SOURCE_STATE_CONFLICT", "The Supabase graph changed before source commit."),
                _ => throw new InvalidDataException("Supabase returned an unknown source commit outcome.")
            };
        }
        catch (CosmicConcurrencyException cause) { return ReplicaFailure(CosmicConcurrencyException.Code, cause.Message, cause); }
        catch (StorageLifecycleConflictException cause) { return ReplicaFailure(StorageLifecycleConflictException.ErrorCode, cause.Message, cause); }
        catch (OperationCanceledException) when (!commitEntered && token.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            return ReplicaFailure(commitEntered ? "COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED" : "COSMIC_CONDITIONAL_WRITE_FAILED",
                "Supabase source commit failed.", commitEntered
                    ? new SupabaseReplicaReconciliationException(request.Write.OperationId, cause) : cause);
        }
    }

    private static OASISResult<IEnumerable<IHolon>> AcknowledgeSource(ConditionalHolonPlan plan, string? receipt)
        => ConditionalHolonAcknowledgement.Create(ConditionalHolonReceipt.Read(
            receipt ?? throw new InvalidDataException("Supabase did not return its committed source receipt."), plan, out _), plan.Operation);

    private static OASISResult<IEnumerable<IHolon>> SourceBindingMismatch()
        => ReplicaFailure("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH", "The Supabase database differs from the enrolled source.");

    private static OASISResult<IEnumerable<IHolon>> SourceOperationReused()
        => ReplicaFailure("COSMIC_OPERATION_ID_REUSED", "The Supabase operation ID belongs to different content.");
}
