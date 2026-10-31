using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS.UnitTests;

public sealed class ThreeFoldOASISTests
{
    [Fact]
    public void DirectConstructor_UsesThreeFoldMetadata()
    {
        var provider = new ThreeFoldOASIS("https://qss.example", "access", "secret", "oasis", useSSL: true);

        Assert.Equal("ThreeFoldOASIS", provider.ProviderName);
        Assert.Equal("ThreeFold QSS provider (S3-compatible object storage via AWSSDK.S3)", provider.ProviderDescription);
        Assert.Equal(ProviderType.ThreeFoldOASIS, provider.ProviderType.Value);
        Assert.Equal(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
    }

    [Fact]
    public void ConnectionStringConstructor_ParsesQssConfiguration()
    {
        var provider = new ThreeFoldOASIS(
            "https://qss.example?accessKey=access&amp;secretKey=secret&amp;bucket=oasis&amp;useSSL=true".Replace("&amp;", "&"));

        Assert.Equal(ProviderType.ThreeFoldOASIS, provider.ProviderType.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://qss.example")]
    [InlineData("https://qss.example?accessKey=access")]
    public void ConnectionStringConstructor_RejectsMissingCredentials(string connectionString)
    {
        Assert.Throws<ArgumentException>(() => new ThreeFoldOASIS(connectionString));
    }
}
