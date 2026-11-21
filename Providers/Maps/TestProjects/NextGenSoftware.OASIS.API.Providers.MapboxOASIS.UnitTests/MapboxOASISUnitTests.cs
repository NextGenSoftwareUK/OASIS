using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.UnitTests;

[TestClass]
public sealed class MapboxOASISProviderTests
{
    [TestMethod]
    public void ExposesMapboxMapIdentity()
    {
        var provider = new MapBoxOASIS();

        Assert.AreEqual(MapProviderType.MapBox, provider.MapProviderType);
        Assert.AreEqual("MapBox", provider.MapProviderName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(provider.MapProviderDescription));
    }

    [TestMethod]
    public async Task RejectsUnsupportedTravelModeBeforeSendingRequest()
    {
        IOASISRoutingProvider provider = new MapBoxOASIS();

        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => provider.ComputeRouteAsync(
            new RoutingCoordinate(52.517037, 13.388860),
            new RoutingCoordinate(52.529407, 13.397634),
            (RoutingType)(-1)));
    }
}
