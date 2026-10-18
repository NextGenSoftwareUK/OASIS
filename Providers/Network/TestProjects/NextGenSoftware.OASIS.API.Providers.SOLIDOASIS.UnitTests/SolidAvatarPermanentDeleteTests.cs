using System.Net;
using System.Net.Http.Headers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidAvatarPermanentDeleteTests
{
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static readonly byte[] Key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    private sealed class Transport : HttpMessageHandler
    {
        public readonly Guid Id=Guid.NewGuid();public bool Avatar=true;public bool Detail=true;
        public string Mode="";public List<string> Deletes=new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            HttpResponseMessage Reply(HttpStatusCode status,string body="")=>new(status){Content=new StringContent(body,System.Text.Encoding.UTF8,"text/turtle")};
            if(request.RequestUri==Root)return Task.FromResult(Reply(HttpStatusCode.OK));
            using var codec=new SolidPrivateRecordCodec(Root,Key);
            var detail=request.RequestUri==codec.Resource<IAvatarDetail>(Id);
            Assert.Equal(detail?codec.Resource<IAvatarDetail>(Id):codec.Resource<IAvatar>(Id),request.RequestUri);
            if(request.Method==HttpMethod.Delete){
                Deletes.Add(request.RequestUri!.AbsoluteUri);
                Assert.Equal(detail?"\"detail\"":"\"avatar\"",Assert.Single(request.Headers.IfMatch).ToString());
                if(!detail&&Mode=="conflict")return Task.FromResult(Reply(HttpStatusCode.PreconditionFailed));
                if(!detail&&Mode=="transport")throw new HttpRequestException("unknown avatar deletion outcome");
                if(detail)Detail=false;else Avatar=false;
                return Task.FromResult(Reply(HttpStatusCode.NoContent));
            }
            Assert.Equal(HttpMethod.Get,request.Method);
            if(detail&&Mode=="read-failed")return Task.FromResult(Reply(HttpStatusCode.Forbidden));
            if(!(detail?Detail:Avatar))return Task.FromResult(Reply(HttpStatusCode.NotFound));
            var response=Reply(HttpStatusCode.OK,detail?codec.Encode<IAvatarDetail>(new AvatarDetail{Id=Id,Username="test"}):codec.Encode<IAvatar>(new Avatar{Id=Id,Username="test"}));
            if(!(detail&&Mode=="missing-tag"))response.Headers.ETag=new EntityTagHeaderValue(detail?"\"detail\"":"\"avatar\"");
            return Task.FromResult(response);
        }
    }
    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task HardDeleteRemovesExistingPairOrAvatarWithoutDetail(bool missingDetail,bool synchronous)
    {
        var transport=new Transport{Detail=!missingDetail};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=synchronous?provider.DeleteAvatar(transport.Id,false):await provider.DeleteAvatarAsync(transport.Id,false);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.Result);Assert.True(result.IsDeleted);
        Assert.False(transport.Avatar);Assert.False(transport.Detail);Assert.Equal(missingDetail?1:2,transport.Deletes.Count);
    }
    [Theory][InlineData("read-failed")][InlineData("missing-tag")]
    public async Task PairPreflightFailurePreventsEveryDelete(string mode)
    {
        var transport=new Transport{Mode=mode};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.DeleteAvatarAsync(transport.Id,false);
        Assert.True(result.IsError);Assert.False(result.Result);Assert.False(result.IsDeleted);
        Assert.Empty(transport.Deletes);Assert.True(transport.Avatar);Assert.True(transport.Detail);
    }
    [Theory][InlineData("conflict")][InlineData("transport")]
    public async Task LaterFailureRetainsAcknowledgedDetailDeletionAndOriginalCause(string mode)
    {
        var transport=new Transport{Mode=mode};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.DeleteAvatarAsync(transport.Id,false);
        Assert.True(result.IsError);Assert.False(result.Result);Assert.False(result.IsDeleted);
        var partial=Assert.IsType<KeyValuePartialMutationException>(result.Exception);
        using var codec=new SolidPrivateRecordCodec(Root,Key);
        Assert.Equal(codec.Resource<IAvatarDetail>(transport.Id).AbsoluteUri,Assert.Single(partial.AcknowledgedKeys));
        Assert.False(transport.Detail);Assert.True(transport.Avatar);Assert.Equal(2,transport.Deletes.Count);
        if(mode=="conflict")Assert.Equal(HttpStatusCode.PreconditionFailed,Assert.IsType<HttpRequestException>(partial.InnerException).StatusCode);
        else Assert.False(Assert.IsType<SolidRecordReconciliationException>(partial.InnerException).WriteAcknowledged);
    }
}
