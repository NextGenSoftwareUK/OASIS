using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class EthereumParentDepthControlledTests
{
    private sealed class Records:EthereumOASIS
    {
        public readonly Holon Parent=new(){Id=Guid.NewGuid(),MetaData=new(){["CustomKey"]="owned-parent"}};
        private readonly Holon child;
        public int Reads;
        public Records():base("http://127.0.0.1:18545","0x0000000000000000000000000000000000000000000000000000000000000001",31337,"0x0000000000000000000000000000000000000000")
        {child=new Holon{Id=Guid.NewGuid(),ParentHolonId=Parent.Id};child.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id});Parent.Children.Add(child);}
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {Reads++;return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=new IHolon[]{Parent,child},IsLoaded=true});}
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task ParentSelectionCountsOneEdgeWithoutReadingAnotherSnapshot(bool custom,bool synchronous,bool recursive)
    {
        var provider=new Records();
        var result=custom
            ?synchronous?provider.LoadHolonsForParentByCustomKey("owned-parent",recursive:recursive,maxChildDepth:1,loadChildrenFromProvider:true):await provider.LoadHolonsForParentByCustomKeyAsync("owned-parent",recursive:recursive,maxChildDepth:1,loadChildrenFromProvider:true)
            :synchronous?provider.LoadHolonsForParent(provider.Parent.Id,recursive:recursive,maxChildDepth:1,loadChildrenFromProvider:true):await provider.LoadHolonsForParentAsync(provider.Parent.Id,recursive:recursive,maxChildDepth:1,loadChildrenFromProvider:true);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(0,result.Result.Single().Children.Count);Assert.AreEqual(1,provider.Reads);
    }
}
