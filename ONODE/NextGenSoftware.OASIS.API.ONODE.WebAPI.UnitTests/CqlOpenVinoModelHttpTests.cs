using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlOpenVinoModelHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_OPENVINO_MODEL_NATIVE_HTTP")]
    public Task OpenVinoModelNativeHttp() => ReviewedPublicMongoHttpFixture.VerifyHttpAsync(
        ProviderType.IntelOpenVINOOASIS, owner =>
        {
            Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_OPENVINO_MODEL_NATIVE_HTTP"));
            var settings = new[] { "1", "2", "3" };
            return (OpenVinoVersionQuerySource.ForReviewedTeachingCatalogue(owner),
                settings.Select(setting => OpenVinoVersionQuerySource.LogicalId(owner, setting)).ToArray());
        });
}
