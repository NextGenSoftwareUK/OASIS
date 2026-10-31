using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using INode=Neo4j.Driver.INode;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jConditionalLifecycleTests
{
    public class TransactionProbe:DispatchProxy
    {
        public INode Node=null!;public int Histories,Queries,Rollbacks;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="Dispose")return null;
            if(method.Name=="RollbackAsync"){Rollbacks++;return Task.CompletedTask;}
            if(method.Name!="RunAsync")throw new InvalidOperationException("Unexpected transaction call.");
            Queries++;var query=(string)args![0]!;string key;
            if(query.StartsWith("MERGE (h:Holon",StringComparison.Ordinal)||query.StartsWith("UNWIND $ids AS id",StringComparison.Ordinal))key="h";
            else if(query.StartsWith("MERGE (v:OasisRecordVersion",StringComparison.Ordinal)){Histories++;key="v";}
            else throw new InvalidOperationException("Unexpected lifecycle statement: "+query);
            var record=DispatchProxy.Create<IRecord,Neo4jPreparationTransactionTests.RecordProbe>();
            var row=(Neo4jPreparationTransactionTests.RecordProbe)record;row.Key=key;row.Value=Node;
            var cursor=DispatchProxy.Create<IResultCursor,Neo4jConditionalReceiptCleanupTests.CursorProbe>();
            ((Neo4jConditionalReceiptCleanupTests.CursorProbe)cursor).Record=record;return Task.FromResult(cursor);
        }
    }

    [DataTestMethod]
    [DataRow(false,"active")] [DataRow(true,"active")]
    [DataRow(false,"cleared")] [DataRow(true,"cleared")]
    [DataRow(false,"retimed")] [DataRow(true,"retimed")]
    [DataRow(false,"reattributed")] [DataRow(true,"reattributed")]
    [DataRow(false,"compatible")] [DataRow(true,"compatible")]
    [DataRow(false,"live")] [DataRow(true,"live")]
    public async Task ConditionalSavePreservesCommittedDeletionBeforeHistoryOrRevisionStamp(bool history,string scenario)
    {
        var actor=Guid.NewGuid();var previous=new Holon {Id=Guid.NewGuid(),Name="owned lifecycle record",Description="owned lifecycle description",Version=3,VersionId=Guid.NewGuid(),
            IsActive=scenario=="live",DeletedDate=scenario=="live"?DateTime.MinValue:new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc),DeletedByAvatarId=actor};
        var revision=Guid.NewGuid();CosmicConcurrency.SetProviderRevisionId(previous,ProviderType.Neo4jOASIS,revision);
        var source=OasisJson.Project<Holon>(previous);source.Name="owned lifecycle edit";
        if(scenario=="active"){source.IsActive=true;source.DeletedDate=DateTime.MinValue;source.DeletedByAvatarId=Guid.Empty;}
        if(scenario=="cleared")source.DeletedDate=DateTime.MinValue;
        if(scenario=="retimed")source.DeletedDate=source.DeletedDate.AddTicks(1);
        if(scenario=="reattributed")source.DeletedByAvatarId=Guid.NewGuid();
        var plan=ConditionalHolonPlan.Create(new(new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.Neo4jOASIS,new[]{new HolonWriteCondition(source.Id,revision)}),ConditionalHolonOperation.Save,new[]{source},saveChildren:false),ProviderType.Neo4jOASIS);
        var node=DispatchProxy.Create<INode,Neo4jNativeIdentityTests.NodeProbe>();
        ((Neo4jNativeIdentityTests.NodeProbe)node).Values=new Dictionary<string,object>{{"Id",previous.Id.ToString()},{"Name",previous.Name},{"Description",previous.Description},{"HolonType",previous.HolonType.ToString()},{"ProviderRevisionId",revision.ToString()},{"SnapshotJson",OasisJson.Serialize(previous)}};
        var tx=DispatchProxy.Create<IAsyncTransaction,TransactionProbe>();var probe=(TransactionProbe)tx;probe.Node=node;
        var stateType=typeof(Provider).GetNestedType("NeoAtomicHolonWrite",BindingFlags.NonPublic)!;
        var state=Activator.CreateInstance(stateType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{plan,actor,CancellationToken.None},null)!;
        var method=typeof(Provider).GetMethod("PrepareConditionalHolonsAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var provider=new Provider("bolt://owned.invalid","owned","owned");
        var task=(Task<List<Holon>>)method.Invoke(provider,new object[]{tx,plan.Nodes.Select(n=>n.Snapshot).ToList(),state,history})!;
        if(scenario is "active" or "cleared" or "retimed" or "reattributed")
        {await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(async()=>await task);Assert.AreEqual(0,probe.Histories);}
        else {var result=await task;Assert.AreEqual(4,result.Single().Version);Assert.AreEqual(previous.DeletedDate,result.Single().DeletedDate);Assert.AreEqual(history?1:0,probe.Histories);}
        Assert.AreEqual(3,source.Version);Assert.AreEqual(previous.VersionId,source.VersionId);
    }
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task OrdinarySaveRejectsResurrectionAtItsLockedObservation(bool synchronous,bool history)
    {
        var previous=new Holon {Id=Guid.NewGuid(),Name="owned ordinary tombstone",Description="owned lifecycle description",Version=3,VersionId=Guid.NewGuid(),IsActive=false,
            DeletedDate=new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc),DeletedByAvatarId=Guid.NewGuid()};
        CosmicConcurrency.SetProviderRevisionId(previous,ProviderType.Neo4jOASIS,Guid.NewGuid());
        var source=new Holon {Id=previous.Id,Name="owned forbidden ordinary resurrection",Version=previous.Version,VersionId=previous.VersionId};
        var before=OasisJson.Serialize(source);
        var node=DispatchProxy.Create<INode,Neo4jNativeIdentityTests.NodeProbe>();
        ((Neo4jNativeIdentityTests.NodeProbe)node).Values=new Dictionary<string,object>{{"Id",previous.Id.ToString()},{"Name",previous.Name},{"Description",previous.Description},{"HolonType",previous.HolonType.ToString()},
            {"ProviderRevisionId",CosmicConcurrency.GetProviderRevisionId(previous,ProviderType.Neo4jOASIS)!.Value.ToString()},{"SnapshotJson",OasisJson.Serialize(previous)}};
        var tx=DispatchProxy.Create<IAsyncTransaction,TransactionProbe>();var probe=(TransactionProbe)tx;probe.Node=node;
        var session=DispatchProxy.Create<IAsyncSession,Neo4jConditionalReceiptCleanupTests.SessionProbe>();((Neo4jConditionalReceiptCleanupTests.SessionProbe)session).Transaction=tx;
        var driver=DispatchProxy.Create<IDriver,Neo4jLifecycleTests.DriverProbe>();((Neo4jLifecycleTests.DriverProbe)driver).SessionFactory=()=>session;
        var provider=new Provider("bolt://owned.invalid","owned","owned"){IsProviderActivated=true,IsVersionControlEnabled=history};
        typeof(Provider).GetField("_driver",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,driver);
        var result=synchronous?provider.SaveHolon(source,saveChildren:false):await provider.SaveHolonAsync(source,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode,result.Message);
        Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreEqual(1,probe.Queries);Assert.AreEqual(1,probe.Rollbacks);Assert.AreEqual(0,probe.Histories);
        Assert.AreEqual(before,OasisJson.Serialize(source));
    }

}
