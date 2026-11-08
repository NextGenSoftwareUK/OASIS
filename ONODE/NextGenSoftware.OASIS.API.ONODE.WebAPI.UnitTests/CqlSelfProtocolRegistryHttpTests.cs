using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SelfProtocolOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlSelfProtocolRegistryHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_SELF_PROTOCOL_REGISTRY_NATIVE_HTTP")]
    public Task SelfProtocolRegistryNativeHttp() => ReviewedPublicMongoHttpFixture.VerifyHttpAsync(
        ProviderType.SelfProtocolOASIS, owner =>
        {
            Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_SELF_PROTOCOL_REGISTRY_NATIVE_HTTP"));
            int[] attestations = { 1, 3, 4 };
            return (SelfProtocolRegistryQuerySource.ForReviewedTeachingCatalogue(owner),
                attestations.Select(attestation => SelfProtocolRegistryQuerySource.LogicalId(owner, attestation)).ToArray());
        });
}
