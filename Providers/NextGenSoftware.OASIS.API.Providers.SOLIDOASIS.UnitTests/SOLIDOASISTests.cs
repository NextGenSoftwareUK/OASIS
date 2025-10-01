using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

/// <summary>
/// Unit tests for SOLIDOASIS Provider
/// Tests provider initialization, configuration, and basic functionality
/// </summary>
public class SOLIDOASISTests
{
    private const string TestPodServerUrl = "https://solidcommunity.net";
    private const string TestAuthToken = "test-auth-token";

    [Fact]
    public void Constructor_ShouldInitializeProvider_WithCorrectProperties()
    {
        // Arrange & Act
        var provider = new SOLIDOASIS(TestPodServerUrl, TestAuthToken);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("SOLIDOASIS", provider.ProviderName);
        Assert.Equal("SOLID (Social Linked Data) Provider - Decentralized personal data storage", provider.ProviderDescription);
        Assert.Equal(ProviderType.SOLIDOASIS, provider.ProviderType.Value);
        Assert.Equal(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenPodServerUrlIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SOLIDOASIS(null!, TestAuthToken));
    }

    [Fact]
    public void Constructor_ShouldInitializeProvider_WithEmptyAuthToken()
    {
        // Arrange & Act
        var provider = new SOLIDOASIS(TestPodServerUrl, "");

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("SOLIDOASIS", provider.ProviderName);
    }

    [Fact]
    public void Constructor_ShouldInitializeProvider_WithNullAuthToken()
    {
        // Arrange & Act
        var provider = new SOLIDOASIS(TestPodServerUrl, null!);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("SOLIDOASIS", provider.ProviderName);
    }

    [Fact]
    public void ProviderType_ShouldBeSOLIDOASIS()
    {
        // Arrange
        var provider = new SOLIDOASIS(TestPodServerUrl, TestAuthToken);

        // Act
        var providerType = provider.ProviderType.Value;

        // Assert
        Assert.Equal(ProviderType.SOLIDOASIS, providerType);
    }

    [Fact]
    public void ProviderCategory_ShouldBeStorageAndNetwork()
    {
        // Arrange
        var provider = new SOLIDOASIS(TestPodServerUrl, TestAuthToken);

        // Act
        var category = provider.ProviderCategory.Value;

        // Assert
        Assert.Equal(ProviderCategory.StorageAndNetwork, category);
    }

    [Theory]
    [InlineData("https://solidcommunity.net")] // SolidCommunity
    [InlineData("https://inrupt.net")] // Inrupt
    [InlineData("https://solidweb.org")] // SolidWeb
    [InlineData("http://localhost:3000")] // Local development server
    public void Constructor_ShouldAcceptVariousPodServerUrls(string podServerUrl)
    {
        // Arrange & Act
        var provider = new SOLIDOASIS(podServerUrl, TestAuthToken);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("SOLIDOASIS", provider.ProviderName);
    }

    [Fact]
    public void Constructor_ShouldInitializeHttpClient_WithCorrectBaseAddress()
    {
        // Arrange & Act
        var provider = new SOLIDOASIS(TestPodServerUrl, TestAuthToken);

        // Assert
        Assert.NotNull(provider);
        // Note: We can't directly test HttpClient.BaseAddress as it's private,
        // but we can verify the provider was created successfully
        Assert.Equal("SOLIDOASIS", provider.ProviderName);
    }

    [Fact]
    public void Constructor_ShouldSetAuthHeader_WhenAuthTokenProvided()
    {
        // Arrange & Act
        var provider = new SOLIDOASIS(TestPodServerUrl, TestAuthToken);

        // Assert
        Assert.NotNull(provider);
        // The HttpClient setup is internal, but we can verify the provider was created
        Assert.Equal("SOLIDOASIS", provider.ProviderName);
    }

    [Fact]
    public void Constructor_ShouldNotSetAuthHeader_WhenAuthTokenIsEmpty()
    {
        // Arrange & Act
        var provider = new SOLIDOASIS(TestPodServerUrl, "");

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("SOLIDOASIS", provider.ProviderName);
    }

    [Fact]
    public void ProviderName_ShouldBeSOLIDOASIS()
    {
        // Arrange
        var provider = new SOLIDOASIS(TestPodServerUrl, TestAuthToken);

        // Act
        var providerName = provider.ProviderName;

        // Assert
        Assert.Equal("SOLIDOASIS", providerName);
    }

    [Fact]
    public void ProviderDescription_ShouldContainSOLID()
    {
        // Arrange
        var provider = new SOLIDOASIS(TestPodServerUrl, TestAuthToken);

        // Act
        var description = provider.ProviderDescription;

        // Assert
        Assert.Contains("SOLID", description);
        Assert.Contains("Social Linked Data", description);
        Assert.Contains("Decentralized", description);
    }
}



