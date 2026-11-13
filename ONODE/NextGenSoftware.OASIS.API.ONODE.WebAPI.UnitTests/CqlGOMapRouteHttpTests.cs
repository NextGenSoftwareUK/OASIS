using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlGOMapRouteHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_GOMAP_ROUTE_NATIVE_HTTP")]
    public Task GOMapRouteNativeHttp() => ReviewedPublicMongoHttpFixture.VerifyHttpAsync(
        ProviderType.GOMapOASIS, owner =>
        {
            Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_GOMAP_ROUTE_NATIVE_HTTP"));
            var attestations = new[] { "west-north", "north-east", "east-west" };
            return (GOMapRouteQuerySource.ForReviewedTeachingCatalogue(owner),
                attestations.Select(attestation => GOMapRouteQuerySource.LogicalId(owner, attestation)).ToArray());
        });
}
