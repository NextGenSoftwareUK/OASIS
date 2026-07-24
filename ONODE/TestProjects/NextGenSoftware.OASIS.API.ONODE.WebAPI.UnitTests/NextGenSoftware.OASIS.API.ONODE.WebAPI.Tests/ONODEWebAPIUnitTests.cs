using NextGenSoftware.OASIS.API.ONODE.WebAPI;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class ONODEWebAPIUnitTests
{
    [Fact]
    public async Task ONODEWebAPI_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var onodeWebAPI = new ONODEWebAPI();
        
        // Assert
        onodeWebAPI.Should().NotBeNull();
        ONODEWebAPI.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Avatar_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Avatar.Should().NotBeNull();
        onodeWebAPI.Avatar.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Holon_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Holon.Should().NotBeNull();
        onodeWebAPI.Holon.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Key_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Key.Should().NotBeNull();
        onodeWebAPI.Key.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Map_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Map.Should().NotBeNull();
        onodeWebAPI.Map.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_NFT_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.NFT.Should().NotBeNull();
        onodeWebAPI.NFT.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Search_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Search.Should().NotBeNull();
        onodeWebAPI.Search.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Wallet_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Wallet.Should().NotBeNull();
        onodeWebAPI.Wallet.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Data_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Data.Should().NotBeNull();
        onodeWebAPI.Data.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Storage_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Storage.Should().NotBeNull();
        onodeWebAPI.Storage.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Link_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Link.Should().NotBeNull();
        onodeWebAPI.Link.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Log_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Log.Should().NotBeNull();
        onodeWebAPI.Log.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Quest_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Quest.Should().NotBeNull();
        onodeWebAPI.Quest.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Mission_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Mission.Should().NotBeNull();
        onodeWebAPI.Mission.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Park_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Park.Should().NotBeNull();
        onodeWebAPI.Park.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Inventory_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Inventory.Should().NotBeNull();
        onodeWebAPI.Inventory.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_OAPP_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.OAPP.Should().NotBeNull();
        onodeWebAPI.OAPP.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Zome_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Zome.Should().NotBeNull();
        onodeWebAPI.Zome.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_CelestialBody_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.CelestialBody.Should().NotBeNull();
        onodeWebAPI.CelestialBody.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_CelestialSpace_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.CelestialSpace.Should().NotBeNull();
        onodeWebAPI.CelestialSpace.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_Chapter_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.Chapter.Should().NotBeNull();
        onodeWebAPI.Chapter.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_GeoHotSpot_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.GeoHotSpot.Should().NotBeNull();
        onodeWebAPI.GeoHotSpot.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }

    [Fact]
    public async Task ONODEWebAPI_Should_Support_GeoNFT_Operations()
    {
        // Arrange
        var onodeWebAPI = new ONODEWebAPI();
        
        // Act & Assert
        onodeWebAPI.GeoNFT.Should().NotBeNull();
        onodeWebAPI.GeoNFT.EndpointName.Should().Be(ONODEWebAPI.EndpointName);
    }
}
