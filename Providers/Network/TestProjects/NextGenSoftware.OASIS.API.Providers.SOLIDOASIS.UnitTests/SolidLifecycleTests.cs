using System.Net;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidLifecycleTests
{
    private sealed class Content : StringContent
    {
        public bool Released;
        public Content():base(""){}
        protected override void Dispose(bool disposing){Released=true;base.Dispose(disposing);}
    }
    private sealed class Transport : HttpMessageHandler
    {
        public int Requests;public HttpStatusCode Status=HttpStatusCode.OK;public List<Content> Bodies=new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Requests++;var body=new Content();Bodies.Add(body);return Task.FromResult(new HttpResponseMessage(Status){Content=body});
        }
    }
    [Fact]
    public async Task DeactivationRetainsClientButFinalDisposalPreventsActivation()
    {
        var transport=new Transport();using var provider=new SOLIDOASIS("https://pod.invalid/","",null!,transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);Assert.True(provider.IsProviderActivated);
        Assert.False((await provider.DeActivateProviderAsync()).IsError);Assert.False(provider.IsProviderActivated);
        Assert.False((await provider.ActivateProviderAsync()).IsError);Assert.Equal(2,transport.Requests);
        provider.Dispose();provider.Dispose();Assert.False(provider.IsProviderActivated);
        var result=await provider.ActivateProviderAsync();Assert.True(result.IsError);Assert.IsType<ObjectDisposedException>(result.Exception);Assert.Equal(2,transport.Requests);
    }
    [Theory][InlineData(HttpStatusCode.OK)][InlineData(HttpStatusCode.Forbidden)]
    public async Task ActivationDisposesResponseAndPreservesFailureStatus(HttpStatusCode status)
    {
        var transport=new Transport{Status=status};using var provider=new SOLIDOASIS("https://pod.invalid/","",null!,transport);
        var result=await provider.ActivateProviderAsync();Assert.True(Assert.Single(transport.Bodies).Released);
        Assert.Equal(status!=HttpStatusCode.OK,result.IsError);
        if(result.IsError)Assert.Equal(status,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
    }
    [Theory][InlineData("http://remote.invalid/")][InlineData("https://user:secret@pod.invalid/")][InlineData("https://pod.invalid/?token=x")][InlineData("relative")]
    public void InvalidPodRootRejectedEvenWithoutStorageKey(string root)
        => Assert.ThrowsAny<ArgumentException>(()=>new SOLIDOASIS(root,"",null!,new Transport()));
}
