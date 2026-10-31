using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeCosmicIdentitySaveLifecyclePreservesPairedTombstones(bool synchronous,bool history)
    {
        const string uri="bolt://127.0.0.1:17687";Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var id=Guid.NewGuid();var marker="owned-identity-lifecycle-"+id.ToString("N");var complete=false;
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=history};
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=driver.AsyncSession();
        async Task<string> Snapshot(string kind)=>(await(await inspection.RunAsync("MATCH (n:"+kind+" {Id:$id}) RETURN n.SnapshotJson AS json",new{id=id.ToString()})).SingleAsync())["json"].As<string>();
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var profile=new Avatar{Id=id,Username=marker,Email=marker+"@example.invalid"};var detail=new AvatarDetail{Id=id,Username=marker,Email=profile.Email};
            Assert.IsFalse((await provider.SaveAvatarAsync(profile)).IsError);Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);
            var deleted=synchronous?provider.DeleteAvatar(id):await provider.DeleteAvatarAsync(id);Assert.IsFalse(deleted.IsError,deleted.Message);
            var priorProfile=await Snapshot("Avatar");var priorDetail=await Snapshot("AvatarDetail");
            var histories=(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) RETURN count(v) AS count",new{id=id.ToString()})).SingleAsync())["count"].As<long>();
            foreach(var change in new[]{"active","retimed","reattributed"})
            {
                var candidate=OasisJson.DeserializeStored<Avatar>(priorProfile);var candidateDetail=OasisJson.DeserializeStored<AvatarDetail>(priorDetail);
                if(change=="active"){candidate.IsActive=true;candidate.DeletedDate=DateTime.MinValue;candidateDetail.IsActive=true;candidateDetail.DeletedDate=DateTime.MinValue;}
                if(change=="retimed"){candidate.DeletedDate=candidate.DeletedDate.AddTicks(1);candidateDetail.DeletedDate=candidateDetail.DeletedDate.AddTicks(1);}
                if(change=="reattributed"){candidate.DeletedByAvatarId=Guid.NewGuid();candidateDetail.DeletedByAvatarId=Guid.NewGuid();}
                var before=OasisJson.Serialize(candidate);var beforeDetail=OasisJson.Serialize(candidateDetail);
                var refused=synchronous?provider.SaveAvatar(candidate):await provider.SaveAvatarAsync(candidate);
                var refusedDetail=synchronous?provider.SaveAvatarDetail(candidateDetail):await provider.SaveAvatarDetailAsync(candidateDetail);
                Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,refused.ErrorCode,refused.Message);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,refusedDetail.ErrorCode,refusedDetail.Message);
                Assert.IsFalse(NativeMutationReconciliation.IsRequired(refused.Exception));Assert.IsFalse(NativeMutationReconciliation.IsRequired(refusedDetail.Exception));
                Assert.AreEqual(before,OasisJson.Serialize(candidate));Assert.AreEqual(beforeDetail,OasisJson.Serialize(candidateDetail));
                Assert.AreEqual(priorProfile,await Snapshot("Avatar"));Assert.AreEqual(priorDetail,await Snapshot("AvatarDetail"));
            }
            Assert.AreEqual(histories,(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) RETURN count(v) AS count",new{id=id.ToString()})).SingleAsync())["count"].As<long>());
            complete=true;
        }
        finally
        {
            Console.WriteLine("Owned Neo identity lifecycle fixture: "+OasisJson.Serialize(new{id,marker,complete}));
            if(complete){await(await inspection.RunAsync("MATCH (n) WHERE n.Id=$id AND (n:Avatar OR n:AvatarDetail) DETACH DELETE n",new{id=id.ToString()})).ConsumeAsync();await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) WHERE v.Kind IN ['Avatar','AvatarDetail'] DETACH DELETE v",new{id=id.ToString()})).ConsumeAsync();}
            await provider.DeActivateProviderAsync();
        }
    }

    [DataTestMethod]
    [DataRow(false,false,"rollback")] [DataRow(true,false,"rollback")] [DataRow(false,true,"rollback")] [DataRow(true,true,"rollback")]
    [DataRow(false,false,"rollbackLost")] [DataRow(true,false,"rollbackLost")] [DataRow(false,true,"rollbackLost")] [DataRow(true,true,"rollbackLost")]
    [DataRow(false,false,"commitLost")] [DataRow(true,false,"commitLost")] [DataRow(false,true,"commitLost")] [DataRow(true,true,"commitLost")]
    [DataRow(false,false,"cleanup")] [DataRow(true,false,"cleanup")] [DataRow(false,true,"cleanup")] [DataRow(true,true,"cleanup")]
    public async Task NativeCosmicIdentitySaveOwnsCommitRollbackAndCleanup(bool synchronous,bool detail,string mode)
    {
        const string uri="bolt://127.0.0.1:17687";Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=true};Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var field=typeof(Provider).GetField("_driver",BindingFlags.Instance|BindingFlags.NonPublic)!;var healthy=(IDriver)field.GetValue(provider)!;
        var faults=new Neo4jOrdinaryWriteFaultProxy.Faults(mode);var id=Guid.NewGuid();var marker="owned-identity-outcome-"+id.ToString("N");var complete=false;
        IHolonBase source=detail?new AvatarDetail{Id=id,Username=marker,Email=marker+"@example.invalid"}:new Avatar{Id=id,Username=marker,Email=marker+"@example.invalid"};
        var before=OasisJson.Serialize(source);using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=driver.AsyncSession();
        try
        {
            field.SetValue(provider,Neo4jOrdinaryWriteFaultProxy.Wrap(healthy,faults));
            bool error,saved,warning;int count;Exception? cause;IHolonBase? acknowledged;
            if(detail){var result=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);error=result.IsError;saved=result.IsSaved;warning=result.IsWarning;count=result.SavedCount;cause=result.Exception;acknowledged=result.Result;}
            else {var result=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);error=result.IsError;saved=result.IsSaved;warning=result.IsWarning;count=result.SavedCount;cause=result.Exception;acknowledged=result.Result;}
            field.SetValue(provider,healthy);
            var rows=await(await inspection.RunAsync("MATCH (n {Id:$id}) WHERE n:Avatar OR n:AvatarDetail RETURN n",new{id=id.ToString()})).ToListAsync();
            if(mode=="cleanup")
            {Assert.IsFalse(error,cause?.ToString());Assert.IsTrue(saved);Assert.IsTrue(warning);Assert.AreEqual(1,count);Assert.AreEqual(1,rows.Count);Assert.AreEqual(id,acknowledged!.Id);Assert.IsFalse(source.IsNewHolon);}
            else
            {
                Assert.IsTrue(error);Assert.IsFalse(saved);Assert.AreEqual(0,count);Assert.IsNull(acknowledged);Assert.AreEqual(before,OasisJson.Serialize(source));
                Assert.AreEqual(mode!="rollback",NativeMutationReconciliation.IsRequired(cause));Assert.AreEqual(mode=="commitLost"?1:0,rows.Count);
            }
            Assert.AreEqual(mode is "commitLost" or "cleanup"?1:0,faults.Commits);Assert.AreEqual(mode is "rollback" or "rollbackLost"?1:0,faults.Rollbacks);
            if(rows.Count==1)
            {var packet=rows.Single()["n"].As<Neo4j.Driver.INode>();var stored=OasisJson.ParseStoredObject(packet.Properties["SnapshotJson"].As<string>());Assert.AreEqual(id.ToString(),stored.Value<string>("Id"));Assert.AreEqual(marker,stored.Value<string>("Username"));Assert.AreEqual(1,stored.Value<int>("Version"));}
            complete=true;
        }
        finally
        {
            field.SetValue(provider,healthy);Console.WriteLine("Owned Neo identity outcome fixture: "+OasisJson.Serialize(new{id,marker,mode,detail,complete}));
            if(complete){await(await inspection.RunAsync("MATCH (n {Id:$id}) WHERE n:Avatar OR n:AvatarDetail DETACH DELETE n",new{id=id.ToString()})).ConsumeAsync();await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) WHERE v.Kind IN ['Avatar','AvatarDetail'] DETACH DELETE v",new{id=id.ToString()})).ConsumeAsync();}
            await provider.DeActivateProviderAsync();
        }
    }
}
