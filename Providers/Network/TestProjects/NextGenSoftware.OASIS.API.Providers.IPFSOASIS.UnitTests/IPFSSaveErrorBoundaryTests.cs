using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

public class IPFSSaveErrorBoundaryTests
{
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task MissingConfigurationReturnsOriginalFailureWithoutClaimingSave(bool synchronous)
    {
        var provider=new IPFSOASIS();
        var record=new Holon {Id=Guid.NewGuid(),Name="no-daemon-error-boundary"};
        var result=synchronous ? provider.SaveHolon(record):await provider.SaveHolonAsync(record);
        Assert.True(result.IsError);Assert.False(result.IsSaved);Assert.Null(result.Result);
        Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Contains("configuration is required",result.Exception.Message);
        Assert.False(record.ProviderUniqueStorageKey.ContainsKey(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.IPFSOASIS));
    }
}
