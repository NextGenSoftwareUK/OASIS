using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonReplicationHostingTests
{
    private static readonly Guid Actor=Guid.Parse("a4f43980-6060-4642-9c94-91d13ee8f47e");
    private static readonly DateTime Now=new(2026,10,19,12,0,0,DateTimeKind.Utc);
    private sealed class Clock : IHyperDriveClock {public DateTime Value=Now;public DateTime UtcNow=>Value;}
    private sealed class Policy : IHolonReplicationAuthorizationPolicy
    {
        public string BindingIdentity {get;set;}="owned-policy-v1";
        public int Calls;public Func<HolonReplicationAuthorizationContext,CancellationToken,Task<OASISResult<bool>>> Apply;
        public Task<OASISResult<bool>> AuthorizeAsync(HolonReplicationAuthorizationContext context,CancellationToken token)
        {Calls++;return Apply?.Invoke(context,token) ?? Task.FromResult(new OASISResult<bool>(true));}
    }
    private sealed class Receiver : ICommittedHolonReplicaStorageProvider
    {
        public int Calls;public string Name;public Guid ActorId,OperationId;
        public Func<CancellationToken,Task> Before;
        public OASISResult<IEnumerable<IHolon>> Rejection;
        public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)=>throw new NotSupportedException();
        public async Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,CancellationToken cancellationToken=default)
        {
            Interlocked.Increment(ref Calls);if(Before!=null)await Before(cancellationToken);
            Name=request.Roots[0].Name;ActorId=request.ActorId;OperationId=request.OperationId;if(Rejection!=null)return Rejection;
            foreach(var node in HolonGraphWritePlan.Create(request.Roots,true,true,0,0).Nodes)
                CosmicConcurrency.SetProviderRevisionId(node.Source,ProviderType.MongoDBOASIS,Guid.NewGuid());
            return new OASISResult<IEnumerable<IHolon>>(request.Roots);
        }
    }
    private sealed class Store : IHolonReplicationReadinessOutbox
    {
        public readonly ConcurrentDictionary<string,HolonDeliveryOutcome> Outcomes=new();
        private readonly ConcurrentDictionary<string,bool> _claimed=new();
        public readonly TaskCompletionSource<bool> Finished=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Claims,Finishes,Peeks;public string LastCode;public bool FailPeek;public HolonDeliveryState? RetainedHead;
        public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string target,string worker,TimeSpan lease,CancellationToken token=default)
        {
            token.ThrowIfCancellationRequested();Interlocked.Increment(ref Claims);
            var item=Item();return Task.FromResult(new OASISResult<HolonDeliveryClaim>(_claimed.TryAdd(target,true)
                ? new HolonDeliveryClaim(1,item.OperationId,target,worker,Guid.NewGuid(),item.PayloadJson,1,Now.Add(lease)):null));
        }
        public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim,TimeSpan lease,CancellationToken token=default)
        {token.ThrowIfCancellationRequested();return Task.FromResult(new OASISResult<bool>(true));}
        public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,string code=null,string message=null,DateTime? retry=null,CancellationToken token=default)
            =>FinishWithRecoveryAsync(claim,outcome,new(false,Array.Empty<string>()),code,message,retry,token);
        public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,HolonDeliveryRecoveryEvidence evidence,
            string errorCode=null,string message=null,DateTime? nextAttemptUtc=null,CancellationToken cancellationToken=default)
        {Outcomes[claim.TargetId]=outcome;LastCode=errorCode;Interlocked.Increment(ref Finishes);Finished.TrySetResult(true);return Task.FromResult(new OASISResult<bool>(true));}
        public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string target,CancellationToken token=default)
        {
            token.ThrowIfCancellationRequested();Interlocked.Increment(ref Peeks);
            if(FailPeek)return Task.FromResult(new OASISResult<HolonDeliveryInspection>{IsError=true,ErrorCode="OWNED_HEAD_UNAVAILABLE"});
            Outcomes.TryGetValue(target,out var outcome);
            if(Outcomes.ContainsKey(target) && outcome==HolonDeliveryOutcome.Completed)return Task.FromResult(new OASISResult<HolonDeliveryInspection>());
            var state=RetainedHead ?? (Outcomes.ContainsKey(target) && outcome==HolonDeliveryOutcome.ReconciliationRequired ? HolonDeliveryState.ReconciliationRequired:HolonDeliveryState.Pending);
            return Task.FromResult(new OASISResult<HolonDeliveryInspection>(new HolonDeliveryInspection(1,Guid.NewGuid(),target,state,0,
                state==HolonDeliveryState.ReconciliationRequired ? "OWNED_RECONCILIATION":null,null,null)));
        }
        public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence,CancellationToken token=default)=>throw new NotSupportedException();
    }
    private static HostedSyncFanOutItem Item(string source="source",Guid? actor=null)
    {
        var record=new Holon{Id=Guid.NewGuid(),Version=1,VersionId=Guid.NewGuid(),Name="frozen original"};
        CosmicConcurrency.SetProviderRevisionId(record,ProviderType.SQLLiteDBOASIS,Guid.NewGuid());
        return CommittedHolonReplicaTarget.CaptureItem(new CommittedHolonReplicaRequest(Guid.NewGuid(),source,ProviderType.SQLLiteDBOASIS,
            actor ?? Actor,ConditionalHolonOperation.Save,new[]{record},saveChildren:false));
    }
    private static HolonReplicationDestination Destination(Receiver receiver,IHolonReplicationAuthorizationPolicy policy=null,string name="target",string endpoint="database-a")
        =>new(name,endpoint,ProviderType.MongoDBOASIS,receiver,policy ?? new Policy());
    private static HolonReplicationCatalog Catalog(Store store,Receiver receiver,IHolonReplicationAuthorizationPolicy policy=null,
        Guid? version=null,string source="source",string identity="store",string endpoint="database-a",string name="target")
        =>new("catalog",version ?? Guid.Parse("6234a70c-cf93-493e-a436-89b217a4ee34"),source,identity,ProviderType.SQLLiteDBOASIS,store,new[]{Destination(receiver,policy,name,endpoint)});
    private static HolonReplicationHostOptions Options()=>new("worker",TimeSpan.FromMilliseconds(10),TimeSpan.FromMilliseconds(20),TimeSpan.FromSeconds(1),1);

    [DataTestMethod]
    [DataRow("version")] [DataRow("source")] [DataRow("store")] [DataRow("endpoint")] [DataRow("name")] [DataRow("policy")]
    public void ChangedCatalogMeaningCreatesDifferentImmutableEnrollmentTarget(string change)
    {
        var store=new Store();var receiver=new Receiver();var original=Catalog(store,receiver);
        var changed=Catalog(store,receiver,new Policy{BindingIdentity=change=="policy" ? "changed":"owned-policy-v1"},
            version:change=="version" ? Guid.NewGuid():original.Version,source:change=="source" ? "other":"source",
            identity:change=="store" ? "other":"store",endpoint:change=="endpoint" ? "other":"database-a",name:change=="name" ? "other":"target");
        Assert.AreNotEqual(original.TargetIds.Single(),changed.TargetIds.Single());
        Assert.AreEqual(original.TargetIds.Single(),Catalog(store,receiver).TargetIds.Single());
        Assert.AreEqual(original.SourceId,original.Enrollment.SourceId);CollectionAssert.AreEqual(original.TargetIds.ToArray(),original.Enrollment.TargetIds.ToArray());
        Assert.ThrowsException<NotSupportedException>(()=>((IList<string>)original.TargetIds)[0]="replacement");
    }
    [TestMethod]
    public async Task ExpiringActorGrantCopiesItsAllowlistAndBindsItsMeaning()
    {
        var actors=new List<Guid>{Actor};var clock=new Clock();var grant=new HolonReplicationActorGrant(actors,Now.AddMinutes(1),clock);
        actors.Clear();var receiver=new Receiver();var catalog=Catalog(new Store(),receiver,grant);var route=catalog.Targets.Single();
        Assert.IsTrue((await route.ApplyReplicatedMutationAsync(Item(),default)).Result);
        var denied=await route.ApplyReplicatedMutationAsync(Item(actor:Guid.NewGuid()),default);Assert.AreEqual("COSMIC_REPLICA_AUTHORIZATION_DENIED",denied.ErrorCode);
        clock.Value=Now.AddMinutes(1);Assert.AreEqual("COSMIC_REPLICA_AUTHORIZATION_DENIED",(await route.ApplyReplicatedMutationAsync(Item(),default)).ErrorCode);
        Assert.AreEqual(1,receiver.Calls);
        Assert.AreNotEqual(grant.BindingIdentity,new HolonReplicationActorGrant(new[]{Actor},Now.AddMinutes(2),clock).BindingIdentity);
    }
    [DataTestMethod]
    [DataRow("deny")] [DataRow("null")] [DataRow("throw")] [DataRow("binding-before")] [DataRow("binding-during")] [DataRow("source")]
    public async Task MissingOrChangedAuthorizationNeverCallsTheReceiver(string mode)
    {
        var policy=new Policy();var receiver=new Receiver();var route=Catalog(new Store(),receiver,policy).Targets.Single();
        policy.Apply=(context,token)=> mode switch {
            "deny"=>Task.FromResult(new OASISResult<bool>(false)),"null"=>Task.FromResult<OASISResult<bool>>(null),
            "throw"=>Task.FromException<OASISResult<bool>>(new InvalidOperationException("owned policy failure")),
            "binding-during"=>Changed(),_=>Task.FromResult(new OASISResult<bool>(true))};
        Task<OASISResult<bool>> Changed(){policy.BindingIdentity="changed";return Task.FromResult(new OASISResult<bool>(true));}
        if(mode=="binding-before")policy.BindingIdentity="changed";
        var result=await route.ApplyReplicatedMutationAsync(Item(mode=="source" ? "other":"source"),default);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.AreEqual(0,receiver.Calls);
        if(mode=="source" || mode=="binding-before")Assert.AreEqual(0,policy.Calls);
    }
    [TestMethod]
    public async Task PolicyCannotRewriteFrozenMutationAndItsWarningsRemainVisible()
    {
        var policy=new Policy{Apply=(context,token)=>{
            context.Mutation.Roots[0].Name="policy rewrite";return Task.FromResult(new OASISResult<bool>(true){IsWarning=true,Exception=new Exception("owned policy warning")});}};
        var receiver=new Receiver();var route=Catalog(new Store(),receiver,policy).Targets.Single();
        var result=await route.ApplyReplicatedMutationAsync(Item(),default);
        Assert.IsTrue(result.Result);Assert.IsTrue(result.IsWarning);Assert.AreEqual("frozen original",receiver.Name);StringAssert.Contains(result.Exception.Message,"owned policy warning");
    }
    [TestMethod]
    public async Task AwaitedAuthorizationCannotApplyAReplacementEnvelope()
    {
        var entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var original=Item();var originalOperation=original.OperationId;
        var replacement=Item(actor:Guid.NewGuid());
        var replacementMutation=CommittedHolonReplicaPayload.Read(replacement.PayloadJson);
        replacementMutation.Roots[0].Name="unauthorized replacement";
        replacement=CommittedHolonReplicaTarget.CaptureItem(replacementMutation);
        var policy=new Policy{Apply=async(context,token)=>{
            entered.TrySetResult(true);await release.Task.WaitAsync(token);
            return new OASISResult<bool>(context.Mutation.ActorId==Actor);
        }};
        var receiver=new Receiver();var route=Catalog(new Store(),receiver,policy).Targets.Single();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pending=route.ApplyReplicatedMutationAsync(original,timeout.Token);
        try
        {
            await entered.Task.WaitAsync(timeout.Token);
            original.OperationId=replacement.OperationId;original.AvatarId=replacement.AvatarId;
            original.EntityId=replacement.EntityId;original.EntityType=replacement.EntityType;
            original.Kind=replacement.Kind;original.VersionId=replacement.VersionId;
            original.PayloadJson=replacement.PayloadJson;original.AttemptCount=999;
        }
        finally{release.TrySetResult(true);}
        var result=await pending;
        Assert.IsTrue(result.Result);Assert.IsFalse(result.IsError);Assert.AreEqual(1,receiver.Calls);
        Assert.AreEqual(Actor,receiver.ActorId,"The receiver must apply the actor authorized before the await.");
        Assert.AreEqual(originalOperation,receiver.OperationId);
        Assert.AreEqual("frozen original",receiver.Name);
    }
    [DataTestMethod]
    [DataRow("operation")] [DataRow("actor")] [DataRow("entity")]
    [DataRow("type")] [DataRow("kind")] [DataRow("version")]
    public async Task PolicyCannotRepairAnOriginallyInvalidRoutingEnvelope(string field)
    {
        var item=Item();var operation=item.OperationId;var actor=item.AvatarId;
        var entity=item.EntityId;var type=item.EntityType;var kind=item.Kind;var version=item.VersionId;
        switch(field)
        {
            case "operation":item.OperationId=Guid.NewGuid();break;
            case "actor":item.AvatarId=Guid.NewGuid();break;
            case "entity":item.EntityId=Guid.NewGuid();break;
            case "type":item.EntityType="other";break;
            case "kind":item.Kind=SyncOperationKind.Delete;break;
            case "version":item.VersionId=Guid.NewGuid();break;
        }
        var policy=new Policy{Apply=(_,_)=>{
            item.OperationId=operation;item.AvatarId=actor;item.EntityId=entity;
            item.EntityType=type;item.Kind=kind;item.VersionId=version;
            return Task.FromResult(new OASISResult<bool>(true));
        }};
        var receiver=new Receiver();var route=Catalog(new Store(),receiver,policy).Targets.Single();
        var result=await route.ApplyReplicatedMutationAsync(item,default);
        Assert.IsTrue(result.IsError);Assert.AreEqual("COSMIC_REPLICA_PAYLOAD_INVALID",result.ErrorCode);
        Assert.AreEqual(0,receiver.Calls,"Policy cannot replace the original routing assertions.");
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task AuthorizationDenialAndFailurePersistReconciliationInsteadOfRetry(bool thrown)
    {
        var store=new Store();var receiver=new Receiver();var policy=new Policy{Apply=(_,_)=>thrown
            ? Task.FromException<OASISResult<bool>>(new InvalidOperationException("policy failed")):Task.FromResult(new OASISResult<bool>(false))};
        var catalog=Catalog(store,receiver,policy);var result=await new HolonOutboxDispatcher(store,catalog.Targets,"worker",new Clock()).DispatchOnceAsync(catalog.TargetIds.Single(),1);
        Assert.IsFalse(result.IsError);Assert.AreEqual(1,result.Result.ReconciliationCount);Assert.AreEqual(0,receiver.Calls);
        Assert.AreEqual(HolonDeliveryOutcome.ReconciliationRequired,store.Outcomes.Values.Single());
        Assert.AreEqual(thrown ? "COSMIC_REPLICA_AUTHORIZATION_FAILED":"COSMIC_REPLICA_AUTHORIZATION_DENIED",store.LastCode);
    }
    [TestMethod]
    public void InvalidOrDuplicateHostRegistrationFailsBeforeChangingServicesOrClaiming()
    {
        var store=new Store();var receiver=new Receiver();var catalog=Catalog(store,receiver);var services=new ServiceCollection();
        Assert.ThrowsException<ArgumentException>(()=>services.AddCosmicHolonReplication(Options(),catalog,catalog));Assert.AreEqual(0,services.Count);
        Assert.ThrowsException<ArgumentNullException>(()=>new HolonReplicationDestination("target","endpoint",ProviderType.MongoDBOASIS,receiver,null));
        var conflict=Catalog(store,receiver,version:Guid.NewGuid(),source:"different");
        Assert.ThrowsException<ArgumentException>(()=>services.AddCosmicHolonReplication(Options(),catalog,conflict));Assert.AreEqual(0,services.Count);
        services.AddCosmicHolonReplication(Options(),catalog);var count=services.Count;
        Assert.ThrowsException<InvalidOperationException>(()=>services.AddCosmicHolonReplication(Options(),catalog));Assert.AreEqual(count,services.Count);Assert.AreEqual(0,store.Claims);
    }
    [TestMethod]
    public async Task GenericHostAutomaticallyProcessesIndependentRoutesAndJoinsShutdown()
    {
        var blockedStarted=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);var store=new Store();
        var blocked=new Receiver{Before=async token=>{blockedStarted.TrySetResult(true);await release.Task.WaitAsync(token);}};var fast=new Receiver();
        var catalog=new HolonReplicationCatalog("catalog",Guid.NewGuid(),"source","store",ProviderType.SQLLiteDBOASIS,store,
            new[]{Destination(blocked,name:"blocked"),Destination(fast,name:"fast")});
        using var host=new HostBuilder().ConfigureServices(services=>services.AddCosmicHolonReplication(Options(),catalog)).Build();
        try
        {
            await host.StartAsync();await blockedStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));await store.Finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(1,fast.Calls);Assert.AreEqual(1,store.Finishes,"A blocked route must not stall a different target.");
            release.TrySetResult(true);
            var deadline=DateTime.UtcNow.AddSeconds(5);while(Volatile.Read(ref store.Finishes)!=2 && DateTime.UtcNow<deadline)await Task.Delay(10);
            Assert.AreEqual(2,store.Finishes);Assert.IsTrue(store.Outcomes.Values.All(outcome=>outcome==HolonDeliveryOutcome.Completed));
        }
        finally{release.TrySetResult(true);using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));await host.StopAsync(stop.Token);}
    }
    [TestMethod]
    public async Task HostCancellationObservesActiveReceiverWithoutFinishingItsClaim()
    {
        var started=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);var stopped=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiver=new Receiver{Before=async token=>{started.TrySetResult(true);try{await Task.Delay(Timeout.Infinite,token);}finally{stopped.TrySetResult(true);}}};
        var store=new Store();using var host=new HostBuilder().ConfigureServices(services=>services.AddCosmicHolonReplication(Options(),Catalog(store,receiver))).Build();
        await host.StartAsync();await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));await host.StopAsync(stop.Token);
        Assert.IsTrue(stopped.Task.IsCompletedSuccessfully);Assert.AreEqual(0,store.Finishes);
    }
    private sealed class ThrowingLogger : ILogger<HolonReplicationHostedService>
    {
        public IDisposable BeginScope<TState>(TState state)=>null;public bool IsEnabled(LogLevel level)=>true;
        public void Log<TState>(LogLevel level,EventId id,TState state,Exception error,Func<TState,Exception,string> formatter)=>throw new InvalidOperationException("owned observer failure");
    }
    [TestMethod]
    public async Task UnexpectedRouteFailureCancelsAndObservesSiblingLoops()
    {
        var stopped=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);var entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting=new Receiver{Before=async token=>{entered.TrySetResult(true);try{await Task.Delay(Timeout.Infinite,token);}finally{stopped.TrySetResult(true);}}};
        var failing=new Receiver{Before=async token=>await entered.Task.WaitAsync(token),Rejection=new OASISResult<IEnumerable<IHolon>>{IsError=true,ErrorCode="TEST_REJECT"}};
        var catalog=new HolonReplicationCatalog("catalog",Guid.NewGuid(),"source","store",ProviderType.SQLLiteDBOASIS,new Store(),
            new[]{Destination(waiting,name:"waiting"),Destination(failing,name:"failure")});
        using var service=new HolonReplicationHostedService(new[]{catalog},Options(),new ThrowingLogger());
        await service.StartAsync(default);
        var failure=await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>service.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.AreEqual("owned observer failure",failure.Message);Assert.IsTrue(stopped.Task.IsCompletedSuccessfully);
    }
    [TestMethod]
    public async Task ExpiredStopDeadlineDoesNotReportSuccessWithAnUnobservedTarget()
    {
        var started=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiver=new Receiver{Before=async token=>{started.TrySetResult(true);await release.Task;}};var store=new Store();
        using var service=new HolonReplicationHostedService(new[]{Catalog(store,receiver)},Options(),NullLogger<HolonReplicationHostedService>.Instance);
        await service.StartAsync(default);await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var deadline=new CancellationTokenSource();deadline.Cancel();
        try
        {
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(()=>service.StopAsync(deadline.Token));
            Assert.IsFalse(service.ExecuteTask.IsCompleted);Assert.AreEqual(0,store.Finishes);
        }
        finally{release.TrySetResult(true);await service.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(5));}
        Assert.AreEqual(0,store.Finishes);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task RestartedBlockedOrUnavailableQueueCannotAppearReadyOrConsumeAClaim(bool unavailable)
    {
        var store=new Store{FailPeek=unavailable,RetainedHead=HolonDeliveryState.ReconciliationRequired};var receiver=new Receiver();
        var services=new ServiceCollection();services.AddCosmicHolonReplication(Options(),Catalog(store,receiver));
        using var container=services.BuildServiceProvider();var state=container.GetRequiredService<HolonReplicationHostState>();
        var health=container.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        Assert.AreEqual(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,(await health.CheckHealthAsync()).Status);
        var service=container.GetServices<IHostedService>().OfType<HolonReplicationHostedService>().Single();await service.StartAsync(default);
        try
        {
            var deadline=DateTime.UtcNow.AddSeconds(5);
            while(state.Snapshot().Count==0 || state.Snapshot()[0].Availability!=HolonReplicationRouteAvailability.Degraded)
            {Assert.IsTrue(DateTime.UtcNow<deadline);await Task.Delay(10);}
            Assert.IsFalse(state.IsReady);Assert.AreEqual(0,store.Claims);Assert.AreEqual(0,store.Finishes);Assert.AreEqual(0,receiver.Calls);
            Assert.AreEqual(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,(await health.CheckHealthAsync()).Status);
            Assert.AreEqual(unavailable ? "OWNED_HEAD_UNAVAILABLE":"COSMIC_OUTBOX_RECONCILIATION_REQUIRED",state.Snapshot()[0].Code);
        }
        finally{using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));await service.StopAsync(stop.Token);}
        Assert.IsFalse(state.IsReady);
    }
    [TestMethod]
    public async Task NativeHeadRecoveryTransitionsHealthWithoutReplayingOrRewritingTheQueue()
    {
        var store=new Store{FailPeek=true};var receiver=new Receiver();using var service=new HolonReplicationHostedService(new[]{Catalog(store,receiver)},Options(),NullLogger<HolonReplicationHostedService>.Instance);
        await service.StartAsync(default);
        try
        {
            var deadline=DateTime.UtcNow.AddSeconds(5);
            while(Volatile.Read(ref store.Peeks)==0){Assert.IsTrue(DateTime.UtcNow<deadline);await Task.Delay(10);}
            Assert.IsFalse(service.State.IsReady);Assert.AreEqual(0,store.Claims);
            store.FailPeek=false;await store.Finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
            while(!service.State.IsReady){Assert.IsTrue(DateTime.UtcNow<deadline);await Task.Delay(10);}
            Assert.AreEqual(1,store.Finishes);Assert.AreEqual(1,receiver.Calls);
        }
        finally{using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));await service.StopAsync(stop.Token);}
        Assert.IsFalse(service.State.IsReady);
    }

}
