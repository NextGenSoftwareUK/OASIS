using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GoogleParentDepthTests
{
    private sealed class Records:GoogleCloudOASIS
    {
        public readonly Guid Parent=Guid.NewGuid();
        private readonly IHolon[] rows;
        public Records():base(enableStorage:false,enableFirestore:false,enableBigQuery:false)
        {
            var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=Parent};
            rows=new IHolon[]{child,new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id}};
        }
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
            =>Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=rows,IsLoaded=true});
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ParentDepthCountsTheSelectedEdge(bool synchronous)
    {
        var provider=new Records();
        var result=synchronous?provider.LoadHolonsForParent(provider.Parent.ToString(),maxChildDepth:1,loadChildrenFromProvider:true)
            :await provider.LoadHolonsForParentAsync(provider.Parent,maxChildDepth:1,loadChildrenFromProvider:true);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(0,result.Result.Single().Children.Count);
    }
}
