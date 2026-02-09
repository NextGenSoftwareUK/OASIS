using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.IntegrationTests;

public class STARWebAPIIntegrationTests
{
    [Fact]
    public void STARAPI_Should_Initialize_Successfully()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Mission_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.Missions.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Chapter_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.Chapters.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Quest_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.Quests.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Game_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.Game.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Inventory_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.InventoryItems.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_GeoHotSpot_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.GeoHotSpots.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_NFT_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.NFTs.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_GeoNFT_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.GeoNFTs.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Map_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.Map.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Park_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.Parks.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_OAPP_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.OAPPs.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_CelestialBody_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.CelestialBodies.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_CelestialSpace_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.CelestialSpaces.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Zome_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.Zomes.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Holon_Operations()
    {
        var starAPI = new STARAPI(new STARDNA());
        starAPI.Holons.Should().NotBeNull();
    }
}
