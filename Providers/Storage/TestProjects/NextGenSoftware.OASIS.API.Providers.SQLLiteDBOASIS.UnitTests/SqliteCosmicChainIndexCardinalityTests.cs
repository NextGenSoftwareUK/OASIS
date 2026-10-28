using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

public partial class SqliteCosmicChainIndexRowsTests
{
    private sealed class CardinalityObserver(CosmicChainQuerySource source,CosmicChainQueryPosition position,string mode) : ICosmicChainQuerySourceObserver
    {
        public int Heads;
        public Task<OASISResult<CosmicChainQueryObservedRange>> ObserveRangeAsync(CosmicChainQuerySource selected,CancellationToken token)
            =>throw new InvalidOperationException("A cardinality query cannot bootstrap the source.");
        public Task<OASISResult<CosmicChainQueryObservedRecord>> ObserveRecordAsync(CosmicChainQuerySource selected,string commit,CancellationToken token)
            =>throw new InvalidOperationException("Index cardinality must not manufacture per-record source reads.");
        public Task<OASISResult<CosmicChainQueryFinalizedHead>> ObserveHeadAsync(CosmicChainQuerySource selected,IReadOnlyList<long> slots,CancellationToken token)
        {
            Heads++;
            CollectionAssert.AreEqual(new[]{position.Slot},slots.ToArray());
            if(mode=="head-error")return Task.FromResult(new OASISResult<CosmicChainQueryFinalizedHead>{IsError=true,ErrorCode="source-unavailable"});
            return Task.FromResult(new OASISResult<CosmicChainQueryFinalizedHead>(new(source.SourceProvider,
                mode=="network"?"other-network":source.NetworkIdentity,position,DateTimeOffset.UtcNow-(mode=="stale"?TimeSpan.FromHours(1):TimeSpan.Zero))));
        }
    }

    [DataTestMethod]
    [DataRow("count")] [DataRow("long")] [DataRow("any")] [DataRow("skip")]
    [DataRow("outside")] [DataRow("take")] [DataRow("zero")] [DataRow("empty")]
    [DataRow("catalogue")] [DataRow("payload-not-read")]
    [DataRow("tamper")] [DataRow("history-tamper")] [DataRow("stale")]
    [DataRow("head-error")] [DataRow("network")] [DataRow("cancelled")] [DataRow("any-empty")] [DataRow("long-zero")]
    public async Task NativeIndexCardinalityUsesAuthenticatedSnapshotVisibilityAndExactWindow(string mode)
    {
        using var store=new SqliteCosmicChainIndexStore(database,source,key,canonicalRowProjection:true);
        var a=Entry("A",actor,false);var b=Entry("B",Guid.NewGuid(),true);
        var hidden=Entry("0-hidden",Guid.NewGuid(),false);var deleted=Entry("0-deleted",actor,true,true);
        Publish(store,a,b,hidden,deleted);
        if(mode=="tamper")Sql("UPDATE CosmicChainIndexRows SET IsPublic=1 WHERE Name='0-hidden'");
        if(mode=="history-tamper")Sql("DELETE FROM CosmicChainIndexHistory WHERE Id='"+a.Receipt.Record.Id+"'");
        // Payload availability is not falsely promoted to per-record observation.
        // Count uses the authenticated canonical projection/history manifest only.
        if(mode=="payload-not-read")Sql("UPDATE CosmicChainIndexHistory SET Payload='damaged-business-payload'");
        var query=Query(mode=="catalogue"?CosmicQueryReadAccess.AuthoritativeRecords(actor,source.SourceProvider,new[]{hidden.Receipt.Record.Id}).Result:null)
            .OrderBy(row=>row.Name).Result;
        if(mode=="skip")query=query.Skip(1).Result;
        if(mode=="outside")query=query.Skip(2).Result;
        if(mode=="take")query=query.Take(1).Result;
        if(mode is "zero" or "long-zero")query=query.Take(0).Result;
        if(mode is "empty" or "any-empty")query=query.Where(row=>row.Name=="missing").Result;
        var observer=new CardinalityObserver(source,position,mode);
        var attempts=new SqlAttemptOwner();using var cancellation=new CancellationTokenSource();
        if(mode=="cancelled")cancellation.Cancel();
        var target=new CosmicScalarTarget(source.SourceProvider,store.IndexedRowCapabilities,store.IndexedScalarCapabilities,source);
        var expected=mode is "outside" or "zero" or "empty" or "long-zero" or "any-empty"?0L:mode is "skip" or "take" or "catalogue"?1L:2L;
        if(mode is "long" or "long-zero")await Check(query.LongCount().Result,expected);
        else if(mode is "any" or "any-empty")await Check(query.Any().Result,mode=="any");
        else await Check(query.Count().Result,(int)expected);

        async Task Check<TResult>(CosmicScalarQuery<Holon,TResult> scalar,TResult expectedValue)
        {
            var plan=CosmicScalarPlanner.Plan(scalar,source.SourceProvider,new[]{target});Assert.IsFalse(plan.IsError,plan.Message);
            var result=await CosmicIndexedReadAttempts.RunAsync(source,attempts,
                ()=>CosmicIndexedScalarExecutor.ExecuteAsync(plan.Result,store,observer,cancellation.Token));
            Assert.AreEqual(mode=="cancelled"?0:1,attempts.Count);
            if(mode is "tamper" or "history-tamper" or "stale" or "head-error" or "network" or "cancelled")
            {Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);}
            else{
                Assert.IsFalse(result.IsError,result.Message+result.Exception);
                Assert.AreEqual(expectedValue,result.Result.Value);
                Assert.AreEqual("authenticated-index-snapshot",result.Result.Assurance);
                Assert.AreEqual(position.Slot,result.Result.Checkpoint.Position.Slot);Assert.AreEqual(1,observer.Heads);
            }
        }
    }

    [DataTestMethod]
    [DataRow("count")] [DataRow("long")] [DataRow("any")]
    [DataRow("window")] [DataRow("zero")] [DataRow("deferred")] [DataRow("predicate")]
    [DataRow("window-predicate")] [DataRow("all")]
    public async Task StandardIndexedCardinalityUsesNativeScalarContractAndDefersSourceValues(string mode)
    {
        using var store=new SqliteCosmicChainIndexStore(database,source,key,canonicalRowProjection:true);
        var a=Entry("A",actor,false);var b=Entry("B",Guid.NewGuid(),true);var hidden=Entry("0-hidden",Guid.NewGuid(),false);
        Publish(store,a,b,hidden);
        var observer=new CardinalityObserver(source,position,"count");
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2}};
        var manager=new HolonManager(null,dna,new ProviderManager(null,dna));
        var created=CosmicLinq.CreateIndexed(manager,store,observer,Query());Assert.IsFalse(created.IsError,created.Message);
        var query=created.Result;var name="missing";
        if(mode=="deferred"){query=query.Where(row=>row.Name==name);name="A";}
        if(mode=="window")query=query.OrderBy(row=>row.Name).Skip(1).Take(1);
        if(mode=="zero")query=query.Take(0);
        var attempts=new SqlAttemptOwner();
        await CosmicIndexedReadAttempts.RunAsync(source,attempts,()=>{
            if(mode is "window-predicate" or "all"){
                var error=Assert.ThrowsException<CosmicLinqException>(()=>{
                    if(mode=="all")_ = query.All(row=>row.IsPublic);
                    else _ = query.Take(1).Count(row=>row.IsPublic);
                });
                Assert.AreEqual(mode=="all"?"CQL_EXPRESSION_UNSUPPORTED":"CQL_CAPABILITY_UNSUPPORTED",error.Code);
            }
            else if(mode=="long")Assert.AreEqual(2L,query.LongCount());
            else if(mode=="any")Assert.IsTrue(query.Any(row=>row.Name=="A"));
            else if(mode=="predicate")Assert.AreEqual(1,query.Count(row=>row.Name=="B"));
            else Assert.AreEqual(mode=="zero"?0:mode is "window" or "deferred"?1:2,query.Count());
            return Task.FromResult(true);
        });
        Assert.AreEqual(mode is "window-predicate" or "all"?0:1,attempts.Count);
        Assert.AreEqual(mode is "window-predicate" or "all"?0:1,observer.Heads);
    }
}
