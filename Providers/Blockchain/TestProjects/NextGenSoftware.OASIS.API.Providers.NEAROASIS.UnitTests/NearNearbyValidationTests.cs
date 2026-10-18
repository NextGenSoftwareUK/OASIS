using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearNearbyValidationTests
{
    [TestMethod]
    public void InvalidNearbyRequestsReturnErrorsBeforeActivation()
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","unused","unused","unused","","");
        foreach(var coordinates in new[]{(90000001L,0L,1),(0L,180000001L,1),(0L,0L,-1)})
        {
            Assert.IsTrue(provider.GetAvatarsNearMe(coordinates.Item1,coordinates.Item2,coordinates.Item3).IsError);
            Assert.IsTrue(provider.GetHolonsNearMe(coordinates.Item1,coordinates.Item2,coordinates.Item3,HolonType.All).IsError);
        }
        Assert.IsTrue(provider.GetHolonsNearMe(0,0,1,(HolonType)int.MaxValue).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
