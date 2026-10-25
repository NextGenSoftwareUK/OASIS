using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleSearchVisibilityTests
{
    private sealed class Probe : OracleDBOASIS
    {
        internal readonly Guid Actor=Guid.NewGuid();
        internal readonly Holon Root,Owned,PublicForeign,Hidden,HiddenLeaf,Historical;
        internal readonly List<Holon> Current;
        internal int Reads;
        internal Probe():base("UnsupportedOracleSearchVisibilityTestOption=true")
        {
            Root=new(){Id=Guid.NewGuid(),CreatedByAvatarId=Actor,Name="owned-search-root",Version=2,MetaData=new(){{"scope","owned-root"}}};
            Owned=new(){Id=Guid.NewGuid(),ParentHolonId=Root.Id,CreatedByAvatarId=Actor,Name="owned-child",Version=7};
            PublicForeign=new(){Id=Guid.NewGuid(),ParentHolonId=Root.Id,CreatedByAvatarId=Guid.NewGuid(),IsPublic=true,Name="foreign-public",Version=8};
            Hidden=new(){Id=Guid.NewGuid(),ParentHolonId=Root.Id,CreatedByAvatarId=Guid.NewGuid(),Name="foreign-private",Version=9};
            HiddenLeaf=new(){Id=Guid.NewGuid(),ParentHolonId=Hidden.Id,CreatedByAvatarId=Actor,Name="owned-under-hidden",Version=10};
            Root.Children.Add(Owned);Root.Children.Add(PublicForeign);Root.Children.Add(Hidden);Hidden.Children.Add(HiddenLeaf);
            Root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(Root).CreateManifest(Root.Id,row=>row.Id));
            Historical=StoredRecordJson.Project<Holon>(Root);Historical.Version=1;
            Current=new(){Root,Owned,PublicForeign,Hidden,HiddenLeaf};
        }
        protected override Task<List<T>> ReadRecordCollectionAsync<T>(string table,int version,bool exportAllVersions=false)
        {
            Reads++;
            if(typeof(T)==typeof(Holon))return Task.FromResult((version==0?Current:new(){Historical}).Cast<T>().ToList());
            if(typeof(T)==typeof(Avatar))return Task.FromResult(new List<T>());
            throw new InvalidOperationException("Unexpected collection; no SDK path is permitted in this control.");
        }
    }

    [DataTestMethod]
    [DataRow(false,0,false)][DataRow(false,0,true)][DataRow(false,1,false)][DataRow(false,1,true)]
    [DataRow(true,0,false)][DataRow(true,0,true)][DataRow(true,1,false)][DataRow(true,1,true)]
    public async Task CurrentAvatarSearchMustFilterWholeDetachedGraphWithoutChangingStoreObservation(bool synchronous,int version,bool loadChildren)
    {
        var provider=new Probe();var originals=provider.Current.Select(StoredRecordJson.Serialize).ToArray();
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=provider.Actor,FilterByMetaData=new(){{"scope","owned-root"}}};
        var result=synchronous?provider.Search(query,loadChildren:loadChildren,version:version):await provider.SearchAsync(query,loadChildren:loadChildren,version:version);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
        var root=(Holon)result.Result.SearchResultHolons.Single();Assert.AreEqual(provider.Root.Id,root.Id);Assert.AreEqual(version==0?2:1,root.Version);
        CollectionAssert.AreEqual(loadChildren?new[]{provider.Owned.Id}:Array.Empty<Guid>(),root.Children.Select(row=>row.Id).ToArray(),"Current-avatar scope must exclude foreign public/private branches, not hoist owned hidden grandchildren.");
        if(loadChildren){
            Assert.AreEqual(7,root.Children.Single().Version);Assert.AreEqual(provider.Root.ChildRoleManifestJson,root.ChildRoleManifestJson);
            root.AcceptChanges();
            var rejected=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
            Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.IsSaved);Assert.IsInstanceOfType<InvalidOperationException>(rejected.Exception,"Scoped search cannot become authority to replace hidden membership.");
        }
        Assert.AreEqual(loadChildren?3:2,provider.Reads,"Output visibility must not add SDK collection reads.");
        CollectionAssert.AreEqual(originals,provider.Current.Select(StoredRecordJson.Serialize).ToArray());
        Assert.AreEqual(3,provider.Root.Children.Count);Assert.AreEqual(1,provider.Hidden.Children.Count);
    }
}
