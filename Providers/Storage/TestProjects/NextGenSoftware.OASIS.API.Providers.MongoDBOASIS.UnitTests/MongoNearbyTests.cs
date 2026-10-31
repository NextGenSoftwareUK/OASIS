using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoNearbyTests
{
    [DataTestMethod]
    [DataRow(90000001L,0L,1)] [DataRow(0L,180000001L,1)] [DataRow(0L,0L,-1)]
    public async Task InvalidRequestFailsBeforeDatabaseAccess(long latitude,long longitude,int radius)
    {
        var provider=new Provider("mongodb://127.0.0.1:1","owned_nearby_unit");
        var avatar=await provider.GetAvatarsNearMeAsync(latitude,longitude,radius);
        Assert.IsTrue(avatar.IsError);Assert.IsNull(avatar.Result);Assert.IsInstanceOfType(avatar.Exception,typeof(ArgumentOutOfRangeException));
        var holon=((IOASISNETProvider)provider).GetHolonsNearMe(latitude,longitude,radius,HolonType.All);
        Assert.IsTrue(holon.IsError);Assert.IsNull(holon.Result);Assert.IsInstanceOfType(holon.Exception,typeof(ArgumentOutOfRangeException));
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task UndefinedTypeFailsBeforeDatabaseAccess()
    {
        var provider=new Provider("mongodb://127.0.0.1:1","owned_nearby_unit");
        var result=await provider.GetHolonsNearMeAsync(0,0,100,(HolonType)int.MaxValue);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
