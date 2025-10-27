using NextGenSoftware.OASIS.STAR.WebAPI;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.IntegrationTests;

public class STARWebAPIIntegrationTests
{
    [Fact]
    public async Task STARWebAPI_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var starWebAPI = new STARWebAPI();
        
        // Assert
        starWebAPI.Should().NotBeNull();
        starWebAPI.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Avatar_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Avatar.Should().NotBeNull();
        starWebAPI.Avatar.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Holon_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Holon.Should().NotBeNull();
        starWebAPI.Holon.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Key_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Key.Should().NotBeNull();
        starWebAPI.Key.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Map_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Map.Should().NotBeNull();
        starWebAPI.Map.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_NFT_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.NFT.Should().NotBeNull();
        starWebAPI.NFT.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Search_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Search.Should().NotBeNull();
        starWebAPI.Search.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Wallet_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Wallet.Should().NotBeNull();
        starWebAPI.Wallet.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Data_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Data.Should().NotBeNull();
        starWebAPI.Data.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Storage_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Storage.Should().NotBeNull();
        starWebAPI.Storage.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Link_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Link.Should().NotBeNull();
        starWebAPI.Link.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Log_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Log.Should().NotBeNull();
        starWebAPI.Log.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Quest_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Quest.Should().NotBeNull();
        starWebAPI.Quest.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Mission_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Mission.Should().NotBeNull();
        starWebAPI.Mission.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Park_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Park.Should().NotBeNull();
        starWebAPI.Park.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Inventory_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Inventory.Should().NotBeNull();
        starWebAPI.Inventory.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_OAPP_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.OAPP.Should().NotBeNull();
        starWebAPI.OAPP.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Zome_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Zome.Should().NotBeNull();
        starWebAPI.Zome.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_CelestialBody_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.CelestialBody.Should().NotBeNull();
        starWebAPI.CelestialBody.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_CelestialSpace_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.CelestialSpace.Should().NotBeNull();
        starWebAPI.CelestialSpace.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_Chapter_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.Chapter.Should().NotBeNull();
        starWebAPI.Chapter.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_GeoHotSpot_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.GeoHotSpot.Should().NotBeNull();
        starWebAPI.GeoHotSpot.ProviderType.Should().Be(ProviderType.STAR);
    }

    [Fact]
    public async Task STARWebAPI_Should_Support_GeoNFT_Operations()
    {
        // Arrange
        var starWebAPI = new STARWebAPI();
        
        // Act & Assert
        starWebAPI.GeoNFT.Should().NotBeNull();
        starWebAPI.GeoNFT.ProviderType.Should().Be(ProviderType.STAR);
    }
}
