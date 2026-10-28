using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using ReplicaProviderType=NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public sealed class Neo4jReplicaReconciliationException(Guid operationId,string sourceId,Exception cause)
    :Exception("Reconcile the original Neo4j committed replica receipt before another mutation.",cause),INativeMutationReconciliationRequired
{
    public Guid OperationId {get;}=operationId;
    public string SourceId {get;}=sourceId;
    public string OriginalTransactionId=>string.Empty;
}

public partial class Neo4jOASIS : IBoundCommittedHolonReplicaStorageProvider
{
    private sealed class NeoReplicaStorageBindingException(string code, string message, Exception cause = null)
        : InvalidOperationException(message, cause)
    { internal string Code { get; } = code; }

    public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage)
        => ApplyBoundCommittedHolonReplicaAsync(request, expectedStorage).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedStorage);
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!IsProviderActivated || _driver == null || _storageDriverEndpoint == null)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_STORAGE_BINDING_INVALID",
                    Message = "A bound receiver requires an active driver with its captured native endpoint." };
            return await ApplyReplicaAsync(request, expectedStorage, cancellationToken).ConfigureAwait(false);
        }
        finally { _lifecycleGate.Release(); }
    }

    private async Task ValidateReplicaStorageBindingAsync(IAsyncTransaction transaction, NeoAtomicHolonWrite write)
    {
        if (write.ExpectedStorage == null) return;
        CosmicStorageIdentity actual;
        try
        {
            var rows = await (await transaction.RunAsync("CALL db.info() YIELD id,name RETURN id,name")
                .ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            actual = ReadStorageIdentity(rows, _storageDriverEndpoint);
        }
        catch (Exception cause) when (cause is Neo4jException || cause is InvalidDataException || cause is ArgumentException)
        {
            throw new NeoReplicaStorageBindingException("COSMIC_REPLICA_STORAGE_BINDING_INVALID",
                "Native receiver identity could not be verified in the replica transaction.", cause);
        }
        write.Cancellation.ThrowIfCancellationRequested();
        if (actual.BindingFingerprint != write.ExpectedStorage.BindingFingerprint)
            throw new NeoReplicaStorageBindingException("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH",
                "The native receiver does not match the enrolled storage identity.");
    }

    private sealed class NeoReplicaConflictException(IEnumerable<Guid> ids)
        :InvalidOperationException("Replica source lineage, local revision or enrollment differs for: "+string.Join(",",ids));

    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        =>ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CancellationToken cancellationToken=default) => ApplyReplicaAsync(request, null, cancellationToken);

    private async Task<OASISResult<IEnumerable<IHolon>>> ApplyReplicaAsync(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plan=CommittedHolonReplicaPlan.Create(request,ReplicaProviderType.Neo4jOASIS);
            return await SaveHolonSnapshotsAsync(request.Roots,request.SaveChildren,request.Recursive,request.MaxChildDepth,0,
                new NeoAtomicHolonWrite(plan,request.ActorId,cancellationToken) { ExpectedStorage = expectedStorage }).ConfigureAwait(false);
        }
        catch(Exception cause)
        {
            return new OASISResult<IEnumerable<IHolon>>{IsError=true,ErrorCount=1,Exception=cause,
                ErrorCode=cause is OperationCanceledException ? "COSMIC_REPLICA_WRITE_CANCELLED":"COSMIC_REPLICA_WRITE_INVALID",
                Message="Committed replica admission failed."};
        }
    }

    private async Task<List<Holon>> PrepareCommittedReplicaHolonsAsync(IAsyncTransaction transaction,List<Holon> submitted,
        Dictionary<Guid,Holon> observed,NeoAtomicHolonWrite write,CommittedHolonReplicaPlan plan,bool archive)
    {
        var previous=new Dictionary<Guid,Holon>();var conflicts=new List<Guid>();
        foreach(var item in submitted.OrderBy(item=>item.Id))
        {
            write.Cancellation.ThrowIfCancellationRequested();
            var row=await(await transaction.RunAsync("MERGE (r:CosmicReplicaHead {HolonId:$id}) ON CREATE SET r.__cosmicClaim=$claim "
                +"SET r.__cosmicLock=true REMOVE r.__cosmicLock RETURN r",
                new{id=item.Id.ToString(),claim=write.Claim}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            var head=row["r"].As<Neo4j.Driver.INode>();var current=observed[item.Id];
            var created=head.Properties.TryGetValue("__cosmicClaim",out var claim) && (string)claim==write.Claim;
            if(created)
            {
                if(current!=null || item.Version!=1 || item.PreviousVersionId!=Guid.Empty)conflicts.Add(item.Id);
                previous.Add(item.Id,null);continue;
            }
            var fields=head.Properties;
            if(!fields.TryGetValue("SnapshotJson",out var json) || json is not string snapshot
                || !fields.TryGetValue("SnapshotDigest",out var digest) || digest is not string checksum
                || !fields.TryGetValue("SourceId",out var source) || source is not string sourceId
                || !fields.TryGetValue("SourceAuthority",out var sourceAuthority) || sourceAuthority is not string sourceKind
                || !fields.TryGetValue("HardDeleted",out var deleted) || deleted is not bool hardDeleted
                || !fields.TryGetValue("ProviderRevisionId",out var token) || token is not string revisionText
                || !Guid.TryParse(revisionText,out var revision) || revision==Guid.Empty)
                throw new InvalidDataException("Committed replica head is incomplete or corrupt.");
            if(checksum!=ReplicaHeadDigest(snapshot))throw new InvalidDataException("Committed replica head checksum is invalid.");
            var stored=OasisJson.DeserializeStored<Holon>(snapshot);
            if(stored==null || stored.Id!=item.Id || stored.VersionId==Guid.Empty || stored.Version<1
                || CosmicConcurrency.GetProviderRevisionId(stored,ReplicaProviderType.Neo4jOASIS)!=revision)
                throw new InvalidDataException("Committed replica head snapshot/revision is inconsistent.");
            previous.Add(item.Id,current ?? stored);
            if(sourceId!=plan.SourceId || sourceKind!=plan.SourceAuthority.ToString()
                || item.PreviousVersionId!=stored.VersionId || item.Version-1!=stored.Version
                || (hardDeleted ? current!=null : current==null || current.VersionId!=stored.VersionId
                    || current.Version!=stored.Version || CosmicConcurrency.GetProviderRevisionId(current,ReplicaProviderType.Neo4jOASIS)!=revision))
                conflicts.Add(item.Id);
        }
        if(conflicts.Count!=0)throw new NeoReplicaConflictException(conflicts);
        foreach(var record in submitted)
        {
            var old=previous[record.Id];
            if(archive && old!=null)await ArchiveRecordVersionAsync(transaction,"Holon",old).ConfigureAwait(false);
            record.ProviderUniqueStorageKey??=new Dictionary<ReplicaProviderType,string>();
            record.ProviderUniqueStorageKey[ReplicaProviderType.Neo4jOASIS]=record.Id.ToString();
            record.ProviderMetaData??=new Dictionary<ReplicaProviderType,Dictionary<string,string>>();
            record.ProviderMetaData[ReplicaProviderType.Neo4jOASIS]=old?.ProviderMetaData!=null
                && old.ProviderMetaData.TryGetValue(ReplicaProviderType.Neo4jOASIS,out var local) && local!=null
                ?new Dictionary<string,string>(local):new Dictionary<string,string>();
            CosmicConcurrency.SetProviderRevisionId(record,ReplicaProviderType.Neo4jOASIS,Guid.NewGuid());
            record.InstanceSavedOnProviderType=new NextGenSoftware.Utilities.EnumValue<ReplicaProviderType>(ReplicaProviderType.Neo4jOASIS);
            record.IsNewHolon=false;
            plan.ValidateAcknowledgedRecord(record);
        }
        return submitted;
    }

    private static async Task WriteCommittedReplicaHeadsAsync(IAsyncTransaction transaction,CommittedHolonReplicaPlan plan,IEnumerable<IHolon> records)
    {
        var rows=records.Select(record=>{
            var json=OasisJson.Serialize(record);
            return new Dictionary<string,object>{["id"]=record.Id.ToString(),["snapshot"]=json,["digest"]=ReplicaHeadDigest(json),
                ["revision"]=CosmicConcurrency.GetProviderRevisionId(record,ReplicaProviderType.Neo4jOASIS)!.Value.ToString("D")};}).ToList();
        var result=await(await transaction.RunAsync("UNWIND $rows AS row MATCH (r:CosmicReplicaHead {HolonId:row.id}) "
            +"SET r.SourceId=$source,r.SourceAuthority=$authority,r.SnapshotJson=row.snapshot,r.SnapshotDigest=row.digest,r.ProviderRevisionId=row.revision,r.HardDeleted=$deleted "
            +"REMOVE r.__cosmicClaim RETURN count(r) AS count",
            new{rows,source=plan.SourceId,authority=plan.SourceAuthority.ToString(),deleted=plan.Operation==ConditionalHolonOperation.HardDelete})
            .ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
        if(result["count"].As<long>()!=rows.Count)throw new InvalidDataException("Committed replica head acknowledgement is incomplete.");
    }
    // Integrity checksum, not authentication against an authorized native database writer.
    private static string ReplicaHeadDigest(string snapshot)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
}
