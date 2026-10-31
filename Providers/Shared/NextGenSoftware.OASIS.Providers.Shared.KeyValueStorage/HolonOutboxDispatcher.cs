using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public sealed class HolonOutboxDispatchResult
{
    public int ClaimedCount {get;set;}
    public int CompletedCount {get;set;}
    public int DeferredCount {get;set;}
    public int ReconciliationCount {get;set;}
}

/// <summary>Bounded delivery of immutable obligations to explicitly registered idempotent targets.</summary>
public sealed class HolonOutboxDispatcher
{
    private readonly IHolonReplicationRecoveryOutbox _store;
    private readonly IReadOnlyDictionary<string,IHyperDriveIdempotentReplicationTarget> _targets;
    private readonly string _workerId;
    private readonly IHyperDriveClock _clock;
    private readonly int _maximumAttempts;
    public HolonOutboxDispatcher(IHolonReplicationRecoveryOutbox store,IEnumerable<IHyperDriveIdempotentReplicationTarget> targets,
        string workerId,IHyperDriveClock clock=null,int maximumAttempts=10)
    {
        _store=store ?? throw new ArgumentNullException(nameof(store));
        if(string.IsNullOrWhiteSpace(workerId) || workerId!=workerId.Trim() || workerId.Length>256)throw new ArgumentException("A stable worker id is required.",nameof(workerId));
        if(maximumAttempts<1)throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        var entries=targets?.ToArray() ?? throw new ArgumentNullException(nameof(targets));
        if(entries.Any(target=>target==null || string.IsNullOrWhiteSpace(target.ReplicationTargetId)
            || target.ReplicationTargetId!=target.ReplicationTargetId.Trim() || target.ReplicationTargetId.Length>256)
            || entries.GroupBy(target=>target.ReplicationTargetId,StringComparer.Ordinal).Any(group=>group.Count()!=1))
            throw new ArgumentException("Distinct stable replication target ids are required.",nameof(targets));
        _targets=entries.ToDictionary(target=>target.ReplicationTargetId,StringComparer.Ordinal);
        _workerId=workerId;_clock=clock ?? new SystemHyperDriveClock();_maximumAttempts=maximumAttempts;
    }

    public async Task<OASISResult<HolonOutboxDispatchResult>> DispatchOnceAsync(string targetId,int maximumCount=25,
        TimeSpan? leaseDuration=null,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lease=leaseDuration ?? TimeSpan.FromMinutes(2);
        if(targetId==null || !_targets.TryGetValue(targetId,out var target))
            return Error("COSMIC_OUTBOX_TARGET_NOT_REGISTERED","The enrolled target must be explicitly registered before claiming work.");
        if(maximumCount<1 || maximumCount>100 || lease<=TimeSpan.Zero || lease>TimeSpan.FromHours(1))
            return Error("COSMIC_OUTBOX_DISPATCH_OPTIONS_INVALID","A batch of 1–100 and a positive lease of at most one hour are required.");
        var counts=new HolonOutboxDispatchResult();
        var report=new OASISResult<HolonOutboxDispatchResult>(counts);
        void ObserveStoreWarning<T>(OASISResult<T> result,string stage)
        {
            if(result==null || result.IsError || !result.IsWarning && result.Exception==null)return;
            report.IsWarning=true;report.WarningCount+=Math.Max(1,result.WarningCount);report.Exception=Combine(report.Exception,result.Exception);
            report.InnerMessages.Add($"{targetId} {stage}: {result.Message}");
        }
        OASISResult<HolonOutboxDispatchResult> Failed(string code,string message,Exception cause=null)
        {
            var failure=Error(code,message,Combine(report.Exception,cause),counts);
            failure.IsWarning=report.IsWarning;failure.WarningCount=report.WarningCount;failure.InnerMessages.AddRange(report.InnerMessages);return failure;
        }
        for(var index=0;index<maximumCount;index++)
        {
            var claimed=await _store.ClaimNextAsync(targetId,_workerId,lease,cancellationToken).ConfigureAwait(false);
            ObserveStoreWarning(claimed,"claim");
            if(claimed==null || claimed.IsError)return Failed(claimed?.ErrorCode ?? "COSMIC_OUTBOX_CLAIM_FAILED",claimed?.Message ?? "No claim result was returned.",claimed?.Exception);
            if(claimed.Result==null){report.Message=claimed.Message;return report;}
            var claim=claimed.Result;counts.ClaimedCount++;
            if(claim.TargetId!=targetId || claim.WorkerId!=_workerId || claim.LeaseToken==Guid.Empty || claim.AttemptCount<1)
                return Failed("COSMIC_OUTBOX_CLAIM_INVALID","The claim does not belong to the selected target and worker.");
            OASISResult<bool> applied;bool ownershipLost;
            try
            {
                var item=CommittedHolonReplicaTarget.ReadItem(claim.PayloadJson);
                if(item.OperationId!=claim.OperationId)throw new InvalidDataException("The claim operation differs from its frozen payload.");
                item.AttemptCount=claim.AttemptCount;
                (applied,ownershipLost)=await ApplyWithRenewalAsync(target,item,claim,lease,
                    renewal=>ObserveStoreWarning(renewal,"lease renewal"),cancellationToken).ConfigureAwait(false);
            }
            catch(OperationCanceledException)when(cancellationToken.IsCancellationRequested){throw;}
            catch(Exception cause)
            {
                applied=new OASISResult<bool>{IsError=true,ErrorCode=cause is InvalidDataException ? "COSMIC_REPLICA_PAYLOAD_INVALID":"COSMIC_OUTBOX_TARGET_EXCEPTION",Message=cause.Message,Exception=cause};
                ownershipLost=false;
            }
            cancellationToken.ThrowIfCancellationRequested();
            if(ownershipLost)return Failed("COSMIC_OUTBOX_RENEW_FAILED",applied.Message,applied.Exception);
            var evidence=new HolonDeliveryRecoveryEvidence(NativeMutationReconciliation.IsRequired(applied?.Exception),
                NativeMutationReconciliation.OriginalTransactionIds(applied?.Exception));
            var reconcile=evidence.RequiresNativeReconciliation
                || applied?.ErrorCode is "COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID" or "COSMIC_REPLICA_PAYLOAD_INVALID"
                    or "COSMIC_REPLICA_SOURCE_NOT_ENROLLED" or "COSMIC_REPLICA_LINEAGE_CONFLICT"
                    or "COSMIC_REPLICA_AUTHORIZATION_DENIED" or "COSMIC_REPLICA_AUTHORIZATION_FAILED";
            var succeeded=applied!=null && !applied.IsError && applied.Result && !reconcile;
            var outcome=succeeded ? HolonDeliveryOutcome.Completed
                :reconcile || claim.AttemptCount>=_maximumAttempts ? HolonDeliveryOutcome.ReconciliationRequired:HolonDeliveryOutcome.Retry;
            var code=succeeded ? null:string.IsNullOrWhiteSpace(applied?.ErrorCode) ? "COSMIC_OUTBOX_TARGET_NOT_APPLIED":applied.ErrorCode;
            var message=succeeded ? null:applied?.Message ?? "The target did not durably acknowledge the operation.";
            if(!succeeded && !reconcile && outcome==HolonDeliveryOutcome.ReconciliationRequired)message="Attempt limit reached. "+message;
            DateTime? retryAt=null;
            if(outcome==HolonDeliveryOutcome.Retry)
            {
                var now=_clock.UtcNow;if(now.Kind!=DateTimeKind.Utc)throw new InvalidOperationException("The dispatcher clock must return UTC.");
                retryAt=now.AddSeconds(Math.Min(300,Math.Pow(2,Math.Min(claim.AttemptCount-1,9))));
            }
            var finished=await _store.FinishWithRecoveryAsync(claim,outcome,evidence,Bound(code,128),Bound(message,4096),retryAt,cancellationToken).ConfigureAwait(false);
            ObserveStoreWarning(finished,"completion");
            if(finished==null || finished.IsError || !finished.Result)
                return Failed(finished?.ErrorCode ?? "COSMIC_OUTBOX_FINISH_FAILED",finished?.Message ?? "Delivery outcome was not persisted.",Combine(applied?.Exception,finished?.Exception));
            if(succeeded)
            {
                counts.CompletedCount++;
                if(applied.IsWarning || applied.Exception!=null)
                {report.IsWarning=true;report.WarningCount++;report.Exception=Combine(report.Exception,applied.Exception);report.InnerMessages.Add($"{targetId}: {applied.Message}");}
                continue;
            }
            if(outcome==HolonDeliveryOutcome.Retry)counts.DeferredCount++;else counts.ReconciliationCount++;
            report.IsWarning=true;report.WarningCount++;report.Exception=Combine(report.Exception,applied?.Exception);
            report.Message="Delivery did not complete; the original obligation and its outcome are retained.";
            report.InnerMessages.Add($"{targetId}: {code} {message}");return report;
        }
        return report;
    }

    private async Task<(OASISResult<bool> Result,bool OwnershipLost)> ApplyWithRenewalAsync(
        IHyperDriveIdempotentReplicationTarget target,HostedSyncFanOutItem item,HolonDeliveryClaim claim,TimeSpan lease,
        Action<OASISResult<bool>> observeRenewal,CancellationToken token)
    {
        OASISResult<bool> initial;
        try{initial=await _store.RenewAsync(claim,lease,token).ConfigureAwait(false);}
        catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}
        catch(Exception cause){return (new OASISResult<bool>{IsError=true,Message=cause.Message,Exception=cause},true);}
        observeRenewal(initial);
        if(initial==null || initial.IsError || !initial.Result)return (initial ?? new OASISResult<bool>{IsError=true,Message="The initial lease renewal returned no result."},true);
        using var applicationCancellation=CancellationTokenSource.CreateLinkedTokenSource(token);
        using var renewalWait=CancellationTokenSource.CreateLinkedTokenSource(applicationCancellation.Token);
        Task<OASISResult<bool>> application=null;
        try
        {
            application=target.ApplyReplicatedMutationAsync(item,applicationCancellation.Token);
            var interval=TimeSpan.FromTicks(Math.Max(1,lease.Ticks/3));
            while(!application.IsCompleted)
            {
                if(await Task.WhenAny(application,Task.Delay(interval,renewalWait.Token)).ConfigureAwait(false)==application)break;
                token.ThrowIfCancellationRequested();
                OASISResult<bool> renewed;
                try{renewed=await _store.RenewAsync(claim,lease,token).ConfigureAwait(false);}
                catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}
                catch(Exception renewalFailure){renewed=new OASISResult<bool>{IsError=true,Message=renewalFailure.Message,Exception=renewalFailure};}
                observeRenewal(renewed);
                if(renewed!=null && !renewed.IsError && renewed.Result)continue;
                var cause=await CancelAndObserveAsync(applicationCancellation,application,renewed?.Exception).ConfigureAwait(false);
                return (new OASISResult<bool>{IsError=true,Message=renewed?.Message ?? "The delivery lease could not be renewed.",Exception=cause},true);
            }
            return (await application.ConfigureAwait(false),false);
        }
        catch(OperationCanceledException cancelled)when(token.IsCancellationRequested)
        {
            var cause=await CancelAndObserveAsync(applicationCancellation,application,cancelled).ConfigureAwait(false);
            throw new OperationCanceledException("Delivery was cancelled; the original obligation remains retained.",cause,token);
        }
        catch(Exception failure)when(application!=null && !application.IsCompleted)
        {
            var cause=await CancelAndObserveAsync(applicationCancellation,application,failure).ConfigureAwait(false);
            return (new OASISResult<bool>{IsError=true,Message="Delivery infrastructure failed while a target was active.",Exception=cause},true);
        }
        // Cancel only the private renewal timer after a completed target; do not invoke its callbacks after acknowledgement.
        finally{await renewalWait.CancelAsync().ConfigureAwait(false);}
    }
    private static async Task<Exception> CancelAndObserveAsync(CancellationTokenSource cancellation,Task<OASISResult<bool>> application,Exception cause)
    {
        try{await cancellation.CancelAsync().ConfigureAwait(false);}catch(Exception failure){cause=Combine(cause,failure);}
        if(application!=null)
        {
            try
            {
                var observed=await application.ConfigureAwait(false);
                if(observed?.Exception!=null)cause=Combine(cause,observed.Exception);
                else if(observed?.IsError==true)cause=Combine(cause,new InvalidOperationException($"Target returned {observed.ErrorCode}: {observed.Message}"));
            }
            catch(Exception observed){cause=Combine(cause,observed);}
        }
        return cause;
    }
    private static string Bound(string text,int maximum)=>text==null || text.Length<=maximum ? text:text.Substring(0,maximum-1)+"…";
    private static Exception Combine(Exception first,Exception second)=>first==null ? second:second==null ? first:new AggregateException(first,second);
    private static OASISResult<HolonOutboxDispatchResult> Error(string code,string message,Exception cause=null,HolonOutboxDispatchResult counts=null)
        =>new(counts){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}
