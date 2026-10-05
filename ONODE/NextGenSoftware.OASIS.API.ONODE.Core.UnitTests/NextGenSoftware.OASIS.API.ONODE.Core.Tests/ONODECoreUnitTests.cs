using NextGenSoftware.OASIS.API.ONODE.Core;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

public class ONODECoreUnitTests
{
    [Fact]
    public void ONODE_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var onode = new ONODE();
        
        // Assert
        onode.Should().NotBeNull();
        onode.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Have_Correct_Provider_Type()
    {
        // Arrange & Act
        var onode = new ONODE();
        
        // Assert
        onode.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Avatar_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Avatar.Should().NotBeNull();
        onode.Avatar.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Holon_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Holon.Should().NotBeNull();
        onode.Holon.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Key_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Key.Should().NotBeNull();
        onode.Key.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Map_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Map.Should().NotBeNull();
        onode.Map.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_NFT_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.NFT.Should().NotBeNull();
        onode.NFT.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Search_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Search.Should().NotBeNull();
        onode.Search.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Wallet_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Wallet.Should().NotBeNull();
        onode.Wallet.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Data_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Data.Should().NotBeNull();
        onode.Data.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Storage_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Storage.Should().NotBeNull();
        onode.Storage.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Link_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Link.Should().NotBeNull();
        onode.Link.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Log_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Log.Should().NotBeNull();
        onode.Log.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Quest_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Quest.Should().NotBeNull();
        onode.Quest.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Mission_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Mission.Should().NotBeNull();
        onode.Mission.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Park_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Park.Should().NotBeNull();
        onode.Park.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Inventory_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Inventory.Should().NotBeNull();
        onode.Inventory.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_OAPP_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.OAPP.Should().NotBeNull();
        onode.OAPP.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Zome_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Zome.Should().NotBeNull();
        onode.Zome.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_CelestialBody_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.CelestialBody.Should().NotBeNull();
        onode.CelestialBody.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_CelestialSpace_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.CelestialSpace.Should().NotBeNull();
        onode.CelestialSpace.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_Chapter_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.Chapter.Should().NotBeNull();
        onode.Chapter.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_GeoHotSpot_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.GeoHotSpot.Should().NotBeNull();
        onode.GeoHotSpot.ProviderType.Should().Be(ProviderType.ONODE);
    }

    [Fact]
    public void ONODE_Should_Support_GeoNFT_Operations()
    {
        // Arrange
        var onode = new ONODE();
        
        // Act & Assert
        onode.GeoNFT.Should().NotBeNull();
        onode.GeoNFT.ProviderType.Should().Be(ProviderType.ONODE);
    }
}
