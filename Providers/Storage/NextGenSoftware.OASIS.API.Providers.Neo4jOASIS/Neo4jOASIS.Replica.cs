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

public partial class Neo4jOASIS : ICommittedHolonReplicaStorageProvider, ICommittedHolonReplicaInitializationStorageProvider
{
    private sealed class NeoReplicaConflictException(IEnumerable<Guid> ids)
        :InvalidOperationException("Replica source lineage, local revision or enrollment differs for: "+string.Join(",",ids));
    private sealed class NeoReplicaBaselineAdmissionException(string message,Exception cause=null):IOException(message,cause);

    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        =>ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();
    public async Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CancellationToken cancellationToken=default)
        =>await ApplyReplicaAsync(request,false,cancellationToken).ConfigureAwait(false);
    public OASISResult<IEnumerable<IHolon>> InitializeCommittedHolonReplica(CommittedHolonReplicaRequest request)
        =>InitializeCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<IEnumerable<IHolon>>> InitializeCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CancellationToken cancellationToken=default)=>ApplyReplicaAsync(request,true,cancellationToken);

    private async Task<OASISResult<IEnumerable<IHolon>>> ApplyReplicaAsync(CommittedHolonReplicaRequest request,bool initialize,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plan=initialize ? CommittedHolonReplicaPlan.CreateInitialBaseline(request,ReplicaProviderType.Neo4jOASIS)
                :CommittedHolonReplicaPlan.Create(request,ReplicaProviderType.Neo4jOASIS);
            return await SaveHolonSnapshotsAsync(request.Roots,request.SaveChildren,request.Recursive,request.MaxChildDepth,0,
                new NeoAtomicHolonWrite(plan,request.ActorId,cancellationToken)).ConfigureAwait(false);
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
        if(plan.IsInitialBaseline)
        {
            if(observed.Values.Any(value=>value!=null))throw new NeoReplicaConflictException(observed.Where(pair=>pair.Value!=null).Select(pair=>pair.Key));
            await VerifyInitialReplicaAbsenceAsync(transaction,submitted.Select(row=>row.Id).ToHashSet(),write.Cancellation).ConfigureAwait(false);
        }
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
                if(current!=null || !plan.IsInitialBaseline && (item.Version!=1 || item.PreviousVersionId!=Guid.Empty))conflicts.Add(item.Id);
                previous.Add(item.Id,null);continue;
            }
            if(plan.IsInitialBaseline){conflicts.Add(item.Id);continue;}
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

    private static async Task VerifyInitialReplicaAbsenceAsync(IAsyncTransaction transaction,HashSet<Guid> ids,CancellationToken token)
    {
        // PrepareConditionalHolonsAsync already holds each selected Holon node's
        // native lock (including new placeholders). Related V2 writers cannot pass it.
        var history=await(await transaction.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids RETURN count(v) AS count",
            new{ids=ids.Select(id=>id.ToString("D")).ToArray()}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
        if(history["count"].As<long>()!=0)throw new NeoReplicaConflictException(ids);

        // Explicit bounded receipt audit; no APOC dependency, metadata inference,
        // unrestricted query fallback, mutation or automatic receipt migration.
        var cursor=await transaction.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.ResultJson IS NOT NULL RETURN r.ResultJson AS json LIMIT 10001").ConfigureAwait(false);
        var count=0;
        while(await cursor.FetchAsync().ConfigureAwait(false))
        {
            token.ThrowIfCancellationRequested();
            if(++count>10000)throw new NeoReplicaBaselineAdmissionException("Initialization requires an explicit receipt audit beyond 10000 acknowledgements.");
            IReadOnlyList<IHolon> roots;
            try{roots=ConditionalHolonReceipt.Read(cursor.Current["json"].As<string>());}
            catch(Exception cause)when(cause is InvalidDataException or ArgumentException or InvalidCastException)
            {throw new NeoReplicaBaselineAdmissionException("A retained acknowledgement cannot establish target absence.",cause);}
            var graph=HolonGraphWritePlan.Create(roots,true,true,0,0);
            if(graph.Nodes.Any(node=>ids.Contains(node.Snapshot.Id)))throw new NeoReplicaConflictException(ids);
        }
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
