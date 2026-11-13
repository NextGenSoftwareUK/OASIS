using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlSandboxContractHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_SANDBOX_CONTRACT_NATIVE_HTTP")]
    public Task SandboxContractNativeHttp() => ReviewedPublicMongoHttpFixture.VerifyHttpAsync(
        ProviderType.TheSandboxOASIS, owner =>
        {
            Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_SANDBOX_CONTRACT_NATIVE_HTTP"));
            var attestations = new[] { "land", "sand", "asset" };
            return (SandboxContractQuerySource.ForReviewedTeachingCatalogue(owner),
                attestations.Select(attestation => SandboxContractQuerySource.LogicalId(owner, attestation)).ToArray());
        });
}
