using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.OASISBootLoader.UnitTests;

[CollectionDefinition("BootLoader registration state", DisableParallelization = true)]
public class BootLoaderRegistrationCollection
{
}

[Collection("BootLoader registration state")]
public class ProviderRegistrationFailureTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task MissingDnaReturnsStartupErrorBeforeRegisteringProvider(
        bool asynchronous, bool activate)
    {
        Assert.False(OASISBootLoader.IsOASISBooted);
        Assert.False(OASISBootLoader.IsOASISBooting);

        var previousPath = OASISBootLoader.OASISDNAPath;
        var missingPath = Path.Combine(
            Path.GetTempPath(), $"oasis-missing-dna-{Guid.NewGuid():N}.json");
        Assert.False(File.Exists(missingPath));

        try
        {
            OASISBootLoader.OASISDNAPath = missingPath;
            var result = asynchronous
                ? activate
                    ? await OASISBootLoader.GetAndActivateStorageProviderAsync(ProviderType.ConvexOASIS)
                    : await OASISBootLoader.RegisterProviderAsync(ProviderType.ConvexOASIS)
                : activate
                    ? OASISBootLoader.GetAndActivateStorageProvider(ProviderType.ConvexOASIS)
                    : OASISBootLoader.RegisterProvider(ProviderType.ConvexOASIS);

            Assert.True(result.IsError);
            Assert.Null(result.Result);
            Assert.Contains("Error booting OASIS", result.Message);
            Assert.False(OASISBootLoader.IsOASISBooted);
            Assert.False(OASISBootLoader.IsOASISBooting);
        }
        finally
        {
            OASISBootLoader.OASISDNAPath = previousPath;
        }
    }
}
