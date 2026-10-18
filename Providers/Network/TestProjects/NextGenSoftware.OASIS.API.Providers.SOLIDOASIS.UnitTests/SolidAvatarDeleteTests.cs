using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidAvatarDeleteTests
{
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static readonly byte[] Key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    private sealed class Transport : HttpMessageHandler
    {
        public Guid Id=Guid.NewGuid();public bool FailRead;public int Deletes;public int Writes;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            HttpResponseMessage Reply(HttpStatusCode status,string body="")=>new(status){Content=new StringContent(body,System.Text.Encoding.UTF8,"text/turtle")};
            if(request.RequestUri==Root)return Task.FromResult(Reply(HttpStatusCode.OK));
            using var codec=new SolidPrivateRecordCodec(Root,Key);
            if(request.RequestUri==codec.Resource<IAvatarDetail>(Id))return Task.FromResult(Reply(HttpStatusCode.NotFound));
            Assert.Equal(codec.Resource<IAvatar>(Id),request.RequestUri);
            if(request.Method==HttpMethod.Delete){Deletes++;return Task.FromResult(Reply(HttpStatusCode.NoContent));}
            if(request.Method==HttpMethod.Put){Writes++;return Task.FromResult(Reply(HttpStatusCode.Forbidden));}
            var response=FailRead?Reply(HttpStatusCode.ServiceUnavailable):Reply(HttpStatusCode.OK,codec.Encode<IAvatar>(new Avatar{Id=Id,Username="test"}));
            response.Headers.ETag=new System.Net.Http.Headers.EntityTagHeaderValue("\"avatar\"");
            return Task.FromResult(response);
        }
    }
    [Theory][InlineData(true,false)][InlineData(false,false)][InlineData(true,true)][InlineData(false,true)]
    public async Task SoftDeletePreservesReadOrSaveFailureWithoutHardDelete(bool failRead,bool synchronous)
    {
        var transport=new Transport{FailRead=failRead};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=synchronous?provider.DeleteAvatar(transport.Id):await provider.DeleteAvatarAsync(transport.Id);
        Assert.True(result.IsError);Assert.False(result.Result);Assert.Equal(0,transport.Deletes);
        Assert.Equal(failRead?0:1,transport.Writes);
        Assert.Equal(failRead?HttpStatusCode.ServiceUnavailable:HttpStatusCode.Forbidden,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
    }
}
