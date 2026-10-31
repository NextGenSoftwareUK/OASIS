using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonOutboxDispatcherTests
{
    private sealed class Clock : IHyperDriveClock
    {public DateTime UtcNow=>new(2026,10,19,12,0,0,DateTimeKind.Utc);}
    private sealed class Store : IHolonReplicationRecoveryOutbox
    {
        public readonly string Payload;
        public readonly Guid Operation;
        public int Claims,Renews,Attempts=1; public bool LoseRenewal,Misbind,LoseFinish,WarnClaim,WarnFinish;
        public Func<int,Task<OASISResult<bool>>> Renewal;
        public readonly List<HolonDeliveryOutcome> Outcomes=new();public string Error;public DateTime? RetryAt;
        public HolonDeliveryRecoveryEvidence Evidence;
        public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence,CancellationToken cancellationToken=default)
            =>throw new NotSupportedException("Inspection is exercised by the native store tests.");
        public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,
            HolonDeliveryRecoveryEvidence recoveryEvidence,string errorCode=null,string message=null,DateTime? nextAttemptUtc=null,CancellationToken cancellationToken=default)
        {Evidence=recoveryEvidence;return FinishAsync(claim,outcome,errorCode,message,nextAttemptUtc,cancellationToken);}
        public Store()
        {
            var row=new Holon{Id=Guid.NewGuid(),Version=1,VersionId=Guid.NewGuid()};
            CosmicConcurrency.SetProviderRevisionId(row,ProviderType.SQLLiteDBOASIS,Guid.NewGuid());Operation=Guid.NewGuid();
            Payload=CommittedHolonReplicaPayload.Capture(new CommittedHolonReplicaRequest(Operation,"source",ProviderType.SQLLiteDBOASIS,
                Guid.NewGuid(),ConditionalHolonOperation.Save,new[]{row},saveChildren:false));
        }
        public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string targetId,string workerId,TimeSpan lease,CancellationToken token=default)
        {
            token.ThrowIfCancellationRequested();Claims++;
            return Task.FromResult(new OASISResult<HolonDeliveryClaim>(Claims==1 ? new HolonDeliveryClaim(1,Operation,Misbind ? "wrong":targetId,
                workerId,Guid.NewGuid(),Payload,Attempts,new Clock().UtcNow.Add(lease)):null){IsWarning=WarnClaim && Claims==1,
                    Exception=WarnClaim && Claims==1 ? new Exception("claim cleanup"):null});
        }
        public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim,TimeSpan lease,CancellationToken token=default)
        {token.ThrowIfCancellationRequested();Renews++;return Renewal?.Invoke(Renews) ?? Task.FromResult(new OASISResult<bool>(!LoseRenewal){IsError=LoseRenewal});}
        public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,string errorCode=null,string message=null,
            DateTime? nextAttemptUtc=null,CancellationToken cancellationToken=default)
        {if(LoseFinish)return Task.FromResult(new OASISResult<bool>{IsError=true,ErrorCode="COSMIC_OUTBOX_LEASE_LOST"});
            Outcomes.Add(outcome);Error=errorCode;RetryAt=nextAttemptUtc;return Task.FromResult(new OASISResult<bool>(true){IsWarning=WarnFinish,Exception=WarnFinish ? new Exception("finish cleanup"):null});}
    }
    private sealed class Target : IHyperDriveIdempotentReplicationTarget
    {
        public string ReplicationTargetId=>"target";public int Calls;public HostedSyncFanOutItem Received;
        public Func<CancellationToken,Task<OASISResult<bool>>> Apply;
        public Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item,CancellationToken token)
        {Calls++;Received=item;return Apply?.Invoke(token) ?? Task.FromResult(new OASISResult<bool>(true));}
    }
    private static HolonOutboxDispatcher Dispatcher(Store store,Target target)=>new(store,new[]{target},"worker",new Clock());
    private sealed class UnexposedNativeIdentity : Exception,INativeMutationReconciliationRequired
    {public string OriginalTransactionId=>null;}

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task RecoveryCaptureUsesOnlyProviderMarkersAndDoesNotInventMissingIdentities(bool unexposed)
    {
        var store=new Store();
        Exception cause=unexposed ? new UnexposedNativeIdentity():new AggregateException(
            new NativeTransactionFollowupException(ProviderType.MongoDBOASIS,"opaque-original",new Exception("signed payload must not be copied")),
            new Exception("untrusted transaction text"),new NativeTransactionFollowupException(ProviderType.MongoDBOASIS,"opaque-original",new Exception()));
        var target=new Target{Apply=_=>Task.FromResult(new OASISResult<bool>(true){Exception=cause})};
        var result=await Dispatcher(store,target).DispatchOnceAsync("target");
        Assert.IsFalse(result.IsError);Assert.AreEqual(HolonDeliveryOutcome.ReconciliationRequired,store.Outcomes[0]);
        Assert.IsTrue(store.Evidence.RequiresNativeReconciliation);Assert.AreEqual(unexposed ? 0:1,store.Evidence.OriginalTransactionIds.Count);
        if(!unexposed)Assert.AreEqual("opaque-original",store.Evidence.OriginalTransactionIds[0]);
    }

    [DataTestMethod]
    [DataRow("success")] [DataRow("false")] [DataRow("null")] [DataRow("conflict")]
    [DataRow("uncertain")] [DataRow("exception")] [DataRow("exhausted")]
    public async Task DispatchRetainsOriginalPayloadAndPersistsDeclaredOutcome(string scenario)
    {
        var store=new Store();var target=new Target();
        target.Apply=_=>scenario switch {
            "false"=>Task.FromResult(new OASISResult<bool>(false)),
            "null"=>Task.FromResult<OASISResult<bool>>(null),
            "conflict"=>Task.FromResult(new OASISResult<bool>{IsError=true,ErrorCode="COSMIC_REPLICA_LINEAGE_CONFLICT"}),
            "uncertain"=>Task.FromResult(new OASISResult<bool>(true){Exception=new NativeTransactionFollowupException(ProviderType.MongoDBOASIS,"synthetic-native-receipt",new Exception("unknown"))}),
            "exception"=>throw new InvalidOperationException("target offline"),
            "exhausted"=>Task.FromResult(new OASISResult<bool>{IsError=true,ErrorCode="OFFLINE"}),
            _=>Task.FromResult(new OASISResult<bool>(true))};
        if(scenario=="exhausted")store.Attempts=10;
        var result=await Dispatcher(store,target).DispatchOnceAsync("target");
        Assert.IsFalse(result.IsError);Assert.AreEqual(store.Payload,target.Received.PayloadJson);Assert.AreEqual(store.Operation,target.Received.OperationId);
        var expected=scenario=="success" ? HolonDeliveryOutcome.Completed:scenario is "conflict" or "uncertain" or "exhausted"
            ? HolonDeliveryOutcome.ReconciliationRequired:HolonDeliveryOutcome.Retry;
        Assert.AreEqual(expected,store.Outcomes[0]);Assert.AreEqual(1,store.Outcomes.Count);
        if(expected==HolonDeliveryOutcome.Retry)Assert.AreEqual(new Clock().UtcNow.AddSeconds(1),store.RetryAt);
        if(expected!=HolonDeliveryOutcome.Completed)Assert.IsTrue(result.IsWarning);
        Assert.AreEqual(scenario=="uncertain",store.Evidence.RequiresNativeReconciliation);
        if(scenario=="uncertain")Assert.AreEqual("synthetic-native-receipt",store.Evidence.OriginalTransactionIds[0]);
    }

    [TestMethod]
    public async Task UnknownTargetDoesNotClaimAndMisboundClaimDoesNotDispatch()
    {
        var store=new Store();var target=new Target();
        Assert.IsTrue((await Dispatcher(store,target).DispatchOnceAsync("unknown")).IsError);Assert.AreEqual(0,store.Claims);
        store.Misbind=true;Assert.IsTrue((await Dispatcher(store,target).DispatchOnceAsync("target")).IsError);
        Assert.AreEqual(0,target.Calls);Assert.AreEqual(0,store.Outcomes.Count);
    }

    [TestMethod]
    public async Task LostInitialRenewalPreventsTargetIoAndCompletion()
    {
        var store=new Store{LoseRenewal=true};var target=new Target();
        Assert.IsTrue((await Dispatcher(store,target).DispatchOnceAsync("target")).IsError);
        Assert.AreEqual(0,target.Calls);Assert.AreEqual(0,store.Outcomes.Count);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task LongDeliveryRenewsAndCannotCompleteAfterRenewalLoss(bool lose)
    {
        var store=new Store();var target=new Target();
        var renewed=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);var stopped=false;
        store.Renewal=count=>{if(count>1){renewed.TrySetResult(true);if(!lose)release.TrySetResult(true);}
            return Task.FromResult(new OASISResult<bool>(count==1 || !lose){IsError=count>1 && lose});};
        target.Apply=async token=>{try{await release.Task.WaitAsync(token);return new OASISResult<bool>(true);}finally{stopped=true;}};
        var result=await Dispatcher(store,target).DispatchOnceAsync("target",leaseDuration:TimeSpan.FromMilliseconds(60)).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.IsTrue(renewed.Task.IsCompleted);Assert.IsTrue(stopped);Assert.AreEqual(lose,result.IsError);
        Assert.AreEqual(lose ? 0:1,store.Outcomes.Count);
    }

    [TestMethod]
    public async Task HostCancellationObservesTargetTerminationAndLeavesOriginalClaimForRecovery()
    {
        var store=new Store();var target=new Target();using var cancellation=new CancellationTokenSource();
        var started=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);var stopped=false;
        target.Apply=async token=>{started.TrySetResult(true);try{await Task.Delay(Timeout.Infinite,token);return new OASISResult<bool>(true);}finally{stopped=true;}};
        var application=Dispatcher(store,target).DispatchOnceAsync("target",cancellationToken:cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async()=>await application);
        Assert.IsTrue(stopped);Assert.AreEqual(0,store.Outcomes.Count);
    }

    [TestMethod]
    public async Task FailedCompletionCannotReportDeliveryAsDurablyCompleted()
    {
        var store=new Store{LoseFinish=true};var target=new Target();
        var result=await Dispatcher(store,target).DispatchOnceAsync("target");
        Assert.IsTrue(result.IsError);Assert.AreEqual("COSMIC_OUTBOX_LEASE_LOST",result.ErrorCode);
        Assert.AreEqual(1,target.Calls);Assert.AreEqual(0,result.Result.CompletedCount);Assert.AreEqual(0,store.Outcomes.Count);
    }

    [TestMethod]
    public async Task AcknowledgedCleanupWarningIsRetainedWithoutRetryingTheMutation()
    {
        var store=new Store();var cause=new Exception("cleanup warning");
        var target=new Target{Apply=_=>Task.FromResult(new OASISResult<bool>(true){IsWarning=true,Exception=cause,Message="receipt is durable"})};
        var result=await Dispatcher(store,target).DispatchOnceAsync("target");
        Assert.IsFalse(result.IsError);Assert.IsTrue(result.IsWarning);Assert.AreSame(cause,result.Exception);
        Assert.AreEqual(1,result.Result.CompletedCount);Assert.AreEqual(HolonDeliveryOutcome.Completed,store.Outcomes[0]);
    }

    [TestMethod]
    public async Task ThrownRenewalFailureStopsBeforeTargetDispatch()
    {
        var store=new Store();var cause=new InvalidOperationException("lease store unavailable");store.Renewal=_=>throw cause;
        var target=new Target();var result=await Dispatcher(store,target).DispatchOnceAsync("target");
        Assert.IsTrue(result.IsError);Assert.AreSame(cause,result.Exception);Assert.AreEqual(0,target.Calls);Assert.AreEqual(0,store.Outcomes.Count);
    }

    [TestMethod]
    public async Task KnownStoreOutcomeWarningsRemainVisibleWithoutRepeatingTargetMutation()
    {
        var store=new Store{WarnClaim=true,WarnFinish=true};
        store.Renewal=_=>Task.FromResult(new OASISResult<bool>(true){IsWarning=true,Exception=new Exception("renew cleanup")});
        var target=new Target();var result=await Dispatcher(store,target).DispatchOnceAsync("target");
        Assert.IsFalse(result.IsError);Assert.IsTrue(result.IsWarning);Assert.AreEqual(3,result.WarningCount);
        Assert.AreEqual(3,((AggregateException)result.Exception).Flatten().InnerExceptions.Count);
        Assert.AreEqual(1,target.Calls);Assert.AreEqual(1,result.Result.CompletedCount);
    }
    [TestMethod]
    public async Task CompletedTargetIsNotCancelledAfterItsAcknowledgement()
    {
        var store=new Store();var target=new Target{Apply=token=>{
            token.Register(()=>throw new InvalidOperationException("must not cancel an acknowledged target"));
            return Task.FromResult(new OASISResult<bool>(true));}};
        var result=await Dispatcher(store,target).DispatchOnceAsync("target",1);
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(1,result.Result.CompletedCount);
        Assert.AreEqual(HolonDeliveryOutcome.Completed,store.Outcomes.Single());
    }
    [TestMethod]
    public async Task LostRenewalObservesTargetEvenWhenItsCancellationCallbackThrows()
    {
        var stopped=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store=new Store{Renewal=count=>Task.FromResult(new OASISResult<bool>(count==1){IsError=count!=1,Exception=count==1 ? null:new InvalidOperationException("owned renewal failure")})};
        var target=new Target{Apply=async token=>{
            using var registration=token.Register(()=>throw new InvalidOperationException("owned cancellation callback failure"));
            try{await Task.Delay(Timeout.Infinite,token);return new OASISResult<bool>(true);}
            catch(OperationCanceledException)when(token.IsCancellationRequested)
            {return new OASISResult<bool>(true){IsWarning=true,Exception=new UnexposedNativeIdentity()};}
            finally{stopped.TrySetResult(true);}}};
        var result=await Dispatcher(store,target).DispatchOnceAsync("target",1,TimeSpan.FromMilliseconds(30)).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(result.IsError);Assert.AreEqual("COSMIC_OUTBOX_RENEW_FAILED",result.ErrorCode);
        Assert.IsTrue(stopped.Task.IsCompletedSuccessfully);Assert.AreEqual(0,store.Outcomes.Count);
        StringAssert.Contains(result.Exception.ToString(),"owned renewal failure");StringAssert.Contains(result.Exception.ToString(),"owned cancellation callback failure");
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception),"A returned native warning must survive cancellation cleanup.");
    }

}
