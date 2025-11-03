using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests;

/// <summary>
/// Integration tests for SOLIDOASIS Provider
/// These tests interact with actual SOLID pod servers
/// ⚠️ Requires active SOLID pod server and authentication
/// </summary>
[Collection("SOLIDIntegration")]
public class SOLIDOASISIntegrationTests : IDisposable
{
    private const string TestPodServerUrl = "https://solidcommunity.net";
    private readonly string _testAuthToken;
    private readonly SOLIDOASIS _provider;

    public SOLIDOASISIntegrationTests()
    {
        // ⚠️ IMPORTANT: Use test credentials only, NEVER real credentials in tests!
        _testAuthToken = Environment.GetEnvironmentVariable("SOLID_TEST_AUTH_TOKEN") 
                          ?? "";

        _provider = new SOLIDOASIS(TestPodServerUrl, _testAuthToken);
    }

    [Fact(Skip = "Integration test - requires live SOLID pod server connection")]
    public async Task ActivateProvider_ShouldConnect_ToSOLIDPodServer()
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

    [Fact(Skip = "Integration test - requires SOLID pod authentication")]
    public async Task LoadAvatar_ShouldRetrieveData_FromSOLIDPod()
    {
        // Arrange
        await _provider.ActivateProviderAsync();
        var testAvatarId = Guid.NewGuid();

        // Act
        var result = await _provider.LoadAvatarAsync(testAvatarId);

        // Assert
        Assert.NotNull(result);
        // Additional assertions depend on SOLID pod implementation
    }

    [Fact(Skip = "Integration test - requires SOLID pod write permissions")]
    public async Task SaveAvatar_ShouldWriteData_ToSOLIDPod()
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

    [Fact(Skip = "Integration test - requires SOLID pod server")]
    public async Task TestSOLIDPodServerConnection()
    {
        // Arrange
        var provider = new SOLIDOASIS(TestPodServerUrl, _testAuthToken);

        // Act
        var result = await provider.ActivateProviderAsync();

        // Assert
        Assert.NotNull(result);
        // Connection test results depend on server availability
    }

    [Fact(Skip = "Integration test - requires multiple SOLID pod servers")]
    public async Task TestMultipleSOLIDPodServers()
    {
        // Arrange
        var podServers = new[]
        {
            "https://solidcommunity.net",
            "https://inrupt.net",
            "https://solidweb.org"
        };

        foreach (var serverUrl in podServers)
        {
            // Act
            var provider = new SOLIDOASIS(serverUrl, _testAuthToken);
            var result = await provider.ActivateProviderAsync();

            // Assert
            Assert.NotNull(result);
            // Test results depend on server availability and configuration
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
        _provider?.DeActivateProviderAsync().Wait();
        _provider?.Dispose();
    }
}



