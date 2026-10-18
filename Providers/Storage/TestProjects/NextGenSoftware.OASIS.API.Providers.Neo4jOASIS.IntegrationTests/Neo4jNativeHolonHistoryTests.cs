using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Enums;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OwnedNativeHolonHistorySelectsRootAndKeepsIndependentCurrentDescendants(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=raw.AsyncSession();
        Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (a) WHERE a:Holon OR a:OasisRecordVersion RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=true};
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var marker=Guid.NewGuid().ToString("N");
            var owner=new Avatar{Id=Guid.NewGuid(),Username=$"export-old-{marker}",Email=$"old-{marker}@example.invalid"};
            Assert.IsFalse((await provider.SaveAvatarAsync(owner)).IsError);
            var root=new Holon{Id=Guid.NewGuid(),Name=$"root-v1-{marker}"};
            root.CreatedByAvatarId=owner.Id;
            root.MetaData["history-marker"]=marker;
            var child=new Holon{Id=Guid.NewGuid(),Name=$"child-v1-{marker}"};
            var grandchild=new Holon{Id=Guid.NewGuid(),Name=$"grandchild-v1-{marker}"};
            root.Children.Add(child);child.Children.Add(grandchild);
            var created=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
            Assert.IsFalse(created.IsError,created.Message);
            Assert.AreEqual(1,root.Version);Assert.AreEqual(1,child.Version);Assert.AreEqual(1,grandchild.Version);
            var rootVersionId=root.VersionId;
            child.Name=$"child-v2-{marker}";
            Assert.IsFalse((await provider.SaveHolonAsync(child,saveChildren:false)).IsError);
            grandchild.Name=$"grandchild-v2-{marker}";
            Assert.IsFalse((await provider.SaveHolonAsync(grandchild,saveChildren:false)).IsError);
            root.Name=$"root-v2-{marker}";
            root.MetaData["history-marker"]="changed";
            Assert.IsFalse((await provider.SaveHolonAsync(root,saveChildren:false)).IsError);
            root.Name=$"root-v3-{marker}";
            Assert.IsFalse((await provider.SaveHolonAsync(root,saveChildren:false)).IsError);
            Assert.AreEqual(3,root.Version);Assert.AreEqual(2,child.Version);Assert.AreEqual(2,grandchild.Version);
            owner.Username=$"export-new-{marker}";owner.Email=$"new-{marker}@example.invalid";
            Assert.IsFalse((await provider.SaveAvatarAsync(owner)).IsError);
            var old=synchronous?provider.LoadHolon(root.Id,version:1):await provider.LoadHolonAsync(root.Id,version:1);
            Assert.IsFalse(old.IsError,old.Message);
            Assert.AreEqual($"root-v1-{marker}",old.Result.Name);Assert.AreEqual(rootVersionId,old.Result.VersionId);
            Assert.AreEqual(1,old.Result.Version);
            Assert.AreEqual(child.Name,old.Result.Children.Single().Name);Assert.AreEqual(2,old.Result.Children.Single().Version);
            Assert.AreEqual(grandchild.Name,old.Result.Children.Single().Children.Single().Name);
            Assert.AreEqual(2,old.Result.Children.Single().Children.Single().Version);
            var byKey=provider.LoadHolon(root.Id.ToString(),loadChildren:false,version:1);
            Assert.IsFalse(byKey.IsError,byKey.Message);Assert.AreEqual($"root-v1-{marker}",byKey.Result.Name);
            Assert.AreEqual(0,byKey.Result.Children.Count());
            Assert.AreEqual(3,(await provider.LoadHolonAsync(root.Id)).Result.Version);
            var all=synchronous?provider.LoadAllHolons(loadChildren:false,version:1):await provider.LoadAllHolonsAsync(loadChildren:false,version:1);
            Assert.IsFalse(all.IsError,all.Message);Assert.AreEqual(3,all.Result.Count());
            Assert.AreEqual($"root-v1-{marker}",all.Result.Single(h=>h.Id==root.Id).Name);
            var metadata=synchronous?provider.LoadHolonsByMetaData("history-marker",marker,version:1):await provider.LoadHolonsByMetaDataAsync("history-marker",marker,version:1);
            Assert.IsFalse(metadata.IsError,metadata.Message);Assert.AreEqual(root.Id,metadata.Result.Single().Id);
            Assert.AreEqual(2,metadata.Result.Single().Children.Single().Version);
            var children=synchronous?provider.LoadHolonsForParent(root.Id,version:1):await provider.LoadHolonsForParentAsync(root.Id,version:1);
            Assert.IsFalse(children.IsError,children.Message);Assert.AreEqual(1,children.Result.Single().Version);
            Assert.AreEqual(2,children.Result.Single().Children.Single().Version);
            var keyedChildren=await provider.LoadHolonsForParentAsync(root.Id.ToString(),version:2);
            Assert.IsFalse(keyedChildren.IsError,keyedChildren.Message);Assert.AreEqual(2,keyedChildren.Result.Single().Version);
            var absent=await provider.LoadAllHolonsAsync(version:99);
            Assert.IsFalse(absent.IsError,absent.Message);Assert.AreEqual(0,absent.Result.Count());
            var query=new SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string>{{"history-marker",marker}},
                SearchGroups=new List<ISearchGroupBase>{new SearchTextGroup{SearchHolons=true,HolonType=HolonType.All,SearchQuery=$"root-v1-{marker}"}}};
            var searched=synchronous?provider.Search(query,version:1):await provider.SearchAsync(query,version:1);
            Assert.IsFalse(searched.IsError,searched.Message);Assert.AreEqual(root.Id,searched.Result.SearchResultHolons.Single().Id);
            Assert.AreEqual(2,searched.Result.SearchResultHolons.Single().Children.Single().Version);
            var exported=synchronous?provider.ExportAll(1):await provider.ExportAllAsync(1);
            Assert.IsFalse(exported.IsError,exported.Message);Assert.AreEqual(3,exported.Result.Count());
            Assert.AreEqual(marker,exported.Result.Single(h=>h.Id==root.Id).MetaData["history-marker"]);
            var currentExport=await provider.ExportAllAsync();
            Assert.IsFalse(currentExport.IsError,currentExport.Message);Assert.AreEqual(3,currentExport.Result.Single(h=>h.Id==root.Id).Version);
            var owned=synchronous?provider.ExportAllDataForAvatarById(owner.Id,1):await provider.ExportAllDataForAvatarByIdAsync(owner.Id,1);
            Assert.IsFalse(owned.IsError,owned.Message);Assert.AreEqual(root.Id,owned.Result.Single().Id);Assert.AreEqual(1,owned.Result.Single().Version);
            var byUsername=synchronous?provider.ExportAllDataForAvatarByUsername($"export-old-{marker}",1):await provider.ExportAllDataForAvatarByUsernameAsync($"export-old-{marker}",1);
            Assert.IsFalse(byUsername.IsError,byUsername.Message);Assert.AreEqual(root.Id,byUsername.Result.Single().Id);
            var byEmail=synchronous?provider.ExportAllDataForAvatarByEmail($"old-{marker}@example.invalid",1):await provider.ExportAllDataForAvatarByEmailAsync($"old-{marker}@example.invalid",1);
            Assert.IsFalse(byEmail.IsError,byEmail.Message);Assert.AreEqual(root.Id,byEmail.Result.Single().Id);
            var absentOwner=await provider.ExportAllDataForAvatarByIdAsync(Guid.NewGuid(),1);
            Assert.IsFalse(absentOwner.IsError,absentOwner.Message);Assert.AreEqual(0,absentOwner.Result.Count());
            Assert.IsTrue((await provider.LoadHolonAsync(root.Id,version:99)).IsError);
            Assert.IsTrue((await provider.LoadHolonAsync(root.Id,version:-1)).IsError);
            Assert.AreEqual(2L,(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id}) RETURN count(v) AS count",new{id=root.Id.ToString()})).SingleAsync())["count"].As<long>());
            var original=(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id,Version:1}) RETURN v.SnapshotJson AS snapshot",new{id=root.Id.ToString()})).SingleAsync())["snapshot"].As<string>();
            await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id,Version:1}) SET v.SnapshotJson='null'",new{id=root.Id.ToString()})).ConsumeAsync();
            try
            {
                var invalid=await provider.LoadHolonAsync(root.Id,version:1);
                Assert.IsTrue(invalid.IsError);Assert.IsNull(invalid.Result);
                Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,invalid.ErrorCode);
                var invalidExport=await provider.ExportAllAsync(1);
                Assert.IsTrue(invalidExport.IsError);Assert.IsNull(invalidExport.Result);
                var invalidOwned=await provider.ExportAllDataForAvatarByIdAsync(owner.Id,1);
                Assert.IsTrue(invalidOwned.IsError);Assert.IsNull(invalidOwned.Result);
                Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,invalidOwned.ErrorCode);
            }
            finally
            {
                await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id,Version:1}) SET v.SnapshotJson=$snapshot",new{id=root.Id.ToString(),snapshot=original})).ConsumeAsync();
            }
            Assert.IsFalse((await provider.DeleteHolonAsync(root.Id)).IsError);
            Assert.IsTrue((await provider.LoadHolonAsync(root.Id,version:1)).IsError);
            Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id}) RETURN count(v) AS count",new{id=root.Id.ToString()})).SingleAsync())["count"].As<long>());
            Assert.IsFalse((await provider.DeleteHolonAsync(child.Id)).IsError);
            Assert.IsFalse((await provider.DeleteHolonAsync(grandchild.Id)).IsError);
            Assert.IsFalse((await provider.DeleteAvatarAsync(owner.Id,false)).IsError);
            Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (a) WHERE a:Holon OR a:OasisRecordVersion RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
