using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.PolygonIDOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlPolygonIdSchemaHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_POLYGON_ID_SCHEMA_NATIVE_HTTP")]
    public Task PolygonIdSchemaNativeHttp() => ReviewedPublicMongoHttpFixture.VerifyHttpAsync(
        ProviderType.PolygonIDOASIS, owner =>
        {
            Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_POLYGON_ID_SCHEMA_NATIVE_HTTP"));
            var attestations = new[] { "age-v3", "country-v2", "auth-v1" };
            return (PolygonIdSchemaQuerySource.ForReviewedTeachingCatalogue(owner),
                attestations.Select(attestation => PolygonIdSchemaQuerySource.LogicalId(owner, attestation)).ToArray());
        });
}
