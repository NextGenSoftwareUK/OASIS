using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    public static IEnumerable<object[]> DeleteOwnershipCases()
    {
        foreach(var kind in new[]{"soft","hard","holon"})foreach(var sync in new[]{false,true})foreach(var mode in new[]{"normal","rollback","rollbackLost","commitLost","cleanup"})yield return new object[]{kind,sync,mode};
        foreach(var kind in new[]{"soft","hard"})foreach(var sync in new[]{false,true})yield return new object[]{kind,sync,"pairRejected"};
    }
    [DataTestMethod,DynamicData(nameof(DeleteOwnershipCases),DynamicDataSourceType.Method)]
    public async Task NativeCosmicDeleteOwnershipPreservesOriginalOutcomes(string kind,bool synchronous,string mode)
    {
        const string uri="bolt://127.0.0.1:17687";Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=true};Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var id=Guid.NewGuid();var marker="owned-delete-outcome-"+id.ToString("N");var complete=false;
        var field=typeof(Provider).GetField("_driver",BindingFlags.NonPublic|BindingFlags.Instance)!;var healthy=(IDriver)field.GetValue(provider)!;
        var faults=new Neo4jOrdinaryWriteFaultProxy.Faults(mode);using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=raw.AsyncSession();
        async Task<List<Neo4j.Driver.INode>> Nodes()=> (await(await inspection.RunAsync("MATCH (n {Id:$id}) WHERE n:Avatar OR n:AvatarDetail OR n:Holon RETURN n",new{id=id.ToString()})).ToListAsync()).Select(row=>row["n"].As<Neo4j.Driver.INode>()).ToList();
        async Task<long> History()=> (await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) RETURN count(v) AS count",new{id=id.ToString()})).SingleAsync())["count"].As<long>();
        try
        {
            if(kind=="holon")Assert.IsFalse((await provider.SaveHolonAsync(new Holon{Id=id,Name=marker},saveChildren:false)).IsError);
            else {Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar{Id=id,Username=marker,Email=marker+"@example.invalid"})).IsError);Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=marker,Email=marker+"@example.invalid"})).IsError);}
            var original=await Nodes();Assert.AreEqual(kind=="holon"?1:2,original.Count);var snapshots=original.ToDictionary(node=>node.Labels.Single(),node=>node.Properties["SnapshotJson"].As<string>());
            if(mode!="normal")field.SetValue(provider,Neo4jOrdinaryWriteFaultProxy.Wrap(healthy,faults));
            bool error,saved,deleted,warning,changed,acknowledged;int count;Exception? cause;string code;
            if(kind=="holon")
            {var r=synchronous?provider.DeleteHolon(id):await provider.DeleteHolonAsync(id);error=r.IsError;saved=r.IsSaved;deleted=r.IsDeleted;warning=r.IsWarning;changed=r.HasAnyHolonsChanged;acknowledged=r.Result!=null;count=r.DeletedCount;cause=r.Exception;code=r.ErrorCode;}
            else
            {var r=synchronous?provider.DeleteAvatar(id,kind=="soft"):await provider.DeleteAvatarAsync(id,kind=="soft");error=r.IsError;saved=r.IsSaved;deleted=r.IsDeleted;warning=r.IsWarning;changed=r.HasAnyHolonsChanged;acknowledged=r.Result;count=r.DeletedCount;cause=r.Exception;code=r.ErrorCode;}
            field.SetValue(provider,healthy);var current=await Nodes();var committed=mode is "normal" or "commitLost" or "cleanup";
            if(committed&&kind!="soft"){Assert.AreEqual(0,current.Count);Assert.AreEqual(0L,await History());}
            else if(committed)
            {
                Assert.AreEqual(2,current.Count);
                var profile=OasisJson.DeserializeStored<Avatar>(current.Single(node=>node.Labels.Single()=="Avatar").Properties["SnapshotJson"].As<string>());
                var detail=OasisJson.DeserializeStored<AvatarDetail>(current.Single(node=>node.Labels.Single()=="AvatarDetail").Properties["SnapshotJson"].As<string>());
                Assert.IsTrue(profile.IsDeleted);Assert.IsTrue(detail.IsDeleted);Assert.IsFalse(profile.IsActive);Assert.IsFalse(detail.IsActive);
                Assert.AreEqual(profile.DeletedDate,detail.DeletedDate);Assert.AreEqual(profile.DeletedByAvatarId,detail.DeletedByAvatarId);Assert.AreEqual(2,profile.Version);Assert.AreEqual(2,detail.Version);
            }
            else {Assert.AreEqual(original.Count,current.Count);foreach(var node in current)Assert.AreEqual(snapshots[node.Labels.Single()],node.Properties["SnapshotJson"].As<string>());Assert.AreEqual(0L,await History());}
            if(mode is "normal" or "cleanup")
            {Assert.IsFalse(error,cause?.ToString());Assert.IsTrue(saved);Assert.IsTrue(deleted);Assert.IsTrue(changed);Assert.IsTrue(acknowledged);Assert.AreEqual(1,count);Assert.AreEqual(mode=="cleanup",warning);}
            else
            {
                Assert.IsTrue(error);Assert.IsFalse(saved);Assert.IsFalse(deleted);Assert.IsFalse(changed);Assert.IsFalse(acknowledged);Assert.AreEqual(0,count);
                Assert.AreEqual(mode is "commitLost" or "rollbackLost",NativeMutationReconciliation.IsRequired(cause));
                if(mode is "commitLost" or "rollbackLost")Assert.AreEqual("COSMIC_WRITE_RECONCILIATION_REQUIRED",code);
            }
            if(mode!="normal"){Assert.AreEqual(mode is "commitLost" or "cleanup"?1:0,faults.Commits);Assert.AreEqual(mode is "rollback" or "rollbackLost" or "pairRejected"?1:0,faults.Rollbacks);}
            if(kind=="soft"&&mode=="normal")
            {
                var before=current.ToDictionary(node=>node.Labels.Single(),node=>node.Properties["SnapshotJson"].As<string>());var histories=await History();
                var repeatFault=new Neo4jOrdinaryWriteFaultProxy.Faults("commitLost");field.SetValue(provider,Neo4jOrdinaryWriteFaultProxy.Wrap(healthy,repeatFault));
                var repeated=synchronous?provider.DeleteAvatar(id):await provider.DeleteAvatarAsync(id);field.SetValue(provider,healthy);
                Assert.AreEqual(0,repeatFault.Commits);Assert.AreEqual(1,repeatFault.Rollbacks);
                Assert.IsFalse(repeated.IsError,repeated.Message);Assert.IsTrue(repeated.Result);Assert.IsTrue(repeated.IsDeleted);Assert.IsFalse(repeated.IsSaved);Assert.IsFalse(repeated.HasAnyHolonsChanged);Assert.AreEqual(0,repeated.DeletedCount);
                foreach(var node in await Nodes())Assert.AreEqual(before[node.Labels.Single()],node.Properties["SnapshotJson"].As<string>());Assert.AreEqual(histories,await History());
            }
            complete=true;
        }
        finally
        {
            field.SetValue(provider,healthy);Console.WriteLine("Owned Neo delete fixture: "+OasisJson.Serialize(new{id,marker,kind,mode,complete}));
            if(complete){await(await inspection.RunAsync("MATCH (n {Id:$id}) WHERE n:Avatar OR n:AvatarDetail OR n:Holon DETACH DELETE n",new{id=id.ToString()})).ConsumeAsync();await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) WHERE v.Kind IN ['Avatar','AvatarDetail','Holon'] DETACH DELETE v",new{id=id.ToString()})).ConsumeAsync();}
            await provider.DeActivateProviderAsync();
        }
    }
}
