using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.IntegrationTests;

[Collection("OASISBoot")]
public class STARWebAPIIntegrationTests
{
    private readonly OASISFixture _fixture;

    public STARWebAPIIntegrationTests(OASISFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void STARAPI_Should_Initialize_Successfully()
    {
        _fixture.StarAPI.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Mission_Operations()
    {
        _fixture.StarAPI.Missions.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Chapter_Operations()
    {
        _fixture.StarAPI.Chapters.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Quest_Operations()
    {
        _fixture.StarAPI.Quests.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Game_Operations()
    {
        _fixture.StarAPI.Game.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Inventory_Operations()
    {
        _fixture.StarAPI.InventoryItems.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_GeoHotSpot_Operations()
    {
        _fixture.StarAPI.GeoHotSpots.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_NFT_Operations()
    {
        _fixture.StarAPI.NFTs.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_GeoNFT_Operations()
    {
        _fixture.StarAPI.GeoNFTs.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Map_Operations()
    {
        _fixture.StarAPI.Map.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Park_Operations()
    {
        _fixture.StarAPI.Parks.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_OAPP_Operations()
    {
        _fixture.StarAPI.OAPPs.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_CelestialBody_Operations()
    {
        _fixture.StarAPI.CelestialBodies.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_CelestialSpace_Operations()
    {
        _fixture.StarAPI.CelestialSpaces.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Zome_Operations()
    {
        _fixture.StarAPI.Zomes.Should().NotBeNull();
    }

    [Fact]
    public void STARAPI_Should_Support_Holon_Operations()
    {
        _fixture.StarAPI.Holons.Should().NotBeNull();
    }
}
