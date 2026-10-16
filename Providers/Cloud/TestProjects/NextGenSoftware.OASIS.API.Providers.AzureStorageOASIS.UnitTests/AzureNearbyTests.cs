using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests;

[TestClass]
public class AzureNearbyTests
{
    [DataTestMethod]
    [DataRow(90000001L,0L,1)] [DataRow(0L,180000001L,1)] [DataRow(0L,0L,-1)]
    public void InvalidRequestFailsBeforeBlobRequest(long latitude,long longitude,int radius)
    {
        var provider=new AzureStorageOASIS("UseDevelopmentStorage=true");
        var avatar=provider.GetAvatarsNearMe(latitude,longitude,radius);
        Assert.IsTrue(avatar.IsError);Assert.IsNull(avatar.Result);Assert.IsInstanceOfType(avatar.Exception,typeof(ArgumentOutOfRangeException));
        var holon=provider.GetHolonsNearMe(latitude,longitude,radius,HolonType.All);
        Assert.IsTrue(holon.IsError);Assert.IsNull(holon.Result);Assert.IsInstanceOfType(holon.Exception,typeof(ArgumentOutOfRangeException));
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public void InvalidTypeFailsBeforeBlobRequest()
    {
        var provider=new AzureStorageOASIS("UseDevelopmentStorage=true");
        var result=provider.GetHolonsNearMe(0,0,100,(HolonType)int.MaxValue);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
