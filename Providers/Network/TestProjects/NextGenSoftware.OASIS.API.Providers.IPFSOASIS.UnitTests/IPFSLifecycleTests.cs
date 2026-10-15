using System;
using System.Threading.Tasks;
using Ipfs.Http;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

public class IPFSLifecycleTests
{
    private static OASISDNA Settings(string endpoint)
    {
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {StorageProviders=new StorageProviderSettings()}};
        dna.OASIS.StorageProviders.IPFSOASIS=new IPFSOASISSettings {ConnectionString=endpoint};
        return dna;
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public async Task DeactivationOnlyDisconnectsClientAndIsIdempotent(bool synchronous,bool existingClient)
    {
        var provider=new IPFSOASIS(Settings("http://127.0.0.1:1"));
        if(existingClient) provider.IPFSClient=new IpfsClient("http://127.0.0.1:1");
        var result=synchronous ? provider.DeActivateProvider():await provider.DeActivateProviderAsync();
        Assert.False(result.IsError); Assert.True(result.Result); Assert.False(provider.IsProviderActivated); Assert.Null(provider.IPFSClient);
        var repeated=synchronous ? provider.DeActivateProvider():await provider.DeActivateProviderAsync();
        Assert.False(repeated.IsError); Assert.True(repeated.Result);
    }

    [Fact]
    public async Task InvalidEndpointPreservesActivationCauseWithoutShuttingDownPreviousClient()
    {
        var provider=new IPFSOASIS(Settings("http://["));
        provider.IPFSClient=new IpfsClient("http://127.0.0.1:1");
        var result=await provider.ActivateProviderAsync();
        Assert.True(result.IsError); Assert.IsType<UriFormatException>(result.Exception);
        Assert.False(provider.IsProviderActivated); Assert.Null(provider.IPFSClient);
    }
}
