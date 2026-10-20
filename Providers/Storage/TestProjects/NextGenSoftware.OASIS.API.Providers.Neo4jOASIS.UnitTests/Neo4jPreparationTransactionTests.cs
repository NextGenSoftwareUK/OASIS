using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jPreparationTransactionTests
{
    public class TransactionProbe:DispatchProxy
    {
        public Dictionary<string,object> Stored=new();
        public int Queries,Commits,Rollbacks,Disposals;
        public bool Fresh;
        public Exception? CommitFailure,RollbackFailure,DisposeFailure;
        public string? WrittenPayload;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            switch(method!.Name)
            {
                case "Dispose":Disposals++;if(DisposeFailure!=null)throw DisposeFailure;return null;
                case "CommitAsync":Commits++;return CommitFailure==null?Task.CompletedTask:Task.FromException(CommitFailure);
                case "RollbackAsync":Rollbacks++;return RollbackFailure==null?Task.CompletedTask:Task.FromException(RollbackFailure);
                case "RunAsync":
                    Queries++;
                    var query=(string)args![0]!;var parameters=args[1]!;
                    object value;
                    string key;
                    if(query.StartsWith("MERGE",StringComparison.Ordinal))
                    {
                        if(Fresh)Stored["__cosmicPreparationClaim"]=parameters.GetType().GetProperty("claim")!.GetValue(parameters)!;
                        var node=DispatchProxy.Create<Neo4j.Driver.INode,Neo4jNativeIdentityTests.NodeProbe>();
                        ((Neo4jNativeIdentityTests.NodeProbe)node).Values=Stored;
                        value=node;key="r";
                    }
                    else if(query.StartsWith("MATCH",StringComparison.Ordinal)&&query.Contains("PreparationJson=$payload",StringComparison.Ordinal))
                    {WrittenPayload=(string)parameters.GetType().GetProperty("payload")!.GetValue(parameters)!;value=1L;key="count";}
                    else throw new InvalidOperationException("Unexpected preparation statement: "+query);
                    var record=DispatchProxy.Create<IRecord,RecordProbe>();
                    ((RecordProbe)record).Key=key;((RecordProbe)record).Value=value;
                    var cursor=DispatchProxy.Create<IResultCursor,Neo4jConditionalReceiptCleanupTests.CursorProbe>();
                    ((Neo4jConditionalReceiptCleanupTests.CursorProbe)cursor).Record=record;
                    return Task.FromResult(cursor);
                default:throw new InvalidOperationException("Unexpected transaction call: "+method.Name);
            }
        }
    }
    public class RecordProbe:DispatchProxy
    {
        public string Key="";public object Value=null!;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
            =>method!.Name=="get_Item"&&(string)args![0]! == Key?Value:throw new InvalidOperationException("Unexpected record call.");
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task ConditionalPromotionRejectsForeignActorOrCorruptPreparationBeforeEntityMutation(bool synchronous,bool foreignActor)
    {
        var actor=Guid.NewGuid();var operation=Guid.NewGuid();var root=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Guid.NewGuid()};
        var transaction=DispatchProxy.Create<IAsyncTransaction,TransactionProbe>();var probe=(TransactionProbe)transaction;
        probe.Stored=new(){["PreparationActorId"]=(foreignActor?Guid.NewGuid():actor).ToString("D"),
            ["PreparationFormat"]=foreignActor?1L:2L,["PreparationJson"]="[]",["PreparationCreatedAtUtc"]=DateTime.UtcNow.ToString("O")};
        var session=DispatchProxy.Create<IAsyncSession,Neo4jConditionalReceiptCleanupTests.SessionProbe>();
        ((Neo4jConditionalReceiptCleanupTests.SessionProbe)session).Transaction=transaction;
        var driver=DispatchProxy.Create<IDriver,Neo4jLifecycleTests.DriverProbe>();var driverProbe=(Neo4jLifecycleTests.DriverProbe)driver;
        var provider=new Provider("bolt://owned.invalid","owned","owned",()=>driver);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);driverProbe.SessionFactory=()=>session;
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.Neo4jOASIS,
            new[]{new HolonWriteCondition(root.Id,null)}),ConditionalHolonOperation.Save,new[]{root},saveChildren:false);
        var result=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(foreignActor?"COSMIC_OPERATION_ID_REUSED":"COSMIC_RECEIPT_INVALID",result.ErrorCode);
        Assert.AreEqual(1,probe.Queries);Assert.AreEqual(0,probe.Commits);Assert.AreEqual(1,probe.Rollbacks);
        Assert.AreEqual(0,root.Version);Assert.IsNull(CosmicConcurrency.GetProviderRevisionId(root,ProviderType.Neo4jOASIS));
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }

    [DataTestMethod]
    [DataRow(false,"new")] [DataRow(true,"new")]
    [DataRow(false,"replay")] [DataRow(true,"replay")]
    [DataRow(false,"actor")] [DataRow(true,"actor")]
    [DataRow(false,"format")] [DataRow(true,"format")]
    [DataRow(false,"cutover")] [DataRow(true,"cutover")]
    [DataRow(false,"commit")] [DataRow(true,"commit")]
    [DataRow(false,"rollback")] [DataRow(true,"rollback")]
    [DataRow(false,"cleanup")] [DataRow(true,"cleanup")]
    [DataRow(false,"rollback-uncertain")] [DataRow(true,"rollback-uncertain")]
    [DataRow(false,"bad-json")] [DataRow(true,"bad-json")]
    [DataRow(false,"commit-cleanup")] [DataRow(true,"commit-cleanup")]
    [DataRow(false,"new-cleanup")] [DataRow(true,"new-cleanup")]
    [DataRow(false,"replay-receipt")] [DataRow(true,"replay-receipt")]
    public async Task PreparationHasOneTransactionOwnerAndPreservesOriginalWinner(bool synchronous,string scenario)
    {
        var id=Guid.NewGuid();var actor=Guid.NewGuid();var operation=Guid.NewGuid();
        var original=new HolonWritePreparation(operation,actor,ProviderType.Neo4jOASIS,
            new[]{new ProtectedHolonPreparation(id,"oasis-preparation1:original-owned-envelope")});
        var candidate=new HolonWritePreparation(operation,actor,ProviderType.Neo4jOASIS,
            new[]{new ProtectedHolonPreparation(id,"oasis-preparation1:changed-owned-envelope")});
        var transaction=DispatchProxy.Create<IAsyncTransaction,TransactionProbe>();var probe=(TransactionProbe)transaction;
        probe.Fresh=scenario is "new" or "commit" or "commit-cleanup" or "new-cleanup";
        if(!probe.Fresh)probe.Stored=new Dictionary<string,object>{
            ["PreparationActorId"]=actor.ToString("D"),["PreparationFormat"]=1L,
            ["PreparationJson"]=ProtectedPreparationJson.Write(original),["PreparationCreatedAtUtc"]=DateTime.UtcNow.ToString("O")};
        if(scenario=="actor")probe.Stored["PreparationActorId"]=Guid.NewGuid().ToString("D");
        if(scenario is "format" or "rollback-uncertain")probe.Stored["PreparationFormat"]=2L;
        if(scenario=="bad-json")probe.Stored["PreparationJson"]="[]";
        if(scenario=="replay-receipt")probe.Stored["ResultJson"]="owned existing receipt left opaque by preparation storage";
        if(scenario=="cutover"){probe.Stored.Clear();probe.Stored["ResultJson"]="owned prior receipt";}
        if(scenario is "commit" or "commit-cleanup")probe.CommitFailure=new IOException("owned commit acknowledgement loss");
        if(scenario is "rollback" or "rollback-uncertain")probe.RollbackFailure=new IOException("owned read rollback failure");
        if(scenario is "cleanup" or "commit-cleanup" or "new-cleanup")probe.DisposeFailure=new IOException("owned dispose failure");
        var session=DispatchProxy.Create<IAsyncSession,Neo4jConditionalReceiptCleanupTests.SessionProbe>();
        ((Neo4jConditionalReceiptCleanupTests.SessionProbe)session).Transaction=transaction;
        var driver=DispatchProxy.Create<IDriver,Neo4jLifecycleTests.DriverProbe>();var driverProbe=(Neo4jLifecycleTests.DriverProbe)driver;
        var provider=new Provider("bolt://owned.invalid","owned","owned",()=>driver);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);driverProbe.SessionFactory=()=>session;
        var storage=(IHolonWritePreparationStorageProvider)provider;
        var result=synchronous?storage.ClaimHolonWritePreparation(candidate):await storage.ClaimHolonWritePreparationAsync(candidate);
        if(scenario is "actor" or "format" or "bad-json" or "cutover" or "commit" or "commit-cleanup" or "rollback-uncertain")
        {
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
            Assert.AreEqual(scenario=="actor"?"COSMIC_PREPARATION_ACTOR_MISMATCH"
                :scenario is "format" or "bad-json"?"COSMIC_PREPARATION_INVALID":"COSMIC_PREPARATION_RECONCILIATION_REQUIRED",result.ErrorCode);
            if(scenario is "commit" or "commit-cleanup" or "rollback-uncertain")Assert.IsInstanceOfType(result.Exception,typeof(INativeMutationReconciliationRequired));
        }
        else
        {
            Assert.IsFalse(result.IsError,result.Message);
            Assert.AreEqual(probe.Fresh?candidate.Records[0].Envelope:original.Records[0].Envelope,result.Result.Records[0].Envelope);
            Assert.AreEqual(scenario is "rollback" or "cleanup" or "new-cleanup",result.IsWarning);
        }
        Assert.AreEqual(probe.Fresh?2:1,probe.Queries);
        Assert.AreEqual(probe.Fresh?1:0,probe.Commits);
        Assert.AreEqual(probe.Fresh?0:1,probe.Rollbacks);
        Assert.AreEqual(1,probe.Disposals);
        Assert.AreEqual(probe.Fresh?ProtectedPreparationJson.Write(candidate):null,probe.WrittenPayload);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
