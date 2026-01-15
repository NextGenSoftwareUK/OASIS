using NextGenSoftware.OASIS.API.Providers.BaseOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS.IntegrationTests;

/// <summary>
/// Integration tests for BaseOASIS Provider
/// These tests interact with actual Base Sepolia testnet or local node
/// ⚠️ Requires active RPC endpoint and test wallet with ETH
/// </summary>
[Collection("BaseIntegration")]
public class BaseOASISIntegrationTests : IDisposable
{
    private const string TestHostUri = "https://sepolia.base.org"; // Base Sepolia Testnet
    private readonly string _testPrivateKey;
    private readonly string _testContractAddress;
    private readonly BaseOASIS _provider;

    public BaseOASISIntegrationTests()
    {
        // ⚠️ IMPORTANT: Use test wallet only, NEVER real private keys in tests!
        _testPrivateKey = Environment.GetEnvironmentVariable("BASE_TEST_PRIVATE_KEY") 
                          ?? "0000000000000000000000000000000000000000000000000000000000000001";
        _testContractAddress = Environment.GetEnvironmentVariable("BASE_TEST_CONTRACT_ADDRESS") 
                               ?? "0x0000000000000000000000000000000000000000";

        _provider = new BaseOASIS(TestHostUri, _testPrivateKey, _testContractAddress);
    }

    [Fact(Skip = "Integration test - requires live Base Sepolia testnet connection")]
    public async Task ActivateProvider_ShouldConnect_ToBaseSepoliaTestnet()
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

    [Fact(Skip = "Integration test - requires live Base testnet and test wallet")]
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

    [Fact(Skip = "Integration test - requires deployed smart contract on Base Sepolia")]
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

    [Fact(Skip = "Integration test - requires test ETH for gas fees")]
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



