using NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.UnitTests;

/// <summary>
/// Unit tests for AvalancheOASIS Provider
/// Tests provider initialization, configuration, and basic functionality
/// </summary>
public class AvalancheOASISTests
{
    private const string TestHostUri = "https://api.avax-test.network/ext/bc/C/rpc";
    private const string TestPrivateKey = "0000000000000000000000000000000000000000000000000000000000000001";
    private const BigInteger TestChainId = 43113; // Avalanche Fuji Testnet
    private const string TestContractAddress = "0x0000000000000000000000000000000000000000";

    [Fact]
    public void Constructor_ShouldInitializeProvider_WithCorrectProperties()
    {
        // Arrange & Act
        var provider = new AvalancheOASIS(TestHostUri, TestPrivateKey, TestChainId, TestContractAddress);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("AvalancheOASIS", provider.ProviderName);
        Assert.Equal("Avalanche Provider", provider.ProviderDescription);
        Assert.Equal(ProviderType.AvalancheOASIS, provider.ProviderType.Value);
        Assert.Equal(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenHostUriIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AvalancheOASIS(null!, TestPrivateKey, TestChainId, TestContractAddress));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenPrivateKeyIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AvalancheOASIS(TestHostUri, null!, TestChainId, TestContractAddress));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenContractAddressIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AvalancheOASIS(TestHostUri, TestPrivateKey, TestChainId, null!));
    }

    [Fact]
    public void ProviderType_ShouldBeAvalancheOASIS()
    {
        // Arrange
        var provider = new AvalancheOASIS(TestHostUri, TestPrivateKey, TestChainId, TestContractAddress);

        // Act
        var providerType = provider.ProviderType.Value;

        // Assert
        Assert.Equal(ProviderType.AvalancheOASIS, providerType);
    }

    [Fact]
    public void ProviderCategory_ShouldBeStorageAndNetwork()
    {
        // Arrange
        var provider = new AvalancheOASIS(TestHostUri, TestPrivateKey, TestChainId, TestContractAddress);

        // Act
        var category = provider.ProviderCategory.Value;

        // Assert
        Assert.Equal(ProviderCategory.StorageAndNetwork, category);
    }

    [Theory]
    [InlineData("https://api.avax.network/ext/bc/C/rpc")] // Mainnet
    [InlineData("https://api.avax-test.network/ext/bc/C/rpc")] // Fuji Testnet
    [InlineData("http://localhost:9650/ext/bc/C/rpc")] // Local node
    public void Constructor_ShouldAcceptVariousHostUris(string hostUri)
    {
        // Arrange & Act
        var provider = new AvalancheOASIS(hostUri, TestPrivateKey, TestChainId, TestContractAddress);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("AvalancheOASIS", provider.ProviderName);
    }

    [Theory]
    [InlineData(43114)] // Avalanche C-Chain Mainnet
    [InlineData(43113)] // Avalanche Fuji Testnet
    [InlineData(43112)] // Local Testnet
    public void Constructor_ShouldAcceptVariousChainIds(int chainId)
    {
        // Arrange & Act
        var provider = new AvalancheOASIS(TestHostUri, TestPrivateKey, chainId, TestContractAddress);

        // Assert
        Assert.NotNull(provider);
    }
}

