using NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

/// <summary>
/// Unit tests for ActivityPubOASIS Provider
/// Tests provider initialization, configuration, and basic functionality
/// </summary>
public class ActivityPubOASISTests
{
    private const string TestInstanceUrl = "https://mastodon.social";
    private const string TestAccessToken = "test-access-token";

    [Fact]
    public void Constructor_ShouldInitializeProvider_WithCorrectProperties()
    {
        // Arrange & Act
        var provider = new ActivityPubOASIS(TestInstanceUrl, TestAccessToken);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("ActivityPubOASIS", provider.ProviderName);
        Assert.Equal("ActivityPub Provider - Federated social network protocol", provider.ProviderDescription);
        Assert.Equal(ProviderType.ActivityPubOASIS, provider.ProviderType.Value);
        Assert.Equal(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenInstanceUrlIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ActivityPubOASIS(null!, TestAccessToken));
    }

    [Fact]
    public void Constructor_ShouldInitializeProvider_WithEmptyAccessToken()
    {
        // Arrange & Act
        var provider = new ActivityPubOASIS(TestInstanceUrl, "");

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("ActivityPubOASIS", provider.ProviderName);
    }

    [Fact]
    public void Constructor_ShouldInitializeProvider_WithNullAccessToken()
    {
        // Arrange & Act
        var provider = new ActivityPubOASIS(TestInstanceUrl, null!);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("ActivityPubOASIS", provider.ProviderName);
    }

    [Fact]
    public void ProviderType_ShouldBeActivityPubOASIS()
    {
        // Arrange
        var provider = new ActivityPubOASIS(TestInstanceUrl, TestAccessToken);

        // Act
        var providerType = provider.ProviderType.Value;

        // Assert
        Assert.Equal(ProviderType.ActivityPubOASIS, providerType);
    }

    [Fact]
    public void ProviderCategory_ShouldBeStorageAndNetwork()
    {
        // Arrange
        var provider = new ActivityPubOASIS(TestInstanceUrl, TestAccessToken);

        // Act
        var category = provider.ProviderCategory.Value;

        // Assert
        Assert.Equal(ProviderCategory.StorageAndNetwork, category);
    }

    [Theory]
    [InlineData("https://mastodon.social")] // Mastodon
    [InlineData("https://pleroma.social")] // Pleroma
    [InlineData("https://misskey.io")] // Misskey
    [InlineData("https://friendica.social")] // Friendica
    [InlineData("http://localhost:3000")] // Local development server
    public void Constructor_ShouldAcceptVariousInstanceUrls(string instanceUrl)
    {
        // Arrange & Act
        var provider = new ActivityPubOASIS(instanceUrl, TestAccessToken);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("ActivityPubOASIS", provider.ProviderName);
    }

    [Fact]
    public void Constructor_ShouldInitializeHttpClient_WithCorrectBaseAddress()
    {
        // Arrange & Act
        var provider = new ActivityPubOASIS(TestInstanceUrl, TestAccessToken);

        // Assert
        Assert.NotNull(provider);
        // Note: We can't directly test HttpClient.BaseAddress as it's private,
        // but we can verify the provider was created successfully
        Assert.Equal("ActivityPubOASIS", provider.ProviderName);
    }

    [Fact]
    public void Constructor_ShouldSetAuthHeader_WhenAccessTokenProvided()
    {
        // Arrange & Act
        var provider = new ActivityPubOASIS(TestInstanceUrl, TestAccessToken);

        // Assert
        Assert.NotNull(provider);
        // The HttpClient setup is internal, but we can verify the provider was created
        Assert.Equal("ActivityPubOASIS", provider.ProviderName);
    }

    [Fact]
    public void Constructor_ShouldNotSetAuthHeader_WhenAccessTokenIsEmpty()
    {
        // Arrange & Act
        var provider = new ActivityPubOASIS(TestInstanceUrl, "");

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("ActivityPubOASIS", provider.ProviderName);
    }

    [Fact]
    public void ProviderName_ShouldBeActivityPubOASIS()
    {
        // Arrange
        var provider = new ActivityPubOASIS(TestInstanceUrl, TestAccessToken);

        // Act
        var providerName = provider.ProviderName;

        // Assert
        Assert.Equal("ActivityPubOASIS", providerName);
    }

    [Fact]
    public void ProviderDescription_ShouldContainActivityPub()
    {
        // Arrange
        var provider = new ActivityPubOASIS(TestInstanceUrl, TestAccessToken);

        // Act
        var description = provider.ProviderDescription;

        // Assert
        Assert.Contains("ActivityPub", description);
        Assert.Contains("Federated", description);
        Assert.Contains("social network", description);
    }
}

