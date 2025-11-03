using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.Managers.UnitTests;

public class ManagersUnitTests
{
    [Fact]
    public void AvatarManager_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var avatarManager = new AvatarManager();
        
        // Assert
        avatarManager.Should().NotBeNull();
        avatarManager.ProviderType.Should().Be(ProviderType.Default);
    }

    [Fact]
    public void HolonManager_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var holonManager = new HolonManager();
        
        // Assert
        holonManager.Should().NotBeNull();
        holonManager.ProviderType.Should().Be(ProviderType.Default);
    }

    [Fact]
    public void KeyManager_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var keyManager = new KeyManager();
        
        // Assert
        keyManager.Should().NotBeNull();
        keyManager.ProviderType.Should().Be(ProviderType.Default);
    }

    [Fact]
    public void MapManager_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var mapManager = new MapManager();
        
        // Assert
        mapManager.Should().NotBeNull();
        mapManager.ProviderType.Should().Be(ProviderType.Default);
    }

    [Fact]
    public void NFTManager_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var nftManager = new NFTManager();
        
        // Assert
        nftManager.Should().NotBeNull();
        nftManager.ProviderType.Should().Be(ProviderType.Default);
    }

    [Fact]
    public void SearchManager_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var searchManager = new SearchManager();
        
        // Assert
        searchManager.Should().NotBeNull();
        searchManager.ProviderType.Should().Be(ProviderType.Default);
    }

    [Fact]
    public void WalletManager_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var walletManager = new WalletManager();
        
        // Assert
        walletManager.Should().NotBeNull();
        walletManager.ProviderType.Should().Be(ProviderType.Default);
    }

    [Fact]
    public void AvatarManager_Should_Handle_Provider_Type_Changes()
    {
        // Arrange
        var avatarManager = new AvatarManager();
        
        // Act
        avatarManager.ProviderType = ProviderType.EthereumOASIS;
        
        // Assert
        avatarManager.ProviderType.Should().Be(ProviderType.EthereumOASIS);
    }

    [Fact]
    public void HolonManager_Should_Handle_Provider_Type_Changes()
    {
        // Arrange
        var holonManager = new HolonManager();
        
        // Act
        holonManager.ProviderType = ProviderType.MongoOASIS;
        
        // Assert
        holonManager.ProviderType.Should().Be(ProviderType.MongoOASIS);
    }

    [Fact]
    public void KeyManager_Should_Handle_Provider_Type_Changes()
    {
        // Arrange
        var keyManager = new KeyManager();
        
        // Act
        keyManager.ProviderType = ProviderType.IPFSOASIS;
        
        // Assert
        keyManager.ProviderType.Should().Be(ProviderType.IPFSOASIS);
    }

    [Fact]
    public void MapManager_Should_Handle_Provider_Type_Changes()
    {
        // Arrange
        var mapManager = new MapManager();
        
        // Act
        mapManager.ProviderType = ProviderType.SQLLiteDBOASIS;
        
        // Assert
        mapManager.ProviderType.Should().Be(ProviderType.SQLLiteDBOASIS);
    }

    [Fact]
    public void NFTManager_Should_Handle_Provider_Type_Changes()
    {
        // Arrange
        var nftManager = new NFTManager();
        
        // Act
        nftManager.ProviderType = ProviderType.PolygonOASIS;
        
        // Assert
        nftManager.ProviderType.Should().Be(ProviderType.PolygonOASIS);
    }

    [Fact]
    public void SearchManager_Should_Handle_Provider_Type_Changes()
    {
        // Arrange
        var searchManager = new SearchManager();
        
        // Act
        searchManager.ProviderType = ProviderType.ElrondOASIS;
        
        // Assert
        searchManager.ProviderType.Should().Be(ProviderType.ElrondOASIS);
    }

    [Fact]
    public void WalletManager_Should_Handle_Provider_Type_Changes()
    {
        // Arrange
        var walletManager = new WalletManager();
        
        // Act
        walletManager.ProviderType = ProviderType.TRONOASIS;
        
        // Assert
        walletManager.ProviderType.Should().Be(ProviderType.TRONOASIS);
    }
}
