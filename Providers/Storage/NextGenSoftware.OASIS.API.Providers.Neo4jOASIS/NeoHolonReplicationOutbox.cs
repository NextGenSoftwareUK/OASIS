using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using INode=Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

/// <summary>Fenced delivery over an existing enrolled graph database. The caller retains driver ownership.</summary>
public sealed class NeoHolonReplicationOutbox : IHolonReplicationReadinessOutbox
{
    private readonly IDriver _driver;
    private const string Now="(datetime.realtime().epochMillis * 10000 + 621355968000000000)";
    public NeoHolonReplicationOutbox(IDriver driver)=>_driver=driver ?? throw new ArgumentNullException(nameof(driver));

    public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string targetId,string workerId,TimeSpan leaseDuration,CancellationToken cancellationToken=default)
        =>Execute(async tx=>{
            var rows=await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {TargetId:$target}) WHERE o.DeliveryState<>'Completed' "
                +"WITH o ORDER BY o.Sequence LIMIT 1 SET o.__deliveryLock=true REMOVE o.__deliveryLock RETURN o,"+Now+" AS now",
                new{target=targetId}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            if(rows.Count==0)return (new OASISResult<HolonDeliveryClaim>{Message="No unfinished obligation exists for this target."},false);
            var row=rows[0]["o"].As<INode>();var state=ValidateRow(row);var now=rows[0]["now"].As<long>();
            if(state.State==HolonDeliveryState.Completed || state.State==HolonDeliveryState.ReconciliationRequired
                || state.State==HolonDeliveryState.InFlight && Ticks(row,"LeaseUntilUtcTicks")>now || Ticks(row,"NextAttemptUtcTicks")>now)
                return (new OASISResult<HolonDeliveryClaim>{Message="The oldest target obligation is blocked, leased or not yet due."},false);
            ValidatePayload(row,state.Operation);
            if(state.Attempts==int.MaxValue)throw new InvalidDataException("Outbox attempt count is exhausted.");
            var until=checked(now+leaseDuration.Ticks);var lease=Guid.NewGuid();
            await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) SET o.DeliveryState='InFlight',"
                +"o.LeaseToken=$lease,o.LeaseWorkerId=$worker,o.LeaseUntilUtcTicks=$until,o.AttemptCount=$attempts",
                new{sequence=Long(row,"Sequence"),lease=lease.ToString("D"),worker=workerId,until,attempts=state.Attempts+1L}).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
            return (new OASISResult<HolonDeliveryClaim>(new HolonDeliveryClaim(Long(row,"Sequence"),state.Operation,targetId,workerId,lease,
                Text(row,"PayloadJson"),state.Attempts+1,new DateTime(until,DateTimeKind.Utc))),true);
        },()=>{Identity(targetId);Identity(workerId);Duration(leaseDuration);},cancellationToken);

    public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim,TimeSpan leaseDuration,CancellationToken cancellationToken=default)
        =>Execute(async tx=>{
            var locked=await LockOwned(tx,claim).ConfigureAwait(false);
            if(locked==null)return (Lost(),false);
            var until=Math.Max(Ticks(locked.Value.Row,"LeaseUntilUtcTicks"),checked(locked.Value.Now+leaseDuration.Ticks));
            await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) SET o.LeaseUntilUtcTicks=$until",
                new{sequence=claim.Sequence,until}).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
            return (new OASISResult<bool>(true),true);
        },()=>{ClaimIdentity(claim);Duration(leaseDuration);},cancellationToken);

    public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,string errorCode=null,
        string message=null,DateTime? nextAttemptUtc=null,CancellationToken cancellationToken=default)
        =>FinishCore(claim,outcome,null,errorCode,message,nextAttemptUtc,cancellationToken);
    public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,
        HolonDeliveryRecoveryEvidence recoveryEvidence,string errorCode=null,string message=null,DateTime? nextAttemptUtc=null,CancellationToken cancellationToken=default)
        =>FinishCore(claim,outcome,recoveryEvidence ?? throw new ArgumentNullException(nameof(recoveryEvidence)),errorCode,message,nextAttemptUtc,cancellationToken);
    private Task<OASISResult<bool>> FinishCore(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,HolonDeliveryRecoveryEvidence evidence,
        string code,string message,DateTime? retry,CancellationToken token)
        =>Execute(async tx=>{
            var locked=await LockOwned(tx,claim).ConfigureAwait(false);
            if(locked==null || outcome==HolonDeliveryOutcome.Retry && retry.Value.Ticks<=locked.Value.Now)return (Lost(),false);
            var json=evidence==null ? null:HolonRecoveryEvidenceJson.Write(evidence);
            await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) SET o += $values",
                new{sequence=claim.Sequence,values=new Dictionary<string,object>{
                    ["DeliveryState"]=outcome==HolonDeliveryOutcome.Retry ? "Pending":outcome.ToString(),
                    ["LeaseToken"]="",["LeaseWorkerId"]="",["LeaseUntilUtcTicks"]=0L,["NextAttemptUtcTicks"]=retry?.Ticks ?? 0L,
                    ["LastErrorCode"]=code,["LastErrorMessage"]=message,["RecoveryEvidenceJson"]=json,
                    ["RecoveryEvidenceDigest"]=json==null ? null:HolonRecoveryEvidenceJson.Digest(json),
                    ["CompletedAtUtcTicks"]=outcome==HolonDeliveryOutcome.Completed ? locked.Value.Now:0L
                }}).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
            return (new OASISResult<bool>(true),true);
        },()=>{
            ClaimIdentity(claim);
            if(!Enum.IsDefined(typeof(HolonDeliveryOutcome),outcome)
                || evidence?.RequiresNativeReconciliation==true && outcome!=HolonDeliveryOutcome.ReconciliationRequired
                || outcome==HolonDeliveryOutcome.Completed && (code!=null || message!=null || retry!=null)
                || outcome!=HolonDeliveryOutcome.Completed && (string.IsNullOrWhiteSpace(code) || code.Length>128 || message?.Length>4096)
                || outcome==HolonDeliveryOutcome.Retry && (!retry.HasValue || retry.Value.Kind!=DateTimeKind.Utc)
                || outcome==HolonDeliveryOutcome.ReconciliationRequired && retry.HasValue)
                throw new ArgumentException("Delivery outcome, diagnostics and recovery evidence disagree.");
        },token);

    public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence,CancellationToken cancellationToken=default)
        =>Execute(async tx=>{
            var rows=await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) RETURN o",new{sequence}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            if(rows.Count==0)return (new OASISResult<HolonDeliveryInspection>{Message="No delivery obligation exists at this sequence."},false);
            var row=rows[0]["o"].As<INode>();var state=ValidateRow(row);
            return (new OASISResult<HolonDeliveryInspection>(new HolonDeliveryInspection(sequence,state.Operation,Text(row,"TargetId"),state.State,
                state.Attempts,Text(row,"LastErrorCode"),Text(row,"LastErrorMessage"),state.Evidence)),false);
        },()=>{if(sequence<=0)throw new ArgumentOutOfRangeException(nameof(sequence));},cancellationToken);

    public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string targetId,CancellationToken cancellationToken=default)
        =>Execute(async tx=>{
            var rows=await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {TargetId:$target}) WHERE o.DeliveryState<>'Completed' "
                +"RETURN o ORDER BY o.Sequence LIMIT 1",new{target=targetId}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            if(rows.Count==0)return (new OASISResult<HolonDeliveryInspection>{Message="No unfinished obligation exists for this target."},false);
            var row=rows[0]["o"].As<INode>();var value=ValidateRow(row);ValidatePayload(row,value.Operation);
            return (new OASISResult<HolonDeliveryInspection>(new HolonDeliveryInspection(Long(row,"Sequence"),value.Operation,targetId,value.State,value.Attempts,
                Text(row,"LastErrorCode"),Text(row,"LastErrorMessage"),value.Evidence)),false);
        },()=>Identity(targetId),cancellationToken);

    private static async Task<(INode Row,long Now)?> LockOwned(IAsyncTransaction tx,HolonDeliveryClaim claim)
    {
        var rows=await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) "
            +"SET o.__deliveryLock=true REMOVE o.__deliveryLock RETURN o,"+Now+" AS now",new{sequence=claim.Sequence}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
        if(rows.Count==0)return null;
        var row=rows[0]["o"].As<INode>();var value=ValidateRow(row);var now=rows[0]["now"].As<long>();
        return value.State==HolonDeliveryState.InFlight && value.Operation==claim.OperationId && Text(row,"TargetId")==claim.TargetId
            && Text(row,"LeaseToken")==claim.LeaseToken.ToString("D") && Text(row,"LeaseWorkerId")==claim.WorkerId
            && Ticks(row,"LeaseUntilUtcTicks")>now ? (row,now):null;
    }

    private async Task<OASISResult<T>> Execute<T>(Func<IAsyncTransaction,Task<(OASISResult<T> Result,bool Changed)>> action,Action validate,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();IAsyncSession session=null;IAsyncTransaction tx=null;OASISResult<T> result=null;
        var known=false;var commit=false;var close=false;var rollbackFailed=false;Exception failure=null;var cancelled=false;
        try
        {
            validate();session=_driver.AsyncSession();tx=await session.BeginTransactionAsync().ConfigureAwait(false);
            var outcome=await action(tx).ConfigureAwait(false);result=outcome.Result;token.ThrowIfCancellationRequested();
            if(outcome.Changed){commit=true;await tx.CommitAsync().ConfigureAwait(false);known=true;}
            else{known=true;close=true;await tx.RollbackAsync().ConfigureAwait(false);}
        }
        catch(Exception cause)
        {
            failure=cause;cancelled=!commit && cause is OperationCanceledException && token.IsCancellationRequested;
            if(tx!=null && !commit && !close)
            {try{await tx.RollbackAsync().ConfigureAwait(false);}catch(Exception cleanup){rollbackFailed=true;failure=Join(failure,cleanup);}}
        }
        foreach(var resource in new IDisposable[]{tx,session})
        {try{resource?.Dispose();}catch(Exception cleanup){failure=Join(failure,cleanup);}}
        if(failure!=null)
        {
            if(!known)
            {
                if(commit || rollbackFailed)failure=new Neo4jMutationReconciliationException(failure);
                if(cancelled && !rollbackFailed)throw new OperationCanceledException("Delivery cancelled before commit; cleanup evidence is retained.",failure,token);
                return Failure<T>(failure);
            }
            result.IsWarning=true;result.WarningCount++;result.Exception=Join(result.Exception,failure);
            result.InnerMessages.Add("Delivery outcome is known, but Neo4j transaction or session cleanup failed.");
        }
        return result;
    }
    private static Exception Join(Exception first,Exception second)=>first==null ? second:second==null ? first:new AggregateException(first,second);
    private static OASISResult<bool> Lost()=>new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_OUTBOX_LEASE_LOST",Message="The obligation is no longer owned by this unexpired lease or retry time is not in the future."};
    private static void Identity(string id){if(string.IsNullOrWhiteSpace(id) || id!=id.Trim() || id.Length>256)throw new ArgumentException("A stable identity is required.");}
    private static void Duration(TimeSpan lease){if(lease<=TimeSpan.Zero || lease>TimeSpan.FromHours(1))throw new ArgumentOutOfRangeException(nameof(lease));}
    private static void ClaimIdentity(HolonDeliveryClaim claim)
    {if(claim==null || claim.Sequence<=0 || claim.OperationId==Guid.Empty || claim.LeaseToken==Guid.Empty)throw new ArgumentException("A fenced claim is required.");Identity(claim.TargetId);Identity(claim.WorkerId);}
    private static string Text(INode row,string key)
    {if(!row.Properties.TryGetValue(key,out var value) || value==null)return null;if(value is not string text)throw new InvalidDataException("Invalid outbox text: "+key);return text;}
    private static long Long(INode row,string key)
    {if(!row.Properties.TryGetValue(key,out var value) || value is not long number)throw new InvalidDataException("Invalid outbox integer: "+key);return number;}
    private static long Ticks(INode row,string key)
    {var value=Long(row,key);if(value<0 || value>DateTime.MaxValue.Ticks)throw new InvalidDataException("Invalid outbox UTC ticks: "+key);return value;}
    private static (Guid Operation,HolonDeliveryState State,int Attempts,HolonDeliveryRecoveryEvidence Evidence) ValidateRow(INode row)
    {
        var attempts=Long(row,"AttemptCount");
        if(Long(row,"Format")!=1 || Long(row,"Sequence")<=0 || attempts<0 || attempts>int.MaxValue
            || !Guid.TryParseExact(Text(row,"OperationId"),"D",out var operation) || operation==Guid.Empty
            || !Enum.TryParse<HolonDeliveryState>(Text(row,"DeliveryState"),out var state)
            || !Enum.IsDefined(typeof(HolonDeliveryState),state) || state.ToString()!=Text(row,"DeliveryState"))
            throw new InvalidDataException("Invalid outbox identity, state or attempt count.");
        try{Identity(Text(row,"TargetId"));}catch(ArgumentException cause){throw new InvalidDataException("Invalid outbox target.",cause);}
        var expiry=Ticks(row,"LeaseUntilUtcTicks");Ticks(row,"NextAttemptUtcTicks");var completed=Ticks(row,"CompletedAtUtcTicks");
        if(state==HolonDeliveryState.InFlight && (expiry==0 || attempts==0)
            || state==HolonDeliveryState.Completed && completed==0 || state!=HolonDeliveryState.Completed && completed!=0)
            throw new InvalidDataException("Outbox lifecycle timestamps disagree with state.");
        if(state==HolonDeliveryState.InFlight && (!Guid.TryParseExact(Text(row,"LeaseToken"),"D",out var lease) || lease==Guid.Empty
            || string.IsNullOrWhiteSpace(Text(row,"LeaseWorkerId"))))throw new InvalidDataException("Invalid active lease identity.");
        return (operation,state,(int)attempts,HolonRecoveryEvidenceJson.ReadStored(Text(row,"RecoveryEvidenceJson"),Text(row,"RecoveryEvidenceDigest"),state));
    }
    private static void ValidatePayload(INode row,Guid operation)
    {
        var json=Text(row,"PayloadJson");
        if(json==null || Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))!=Text(row,"PayloadDigest"))throw new InvalidDataException("Outbox payload checksum differs.");
        var delivery=CommittedHolonReplicaPayload.Read(json);
        if(delivery.OperationId!=operation || delivery.SourceId!=Text(row,"SourceId") || delivery.SourceAuthority!=ProviderType.Neo4jOASIS
            || Text(row,"SourceAuthority")!=ProviderType.Neo4jOASIS.ToString() || delivery.ActorId.ToString("D")!=Text(row,"ActorId"))
            throw new InvalidDataException("Outbox payload and enrollment disagree.");
    }
    private static OASISResult<T> Failure<T>(Exception cause)=>new(){IsError=true,ErrorCount=1,Exception=cause,Message=cause.Message,
        ErrorCode=Core.Helpers.NativeMutationReconciliation.IsRequired(cause) ? "COSMIC_OUTBOX_RECONCILIATION_REQUIRED"
            :cause is InvalidDataException ? "COSMIC_OUTBOX_CORRUPT":cause is TransientException ? "COSMIC_OUTBOX_CONCURRENT_WRITE":"COSMIC_OUTBOX_OPERATION_FAILED"};
}
