using NextGenSoftware.OASIS.API.Providers.BaseOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS.UnitTests;

/// <summary>
/// Unit tests for BaseOASIS Provider
/// Tests provider initialization, configuration, and basic functionality
/// </summary>
public class BaseOASISTests
{
    private const string TestHostUri = "http://127.0.0.1:1"; // closed local port: activation must fail offline
    private const string TestPrivateKey = "0000000000000000000000000000000000000000000000000000000000000001";
    private static readonly BigInteger TestChainId = 84532; // Base Sepolia Testnet
    private const string TestContractAddress = "0x0000000000000000000000000000000000000000";

    [Fact]
    public void Constructor_ShouldInitializeProvider_WithCorrectProperties()
    {
        // Arrange & Act
        var provider = new BaseOASIS(TestHostUri, TestPrivateKey, TestChainId, TestContractAddress);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("BaseOASIS", provider.ProviderName);
        Assert.Equal("Base Provider - EVM-compatible using Web3Core", provider.ProviderDescription);
        Assert.Equal(ProviderType.BaseOASIS, provider.ProviderType.Value);
        Assert.Equal(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
    }

    [Fact]
    public async Task Activation_FailsWithoutThrowing_WhenHostUriIsMissing()
    {
        // Providers are built unconfigured and checked at activation, so a
        // missing setting must surface as an error result, not an exception.
        var provider = new BaseOASIS(null!, TestPrivateKey, TestChainId, TestContractAddress);

        var result = await provider.ActivateProviderAsync();

        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task Activation_FailsWithoutThrowing_WhenPrivateKeyIsMissing()
    {
        // Providers are built unconfigured and checked at activation, so a
        // missing setting must surface as an error result, not an exception.
        var provider = new BaseOASIS(TestHostUri, null!, TestChainId, TestContractAddress);

        var result = await provider.ActivateProviderAsync();

        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task Activation_FailsWithoutThrowing_WhenContractAddressIsMissing()
    {
        // Providers are built unconfigured and checked at activation, so a
        // missing setting must surface as an error result, not an exception.
        var provider = new BaseOASIS(TestHostUri, TestPrivateKey, TestChainId, null!);

        var result = await provider.ActivateProviderAsync();

        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public void ProviderType_ShouldBeBaseOASIS()
    {
        // Arrange
        var provider = new BaseOASIS(TestHostUri, TestPrivateKey, TestChainId, TestContractAddress);

        // Act
        var providerType = provider.ProviderType.Value;

        // Assert
        Assert.Equal(ProviderType.BaseOASIS, providerType);
    }

    [Fact]
    public void ProviderCategory_ShouldBeStorageAndNetwork()
    {
        // Arrange
        var provider = new BaseOASIS(TestHostUri, TestPrivateKey, TestChainId, TestContractAddress);

        // Act
        var category = provider.ProviderCategory.Value;

        // Assert
        Assert.Equal(ProviderCategory.StorageAndNetwork, category);
    }

    [Theory]
    [InlineData("https://mainnet.base.org")] // Base Mainnet
    [InlineData("https://sepolia.base.org")] // Base Sepolia Testnet
    [InlineData("http://localhost:8545")] // Local node
    public void Constructor_ShouldAcceptVariousHostUris(string hostUri)
    {
        // Arrange & Act
        var provider = new BaseOASIS(hostUri, TestPrivateKey, TestChainId, TestContractAddress);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("BaseOASIS", provider.ProviderName);
    }

    [Theory]
    [InlineData(8453)] // Base Mainnet
    [InlineData(84532)] // Base Sepolia Testnet
    [InlineData(84531)] // Base Goerli Testnet (deprecated)
    public void Constructor_ShouldAcceptVariousChainIds(int chainId)
    {
        // Arrange & Act
        var provider = new BaseOASIS(TestHostUri, TestPrivateKey, new BigInteger(chainId), TestContractAddress);

        // Assert
        Assert.NotNull(provider);
    }
}



