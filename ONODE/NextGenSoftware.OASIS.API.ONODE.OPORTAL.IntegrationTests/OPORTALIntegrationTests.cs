using NextGenSoftware.OASIS.API.ONODE.OPORTAL;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.ONODE.OPORTAL.IntegrationTests;

public class OPORTALIntegrationTests
{
    [Fact]
    public async Task OPORTAL_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var oportal = new OPORTAL();
        
        // Assert
        oportal.Should().NotBeNull();
        oportal.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Avatar_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Avatar.Should().NotBeNull();
        oportal.Avatar.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Holon_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Holon.Should().NotBeNull();
        oportal.Holon.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Key_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Key.Should().NotBeNull();
        oportal.Key.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Map_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Map.Should().NotBeNull();
        oportal.Map.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_NFT_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.NFT.Should().NotBeNull();
        oportal.NFT.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Search_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Search.Should().NotBeNull();
        oportal.Search.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Wallet_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Wallet.Should().NotBeNull();
        oportal.Wallet.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Data_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Data.Should().NotBeNull();
        oportal.Data.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Storage_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Storage.Should().NotBeNull();
        oportal.Storage.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Link_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Link.Should().NotBeNull();
        oportal.Link.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Log_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Log.Should().NotBeNull();
        oportal.Log.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Quest_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Quest.Should().NotBeNull();
        oportal.Quest.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Mission_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Mission.Should().NotBeNull();
        oportal.Mission.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Park_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Park.Should().NotBeNull();
        oportal.Park.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Inventory_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Inventory.Should().NotBeNull();
        oportal.Inventory.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_OAPP_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.OAPP.Should().NotBeNull();
        oportal.OAPP.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Zome_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Zome.Should().NotBeNull();
        oportal.Zome.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_CelestialBody_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.CelestialBody.Should().NotBeNull();
        oportal.CelestialBody.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_CelestialSpace_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.CelestialSpace.Should().NotBeNull();
        oportal.CelestialSpace.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_Chapter_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.Chapter.Should().NotBeNull();
        oportal.Chapter.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_GeoHotSpot_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.GeoHotSpot.Should().NotBeNull();
        oportal.GeoHotSpot.ProviderType.Should().Be(ProviderType.OPORTAL);
    }

    [Fact]
    public async Task OPORTAL_Should_Support_GeoNFT_Operations()
    {
        // Arrange
        var oportal = new OPORTAL();
        
        // Act & Assert
        oportal.GeoNFT.Should().NotBeNull();
        oportal.GeoNFT.ProviderType.Should().Be(ProviderType.OPORTAL);
    }
}
