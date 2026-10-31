using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubHostConfigurationTests
{
    [Fact]
    public void DefaultSettingsConstructAnInactivePublicPointProfile()
    {
        using var provider = ActivityPubProviderConfiguration.Create(new ActivityPubOASISProviderSettings());
        Assert.False(provider.IsProviderActivated);
        Assert.Null(provider.QueryCapabilities.StringKeySetLookup);
    }

    [Fact]
    public void HostExplicitlyEnrolsBatchQueriesWithoutActivatingOrChangingDefault()
    {
        using var provider = ActivityPubProviderConfiguration.Create(new ActivityPubOASISProviderSettings
            { BaseUrl = "https://example.invalid", EnableCqlStatusKeySets = true });
        Assert.False(provider.IsProviderActivated);
        Assert.NotNull(provider.QueryCapabilities.StringKeySetLookup);
    }

    [Theory]
    [InlineData("https://example.invalid/api/v1")]
    [InlineData("https://user:secret@example.invalid")]
    [InlineData("https://example.invalid/?token=private")]
    [InlineData("https://example.invalid/#fragment")]
    [InlineData("http://example.invalid")]
    public void RejectInvalidInstanceScopeBeforeConstruction(string origin)
        => Assert.Throws<ArgumentException>(() => ActivityPubProviderConfiguration.Create(
            new ActivityPubOASISProviderSettings { BaseUrl = origin }));

    [Fact]
    public void ConnectionOverrideCannotRebindConfiguredInstance()
        => Assert.Throws<ArgumentException>(() => ActivityPubProviderConfiguration.Create(
            new ActivityPubOASISProviderSettings { BaseUrl = "https://example.invalid" }, "https://other.invalid"));

    [Fact]
    public void MissingConfiguredTokenFailsRatherThanDowngradingToAnonymous()
        => Assert.Throws<ArgumentException>(() => ActivityPubProviderConfiguration.Create(
            new ActivityPubOASISProviderSettings { AccessTokenEnvironmentVariable = "OASIS_TEST_MISSING_ACTIVITYPUB_TOKEN_67B4F0A1" }));

    [Fact]
    public void EquivalentOriginOverrideRetainsBatchEnrollment()
    {
        using var provider = ActivityPubProviderConfiguration.Create(new ActivityPubOASISProviderSettings
            { BaseUrl = "https://example.invalid", EnableCqlStatusKeySets = true }, "https://example.invalid:443/");
        Assert.NotNull(provider.QueryCapabilities.StringKeySetLookup);
    }
}
