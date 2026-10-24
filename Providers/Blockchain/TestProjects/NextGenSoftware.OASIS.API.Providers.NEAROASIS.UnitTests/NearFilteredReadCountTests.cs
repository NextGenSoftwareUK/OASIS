using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearFilteredReadCountTests
{
    private sealed class Records(bool malformed):NEAROASIS
    {
        private Dictionary<string,object> Coordinates()=>new(){{"Latitude",0d},{"Longitude",malformed?"invalid":0d}};
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)=>
            Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=new[]{new Holon{Id=Guid.NewGuid(),MetaData=Coordinates()},
                new Holon{Id=Guid.NewGuid(),MetaData=new(){{"Latitude",2d},{"Longitude",0d}}}},IsLoaded=true,LoadedCount=2});
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version=0)=>
            Task.FromResult(new OASISResult<IEnumerable<IAvatar>>{Result=new[]{new Avatar{Id=Guid.NewGuid(),MetaData=Coordinates()},
                new Avatar{Id=Guid.NewGuid(),MetaData=new(){{"Latitude",2d},{"Longitude",0d}}}},IsLoaded=true,LoadedCount=2});
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task MetadataCountsSelectedRoots(bool empty,bool synchronous)
    {
        var provider=new Records(false);var pairs=new Dictionary<string,string>{{"Latitude",empty?"9":"0"}};
        var result=synchronous?provider.LoadHolonsByMetaData(pairs,MetaKeyValuePairMatchMode.All,HolonType.All,loadChildren:false)
            :await provider.LoadHolonsByMetaDataAsync(pairs,MetaKeyValuePairMatchMode.All,HolonType.All,loadChildren:false);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);
        Assert.AreEqual(empty?0:1,result.Result.Count());Assert.AreEqual(result.Result.Count(),result.LoadedCount);
    }

    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public void NearbyCountsSelectionAndClearsFailure(bool avatars,int scenario)
    {
        var provider=new Records(scenario==2);long latitude=scenario==1?1000000:0;
        if(avatars)AssertNearby(provider.GetAvatarsNearMe(latitude,0,0),scenario);
        else AssertNearby(provider.GetHolonsNearMe(latitude,0,0,HolonType.All),scenario);
    }
    private static void AssertNearby<T>(OASISResult<IEnumerable<T>> result,int scenario)
    {
        Assert.AreEqual(scenario==2,result.IsError,result.Message);
        Assert.AreEqual(scenario!=2,result.IsLoaded);
        Assert.AreEqual(scenario==0?1:0,result.LoadedCount);
        if(scenario==2)Assert.IsNull(result.Result);else Assert.AreEqual(scenario==0?1:0,result.Result.Count());
    }
}
