using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

[TestClass]
public class AWSNearbyTests
{
    [DataTestMethod]
    [DataRow(90000001L,0L,1)] [DataRow(0L,180000001L,1)] [DataRow(0L,0L,-1)]
    public void InvalidCoordinatesFailBeforeRequests(long latitude,long longitude,int radius)
    {
        var provider=new AWSOASIS("us-east-1","owned-local","owned-local","http://127.0.0.1:1");
        var network=(IOASISNETProvider)provider;
        var avatars=network.GetAvatarsNearMe(latitude,longitude,radius);
        Assert.IsTrue(avatars.IsError);Assert.IsNull(avatars.Result);Assert.IsInstanceOfType(avatars.Exception,typeof(ArgumentOutOfRangeException));
        var holons=network.GetHolonsNearMe(latitude,longitude,radius,HolonType.All);
        Assert.IsTrue(holons.IsError);Assert.IsNull(holons.Result);Assert.IsInstanceOfType(holons.Exception,typeof(ArgumentOutOfRangeException));
        Assert.IsFalse(provider.IsProviderActivated);provider.DeActivateProvider();
    }

    [TestMethod]
    public void InvalidTypeFailsBeforeRequests()
    {
        var provider=new AWSOASIS("us-east-1","owned-local","owned-local","http://127.0.0.1:1");
        var result=((IOASISNETProvider)provider).GetHolonsNearMe(0,0,100,(HolonType)int.MaxValue);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));
        Assert.IsFalse(provider.IsProviderActivated);provider.DeActivateProvider();
    }
}
