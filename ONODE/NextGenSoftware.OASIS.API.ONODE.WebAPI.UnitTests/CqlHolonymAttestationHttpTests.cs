using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.HolonymOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlHolonymAttestationHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_HOLONYM_ATTESTATION_NATIVE_HTTP")]
    public Task HolonymAttestationNativeHttp() => ReviewedPublicMongoHttpFixture.VerifyHttpAsync(
        ProviderType.HolonymOASIS, owner =>
        {
            Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_HOLONYM_ATTESTATION_NATIVE_HTTP"));
            var attestations = HolonymAttestationQuerySource.ReviewedIds;
            return (HolonymAttestationQuerySource.ForReviewedTeachingCatalogue(owner),
                attestations.Select(attestation => HolonymAttestationQuerySource.LogicalId(owner, attestation)).ToArray());
        });
}
