using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests;

[TestClass]
public class AzureGraphPlanTests
{
    private sealed class ReadProbe(IHolon[] rows):AzureStorageOASIS("UseDevelopmentStorage=true")
    {
        public int Reads;
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {Reads++;return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=rows,IsLoaded=true,LoadedCount=rows.Length});}
    }

    [DataTestMethod,DataRow(false),DataRow(true)]
    public async Task ParentQueryCountsOnlySelectedChildren(bool synchronous)
    {
        var parent=Guid.NewGuid();var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent};
        var provider=new ReadProbe(new IHolon[]{child,new Holon{Id=Guid.NewGuid(),ParentHolonId=Guid.NewGuid()}});
        var result=synchronous?provider.LoadHolonsForParent(parent,loadChildren:false):await provider.LoadHolonsForParentAsync(parent,loadChildren:false);
        Assert.IsFalse(result.IsError);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
        CollectionAssert.AreEqual(new[]{child.Id},System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(result.Result,row=>row.Id)));
        Assert.AreEqual(1,provider.Reads);
    }

    private sealed class WriteProbe : AzureStorageOASIS
    {
        public int Writes;
        public WriteProbe():base("UseDevelopmentStorage=true") { }
        protected override Task<OASISResult<IHolon>> SavePlannedHolonAsync(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite write,Action validatePublication)
        {var holon=write.Source;var staged=write.Snapshot;validatePublication();Writes++;holon.Id=staged.Id;holon.ParentHolonId=staged.ParentHolonId;return Task.FromResult(new OASISResult<IHolon> {Result=holon,IsSaved=true});}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InvalidSelectedGraphDoesNotWriteOrPublishIdentities(bool synchronous)
    {
        foreach(var invalid in new[] {"cycle","duplicate-id","reference","parent","null","blob-key","depth"})
        {
            var provider=new WriteProbe();var root=new Holon();var child=new Holon();
            root.Children=new List<IHolon> {child};
            if(invalid=="cycle") child.Children=new List<IHolon> {root};
            if(invalid=="duplicate-id") {root.Id=Guid.NewGuid();child.Id=root.Id;}
            if(invalid=="reference") root.Children.Add(child);
            if(invalid=="parent") child.ParentHolonId=Guid.NewGuid();
            if(invalid=="null") root.Children.Add(null!);
            if(invalid=="blob-key") {root.CustomKey="owned-collision";child.CustomKey="owned-collision";}
            var rootId=root.Id;var childId=child.Id;var depth=invalid=="depth" ? -1:0;
            var result=synchronous ? provider.SaveHolons(new[] {root},maxChildDepth:depth):await provider.SaveHolonsAsync(new[] {root},maxChildDepth:depth);
            Assert.IsTrue(result.IsError,invalid);Assert.IsNotNull(result.Exception);Assert.AreEqual(0,provider.Writes,invalid);
            Assert.AreEqual(rootId,root.Id);Assert.AreEqual(childId,child.Id);
        }
    }
}
