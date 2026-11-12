using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SubqueryOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlSubqueryDictionaryHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_SUBQUERY_DICTIONARY_NATIVE_HTTP")]
    public Task SubqueryDictionaryNativeHttp() => ReviewedPublicMongoHttpFixture.VerifyHttpAsync(
        ProviderType.SubqueryOASIS, owner =>
        {
            Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_SUBQUERY_DICTIONARY_NATIVE_HTTP"));
            long[] heights = { 400000000, 400000002, 400000003 };
            return (SubqueryDictionaryBlockQuerySource.ForReviewedTeachingCatalogue(owner),
                heights.Select(height => SubqueryDictionaryBlockQuerySource.LogicalId(owner, height)).ToArray());
        });
}
