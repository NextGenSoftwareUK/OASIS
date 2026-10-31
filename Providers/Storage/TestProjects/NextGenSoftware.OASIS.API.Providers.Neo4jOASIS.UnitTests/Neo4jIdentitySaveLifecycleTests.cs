using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using INode=Neo4j.Driver.INode;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jIdentitySaveLifecycleTests
{
    public class TransactionProbe:DispatchProxy
    {
        public INode Previous=null!;public int Writes,Histories,Rollbacks,Commits;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="Dispose")return null;
            if(method.Name=="CommitAsync"){Commits++;return Task.CompletedTask;}
            if(method.Name=="RollbackAsync"){Rollbacks++;return Task.CompletedTask;}
            if(method.Name!="RunAsync")throw new InvalidOperationException("Unexpected transaction call.");
            var query=(string)args![0]!;var node=Previous;
            if(query.StartsWith("MERGE (v:OasisRecordVersion",StringComparison.Ordinal))Histories++;
            else if(query.StartsWith("MERGE (a:",StringComparison.Ordinal))
            {
                Writes++;var parameters=args[1]!;
                var json=parameters is IDictionary<string,object> map?(string)map["snapshot"]:(string)parameters.GetType().GetProperty("snapshot")!.GetValue(parameters)!;
                var value=OasisJson.ParseStoredObject(json);
                node=DispatchProxy.Create<INode,Neo4jNativeIdentityTests.NodeProbe>();
                ((Neo4jNativeIdentityTests.NodeProbe)node).Values=new Dictionary<string,object>{{"Id",value.Value<string>("Id")!},{"Username",value.Value<string>("Username")!},{"Email",value.Value<string>("Email")!},{"SnapshotJson",json}};
            }
            else if(!query.StartsWith("MATCH (a:",StringComparison.Ordinal))throw new InvalidOperationException("Unexpected identity statement: "+query);
            var record=DispatchProxy.Create<IRecord,Neo4jPreparationTransactionTests.RecordProbe>();
            var row=(Neo4jPreparationTransactionTests.RecordProbe)record;row.Key=query.StartsWith("MERGE (v:",StringComparison.Ordinal)?"v":"a";row.Value=node;
            var cursor=DispatchProxy.Create<IResultCursor,Neo4jConditionalReceiptCleanupTests.CursorProbe>();
            ((Neo4jConditionalReceiptCleanupTests.CursorProbe)cursor).Record=record;return Task.FromResult(cursor);
        }
    }
    public static IEnumerable<object[]> Cases()
    {
        foreach(var detail in new[]{false,true})foreach(var synchronous in new[]{false,true})foreach(var history in new[]{false,true})
            foreach(var scenario in new[]{"active","cleared","retimed","reattributed","compatible","live"})yield return new object[]{detail,synchronous,history,scenario};
    }
    [DataTestMethod,DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task IdentitySaveChecksLockedTombstoneBeforeHistoryAndKeepsKnownFailuresDefinite(bool detail,bool synchronous,bool history,string scenario)
    {
        IHolonBase previous=detail?new AvatarDetail {Username="owned-lifecycle",Email="owned@example.invalid"}:new Avatar {Username="owned-lifecycle",Email="owned@example.invalid"};
        previous.Id=Guid.NewGuid();previous.Version=3;previous.VersionId=Guid.NewGuid();previous.IsActive=scenario=="live";
        previous.DeletedDate=scenario=="live"?DateTime.MinValue:new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc);previous.DeletedByAvatarId=Guid.NewGuid();
        var json=OasisJson.Serialize(previous);IHolonBase source=detail?OasisJson.DeserializeStored<AvatarDetail>(json):OasisJson.DeserializeStored<Avatar>(json);
        if(scenario=="active"){source.IsActive=true;source.DeletedDate=DateTime.MinValue;source.DeletedByAvatarId=Guid.Empty;}
        if(scenario=="cleared")source.DeletedDate=DateTime.MinValue;
        if(scenario=="retimed")source.DeletedDate=source.DeletedDate.AddTicks(1);
        if(scenario=="reattributed")source.DeletedByAvatarId=Guid.NewGuid();
        var before=OasisJson.Serialize(source);
        var node=DispatchProxy.Create<INode,Neo4jNativeIdentityTests.NodeProbe>();((Neo4jNativeIdentityTests.NodeProbe)node).Values=new Dictionary<string,object>{{"Id",previous.Id.ToString()},{"Username","owned-lifecycle"},{"Email","owned@example.invalid"},{"SnapshotJson",json}};
        var tx=DispatchProxy.Create<IAsyncTransaction,TransactionProbe>();var probe=(TransactionProbe)tx;probe.Previous=node;
        var session=DispatchProxy.Create<IAsyncSession,Neo4jConditionalReceiptCleanupTests.SessionProbe>();((Neo4jConditionalReceiptCleanupTests.SessionProbe)session).Transaction=tx;
        var driver=DispatchProxy.Create<IDriver,Neo4jLifecycleTests.DriverProbe>();((Neo4jLifecycleTests.DriverProbe)driver).SessionFactory=()=>session;
        var provider=new Provider("bolt://owned.invalid","owned","owned"){IsProviderActivated=true,IsVersionControlEnabled=history};
        typeof(Provider).GetField("_driver",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,driver);
        bool error,saved;string code;Exception exception;
        if(detail){var result=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);error=result.IsError;saved=result.IsSaved;code=result.ErrorCode;exception=result.Exception;}
        else {var result=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);error=result.IsError;saved=result.IsSaved;code=result.ErrorCode;exception=result.Exception;}
        if(scenario is "active" or "cleared" or "retimed" or "reattributed")
        {
            Assert.IsTrue(error);Assert.IsFalse(saved);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,code);
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(exception));Assert.AreEqual(0,probe.Writes);Assert.AreEqual(0,probe.Histories);Assert.AreEqual(0,probe.Commits);Assert.AreEqual(1,probe.Rollbacks);
            Assert.AreEqual(before,OasisJson.Serialize(source));
        }
        else {Assert.IsFalse(error,exception?.ToString());Assert.IsTrue(saved);Assert.AreEqual(1,probe.Writes);Assert.AreEqual(history?1:0,probe.Histories);Assert.AreEqual(1,probe.Commits);}
    }
}
