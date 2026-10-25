using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GoogleScopedGraphVisibilityTests
{
    private sealed class Records:GoogleCloudOASIS
    {
        public readonly Guid Owner=Guid.NewGuid();
        public readonly Holon Root,Allowed,Foreign,Buried;
        public Records():base(enableStorage:false,enableFirestore:false,enableBigQuery:false)
        {
            Root=new Holon{Id=Guid.NewGuid(),Name="visibility-root",CreatedByAvatarId=Owner,Version=2,IsActive=true};
            Allowed=new Holon{Id=Guid.NewGuid(),Name="allowed",ParentHolonId=Root.Id,CreatedByAvatarId=Owner,Version=7,IsActive=true};
            Foreign=new Holon{Id=Guid.NewGuid(),Name="foreign",ParentHolonId=Root.Id,CreatedByAvatarId=Guid.NewGuid(),Version=3,IsActive=true};
            Buried=new Holon{Id=Guid.NewGuid(),Name="owned behind foreign",ParentHolonId=Foreign.Id,CreatedByAvatarId=Owner,Version=9,IsActive=true};
            Foreign.Children.Add(Buried);Root.Children.Add(Allowed);Root.Children.Add(Foreign);
        }
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {
            IEnumerable<IHolon> rows=version==1?new[]{new Holon{Id=Root.Id,Name=Root.Name,Version=1,IsActive=true,CreatedByAvatarId=Owner,Children=loadChildren?Root.Children.ToList():new List<IHolon>()}}
                :new IHolon[]{Root,Allowed,Foreign,Buried};
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=rows,IsLoaded=true});
        }
    }

    [DataTestMethod]
    [DataRow(false,false,0)][DataRow(true,false,0)][DataRow(false,true,0)][DataRow(true,true,0)]
    [DataRow(false,false,1)][DataRow(true,false,1)][DataRow(false,true,1)][DataRow(true,true,1)]
    public async Task ScopedSearchAndExportRemoveForeignDescendantsWithoutMutatingUnscopedGraphs(bool sync,bool export,int version)
    {
        var provider=new Records();IHolon selected;
        if(export){
            var result=sync?provider.ExportAllDataForAvatarById(provider.Owner,version):await provider.ExportAllDataForAvatarByIdAsync(provider.Owner,version);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());selected=result.Result.Single(row=>row.Id==provider.Root.Id);
        }else{
            var query=new SearchParams{AvatarId=provider.Owner,SearchOnlyForCurrentAvatar=true,SearchGroups=new(){new SearchTextGroup{SearchHolons=true,SearchQuery="visibility-root",HolonSearchParams=new SearchHolonParams{Name=true}}}};
            var result=sync?provider.Search(query,version:version):await provider.SearchAsync(query,version:version);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());selected=result.Result.SearchResultHolons.Single();
        }
        Assert.AreEqual(version==1?1:2,selected.Version);
        CollectionAssert.AreEquivalent(new[]{provider.Allowed.Id},selected.Children.Select(row=>row.Id).ToArray());
        Assert.AreEqual(7,selected.Children.Single().Version,"Descendants retain independent current versions.");
        CollectionAssert.AreEquivalent(new[]{provider.Allowed.Id,provider.Foreign.Id},provider.Root.Children.Select(row=>row.Id).ToArray());
        Assert.AreEqual(provider.Buried.Id,provider.Foreign.Children.Single().Id);
        var unscoped=await provider.LoadAllHolonsAsync();Assert.AreEqual(2,unscoped.Result.Single(row=>row.Id==provider.Root.Id).Children.Count());
    }
}
