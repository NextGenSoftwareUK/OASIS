using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

public class IPFSSaveErrorBoundaryTests
{
    [Theory]
    [InlineData("cycle")] [InlineData("duplicate")] [InlineData("null")]
    [InlineData("parent")] [InlineData("depth")] [InlineData("empty-id")]
    public async Task InvalidSelectedGraphFailsBeforeStorage(string kind)
    {
        var provider=new IPFSOASIS();var root=new Holon {Id=Guid.NewGuid()};var child=new Holon {Id=Guid.NewGuid()};
        root.Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>{child};
        if(kind=="cycle") child.Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>{root};
        if(kind=="duplicate") child.Id=root.Id;
        if(kind=="null") root.Children.Add(null!);
        if(kind=="parent") child.ParentHolonId=Guid.NewGuid();
        if(kind=="empty-id") child.Id=Guid.Empty;
        var result=await provider.SaveHolonAsync(root,maxChildDepth:kind=="depth" ? -1:0);
        Assert.True(result.IsError);Assert.Equal("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.False(result.IsSaved);Assert.Null(result.Result);
        Assert.Empty(root.ProviderUniqueStorageKey);Assert.Empty(child.ProviderUniqueStorageKey);
    }

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
