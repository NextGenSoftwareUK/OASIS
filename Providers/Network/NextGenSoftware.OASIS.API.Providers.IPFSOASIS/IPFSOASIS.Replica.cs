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
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS : IBoundCommittedHolonReplicaStorageProvider
{
    private const ProviderType ReplicaAuthority = Core.Enums.ProviderType.IPFSOASIS;

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
                var settings = dna.OASIS.StorageProviders.IPFSOASIS;
                if (IPFSClient == null || string.IsNullOrWhiteSpace(settings.LookUpIPFSAddress))
                    throw new InvalidOperationException("An active IPFS client and published catalogue are required for replica apply.");

                var envelope = await ReadCqlEnvelopeAsync(settings.LookUpIPFSAddress, cancellationToken).ConfigureAwait(false);
                var catalogue = ParseCatalogue(UnprotectIndex(envelope));
                var records = DeserializeLookup(catalogue, out var storeId);
                var storage = CreateCatalogueStorageIdentity(settings.ConnectionString, storeId);
                if (expectedStorage != null && storage.BindingFingerprint != expectedStorage.BindingFingerprint)
                    return ReplicaFailure("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH",
                        new InvalidOperationException("The IPFS catalogue does not match the enrolled replica store."));

                var receipts = GetReplicaObject(catalogue, "replicaReceipts");
                var operationId = plan.OperationId.ToString("D");
                if (receipts[operationId] is JToken receipt)
                    return ReplicaSuccess(ConditionalHolonReceipt.Read(ReplicaText(receipt), plan), plan.Nodes.Count);

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
                return ReplicaSuccess(acknowledgedRoots, plan.Nodes.Count);
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
            return ReplicaFailure("COSMIC_REPLICA_WRITE_FAILED", cause);
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
                    throw new InvalidDataException("The IPFS replica does not begin at an unused version-one identity.");
                previousCids.Add(record.Id, null);
                continue;
            }

            var previousCid = ReplicaText(head["cid"]);
            var validated = HolonReplicaHeadRecord.Read(record.Id, ReplicaAuthority, ReplicaText(head["source"]),
                ReplicaText(head["authority"]), ReplicaText(head["snapshot"]), ReplicaText(head["digest"]),
                ReplicaText(head["revision"]), head["hardDeleted"]?.Type == JTokenType.Boolean
                    ? head.Value<bool>("hardDeleted") : throw new InvalidDataException("Invalid IPFS replica deletion state."));
            var current = currentCid == null ? null : DecodePrivateRecord<Holon>(currentCid, "holon", record.Id,
                await ReadCqlEnvelopeAsync(currentCid, cancellationToken).ConfigureAwait(false));
            HolonReplicaHeadRecord.ValidateCurrent(record.Id, ReplicaAuthority, current, validated.Evidence);
            if (validated.Evidence.SourceId != plan.SourceId || validated.Evidence.SourceAuthority != plan.SourceAuthority
                || validated.Evidence.VersionId != record.PreviousVersionId || validated.Evidence.Version != record.Version - 1
                || (!validated.Evidence.HardDeleted && currentCid != previousCid))
                throw new InvalidDataException("The IPFS replica lineage does not match the enrolled source.");
            previousCids.Add(record.Id, previousCid);
        }
        return previousCids;
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
        cancellationToken.ThrowIfCancellationRequested();
        var native = await IPFSClient.FileSystem.AddTextAsync(ProtectRecord("holon", record.Id, snapshot)).ConfigureAwait(false);
        var cid = native.Id.ToString();
        record.ProviderUniqueStorageKey[ReplicaAuthority] = cid;
        var hardDeleted = plan.Operation == ConditionalHolonOperation.HardDelete;
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

    private static JObject GetReplicaObject(JObject catalogue, string key)
        => catalogue[key] == null ? new JObject() : catalogue[key] as JObject
            ?? throw new InvalidDataException("Invalid IPFS replica catalogue field: " + key);

    private static string ReplicaText(JToken value)
        => value?.Type == JTokenType.String ? value.Value<string>()
            : throw new InvalidDataException("Invalid IPFS replica catalogue value.");

    private static OASISResult<IEnumerable<IHolon>> ReplicaSuccess(IEnumerable<IHolon> roots, int count)
        => new(roots) { IsSaved = true, SavedCount = count };

    private static OASISResult<IEnumerable<IHolon>> ReplicaFailure(string code, Exception cause)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Exception = cause, Message = cause.Message };
}
