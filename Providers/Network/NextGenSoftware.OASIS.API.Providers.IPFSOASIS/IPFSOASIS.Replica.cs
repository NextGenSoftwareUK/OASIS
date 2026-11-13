using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS : IBoundCommittedHolonReplicaStorageProvider, ICommittedHolonReplicaReceiptStorageProvider,
    IHolonReplicaHeadAuditStorageProvider, IConditionalHolonReceiptEvidenceStorageProvider, IConditionalHolonDeltaStorageProvider
{
    private const ProviderType ReplicaAuthority = Core.Enums.ProviderType.IPFSOASIS;
    private sealed class ReplicaLineageConflictException(string message, Exception cause = null)
        : InvalidOperationException(message, cause);
    private sealed class ReplicaStorageBindingException(string message) : InvalidOperationException(message);

    // Atomic callers acquire both catalogue locks before reading this snapshot.
    private async Task<(JObject Catalogue, Dictionary<string, HolonResume> Records, CosmicStorageIdentity Storage)>
        ReadBoundCatalogueAsync(OASISDNA dna, CancellationToken cancellationToken)
    {
        var settings = dna.OASIS.StorageProviders.IPFSOASIS;
        if (IPFSClient == null)
            throw new InvalidOperationException("An active IPFS client is required.");
        if (string.IsNullOrWhiteSpace(settings.LookUpIPFSAddress))
            throw new ReplicaStorageBindingException("The IPFS catalogue has no published storage identity.");

        var envelope = await ReadCqlEnvelopeAsync(settings.LookUpIPFSAddress, cancellationToken).ConfigureAwait(false);
        var catalogue = ParseCatalogue(UnprotectIndex(envelope, cancellationToken));
        var records = DeserializeLookup(catalogue, out var storeId);
        cancellationToken.ThrowIfCancellationRequested();
        if (storeId == Guid.Empty)
            throw new ReplicaStorageBindingException("The IPFS catalogue has no persisted storage identity.");
        return (catalogue, records, CreateCatalogueStorageIdentity(settings.ConnectionString, storeId));
    }

    public async Task<OASISResult<HolonReplicaHeadReadResult>> ReadHolonReplicaHeadsAsync(
        HolonReplicaHeadReadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var gate = CatalogueGate;
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var (catalogue, records, storage) = await ReadBoundCatalogueAsync(ReadPersistedCatalogueDna(), cancellationToken)
                    .ConfigureAwait(false);
                if (storage.BindingFingerprint != request.ExpectedStorage.BindingFingerprint)
                    return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_STORAGE_BINDING_MISMATCH",
                        Message = "The IPFS catalogue does not match the enrolled replica store." };

                var heads = GetReplicaObject(catalogue, "replicaHeads");
                var currentCids = records.Where(entry => entry.Value.RecordKind == "holon")
                    .ToDictionary(entry => entry.Value.Id, entry => entry.Key);
                var observations = new List<HolonReplicaHeadObservation>(request.HolonIds.Count);
                foreach (var id in request.HolonIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentCids.TryGetValue(id, out var currentCid);
                    var head = heads[id.ToString("D")];
                    var evidence = head == null ? null :
                        await ValidateReplicaHeadAsync(id, currentCid, head, cancellationToken).ConfigureAwait(false);
                    observations.Add(new(id, currentCid != null, evidence));
                }
                return new(new HolonReplicaHeadReadResult(storage, observations)) { IsLoaded = true };
            }
            finally
            {
                gate.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED",
                Exception = cause, Message = cause.Message };
        }
    }

    public async Task<OASISResult<CommittedHolonReplicaReceiptEvidence>> ReadCommittedHolonReplicaReceiptAsync(
        CommittedHolonReplicaReceiptReadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = CommittedHolonReplicaReceiptProjection.Prepare(request);
            var gate = CatalogueGate;
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // The pointer is read once. Its CID identifies an immutable native snapshot.
                var (catalogue, _, storage) = await ReadBoundCatalogueAsync(ReadPersistedCatalogueDna(), cancellationToken)
                    .ConfigureAwait(false);
                if (storage.BindingFingerprint != request.ExpectedStorage.BindingFingerprint)
                    return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_STORAGE_BINDING_MISMATCH",
                        Message = "The IPFS catalogue does not match the enrolled replica store." };

                var receipt = GetReplicaObject(catalogue, "replicaReceipts")[plan.OperationId.ToString("D")];
                var evidence = CommittedHolonReplicaReceiptProjection.Project(request, plan,
                    receipt == null ? null : plan.Fingerprint, receipt == null ? null : ReplicaText(receipt),
                    HolonReplicaHeadReadConsistency.NativeSnapshot);
                return new(evidence) { IsLoaded = true };
            }
            finally
            {
                gate.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_READ_FAILED",
                Exception = cause, Message = cause.Message };
        }
    }

    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        => ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CancellationToken cancellationToken = default)
        => ApplyReplicaAsync(request, null, false, cancellationToken);

    public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage)
        => ApplyBoundCommittedHolonReplicaAsync(request, expectedStorage).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
        => ApplyReplicaAsync(request, expectedStorage, true, cancellationToken);

    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CancellationToken cancellationToken = default)
        => ReadConditionalOutcomeAsync(original, null, false, cancellationToken);

    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadBoundConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CosmicStorageIdentity expectedSource,
        CancellationToken cancellationToken = default)
        => ReadConditionalOutcomeAsync(original, expectedSource, true, cancellationToken);

    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonReceiptEvidenceAsync(
        ConditionalHolonReceiptEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ReadConditionalOutcomeAsync(request.Original, request.ExpectedSource, true, cancellationToken, request);
    }

    private async Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalOutcomeAsync(
        ConditionalHolonWriteRequest original, CosmicStorageIdentity expectedSource, bool requireBinding,
        CancellationToken cancellationToken, ConditionalHolonReceiptEvidenceRequest evidenceRequest = null)
    {
        try
        {
            if (requireBinding)
                ArgumentNullException.ThrowIfNull(expectedSource);
            var plan = ConditionalHolonPlan.Create(original, ReplicaAuthority);
            var gate = CatalogueGate;
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var (catalogue, _, storage) = await ReadBoundCatalogueAsync(
                    ReadPersistedCatalogueDna(), cancellationToken).ConfigureAwait(false);
                if (expectedSource != null && storage.BindingFingerprint != expectedSource.BindingFingerprint)
                    return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_SOURCE_STORAGE_BINDING_MISMATCH",
                        Message = "The IPFS catalogue does not match the requested source store." };

                var receipt = GetReplicaObject(catalogue, "conditionalReceipts")[plan.OperationId.ToString("D")];
                HolonCommittedDelta delta = null;
                ConditionalHolonReceiptEvidence evidence = null;
                if (receipt != null)
                {
                    var roots = ConditionalHolonReceipt.Read(ReplicaText(receipt), plan, out delta);
                    if (evidenceRequest != null)
                        evidence = ConditionalHolonReceiptEvidenceProjection.Create(evidenceRequest, roots);
                }
                var state = receipt == null ? ConditionalHolonReceiptState.Absent : ConditionalHolonReceiptState.Committed;
                return new(new ConditionalHolonWriteOutcome(plan.OperationId, plan.ActorId, plan.Authority,
                    plan.Operation, state, delta?.SummaryJson, evidence)) { IsLoaded = true };
            }
            finally
            {
                gate.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_CONDITIONAL_RECEIPT_READ_FAILED",
                Message = cause.Message, Exception = cause };
        }
    }

    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
        => ExecuteConditionalHolonWriteAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(
        ConditionalHolonWriteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = ConditionalHolonPlan.Create(request, ReplicaAuthority);
            var gate = CatalogueGate;
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var lease = await AcquireCatalogueFileLeaseAsync(cancellationToken).ConfigureAwait(false);
                var dna = ReadPersistedCatalogueDna();
                var (catalogue, records, _) = await ReadBoundCatalogueAsync(dna, cancellationToken).ConfigureAwait(false);
                var receipts = GetReplicaObject(catalogue, "conditionalReceipts");
                var operationId = plan.OperationId.ToString("D");
                if (receipts[operationId] is JToken receipt)
                    return AcknowledgeConditionalWrite(ReplicaText(receipt), plan);

                var currentCids = records.Where(entry => entry.Value.RecordKind == "holon")
                    .ToDictionary(entry => entry.Value.Id, entry => entry.Key);
                var previous = new Dictionary<Guid, Holon>();
                var conflicts = new List<CosmicConcurrencyConflict>();
                foreach (var node in plan.Nodes)
                {
                    var id = node.Snapshot.Id;
                    currentCids.TryGetValue(id, out var cid);
                    var current = cid == null ? null : DecodePrivateRecord<Holon>(cid, "holon", id,
                        await ReadCqlEnvelopeAsync(cid, cancellationToken).ConfigureAwait(false), cancellationToken);
                    previous.Add(id, current);
                    var expected = plan.ExpectedRevisions[id];
                    var actual = current == null ? (Guid?)null : CosmicConcurrency.GetProviderRevisionId(current, ReplicaAuthority) ?? Guid.Empty;
                    if (expected != actual)
                        conflicts.Add(new CosmicConcurrencyConflict { HolonId = id, ExpectedRevisionId = expected,
                            ActualRevisionId = actual, ActualVersionId = current?.VersionId, ActualVersion = current?.Version });
                }
                if (conflicts.Count != 0)
                    throw new CosmicConcurrencyException(conflicts);

                var snapshots = new Dictionary<Guid, Holon>();
                var now = DateTime.UtcNow;
                foreach (var node in plan.Nodes)
                {
                    var prior = previous[node.Snapshot.Id];
                    var saving = plan.Operation == ConditionalHolonOperation.Save;
                    var record = saving ? node.Snapshot : OasisJson.Project<Holon>(prior);
                    if (saving)
                        HolonSaveLifecycle.RequireCompatibleSave(prior, record);
                    record.ChildRoleManifestJson = HolonChildRolePersistence.Resolve(node,
                        saving ? HolonChildRoleWriteKind.SourceSave : HolonChildRoleWriteKind.SourceDelete,
                        prior != null, prior?.ChildRoleManifestJson);
                    record.Version = 0;
                    record.VersionId = Guid.Empty;
                    record.PreviousVersionId = Guid.Empty;
                    await StoredRecordVersions.StampAsync(record.Id, record, prior, null).ConfigureAwait(false);
                    record.CreatedDate = prior?.CreatedDate ?? now;
                    record.CreatedByAvatarId = prior?.CreatedByAvatarId ?? plan.ActorId;
                    record.CreatedProviderType = prior?.CreatedProviderType ?? new NextGenSoftware.Utilities.EnumValue<ProviderType>(ReplicaAuthority);
                    record.ModifiedDate = prior == null ? DateTime.MinValue : now;
                    record.ModifiedByAvatarId = prior == null ? Guid.Empty : plan.ActorId;
                    record.InstanceSavedOnProviderType = new NextGenSoftware.Utilities.EnumValue<ProviderType>(ReplicaAuthority);
                    record.IsNewHolon = false;
                    if (!saving)
                    {
                        record.IsActive = false;
                        record.DeletedDate = now;
                        record.DeletedByAvatarId = plan.ActorId;
                    }
                    record.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                    record.ProviderUniqueStorageKey.Remove(ReplicaAuthority);
                    record.PreviousVersionProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                    record.PreviousVersionProviderUniqueStorageKey.Remove(ReplicaAuthority);
                    currentCids.TryGetValue(record.Id, out var previousCid);
                    if (previousCid != null)
                        record.PreviousVersionProviderUniqueStorageKey[ReplicaAuthority] = previousCid;
                    CosmicConcurrency.SetProviderRevisionId(record, ReplicaAuthority, Guid.NewGuid());
                    await StageCatalogueHolonAsync(record, StoredRecordJson.Serialize<IHolon>(record), previousCid,
                        plan.Operation == ConditionalHolonOperation.HardDelete, records, cancellationToken).ConfigureAwait(false);
                    snapshots.Add(record.Id, record);
                }

                foreach (var record in snapshots.Values)
                    record.Children = plan.ChildIds[record.Id].Select(id => (IHolon)snapshots[id]).ToList();
                var roots = plan.RootIds.Select(id => (IHolon)snapshots[id]).ToArray();
                var delta = HolonCommittedDelta.Capture(plan.OperationId, plan.ActorId, plan.Authority, plan.Operation,
                    now, previous.Values.Where(record => record != null), snapshots.Values, retainStoredValues: false);
                var acknowledgement = ConditionalHolonReceipt.Write(roots, plan, delta);
                var acknowledged = AcknowledgeConditionalWrite(acknowledgement, plan);
                receipts[operationId] = acknowledgement;
                catalogue["conditionalReceipts"] = receipts;
                await PublishCatalogueAsync(catalogue, records, dna, cancellationToken).ConfigureAwait(false);
                return acknowledged;
            }
            finally
            {
                gate.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception cause)
        {
            return ReplicaFailure(cause is CosmicConcurrencyException ? CosmicConcurrencyException.Code
                : "COSMIC_CONDITIONAL_WRITE_FAILED", cause);
        }
    }

    private static OASISResult<IEnumerable<IHolon>> AcknowledgeConditionalWrite(string receipt, ConditionalHolonPlan plan)
    {
        var roots = ConditionalHolonReceipt.Read(receipt, plan, out var delta);
        var result = ConditionalHolonAcknowledgement.Create(roots, plan.Operation);
        result.MetaData ??= new Dictionary<string, string>();
        result.MetaData["COSMIC.OperationId"] = plan.OperationId.ToString("D");
        if (delta != null)
            result.MetaData[HolonCommittedDelta.ResultMetadataKey] = delta.SummaryJson;
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ApplyReplicaAsync(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage, bool requireBinding, CancellationToken cancellationToken)
    {
        try
        {
            if (requireBinding)
                ArgumentNullException.ThrowIfNull(expectedStorage);
            CommittedHolonReplicaPlan.ValidateNativeDestination(request, expectedStorage);
            var plan = CommittedHolonReplicaPlan.Create(request, ReplicaAuthority);
            var gate = CatalogueGate;
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var lease = await AcquireCatalogueFileLeaseAsync(cancellationToken).ConfigureAwait(false);
                var dna = ReadPersistedCatalogueDna();
                var (catalogue, records, storage) = await ReadBoundCatalogueAsync(dna, cancellationToken).ConfigureAwait(false);
                if (expectedStorage != null && storage.BindingFingerprint != expectedStorage.BindingFingerprint)
                    return ReplicaFailure("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH",
                        new InvalidOperationException("The IPFS catalogue does not match the enrolled replica store."));

                var receipts = GetReplicaObject(catalogue, "replicaReceipts");
                var operationId = plan.OperationId.ToString("D");
                if (receipts[operationId] is JToken receipt)
                {
                    try
                    {
                        return ReplicaSuccess(ConditionalHolonReceipt.Read(ReplicaText(receipt), plan), plan);
                    }
                    catch (InvalidDataException cause)
                    {
                        return ReplicaFailure("COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID", cause);
                    }
                }

                var heads = GetReplicaObject(catalogue, "replicaHeads");
                var previousCids = await ValidateReplicaHeadsAsync(records, heads, plan, cancellationToken).ConfigureAwait(false);
                var snapshots = plan.Nodes.ToDictionary(node => node.Snapshot.Id, node => node.Snapshot);
                foreach (var record in snapshots.Values)
                    await StageReplicaRecordAsync(record, previousCids[record.Id], plan, records, heads, cancellationToken)
                        .ConfigureAwait(false);

                foreach (var record in snapshots.Values)
                    record.Children = plan.ChildIds[record.Id].Select(id => (IHolon)snapshots[id]).ToList();
                var roots = plan.RootIds.Select(id => (IHolon)snapshots[id]).ToArray();
                var acknowledgement = ConditionalHolonReceipt.Write(roots, plan);
                var acknowledgedRoots = ConditionalHolonReceipt.Read(acknowledgement, plan);
                receipts[operationId] = acknowledgement;
                catalogue["replicaHeads"] = heads;
                catalogue["replicaReceipts"] = receipts;
                await PublishCatalogueAsync(catalogue, records, dna, cancellationToken).ConfigureAwait(false);
                return ReplicaSuccess(acknowledgedRoots, plan);
            }
            finally
            {
                gate.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception cause)
        {
            var code = cause switch
            {
                ReplicaLineageConflictException => "COSMIC_REPLICA_LINEAGE_CONFLICT",
                ReplicaStorageBindingException => "COSMIC_REPLICA_STORAGE_BINDING_INVALID",
                _ => "COSMIC_REPLICA_WRITE_FAILED"
            };
            return ReplicaFailure(code, cause);
        }
    }

    private async Task<Dictionary<Guid, string>> ValidateReplicaHeadsAsync(Dictionary<string, HolonResume> records,
        JObject heads, CommittedHolonReplicaPlan plan, CancellationToken cancellationToken)
    {
        var previousCids = new Dictionary<Guid, string>();
        var currentCids = records.Where(entry => entry.Value.RecordKind == "holon")
            .ToDictionary(entry => entry.Value.Id, entry => entry.Key);
        foreach (var node in plan.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = node.Snapshot;
            currentCids.TryGetValue(record.Id, out var currentCid);
            var head = heads[record.Id.ToString("D")];
            if (head == null)
            {
                if (currentCid != null || record.Version != 1)
                    throw new ReplicaLineageConflictException("The IPFS replica does not begin at an unused version-one identity.");
                previousCids.Add(record.Id, null);
                continue;
            }

            var evidence = await ValidateReplicaHeadAsync(record.Id, currentCid, head, cancellationToken).ConfigureAwait(false);
            if (evidence.SourceId != plan.SourceId || evidence.SourceAuthority != plan.SourceAuthority
                || evidence.VersionId != record.PreviousVersionId || evidence.Version != record.Version - 1)
                throw new ReplicaLineageConflictException("The IPFS replica lineage does not match the enrolled source.");
            previousCids.Add(record.Id, ReplicaText(head["cid"]));
        }
        return previousCids;
    }

    private async Task<HolonReplicaHeadEvidence> ValidateReplicaHeadAsync(Guid id, string currentCid, JToken head,
        CancellationToken cancellationToken)
    {
        var headCid = ReplicaText(head["cid"]);
        var validated = HolonReplicaHeadRecord.Read(id, ReplicaAuthority, ReplicaText(head["source"]),
            ReplicaText(head["authority"]), ReplicaText(head["snapshot"]), ReplicaText(head["digest"]),
            ReplicaText(head["revision"]), head["hardDeleted"]?.Type == JTokenType.Boolean
                ? head.Value<bool>("hardDeleted") : throw new InvalidDataException("Invalid IPFS replica deletion state."));
        if (validated.Evidence.HardDeleted ? currentCid != null : currentCid != headCid)
            throw new ReplicaLineageConflictException("The IPFS replica head CID differs from the current catalogue.");
        if (validated.Evidence.HardDeleted)
        {
            var tombstone = DecodePrivateRecord<Holon>(headCid, "holon", id,
                await ReadCqlEnvelopeAsync(headCid, cancellationToken).ConfigureAwait(false), cancellationToken);
            if (!tombstone.IsDeleted || tombstone.VersionId != validated.Evidence.VersionId ||
                tombstone.Version != validated.Evidence.Version ||
                tombstone.PreviousVersionId != validated.Evidence.PreviousVersionId ||
                CosmicConcurrency.GetProviderRevisionId(tombstone, ReplicaAuthority) != validated.Evidence.ProviderRevisionId ||
                CommittedHolonReplicaPlan.PortableContentDigest(tombstone, ReplicaAuthority) != validated.Evidence.PortableContentDigest)
                throw new InvalidDataException("The native IPFS tombstone differs from its protected replica head.");
        }
        var current = currentCid == null ? null : DecodePrivateRecord<Holon>(currentCid, "holon", id,
            await ReadCqlEnvelopeAsync(currentCid, cancellationToken).ConfigureAwait(false), cancellationToken);
        try
        {
            HolonReplicaHeadRecord.ValidateCurrent(id, ReplicaAuthority, current, validated.Evidence);
        }
        catch (InvalidDataException cause)
        {
            throw new ReplicaLineageConflictException(cause.Message, cause);
        }
        return validated.Evidence;
    }

    private async Task StageReplicaRecordAsync(Holon record, string previousCid, CommittedHolonReplicaPlan plan,
        Dictionary<string, HolonResume> records, JObject heads, CancellationToken cancellationToken)
    {
        record.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
        record.ProviderUniqueStorageKey.Remove(ReplicaAuthority);
        record.PreviousVersionProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
        record.PreviousVersionProviderUniqueStorageKey.Remove(ReplicaAuthority);
        if (previousCid != null)
            record.PreviousVersionProviderUniqueStorageKey[ReplicaAuthority] = previousCid;
        record.ProviderMetaData?.Remove(ReplicaAuthority);
        var revision = Guid.NewGuid();
        CosmicConcurrency.SetProviderRevisionId(record, ReplicaAuthority, revision);
        record.InstanceSavedOnProviderType = new NextGenSoftware.Utilities.EnumValue<ProviderType>(ReplicaAuthority);
        plan.ValidateAcknowledgedRecord(record);

        var snapshot = StoredRecordJson.Serialize<IHolon>(record);
        var hardDeleted = plan.Operation == ConditionalHolonOperation.HardDelete;
        var cid = await StageCatalogueHolonAsync(record, snapshot, previousCid, hardDeleted, records, cancellationToken)
            .ConfigureAwait(false);
        heads[record.Id.ToString("D")] = new JObject
        {
            ["cid"] = cid,
            ["source"] = plan.SourceId,
            ["authority"] = plan.SourceAuthority.ToString(),
            ["snapshot"] = snapshot,
            ["digest"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))),
            ["revision"] = revision.ToString("D"),
            ["hardDeleted"] = hardDeleted
        };
    }

    private async Task<string> StageCatalogueHolonAsync(Holon record, string snapshot, string previousCid,
        bool hardDeleted, Dictionary<string, HolonResume> records, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var encrypted = ProtectRecord("holon", record.Id, snapshot);
        if (encrypted.Length > CqlMaximumEnvelopeBytes)
            throw new InvalidDataException("The encrypted IPFS holon exceeds the native query and replica read limit.");
        var native = await IPFSClient.FileSystem.AddTextAsync(encrypted,
            cancel: cancellationToken).ConfigureAwait(false);
        var cid = native.Id.ToString();
        record.ProviderUniqueStorageKey[ReplicaAuthority] = cid;
        if (hardDeleted)
            records.Remove(previousCid);
        else
            ReplaceIndexEntry(records, cid, new HolonResume
            {
                Id = record.Id,
                RecordKind = "holon",
                HolonType = record.HolonType,
                ParentHolonId = record.ParentHolonId,
                ProviderUniqueStorageKey = new Dictionary<ProviderType, string>(record.ProviderUniqueStorageKey)
            });
        return cid;
    }

    private static JObject GetReplicaObject(JObject catalogue, string key)
        => catalogue[key] == null ? new JObject() : catalogue[key] as JObject
            ?? throw new InvalidDataException("Invalid IPFS replica catalogue field: " + key);

    private static string ReplicaText(JToken value)
        => value?.Type == JTokenType.String ? value.Value<string>()
            : throw new InvalidDataException("Invalid IPFS replica catalogue value.");

    private static OASISResult<IEnumerable<IHolon>> ReplicaSuccess(IEnumerable<IHolon> roots, CommittedHolonReplicaPlan plan)
        => new(roots)
        {
            IsSaved = plan.Operation == ConditionalHolonOperation.Save,
            SavedCount = plan.Operation == ConditionalHolonOperation.Save ? plan.Nodes.Count : 0,
            IsDeleted = plan.Operation != ConditionalHolonOperation.Save
        };

    private static OASISResult<IEnumerable<IHolon>> ReplicaFailure(string code, Exception cause)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Exception = cause, Message = cause.Message };
}
