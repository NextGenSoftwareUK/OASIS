using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQueryGraphReadTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ScopedInterfaceProjectsActualProviderGraphWithoutPrivateChildren(bool sync,bool includePublic)
    {
        var actor=Guid.NewGuid();var root=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,HolonType=HolonType.Quest};
        var owned=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=actor};
        var visible=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=Guid.NewGuid(),IsPublic=true};
        var hidden=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=Guid.NewGuid()};
        var rows=new BigQueryCollectionTests.Rows{Selected=new[]{root,owned,visible,hidden}.Select(StoredRecordJson.Serialize).ToArray()};
        IOASISStorageProvider provider=BigQueryCollectionTests.Create(rows);
        var result=sync?provider.LoadAllHolons(actor,includePublic,HolonType.Quest,loadChildrenFromProvider:true)
            :await provider.LoadAllHolonsAsync(actor,includePublic,HolonType.Quest,loadChildrenFromProvider:true);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
        CollectionAssert.AreEqual(includePublic?new[]{owned.Id,visible.Id}:new[]{owned.Id},result.Result.Single().Children.Select(child=>child.Id).ToArray());
        Assert.IsNull(rows.Stored,"Visibility projection must not write storage.");
    }

    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task NegativeGraphDepthRejectsBeforeNativeRead(bool sync,int route)
    {
        var called=false;var provider=BigQueryCollectionTests.Create(new(){Selected=Array.Empty<string>(),BeforeRead=()=>called=true});var id=Guid.NewGuid();bool error;Exception? cause;
        if(route==0){var r=sync?provider.LoadHolon(id,maxChildDepth:-1):await provider.LoadHolonAsync(id,maxChildDepth:-1);error=r.IsError;cause=r.Exception;}
        else{var r=route==1?(sync?provider.LoadHolonsForParent(id,curentChildDepth:-1):await provider.LoadHolonsForParentAsync(id,curentChildDepth:-1)):(sync?provider.LoadAllHolons(maxChildDepth:-1):await provider.LoadAllHolonsAsync(maxChildDepth:-1));error=r.IsError;cause=r.Exception;}
        Assert.IsTrue(error);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(cause);Assert.IsFalse(called);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ChildSourceFailureRetainsCauseAndNoPartialRoot(bool sync,bool keepGoing)
    {
        var root=new Holon{Id=Guid.NewGuid()};var calls=0;var cause=new IOException("Controlled child source failure.");
        var provider=BigQueryCollectionTests.Create(new(){PointSelected=new[]{StoredRecordJson.Serialize(root)},Selected=Array.Empty<string>(),BeforeRead=()=>{if(++calls==2)throw cause;}});
        var result=sync?provider.LoadHolon(root.Id,continueOnError:keepGoing,loadChildrenFromProvider:true):await provider.LoadHolonAsync(root.Id,continueOnError:keepGoing,loadChildrenFromProvider:true);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.AreSame(cause,result.Exception);Assert.AreEqual(2,calls);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task InitialDepthAndParentRootEdgeShareSameBudget(bool sync,bool parent)
    {
        var root=new Holon{Id=Guid.NewGuid(),HolonType=HolonType.Quest};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};var grand=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id};
        var provider=BigQueryCollectionTests.Create(new(){Selected=new[]{root,child,grand}.Select(StoredRecordJson.Serialize).ToArray(),ParentSelected=new[]{StoredRecordJson.Serialize(child)}});
        var result=parent?(sync?provider.LoadHolonsForParent(root.Id,maxChildDepth:2,curentChildDepth:1,loadChildrenFromProvider:true):await provider.LoadHolonsForParentAsync(root.Id,maxChildDepth:2,curentChildDepth:1,loadChildrenFromProvider:true)):(sync?provider.LoadAllHolons(HolonType.Quest,maxChildDepth:2,curentChildDepth:1,loadChildrenFromProvider:true):await provider.LoadAllHolonsAsync(HolonType.Quest,maxChildDepth:2,curentChildDepth:1,loadChildrenFromProvider:true));
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,result.LoadedCount);var selected=result.Result.Single();Assert.AreEqual(parent?0:1,selected.Children.Count);if(!parent)Assert.AreEqual(0,selected.Children.Single().Children.Count);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task DisabledProviderChildrenDoesNotFetchChildPool(bool sync,bool children)
    {
        var root=new Holon{Id=Guid.NewGuid()};var calls=0;var provider=BigQueryCollectionTests.Create(new(){PointSelected=new[]{StoredRecordJson.Serialize(root)},BeforeRead=()=>{if(++calls>1)throw new InvalidOperationException("Unrequested child pool read.");}});
        var result=sync?provider.LoadHolon(root.Id,loadChildren:children,loadChildrenFromProvider:false):await provider.LoadHolonAsync(root.Id,loadChildren:children,loadChildrenFromProvider:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,calls);Assert.AreEqual(0,result.Result.Children.Count);
    }

    public static IEnumerable<object[]> GraphCases(){foreach(var route in new[]{0,1,2})foreach(var sync in new[]{false,true})foreach(var recursive in new[]{false,true})foreach(var children in new[]{false,true})foreach(var depth in new[]{0,1,2})yield return new object[]{route,sync,recursive,children,depth};}
    [DataTestMethod][DynamicData(nameof(GraphCases),DynamicDataSourceType.Method)]
    public async Task SharedGraphProjectionHonorsDepthRecursionAndRootCounts(int route,bool sync,bool recursive,bool children,int depth)
    {
        var root=new Holon{Id=Guid.NewGuid(),HolonType=HolonType.Quest,Version=2};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,HolonType=HolonType.Park,Version=8};
        var grand=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id,HolonType=HolonType.Park,Version=1};
        var deleted=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,DeletedDate=DateTime.UtcNow};
        var client=new BigQueryCollectionTests.Rows{Selected=new[]{root,child,grand,deleted}.Select(StoredRecordJson.Serialize).ToArray(),PointSelected=new[]{StoredRecordJson.Serialize(root)},ParentSelected=new[]{StoredRecordJson.Serialize(child),StoredRecordJson.Serialize(deleted)}};
        var original=client.Selected.ToArray();var provider=BigQueryCollectionTests.Create(client);IHolon selected;bool error,loaded;int count;Exception? cause;
        if(route==0){var r=sync?provider.LoadHolon(root.Id,children,recursive,depth,loadChildrenFromProvider:true):await provider.LoadHolonAsync(root.Id,children,recursive,depth,loadChildrenFromProvider:true);selected=r.Result;error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;cause=r.Exception;}
        else{var r=route==1?(sync?provider.LoadHolonsForParent(root.Id,loadChildren:children,recursive:recursive,maxChildDepth:depth,loadChildrenFromProvider:true):await provider.LoadHolonsForParentAsync(root.Id,loadChildren:children,recursive:recursive,maxChildDepth:depth,loadChildrenFromProvider:true)):(sync?provider.LoadAllHolons(HolonType.Quest,children,recursive,depth,loadChildrenFromProvider:true):await provider.LoadAllHolonsAsync(HolonType.Quest,children,recursive,depth,loadChildrenFromProvider:true));error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;cause=r.Exception;selected=r.Result.Single();}
        Assert.IsFalse(error,cause?.ToString());Assert.IsTrue(loaded);Assert.AreEqual(1,count);Assert.AreEqual(route==1?child.Id:root.Id,selected.Id);
        var start=route==1?1:0;var hasChild=children&&(route!=1||recursive)&&(depth==0||start<depth);
        Assert.AreEqual(hasChild?1:0,selected.Children.Count);
        if(hasChild){var actual=selected.Children.Single();Assert.AreEqual(route==1?grand.Id:child.Id,actual.Id);Assert.AreEqual(route==1?1:8,actual.Version);var hasGrand=route!=1&&recursive&&(depth==0||depth>1);Assert.AreEqual(hasGrand?1:0,actual.Children.Count);if(hasGrand)Assert.AreEqual(grand.Id,actual.Children.Single().Id);}
        CollectionAssert.AreEqual(original,client.Selected);Assert.IsNull(client.Stored,"Graph reads must not write synthetic storage.");
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task CyclicProviderGraphFailsWithoutPartialPayload(bool sync,bool keepGoing)
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.ParentHolonId=child.Id;
        var provider=BigQueryCollectionTests.Create(new(){Selected=new[]{root,child}.Select(StoredRecordJson.Serialize).ToArray(),PointSelected=new[]{StoredRecordJson.Serialize(root)}});
        var result=sync?provider.LoadHolon(root.Id,continueOnError:keepGoing,loadChildrenFromProvider:true):await provider.LoadHolonAsync(root.Id,continueOnError:keepGoing,loadChildrenFromProvider:true);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
    }
}
