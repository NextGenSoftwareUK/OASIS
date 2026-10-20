using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using INode = Neo4j.Driver.INode;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public sealed class Neo4jConditionalReconciliationException(Guid operationId, Exception cause)
    : Exception("Reconcile the original Neo4j conditional operation receipt before another mutation.", cause), INativeMutationReconciliationRequired
{
    public Guid OperationId { get; } = operationId;
    public string OriginalTransactionId => string.Empty; // A logical operation ID is not a native transaction ID.
}

public partial class Neo4jOASIS : IConditionalHolonStorageProvider,IAtomicHolonReplicationStorageProvider
{
    private class NeoHolonTransactionState
    {
        internal bool CommitDispatched, RollbackFailed, KnownCommitted;
    }

    private sealed class NeoAtomicHolonWrite(IAtomicHolonWritePlan plan, Guid actor, CancellationToken cancellation) : NeoHolonTransactionState
    {
        internal IAtomicHolonWritePlan Plan { get; } = plan;
        internal Guid Actor { get; } = actor;
        internal string Claim { get; } = Guid.NewGuid().ToString("D");
        internal CancellationToken Cancellation { get; } = cancellation;
    }
    private sealed class NeoConditionalIntentException() : InvalidOperationException("Operation ID is bound to different write intent or actor.");
    private sealed class NeoConditionalReceiptException(Exception cause) : IOException("Stored conditional acknowledgement is invalid.",cause);

    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
        => ExecuteConditionalHolonWriteAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(ConditionalHolonWriteRequest request,
        CancellationToken cancellationToken=default)
        => ExecuteConditionalCoreAsync(request,request?.Write.ReplicationEnrollment,cancellationToken);

    public OASISResult<IEnumerable<IHolon>> ExecuteReplicatingHolonWrite(ConditionalHolonWriteRequest request,HolonReplicationEnrollment enrollment)
        => ExecuteReplicatingHolonWriteAsync(request,enrollment).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteReplicatingHolonWriteAsync(ConditionalHolonWriteRequest request,
        HolonReplicationEnrollment enrollment,CancellationToken cancellationToken=default)
        => enrollment==null ? Task.FromResult(new OASISResult<IEnumerable<IHolon>> {IsError=true,ErrorCount=1,
            ErrorCode="COSMIC_REPLICATION_ENROLLMENT_REQUIRED",Message="Atomic replication requires explicit enrollment."})
            : ExecuteConditionalCoreAsync(request,enrollment,cancellationToken);

    private async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalCoreAsync(ConditionalHolonWriteRequest request,
        HolonReplicationEnrollment enrollment,CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plan=enrollment==null ? ConditionalHolonPlan.Create(request,Core.Enums.ProviderType.Neo4jOASIS)
                :ConditionalHolonPlan.CreateReplicating(request,Core.Enums.ProviderType.Neo4jOASIS,enrollment);
            return await SaveHolonSnapshotsAsync(request.Roots,request.SaveChildren,request.Recursive,request.MaxChildDepth,0,
                new NeoAtomicHolonWrite(plan,request.Write.ActorId,cancellationToken)).ConfigureAwait(false);
        }
        catch(Exception cause)
        {
            return new OASISResult<IEnumerable<IHolon>> {IsError=true,ErrorCount=1,Exception=cause,
                ErrorCode=cause is OperationCanceledException ? "COSMIC_CONDITIONAL_WRITE_CANCELLED":"COSMIC_CONDITIONAL_WRITE_INVALID",
                Message="Neo4j conditional write admission failed."};
        }
    }

    private static async Task<List<IHolon>> ReadConditionalReceiptAsync(IAsyncTransaction transaction,NeoAtomicHolonWrite write)
    {
        var row=await(await transaction.RunAsync("MERGE (r:CosmicWriteReceipt {OperationId:$operation}) "
            +"ON CREATE SET r.__cosmicClaim=$claim SET r.__cosmicLock=true REMOVE r.__cosmicLock RETURN r",
            new{operation=write.Plan.OperationId.ToString("D"),claim=write.Claim}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
        var receipt=row["r"].As<INode>();
        if(receipt.Properties.TryGetValue("ResultJson",out var json))
        {
            if(!receipt.Properties.TryGetValue("Fingerprint",out var intent) || intent is not string fingerprint)
                throw new NeoConditionalReceiptException(new InvalidDataException("Receipt intent is missing."));
            if(fingerprint!=write.Plan.Fingerprint)throw new NeoConditionalIntentException();
            try {return ConditionalHolonReceipt.Read((string)json,write.Plan).ToList();}
            catch(Exception invalid){throw new NeoConditionalReceiptException(invalid);}
        }
        if(receipt.Properties.ContainsKey("PreparationJson")&&!receipt.Properties.ContainsKey("__cosmicClaim"))
        {
            try{RequirePreparationOwner(receipt,write.Actor);}
            catch(PreparationActorException){throw new NeoConditionalIntentException();}
            catch(InvalidDataException invalid){throw new NeoConditionalReceiptException(invalid);}
            var promoted=await(await transaction.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) "
                +"WHERE r.ResultJson IS NULL AND r.__cosmicClaim IS NULL SET r.__cosmicClaim=$claim RETURN count(r) AS count",
                new{operation=write.Plan.OperationId.ToString("D"),claim=write.Claim}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            if(promoted["count"].As<long>()!=1)throw new NeoConditionalReceiptException(new InvalidDataException("Preparation promotion is incomplete."));
            return null;
        }
        if(!receipt.Properties.TryGetValue("__cosmicClaim",out var owner) || (string)owner!=write.Claim)
            throw new NeoConditionalReceiptException(new InvalidDataException("Receipt has no committed acknowledgement."));
        return null;
    }

    private async Task<List<Holon>> PrepareConditionalHolonsAsync(IAsyncTransaction transaction,List<Holon> submitted,
        NeoAtomicHolonWrite write,bool archive)
    {
        var observed=new Dictionary<Guid,Holon>();var conflicts=new List<CosmicConcurrencyConflict>();
        var conditional=write.Plan as ConditionalHolonPlan;
        foreach(var item in submitted.OrderBy(item=>item.Id))
        {
            write.Cancellation.ThrowIfCancellationRequested();
            var row=await(await transaction.RunAsync("MERGE (h:Holon {Id:$id}) ON CREATE SET h.__cosmicCreatedBy=$claim "
                +"SET h.__oasisHolonLock=true REMOVE h.__oasisHolonLock RETURN h",
                new{id=item.Id.ToString(),claim=write.Claim}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            var native=row["h"].As<INode>();
            var created=native.Properties.TryGetValue("__cosmicCreatedBy",out var claim) && (string)claim==write.Claim;
            var current=created ? null:ReadHolonNode(native);
            observed.Add(item.Id,current);
            if(conditional==null)continue;
            var expected=conditional.ExpectedRevisions[item.Id];
            var actual=current==null ? (Guid?)null:CosmicConcurrency.GetProviderRevisionId(current,Core.Enums.ProviderType.Neo4jOASIS);
            if(expected.HasValue ? current==null || expected!=actual : current!=null)
                conflicts.Add(new CosmicConcurrencyConflict {HolonId=item.Id,ExpectedRevisionId=expected,ActualRevisionId=actual,
                    ActualVersionId=current?.VersionId,ActualVersion=current?.Version});
        }
        if(conflicts.Count!=0)throw new CosmicConcurrencyException(conflicts);
        if(write.Plan is CommittedHolonReplicaPlan replica)
            return await PrepareCommittedReplicaHolonsAsync(transaction,submitted,observed,write,replica,archive).ConfigureAwait(false);
        var prepared=new List<Holon>();var now=DateTime.UtcNow;
        foreach(var item in submitted)
        {
            var previous=observed[item.Id];
            var record=write.Plan.Operation==ConditionalHolonOperation.Save ? item:OasisJson.Project<Holon>(previous);
            record.Version=0;record.VersionId=Guid.Empty;record.PreviousVersionId=Guid.Empty;
            await StoredRecordVersions.StampAsync(record.Id,record,previous,
                archive ? stored=>ArchiveRecordVersionAsync(transaction,"Holon",stored):null).ConfigureAwait(false);
            record.CreatedDate=previous?.CreatedDate ?? now;record.CreatedByAvatarId=previous?.CreatedByAvatarId ?? write.Actor;
            record.CreatedProviderType=previous?.CreatedProviderType ?? new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
            record.ModifiedDate=previous==null ? DateTime.MinValue:now;record.ModifiedByAvatarId=previous==null ? Guid.Empty:write.Actor;
            record.ProviderUniqueStorageKey??=new Dictionary<Core.Enums.ProviderType,string>();
            record.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS]=record.Id.ToString();record.IsNewHolon=false;
            CosmicConcurrency.SetProviderRevisionId(record,Core.Enums.ProviderType.Neo4jOASIS,Guid.NewGuid());
            if(write.Plan.Operation!=ConditionalHolonOperation.Save)
            {record.DeletedDate=now;record.DeletedByAvatarId=write.Actor;record.IsActive=false;}
            prepared.Add(record);
        }
        return prepared;
    }

    private static async Task WriteConditionalReceiptAsync(IAsyncTransaction transaction,NeoAtomicHolonWrite write,List<IHolon> roots)
    {
        var json=ConditionalHolonReceipt.Write(roots,write.Plan);
        _=ConditionalHolonReceipt.Read(json,write.Plan);
        var row=await(await transaction.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) WHERE r.__cosmicClaim=$claim "
            +"SET r.Fingerprint=$fingerprint,r.ResultJson=$json,r.CommittedAtUtc=$now REMOVE r.__cosmicClaim RETURN count(r) AS count",
            new{operation=write.Plan.OperationId.ToString("D"),claim=write.Claim,fingerprint=write.Plan.Fingerprint,json,now=DateTime.UtcNow.ToString("O")})
            .ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
        if(row["count"].As<long>()!=1)throw new InvalidDataException("Neo4j conditional receipt acknowledgement is incomplete.");
    }

    private static void SetConditionalAcknowledgement(OASISResult<IEnumerable<IHolon>> result,List<IHolon> roots,NeoAtomicHolonWrite write)
    {
        var pending=new Stack<IHolon>(roots);
        while(pending.Count!=0)
        {
            var record=pending.Pop();record.IsNewHolon=false;
            foreach(var child in record.Children ?? Enumerable.Empty<IHolon>())pending.Push(child);
        }
        result.Result=roots;result.IsSaved=write.Plan.Operation==ConditionalHolonOperation.Save;result.IsDeleted=!result.IsSaved;
        result.SavedCount=result.IsSaved ? write.Plan.Nodes.Count:0;result.DeletedCount=result.IsDeleted ? write.Plan.Nodes.Count:0;
        result.MetaData??=new Dictionary<string,string>();result.MetaData["COSMIC.OperationId"]=write.Plan.OperationId.ToString("D");
    }

    private static OASISResult<IEnumerable<IHolon>> ConditionalNeoFailure(OASISResult<IEnumerable<IHolon>> result,NeoAtomicHolonWrite write,Exception cause)
    {
        if(write.KnownCommitted)
        {result.IsWarning=true;result.WarningCount++;result.Exception=cause;result.Message="The atomic holon acknowledgement is known, but Neo4j session cleanup failed.";return result;}
        var uncertain=write.CommitDispatched || write.RollbackFailed;
        result.IsError=true;result.ErrorCount++;result.IsSaved=false;result.IsDeleted=false;
        result.Exception=uncertain ? write.Plan is CommittedHolonReplicaPlan replica
            ? new Neo4jReplicaReconciliationException(write.Plan.OperationId,replica.SourceId,cause)
            : new Neo4jConditionalReconciliationException(write.Plan.OperationId,cause):cause;
        result.ErrorCode=uncertain ? "COSMIC_WRITE_RECONCILIATION_REQUIRED":cause is CosmicConcurrencyException ? CosmicConcurrencyException.Code
            :cause is NeoReplicaConflictException ? "COSMIC_REPLICA_LINEAGE_CONFLICT"
            :cause is NeoConditionalIntentException ? "COSMIC_OPERATION_ID_REUSED":cause is NeoConditionalReceiptException ? "COSMIC_RECEIPT_INVALID"
            :cause is OperationCanceledException ? write.Plan is CommittedHolonReplicaPlan ? "COSMIC_REPLICA_WRITE_CANCELLED":"COSMIC_CONDITIONAL_WRITE_CANCELLED"
            :cause is TransientException transient && transient.Code=="Neo.TransientError.Transaction.DeadlockDetected" ? "COSMIC_CONCURRENT_WRITE"
            :write.Plan is CommittedHolonReplicaPlan ? "COSMIC_REPLICA_WRITE_FAILED":"COSMIC_CONDITIONAL_WRITE_FAILED";
        result.Message=result.Exception.Message;result.MetaData??=new Dictionary<string,string>();
        result.MetaData["COSMIC.OperationId"]=write.Plan.OperationId.ToString("D");return result;
    }
}
