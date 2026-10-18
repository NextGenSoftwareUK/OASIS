using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jNearbyTests
{
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
