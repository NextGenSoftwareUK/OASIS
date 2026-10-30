using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class QuestMetadataHydrationContractTests
{
    [Theory]
    [InlineData("31.550330000000002")]
    [InlineData("1.0000000000000002")]
    [InlineData("-74.72613800000002")]
    public void FloatingCoordinatesPreserveExactJsonDoubleRoundTrip(string json)
    {
        var hotspot = new GeoHotSpot { MetaData = new() { ["Lat"] = new Newtonsoft.Json.Linq.JRaw(json) } };
        var hydrate = typeof(HolonManager).GetMethod("MapV2MetaData", BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(GeoHotSpot));
        hydrate.Invoke(null, new object[] { hotspot });
        using var document = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal(BitConverter.DoubleToInt64Bits(document.RootElement.GetDouble()), BitConverter.DoubleToInt64Bits(hotspot.Lat));
    }

    [Fact]
    public void UnrepresentableCoordinateMetadataIsStillRejectedBeforeAssignment()
    {
        var hotspot = new GeoHotSpot { MetaData = new() { ["Lat"] = new Newtonsoft.Json.Linq.JRaw("1.000000000000000000000000001") } };
        var hydrate = typeof(HolonManager).GetMethod("MapV2MetaData", BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(GeoHotSpot));
        var error = Assert.Throws<TargetInvocationException>(() => hydrate.Invoke(null, new object[] { hotspot }));
        Assert.IsType<System.IO.InvalidDataException>(error.InnerException);
        Assert.Equal(0d, hotspot.Lat);
    }

    [Fact]
    public void PersistedQuestCollectionsDecodeTheirJsonStringContract()
    {
        var quest = new Quest { MetaData = new() { ["Requirements"] = "[\"visit portal\"]", ["Quests"] = "[]" } };
        var hydrate = typeof(HolonManager).GetMethod("MapV2MetaData", BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Quest));
        hydrate.Invoke(null, new object[] { quest });
        Assert.Equal(new[] { "visit portal" }, quest.Requirements);
        Assert.Empty(quest.Quests);
    }

    [Fact]
    public void PersistedHotSpotRewardIdsDecodeTheirJsonStringContract()
    {
        var hotspot = new GeoHotSpot { MetaData = new() { ["RewardIds"] = "[\"reward-one\"]" } };
        var hydrate = typeof(HolonManager).GetMethod("MapV2MetaData", BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(GeoHotSpot));
        hydrate.Invoke(null, new object[] { hotspot });
        Assert.Equal(new[] { "reward-one" }, hotspot.RewardIds);
    }
}
