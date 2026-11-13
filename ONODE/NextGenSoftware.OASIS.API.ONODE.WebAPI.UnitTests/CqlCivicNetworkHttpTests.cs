using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.CivicOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlCivicNetworkHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_CIVIC_NETWORK_NATIVE_HTTP")]
    public Task CivicNetworkNativeHttp() => ReviewedPublicMongoHttpFixture.VerifyHttpAsync(
        ProviderType.CivicOASIS, owner =>
        {
            Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_CIVIC_NETWORK_NATIVE_HTTP"));
            var attestations = new[] { 1, 4, 10 };
            return (CivicNetworkQuerySource.ForReviewedTeachingCatalogue(owner),
                attestations.Select(attestation => CivicNetworkQuerySource.LogicalId(owner, attestation)).ToArray());
        });
}
