using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeExecutionStrategyPreservesIntentAndNeverReplaysUnknownOutcome(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyExecutionStrategy,
            nativeJourneySuffix:"-execution-strategy-native.json");

    // A client-boundary fault decorator. Every actual write and receipt read is
    // delegated to the shipping provider and its real isolated native database.
    private sealed class ExecutionBoundary(ICosmicExecutionStrategyStorageProvider native,string fault)
        :ICosmicExecutionStrategyStorageProvider
    {
        public int Calls,NativeWrites,Recoveries;
        public ProviderType ExecutionStrategyAuthority=>native.ExecutionStrategyAuthority;
        public async Task<OASISResult<IEnumerable<IHolon>>> ExecuteBoundConditionalHolonWriteAsync(
            ConditionalHolonWriteRequest request,CosmicStorageIdentity source,CancellationToken token=default)
        {
            Calls++;
            if(fault=="abort" && Calls==1)
                return new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_CONCURRENT_WRITE",
                    Exception=new CosmicTransactionAbortedException(request.Write.OperationId,request.Write.ActorId,
                        source,new IOException("Injected client-boundary failure before native dispatch."))};
            if(fault!="absent")
            {
                NativeWrites++;
                var result=await native.ExecuteBoundConditionalHolonWriteAsync(request,source,token);
                if(fault!="lost" || result.IsError)return result;
            }
            return new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_WRITE_RECONCILIATION_REQUIRED",
                Exception=new CosmicTransactionReconciliationRequiredException(request.Write.OperationId,
                    new IOException("Injected lost transport acknowledgement."))};
        }
        public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
            =>native.ExecuteConditionalHolonWrite(request);
        public Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(ConditionalHolonWriteRequest request,CancellationToken token=default)
            =>native.ExecuteConditionalHolonWriteAsync(request,token);
        public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonWriteOutcomeAsync(ConditionalHolonWriteRequest request,CancellationToken token=default)
            =>native.ReadConditionalHolonWriteOutcomeAsync(request,token);
        public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadBoundConditionalHolonWriteOutcomeAsync(
            ConditionalHolonWriteRequest request,CosmicStorageIdentity source,CancellationToken token=default)
        {Recoveries++;return native.ReadBoundConditionalHolonWriteOutcomeAsync(request,source,token);}
    }

    private static async Task<NativeFixtureJourneyProof> VerifyExecutionStrategy(IOASISStorageProvider provider,
        List<Guid> owned,Dictionary<string,long> timings)
    {
        Assert.IsInstanceOfType<ICosmicExecutionStrategyStorageProvider>(provider);
        var native=(ICosmicExecutionStrategyStorageProvider)provider;
        var captured=await ((ICosmicStorageIdentityProvider)provider).CaptureStorageIdentityAsync();
        Assert.IsFalse(captured.IsError,captured.Message);var source=captured.Result;
        var options=new CosmicExecutionStrategyOptions(3,TimeSpan.Zero,TimeSpan.Zero);
        var strategy=new CosmicExecutionStrategy(native,source,options);
        var actor=Guid.NewGuid();
        ConditionalHolonWriteRequest Create(string label)
        {
            var row=new Holon{Id=Guid.NewGuid(),Name=label,IsNewHolon=true,IsActive=true,
                IsPublic=false,CreatedByAvatarId=actor};owned.Add(row.Id);
            return new(new ConditionalHolonWrite(Guid.NewGuid(),actor,native.ExecutionStrategyAuthority,
                new[]{new HolonWriteCondition(row.Id,null)}),ConditionalHolonOperation.Save,new[]{row},saveChildren:false);
        }
        async Task<T> Timed<T>(string label,Func<Task<T>> action)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(label,timer.ElapsedMilliseconds);}
        }
        var first=Create("O07 exact native intent");
        var saved=await Timed("native-create",()=>strategy.ExecuteAsync(first));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        Assert.AreEqual("1",saved.MetaData["COSMIC.ExecutionAttempts"]);
        var acknowledged=saved.Result.Single();Assert.AreEqual(1,acknowledged.Version);
        var replay=await Timed("exact-receipt-replay",()=>strategy.ExecuteAsync(first));
        Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);Assert.AreEqual(1,replay.Result.Single().Version);
        Assert.AreEqual(acknowledged.VersionId,replay.Result.Single().VersionId);
        var aborted=Create("O07 aborted before dispatch");
        var abortBoundary=new ExecutionBoundary(native,"abort");
        var retried=await Timed("bounded-abort-retry",()=>new CosmicExecutionStrategy(abortBoundary,source,options).ExecuteAsync(aborted));
        Assert.IsFalse(retried.IsError,retried.Message+retried.Exception);Assert.IsTrue(retried.IsSaved);
        Assert.AreEqual(2,abortBoundary.Calls);Assert.AreEqual(1,abortBoundary.NativeWrites);
        Assert.AreEqual("2",retried.MetaData["COSMIC.ExecutionAttempts"]);
        Assert.AreEqual(1,retried.Result.Single().Version);
        var uncertain=Create("O07 committed with lost acknowledgement");
        var lost=new ExecutionBoundary(native,"lost");
        var unknown=await Timed("committed-recovery-no-replay",()=>new CosmicExecutionStrategy(lost,source,options).ExecuteAsync(uncertain));
        Assert.IsTrue(unknown.IsError);Assert.IsFalse(unknown.IsSaved);
        Assert.AreEqual("Committed",unknown.MetaData["COSMIC.ReceiptRecovery"]);
        Assert.AreEqual(1,lost.Calls);Assert.AreEqual(1,lost.NativeWrites);Assert.AreEqual(1,lost.Recoveries);
        var absent=Create("O07 receipt absence does not authorize replay");
        var missing=new ExecutionBoundary(native,"absent");
        var noReceipt=await Timed("absent-recovery-no-replay",()=>new CosmicExecutionStrategy(missing,source,options).ExecuteAsync(absent));
        Assert.IsTrue(noReceipt.IsError);Assert.AreEqual("Absent",noReceipt.MetaData["COSMIC.ReceiptRecovery"]);
        Assert.AreEqual(1,missing.Calls);Assert.AreEqual(0,missing.NativeWrites);Assert.AreEqual(1,missing.Recoveries);
        var changed=new Holon{Id=acknowledged.Id,Name="O07 rejected stale revision",CreatedByAvatarId=actor,
            IsNewHolon=false,IsActive=true,Version=acknowledged.Version,VersionId=acknowledged.VersionId};
        var conflict=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),actor,native.ExecutionStrategyAuthority,
            new[]{new HolonWriteCondition(changed.Id,Guid.NewGuid())}),ConditionalHolonOperation.Save,new[]{changed},saveChildren:false);
        var rejected=await Timed("concurrency-is-not-transient",()=>strategy.ExecuteAsync(conflict));
        Assert.IsTrue(rejected.IsError);Assert.AreEqual(CosmicConcurrencyException.Code,rejected.ErrorCode);
        Assert.AreEqual("1",rejected.MetaData["COSMIC.ExecutionAttempts"]);
        var wrong=Create("O07 wrong physical store must not write");
        var wrongSource=new CosmicStorageIdentity(source.Authority,source.EndpointFingerprint,Guid.NewGuid());
        var wrongResult=await Timed("native-source-binding",()=>new CosmicExecutionStrategy(native,wrongSource,options).ExecuteAsync(wrong));
        Assert.IsTrue(wrongResult.IsError);Assert.AreEqual("1",wrongResult.MetaData["COSMIC.ExecutionAttempts"]);
        var stolen=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(first.Write.OperationId,Guid.NewGuid(),
            first.Write.Authority,first.Write.Conditions),first.Operation,first.Roots,saveChildren:false);
        var denied=await Timed("receipt-actor-binding",()=>native.ReadBoundConditionalHolonWriteOutcomeAsync(stolen,source));
        Assert.IsTrue(denied.IsError,"Another actor cannot consume the original receipt.");
        var cancelledBoundary=new ExecutionBoundary(native,"abort");
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var cancelled=await Timed("cancel-before-dispatch",()=>new CosmicExecutionStrategy(cancelledBoundary,source,options).ExecuteAsync(first,cancellation.Token));
        Assert.IsTrue(cancelled.IsError);Assert.AreEqual(0,cancelledBoundary.Calls);
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        using var reader=CosmicSession.Create(manager,(ICosmicQueryStorageProvider)provider,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var ids=owned.ToArray();var query=reader.Query<Holon>().Result.Where(row=>ids.Contains(row.Id));
        Assert.IsFalse(query.IsError,query.Message);
        var visible=await Timed("native-final-state",()=>reader.ReadAsync(query.Result,trackResults:false));
        Assert.IsFalse(visible.IsError,visible.Message+visible.Exception);Assert.AreEqual(3,visible.Result.Count);
        Assert.IsTrue(visible.Result.All(row=>row.Version==1));
        Assert.AreEqual(0,first.Roots[0].Version);Assert.AreEqual(0,uncertain.Roots[0].Version);
        Assert.AreEqual("O07 exact native intent",first.Roots[0].Name);
        return new NativeFixtureJourneyProof(10,3,new Dictionary<string,bool>{
            ["nativeSourceBoundCreate"]=true,["stableIdempotentReplay"]=true,["boundedConfirmedAbortRetry"]=true,
            ["unknownCommitReceiptNoReplay"]=true,["absentReceiptNoReplay"]=true,["callerSnapshotPreserved"]=true,
            ["optimisticConflictNotRetried"]=true,["cancellationBound"]=true,["physicalSourceBinding"]=true,
            ["receiptActorIsolation"]=true});
    }
}
