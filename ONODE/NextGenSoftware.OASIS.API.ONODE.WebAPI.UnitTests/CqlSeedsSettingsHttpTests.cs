using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlSeedsSettingsHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_SEEDS_SETTINGS_NATIVE_HTTP")]
    public Task SeedsSettingsNativeHttp() => ReviewedPublicMongoHttpFixture.VerifyHttpAsync(
        ProviderType.SEEDSOASIS, owner =>
        {
            Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_SEEDS_SETTINGS_NATIVE_HTTP"));
            var settings = new[] { "unity.high", "unity.medium", "unity.low" };
            return (SeedsSettingsQuerySource.ForReviewedTeachingCatalogue(owner),
                settings.Select(setting => SeedsSettingsQuerySource.LogicalId(owner, setting)).ToArray());
        });
}
