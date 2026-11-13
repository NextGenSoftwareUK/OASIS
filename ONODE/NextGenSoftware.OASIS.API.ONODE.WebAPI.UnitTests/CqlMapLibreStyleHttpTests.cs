using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MapLibreOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlMapLibreStyleHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_MAPLIBRE_STYLE_NATIVE_HTTP")]
    public Task MapLibreStyleNativeHttp() => ReviewedPublicMongoHttpFixture.VerifyHttpAsync(
        ProviderType.MapLibreOASIS, owner =>
        {
            Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_MAPLIBRE_STYLE_NATIVE_HTTP"));
            var attestations = new[] { "world", "bright", "terrain" };
            return (MapLibreStyleQuerySource.ForReviewedTeachingCatalogue(owner),
                attestations.Select(attestation => MapLibreStyleQuerySource.LogicalId(owner, attestation)).ToArray());
        });
}
