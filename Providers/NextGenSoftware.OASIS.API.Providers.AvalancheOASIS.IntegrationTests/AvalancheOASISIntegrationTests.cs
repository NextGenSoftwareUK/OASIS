using NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.IntegrationTests;

/// <summary>
/// Integration tests for AvalancheOASIS Provider
/// These tests interact with actual Avalanche testnet (Fuji) or local node
/// ⚠️ Requires active RPC endpoint and test wallet with AVAX
/// </summary>
[Collection("AvalancheIntegration")]
public class AvalancheOASISIntegrationTests : IDisposable
{
    private const string TestHostUri = "https://api.avax-test.network/ext/bc/C/rpc"; // Fuji Testnet
    private const BigInteger TestChainId = 43113; // Fuji Testnet Chain ID
    private readonly string _testPrivateKey;
    private readonly string _testContractAddress;
    private readonly AvalancheOASIS _provider;

    public AvalancheOASISIntegrationTests()
    {
        // ⚠️ IMPORTANT: Use test wallet only, NEVER real private keys in tests!
        _testPrivateKey = Environment.GetEnvironmentVariable("AVALANCHE_TEST_PRIVATE_KEY") 
                          ?? "0000000000000000000000000000000000000000000000000000000000000001";
        _testContractAddress = Environment.GetEnvironmentVariable("AVALANCHE_TEST_CONTRACT_ADDRESS") 
                               ?? "0x0000000000000000000000000000000000000000";

        _provider = new AvalancheOASIS(TestHostUri, _testPrivateKey, TestChainId, _testContractAddress);
    }

    [Fact(Skip = "Integration test - requires live Avalanche Fuji testnet connection")]
    public async Task ActivateProvider_ShouldConnect_ToAvalancheFujiTestnet()
    {
        // Arrange
        // Provider already instantiated in constructor

        // Act
        var result = await _provider.ActivateProviderAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.True(result.Result);
    }

    [Fact(Skip = "Integration test - requires live Avalanche testnet and test wallet")]
    public async Task GetBalance_ShouldReturn_ValidBalance()
    {
        // Arrange
        await _provider.ActivateProviderAsync();

        // Act
        // This would call a balance check method when implemented
        // var balance = await _provider.GetBalanceAsync(testAddress);

        // Assert
        // Assert.NotNull(balance);
        // Assert.True(balance >= 0);
    }

    [Fact(Skip = "Integration test - requires deployed smart contract on Fuji testnet")]
    public async Task LoadAvatar_ShouldRetrieveData_FromBlockchain()
    {
        // Arrange
        await _provider.ActivateProviderAsync();
        var testAvatarId = Guid.NewGuid();

        // Act
        var result = await _provider.LoadAvatarAsync(testAvatarId);

        // Assert
        Assert.NotNull(result);
        // Additional assertions depend on contract implementation
    }

    [Fact(Skip = "Integration test - requires test AVAX for gas fees")]
    public async Task SaveAvatar_ShouldWriteData_ToBlockchain()
    {
        // Arrange
        await _provider.ActivateProviderAsync();
        // var testAvatar = CreateTestAvatar();

        // Act
        // var result = await _provider.SaveAvatarAsync(testAvatar);

        // Assert
        // Assert.NotNull(result);
        // Assert.False(result.IsError);
        // Assert.NotNull(result.Result);
    }

    [Fact(Skip = "Integration test - requires live connection")]
    public async Task DeactivateProvider_ShouldCleanupResources()
    {
        // Arrange
        await _provider.ActivateProviderAsync();

        // Act
        var result = await _provider.DeActivateProviderAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
    }

    public void Dispose()
    {
        // Cleanup if needed
        _provider?.DeActivateProviderAsync().Wait();
    }
}



