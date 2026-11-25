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
    private const string TestHostUri = "http://127.0.0.1:1"; // closed local port: activation must fail offline
    private const string TestPrivateKey = "0000000000000000000000000000000000000000000000000000000000000001";
    private static readonly BigInteger TestChainId = 43113; // Avalanche Fuji Testnet
    private const string TestContractAddress = "0x0000000000000000000000000000000000000000";

    [Fact]
    public void Constructor_ShouldInitializeProvider_WithCorrectProperties()
    {
        // Arrange & Act
        var provider = new AvalancheOASIS(TestHostUri, TestPrivateKey, TestContractAddress, TestChainId);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("AvalancheOASIS", provider.ProviderName);
        Assert.Equal("Avalanche Provider - EVM-compatible using Web3Core", provider.ProviderDescription);
        Assert.Equal(ProviderType.AvalancheOASIS, provider.ProviderType.Value);
        Assert.Equal(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
    }

    [Fact]
    public async Task Activation_FailsWithoutThrowing_WhenHostUriIsMissing()
    {
        // Providers are built unconfigured and checked at activation, so a
        // missing setting must surface as an error result, not an exception.
        var provider = new AvalancheOASIS(null!, TestPrivateKey, TestContractAddress, TestChainId);

        var result = await provider.ActivateProviderAsync();

        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task Activation_FailsWithoutThrowing_WhenPrivateKeyIsMissing()
    {
        // Providers are built unconfigured and checked at activation, so a
        // missing setting must surface as an error result, not an exception.
        var provider = new AvalancheOASIS(TestHostUri, null!, TestContractAddress, TestChainId);

        var result = await provider.ActivateProviderAsync();

        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task Activation_FailsWithoutThrowing_WhenContractAddressIsMissing()
    {
        // Providers are built unconfigured and checked at activation, so a
        // missing setting must surface as an error result, not an exception.
        var provider = new AvalancheOASIS(TestHostUri, TestPrivateKey, null!, TestChainId);

        var result = await provider.ActivateProviderAsync();

        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public void ProviderType_ShouldBeAvalancheOASIS()
    {
        // Arrange
        var provider = new AvalancheOASIS(TestHostUri, TestPrivateKey, TestContractAddress, TestChainId);

        // Act
        var providerType = provider.ProviderType.Value;

        // Assert
        Assert.Equal(ProviderType.AvalancheOASIS, providerType);
    }

    [Fact]
    public void ProviderCategory_ShouldBeStorageAndNetwork()
    {
        // Arrange
        var provider = new AvalancheOASIS(TestHostUri, TestPrivateKey, TestContractAddress, TestChainId);

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
        var provider = new AvalancheOASIS(hostUri, TestPrivateKey, TestContractAddress, TestChainId);

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
        var provider = new AvalancheOASIS(TestHostUri, TestPrivateKey, TestContractAddress, new BigInteger(chainId));

        // Assert
        Assert.NotNull(provider);
    }
}



