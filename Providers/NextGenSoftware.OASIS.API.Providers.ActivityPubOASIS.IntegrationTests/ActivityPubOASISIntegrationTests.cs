using NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

/// <summary>
/// Integration tests for ActivityPubOASIS Provider
/// These tests interact with actual ActivityPub instances (Mastodon, Pleroma, etc.)
/// ⚠️ Requires active ActivityPub instance and OAuth authentication
/// </summary>
[Collection("ActivityPubIntegration")]
public class ActivityPubOASISIntegrationTests : IDisposable
{
    private const string TestInstanceUrl = "https://mastodon.social";
    private readonly string _testAccessToken;
    private readonly ActivityPubOASIS _provider;

    public ActivityPubOASISIntegrationTests()
    {
        // ⚠️ IMPORTANT: Use test credentials only, NEVER real credentials in tests!
        _testAccessToken = Environment.GetEnvironmentVariable("ACTIVITYPUB_TEST_ACCESS_TOKEN") 
                          ?? "";

        _provider = new ActivityPubOASIS(TestInstanceUrl, _testAccessToken);
    }

    [Fact(Skip = "Integration test - requires live ActivityPub instance connection")]
    public async Task ActivateProvider_ShouldConnect_ToActivityPubInstance()
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

    [Fact(Skip = "Integration test - requires ActivityPub instance and OAuth token")]
    public async Task LoadAvatar_ShouldRetrieveData_FromActivityPubInstance()
    {
        // Arrange
        await _provider.ActivateProviderAsync();
        var testAvatarId = Guid.NewGuid();

        // Act
        var result = await _provider.LoadAvatarAsync(testAvatarId);

        // Assert
        Assert.NotNull(result);
        // Additional assertions depend on ActivityPub implementation
    }

    [Fact(Skip = "Integration test - requires ActivityPub write permissions")]
    public async Task SaveAvatar_ShouldWriteData_ToActivityPubInstance()
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

    [Fact(Skip = "Integration test - requires ActivityPub instance")]
    public async Task TestActivityPubInstanceConnection()
    {
        // Arrange
        var provider = new ActivityPubOASIS(TestInstanceUrl, _testAccessToken);

        // Act
        var result = await provider.ActivateProviderAsync();

        // Assert
        Assert.NotNull(result);
        // Connection test results depend on instance availability
    }

    [Fact(Skip = "Integration test - requires multiple ActivityPub instances")]
    public async Task TestMultipleActivityPubInstances()
    {
        // Arrange
        var instances = new[]
        {
            "https://mastodon.social",
            "https://pleroma.social",
            "https://misskey.io",
            "https://friendica.social"
        };

        foreach (var instanceUrl in instances)
        {
            // Act
            var provider = new ActivityPubOASIS(instanceUrl, _testAccessToken);
            var result = await provider.ActivateProviderAsync();

            // Assert
            Assert.NotNull(result);
            // Test results depend on instance availability and configuration
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
        _provider?.DeActivateProviderAsync().Wait();
        _provider?.Dispose();
    }
}

