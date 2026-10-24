using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jNearbyTests
{
    private sealed class Probe(IAvatar[] avatars,IHolon[] holons):Provider("bolt://127.0.0.1:17688","neo4j","owned",()=>System.Reflection.DispatchProxy.Create<Neo4j.Driver.IDriver,Neo4jLifecycleTests.DriverProbe>())
    {
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version=0)=>Task.FromResult(new OASISResult<IEnumerable<IAvatar>>(avatars){IsLoaded=true,LoadedCount=7});
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)=>Task.FromResult(new OASISResult<IEnumerable<IHolon>>(holons){IsLoaded=true,LoadedCount=7});
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NearbyCountsSelectedRowsAndClearsSourceCountOnFailure(bool avatars,bool corrupt)
    {
        IHolonBase[] rows=Enumerable.Range(0,3).Select(index=>{
            IHolonBase row=avatars?new Avatar():new Holon();row.Id=Guid.NewGuid();row.IsActive=index!=2;
            row.MetaData["Latitude"]=index==1?corrupt?"bad":"50":"0";row.MetaData["Longitude"]="0";return row;
        }).ToArray();
        var provider=new Probe(avatars?rows.Cast<IAvatar>().ToArray():[],avatars?[]:rows.Cast<IHolon>().ToArray());
        void Check<T>(OASISResult<IEnumerable<T>> result){Assert.AreEqual(corrupt,result.IsError);Assert.AreEqual(corrupt?0:1,result.LoadedCount);Assert.AreEqual(!corrupt,result.IsLoaded);if(corrupt){Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));}else Assert.AreEqual(1,result.Result.Count());}
        try{if(avatars)Check(provider.GetAvatarsNearMe(0,0,100));else Check(provider.GetHolonsNearMe(0,0,100));}
        finally{await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod]
    [DataRow(true,91000000L,0L,100,false)]
    [DataRow(false,0L,181000000L,100,false)]
    [DataRow(true,0L,0L,-1,false)]
    [DataRow(false,0L,0L,100,true)]
    public void InvalidNearbyRequestFailsBeforeNativeCalls(bool avatars,long latitude,long longitude,int radius,bool badType)
    {
        var driver=System.Reflection.DispatchProxy.Create<Neo4j.Driver.IDriver,Neo4jLifecycleTests.DriverProbe>();
        var provider=new Provider("bolt://127.0.0.1:17687","neo4j","owned",()=>driver);
        var result=avatars ? (object)provider.GetAvatarsNearMe(latitude,longitude,radius)
            :provider.GetHolonsNearMe(latitude,longitude,radius,badType ? (HolonType)int.MaxValue:HolonType.All);
        var type=result.GetType();
        Assert.AreEqual(true,type.GetProperty("IsError")!.GetValue(result));
        Assert.IsInstanceOfType(type.GetProperty("Exception")!.GetValue(result),typeof(ArgumentOutOfRangeException));
        Assert.AreEqual(0,((Neo4jLifecycleTests.DriverProbe)driver).Verifications);
    }
}
