using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeVisibilityDefaultsPreserveOwnerPublicAndScopedRootCounts(bool sync)
    {
        var storage=(IOASISStorageProvider)_provider;var owner=Guid.NewGuid();var parent=Guid.NewGuid();var marker=Guid.NewGuid().ToString("D");
        var own=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,ParentHolonId=parent};
        var shared=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),ParentHolonId=parent,IsPublic=true};
        var hidden=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),ParentHolonId=parent};
        var unrelated=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,ParentHolonId=Guid.NewGuid()};
        foreach(var row in new[]{own,shared,hidden,unrelated}){
            row.MetaData=new(){{"visibilityFixture",marker}};
            var saved=await storage.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
        }
        var pairs=new Dictionary<string,string>{{"visibilityFixture",marker}};
        var all=await storage.LoadAllHolonsAsync(loadChildren:false);
        var children=await storage.LoadHolonsForParentAsync(parent,loadChildren:false);
        var metadata=await storage.LoadHolonsByMetaDataAsync("visibilityFixture",marker,loadChildren:false);
        foreach(var source in new[]{all,children,metadata})Assert.IsFalse(source.IsError,source.Exception?.ToString());
        void Verify(OASISResult<IEnumerable<IHolon>> result,bool includePublic,int nativeCount,bool parentOnly=false,bool allowOtherPublic=false){
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);
            Assert.IsTrue(result.Result.All(row=>row.CreatedByAvatarId==owner||includePublic&&row.IsPublic));
            var selected=result.Result.Where(row=>row.MetaData!=null&&row.MetaData.TryGetValue("visibilityFixture",out var value)&&Convert.ToString(value)==marker).Select(row=>row.Id).ToArray();
            var expected=new List<Guid>{own.Id};if(!parentOnly)expected.Add(unrelated.Id);if(includePublic)expected.Add(shared.Id);
            CollectionAssert.AreEquivalent(expected,selected);
            if(!allowOtherPublic)Assert.AreEqual(selected.Length,result.Result.Count(),"Parent/metadata scopes cannot return other allowed-but-unrequested public records.");
            // LoadedCount counts returned roots. Native scan/capacity observations
            // remain a separate diagnostic; they are not visible-result cardinality.
            Assert.AreEqual(result.Result.Count(),result.LoadedCount);
            Assert.IsTrue(nativeCount>=result.LoadedCount);
        }
        foreach(var includePublic in new[]{false,true}){
            Verify(sync?storage.LoadAllHolons(owner,includePublic,loadChildren:false):await storage.LoadAllHolonsAsync(owner,includePublic,loadChildren:false),includePublic,all.LoadedCount,allowOtherPublic:true);
            Verify(sync?storage.LoadHolonsForParent(parent,owner,includePublic,loadChildren:false):await storage.LoadHolonsForParentAsync(parent,owner,includePublic,loadChildren:false),includePublic,children.LoadedCount,parentOnly:true);
            Verify(sync?storage.LoadHolonsByMetaData("visibilityFixture",marker,owner,includePublic,loadChildren:false):await storage.LoadHolonsByMetaDataAsync("visibilityFixture",marker,owner,includePublic,loadChildren:false),includePublic,metadata.LoadedCount);
            Verify(sync?storage.LoadHolonsByMetaData(pairs,MetaKeyValuePairMatchMode.All,owner,includePublic,loadChildren:false):await storage.LoadHolonsByMetaDataAsync(pairs,MetaKeyValuePairMatchMode.All,owner,includePublic,loadChildren:false),includePublic,metadata.LoadedCount);
        }
        // Full-suite exclusive baseline/intention and conditional cleanup own these rows.
    }
}
