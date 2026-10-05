using NextGenSoftware.OASIS.API.ONODE.WebAPI;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests;

public class ONODEWebAPIIntegrationTests
{
    [Fact]
    public async Task ONODEWebAPI_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var onodeWebAPI = new ONODEWebAPI();
        
        // Assert
        onodeWebAPI.Should().NotBeNull();
        onodeWebAPI.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Avatar_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Avatar.Should().NotBeNull();
        onodeWebAPI.Avatar.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Holon_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Holon.Should().NotBeNull();
        onodeWebAPI.Holon.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Key_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Key.Should().NotBeNull();
        onodeWebAPI.Key.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Map_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Map.Should().NotBeNull();
        onodeWebAPI.Map.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_NFT_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.NFT.Should().NotBeNull();
        onodeWebAPI.NFT.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Search_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Search.Should().NotBeNull();
        onodeWebAPI.Search.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Wallet_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Wallet.Should().NotBeNull();
        onodeWebAPI.Wallet.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Data_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Data.Should().NotBeNull();
        onodeWebAPI.Data.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Storage_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Storage.Should().NotBeNull();
        onodeWebAPI.Storage.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Link_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Link.Should().NotBeNull();
        onodeWebAPI.Link.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Log_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Log.Should().NotBeNull();
        onodeWebAPI.Log.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Quest_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Quest.Should().NotBeNull();
        onodeWebAPI.Quest.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Mission_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Mission.Should().NotBeNull();
        onodeWebAPI.Mission.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Park_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Park.Should().NotBeNull();
        onodeWebAPI.Park.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Inventory_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Inventory.Should().NotBeNull();
        onodeWebAPI.Inventory.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_OAPP_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.OAPP.Should().NotBeNull();
        onodeWebAPI.OAPP.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Zome_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Zome.Should().NotBeNull();
        onodeWebAPI.Zome.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_CelestialBody_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.CelestialBody.Should().NotBeNull();
        onodeWebAPI.CelestialBody.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_CelestialSpace_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.CelestialSpace.Should().NotBeNull();
        onodeWebAPI.CelestialSpace.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Chapter_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Chapter.Should().NotBeNull();
        onodeWebAPI.Chapter.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_GeoHotSpot_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.GeoHotSpot.Should().NotBeNull();
        onodeWebAPI.GeoHotSpot.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_GeoNFT_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.GeoNFT.Should().NotBeNull();
        onodeWebAPI.GeoNFT.ProviderType.Should().Be(ProviderType.ONODE);
    }
}
