using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class IndependentChildVersionTests
{
    private static T Value<T>(OASISResult<T> result){Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);Assert.IsNotNull(result.Result);return result.Result;}

    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(3,false)][DataRow(4,false)][DataRow(5,false)][DataRow(6,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(3,true)][DataRow(4,true)][DataRow(5,true)][DataRow(6,true)]
    public async Task HistoricalRootsRetainCurrentIndependentDescendants(int route,bool synchronous)
    {
        var provider=new TestProvider(new InMemoryBackend()){IsVersionControlEnabled=true};
        var parent=Guid.NewGuid();var root=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent,Name="historical-root",MetaData=new Dictionary<string,object>{{"kind","historical"}}};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="child"};
        var leaf=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id,Name="leaf"};
        foreach(var record in new[]{root,child,leaf})Value(await provider.SaveHolonAsync(record,saveChildren:false));
        root.Name="current-root";root.ParentHolonId=Guid.NewGuid();root.MetaData["kind"]="current";Value(await provider.SaveHolonAsync(root,saveChildren:false));
        for(var version=2;version<=3;version++){child.Name="child-"+version;Value(await provider.SaveHolonAsync(child,saveChildren:false));}
        for(var version=2;version<=4;version++){leaf.Name="leaf-"+version;Value(await provider.SaveHolonAsync(leaf,saveChildren:false));}
        IHolon selected;
        if(route==0)selected=Value(synchronous?provider.LoadHolon(root.Id,version:1):await provider.LoadHolonAsync(root.Id,version:1));
        else if(route==1)selected=Value(synchronous?provider.LoadHolon(root.Id.ToString(),version:1):await provider.LoadHolonAsync(root.Id.ToString(),version:1));
        else if(route==2)selected=Value(synchronous?provider.LoadAllHolons(version:1):await provider.LoadAllHolonsAsync(version:1)).Single(row=>row.Id==root.Id);
        else if(route==3)selected=Value(synchronous?provider.LoadHolonsForParent(parent,version:1):await provider.LoadHolonsForParentAsync(parent,version:1)).Single();
        else if(route==4)selected=Value(synchronous?provider.LoadHolonsForParent(parent.ToString(),version:1):await provider.LoadHolonsForParentAsync(parent.ToString(),version:1)).Single();
        else if(route==5)selected=Value(synchronous?provider.LoadHolonsByMetaData("kind","historical",version:1):await provider.LoadHolonsByMetaDataAsync("kind","historical",version:1)).Single();
        else{
            var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase>{new SearchTextGroup{SearchHolons=true,SearchQuery="historical-root"}}};
            selected=Value(synchronous?provider.Search(query,version:1):await provider.SearchAsync(query,version:1)).SearchResultHolons.Single();
        }
        Assert.AreEqual(1,selected.Version);Assert.AreEqual("historical-root",selected.Name);Assert.AreEqual(parent,selected.ParentHolonId);
        var selectedChild=selected.Children.Single();Assert.AreEqual(child.Id,selectedChild.Id);Assert.AreEqual(3,selectedChild.Version);Assert.AreEqual("child-3",selectedChild.Name);
        var selectedLeaf=selectedChild.Children.Single();Assert.AreEqual(leaf.Id,selectedLeaf.Id);Assert.AreEqual(4,selectedLeaf.Version);Assert.AreEqual("leaf-4",selectedLeaf.Name);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task HistoricalRootDoesNotResurrectCurrentlyDeletedChild(bool synchronous)
    {
        var provider=new TestProvider(new InMemoryBackend()){IsVersionControlEnabled=true};
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};
        Value(await provider.SaveHolonAsync(root,saveChildren:false));Value(await provider.SaveHolonAsync(child,saveChildren:false));
        child.DeletedDate=DateTime.UtcNow;Value(await provider.SaveHolonAsync(child,saveChildren:false));
        var selected=Value(synchronous?provider.LoadHolon(root.Id,version:1):await provider.LoadHolonAsync(root.Id,version:1));
        Assert.AreEqual(1,selected.Version);Assert.AreEqual(0,selected.Children.Count());
    }
}
