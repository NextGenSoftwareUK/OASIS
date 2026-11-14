using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;

public partial class SupabaseOASIS : IBoundCommittedHolonReplicaStorageProvider
{
    private const Core.Enums.ProviderType ReplicaAuthority = Core.Enums.ProviderType.SupabaseOASIS;
    private static readonly JsonSerializerOptions ReplicaRpcJson = new(_jsonOpts)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        => ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
    {
        var identity = await CaptureStorageIdentityAsync(cancellationToken).ConfigureAwait(false);
        return identity.IsError ? ReplicaFailure(identity.ErrorCode, identity.Message, identity.Exception)
            : await ApplyBoundCommittedHolonReplicaAsync(request, identity.Result, cancellationToken).ConfigureAwait(false);
    }

    public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage)
        => ApplyBoundCommittedHolonReplicaAsync(request, expectedStorage).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedStorage);
        var token = cancellationToken;
        var commitEntered = false;
        try
        {
            token.ThrowIfCancellationRequested();
            CommittedHolonReplicaPlan.ValidateNativeDestination(request, expectedStorage);
            var plan = CommittedHolonReplicaPlan.Create(request, ReplicaAuthority);
            if (!IsProviderActivated)
                throw new InvalidOperationException("Activate Supabase before applying a replica.");
            if (!Guid.TryParse(expectedStorage.StoreId, out var storeId) || storeId == Guid.Empty
                || CreateSupabaseStorageIdentity(storeId).BindingFingerprint != expectedStorage.BindingFingerprint)
                return ReplicaFailure("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", "The Supabase endpoint differs from the enrolled store.");

            using var receiptDocument = await ReadReplicaRpcAsync("oasis_read_replica_receipt",
                new { expected_store = storeId, requested_operation = plan.OperationId }, token).ConfigureAwait(false);
            var receipt = SingleReplicaRpcRow(receiptDocument);
            if (!receipt.GetProperty("store_matches").GetBoolean())
                return ReplicaFailure("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", "The Supabase database differs from the enrolled store.");
            var originalHash = receipt.GetProperty("request_hash").GetString();
            if (originalHash != null)
                return originalHash == plan.Fingerprint
                    ? AcknowledgeReplica(plan, receipt.GetProperty("result_json").GetString())
                    : ReplicaFailure("COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID", "The Supabase receipt belongs to another request.");

            using var state = await ReadReplicaRpcAsync("oasis_read_replica_state",
                new { expected_store = storeId, requested_ids = plan.Nodes.Select(node => node.Snapshot.Id).ToArray() },
                token).ConfigureAwait(false);
            var observations = state.RootElement.EnumerateArray().ToDictionary(row => row.GetProperty("id").GetGuid());
            if (observations.Count != plan.Nodes.Count)
                throw new InvalidDataException("Supabase returned an incomplete replica state observation.");

            var records = new Dictionary<Guid, IHolon>();
            var mutations = new List<object>();
            var hardDelete = plan.Operation == ConditionalHolonOperation.HardDelete;
            foreach (var node in plan.Nodes)
            {
                var record = node.Snapshot;
                var observed = observations[record.Id];
                if (!observed.GetProperty("store_matches").GetBoolean())
                    return ReplicaFailure("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", "The Supabase database differs from the enrolled store.");

                var currentRow = observed.GetProperty("current_row");
                var observedHead = observed.GetProperty("replica_head");
                Holon? current = null;
                if (currentRow.ValueKind != JsonValueKind.Null)
                {
                    current = Deserialize<Holon>(currentRow.GetProperty("data_json").GetString()!);
                    var parent = currentRow.GetProperty("parent_holon_id");
                    if (current == null || current.Id != record.Id || current.IsDeleted != currentRow.GetProperty("is_deleted").GetBoolean()
                        || (int)current.HolonType != currentRow.GetProperty("holon_type").GetInt32()
                        || current.ParentHolonId != (parent.ValueKind == JsonValueKind.Null ? Guid.Empty : parent.GetGuid()))
                        throw new InvalidDataException("The Supabase native row differs from its holon snapshot.");
                }
                if (observedHead.ValueKind != JsonValueKind.Null)
                {
                    var head = HolonReplicaHeadRecord.Read(record.Id, ReplicaAuthority,
                        observedHead.GetProperty("source_id").GetString(), observedHead.GetProperty("source_authority").GetString(),
                        observedHead.GetProperty("snapshot_json").GetString(), observedHead.GetProperty("snapshot_digest").GetString(),
                        observedHead.GetProperty("revision_id").GetString(), observedHead.GetProperty("hard_deleted").GetBoolean()).Evidence;
                    HolonReplicaHeadRecord.ValidateCurrent(record.Id, ReplicaAuthority, current, head);
                    if (head.SourceId != plan.SourceId || head.SourceAuthority != plan.SourceAuthority
                        || head.VersionId != record.PreviousVersionId || head.Version != record.Version - 1)
                        return ReplicaFailure("COSMIC_REPLICA_LINEAGE_CONFLICT", "The Supabase replica predecessor differs from the committed source.");
                }
                else if (current != null || record.Version != 1)
                    return ReplicaFailure("COSMIC_REPLICA_LINEAGE_CONFLICT", "The Supabase replica requires an initial source revision.");

                record.ProviderUniqueStorageKey ??= new();
                record.ProviderUniqueStorageKey[ReplicaAuthority] = record.Id.ToString("D");
                record.PreviousVersionProviderUniqueStorageKey?.Remove(ReplicaAuthority);
                record.ProviderMetaData?.Remove(ReplicaAuthority);
                record.InstanceSavedOnProviderType = new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(ReplicaAuthority);
                var revision = Guid.NewGuid();
                CosmicConcurrency.SetProviderRevisionId(record, ReplicaAuthority, revision);
                var snapshot = StoredRecordJson.Serialize<IHolon>(record);
                var stored = StoredRecordJson.DeserializeStored<Holon>(snapshot)
                    ?? throw new InvalidDataException("Supabase could not materialize the replica snapshot.");
                plan.ValidateAcknowledgedRecord(stored);
                records.Add(stored.Id, stored);

                mutations.Add(new
                {
                    id = stored.Id,
                    expected_current = currentRow,
                    expected_head = observedHead,
                    current = hardDelete ? null : new { id = stored.Id,
                        parent_holon_id = stored.ParentHolonId == Guid.Empty ? (Guid?)null : stored.ParentHolonId,
                        holon_type = (int)stored.HolonType, is_deleted = stored.IsDeleted, data_json = Serialize(stored) },
                    head = new { id = stored.Id, source_id = plan.SourceId, source_authority = plan.SourceAuthority.ToString(),
                        snapshot_json = snapshot, snapshot_digest = HolonRecoveryEvidenceJson.Digest(snapshot),
                        revision_id = revision, hard_deleted = hardDelete }
                });
            }

            foreach (var edge in plan.ChildIds)
                records[edge.Key].Children = edge.Value.Select(id => records[id]).ToList();
            var receiptJson = ConditionalHolonReceipt.Write(plan.RootIds.Select(id => records[id]).ToArray(), plan);
            _ = ConditionalHolonReceipt.Read(receiptJson, plan);
            token.ThrowIfCancellationRequested();
            commitEntered = true;
            using var result = await ReadReplicaRpcAsync("oasis_commit_replica",
                new { expected_store = storeId, requested_operation = plan.OperationId, expected_hash = plan.Fingerprint,
                    receipt_json = receiptJson, mutations }, CancellationToken.None).ConfigureAwait(false);
            var outcome = SingleReplicaRpcRow(result);
            return outcome.GetProperty("outcome").GetString() switch
            {
                "committed" => AcknowledgeReplica(plan, outcome.GetProperty("committed_json").GetString()),
                "binding_mismatch" => ReplicaFailure("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", "The Supabase database differs from the enrolled store."),
                "operation_reused" => ReplicaFailure("COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID", "The Supabase receipt belongs to another request."),
                "state_conflict" => ReplicaFailure("COSMIC_REPLICA_LINEAGE_CONFLICT", "The Supabase graph changed before replica commit."),
                _ => throw new InvalidDataException("Supabase returned an unknown replica commit outcome.")
            };
        }
        catch (OperationCanceledException) when (!commitEntered && token.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            return ReplicaFailure(commitEntered ? "COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED" : "COSMIC_REPLICA_WRITE_FAILED",
                "Supabase replica apply failed.", commitEntered ? new SupabaseReplicaReconciliationException(request.OperationId, cause) : cause);
        }
    }

    private async Task<JsonDocument> ReadReplicaRpcAsync(string function, object args, CancellationToken token)
    {
        using var request = CreatePostgrestRequest(HttpMethod.Post, $"rpc/{function}");
        // The database distinguishes an explicit null (delete or absent row) from an omitted field.
        request.Content = new StringContent(JsonSerializer.Serialize(args, ReplicaRpcJson), Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(content, cancellationToken: token).ConfigureAwait(false);
    }

    private static JsonElement SingleReplicaRpcRow(JsonDocument document)
        => document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() == 1
            ? document.RootElement[0] : throw new InvalidDataException("Supabase did not return exactly one replica outcome.");

    private static OASISResult<IEnumerable<IHolon>> AcknowledgeReplica(CommittedHolonReplicaPlan plan, string? receipt)
        => ConditionalHolonAcknowledgement.Create(ConditionalHolonReceipt.Read(
            receipt ?? throw new InvalidDataException("Supabase did not return a committed receipt."), plan), plan.Operation);

    private static OASISResult<IEnumerable<IHolon>> ReplicaFailure(string code, string message, Exception? cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

    private sealed class SupabaseReplicaReconciliationException(Guid operationId, Exception cause)
        : Exception("Supabase replica commit requires reconciliation against the original receipt.", cause),
            INativeMutationReconciliationRequired
    {
        public Guid OperationId { get; } = operationId;
        public string OriginalTransactionId => string.Empty;
    }
}
