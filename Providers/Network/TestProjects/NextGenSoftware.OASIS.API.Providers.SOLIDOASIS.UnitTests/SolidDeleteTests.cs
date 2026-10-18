using System.Net;
using System.Net.Http.Headers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidDeleteTests
{
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static readonly byte[] Key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    private sealed class Transport : HttpMessageHandler
    {
        public Guid Id=Guid.NewGuid();public string Mode="";public int Deletes;public bool Present=true;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            HttpResponseMessage Reply(HttpStatusCode status,string body="")=>new(status){Content=new StringContent(body,System.Text.Encoding.UTF8,"text/turtle")};
            if(request.RequestUri==Root)return Task.FromResult(Reply(HttpStatusCode.OK));
            using var codec=new SolidPrivateRecordCodec(Root,Key);
            Assert.Equal(codec.Resource<IHolon>(Id),request.RequestUri);
            if(request.Method==HttpMethod.Delete){
                Deletes++;Assert.Equal("\"revision-1\"",Assert.Single(request.Headers.IfMatch).ToString());
                if(Mode=="transport")throw new HttpRequestException("controlled unknown delete outcome");
                if(Mode=="conflict")return Task.FromResult(Reply(HttpStatusCode.PreconditionFailed));
                if(Mode!="stale")Present=false;
                return Task.FromResult(Reply(HttpStatusCode.NoContent));
            }
            Assert.Equal(HttpMethod.Get,request.Method);
            if(!Present)return Task.FromResult(Reply(HttpStatusCode.NotFound));
            var response=Reply(HttpStatusCode.OK,codec.Encode<IHolon>(new Holon{Id=Id,Name="original"}));
            if(Mode!="missing-tag")response.Headers.ETag=new EntityTagHeaderValue("\"revision-1\"",Mode=="weak-tag");
            return Task.FromResult(response);
        }
    }
    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task ConditionalDeleteUsesCanonicalResourceAndConfirmsAbsence(bool sync,bool byKey)
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=sync?(byKey?provider.DeleteHolon(transport.Id.ToString()):provider.DeleteHolon(transport.Id)):
            (byKey?await provider.DeleteHolonAsync(transport.Id.ToString()):await provider.DeleteHolonAsync(transport.Id));
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(transport.Id,result.Result.Id);
        Assert.Equal(1,transport.Deletes);Assert.False(transport.Present);
    }
    [Theory][InlineData("missing-tag",0,false)][InlineData("weak-tag",0,false)][InlineData("conflict",1,false)][InlineData("transport",1,true)][InlineData("stale",1,true)]
    public async Task FailedDeleteNeverReportsSuccessOrReplays(string mode,int deletes,bool uncertain)
    {
        var transport=new Transport{Mode=mode};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.DeleteHolonAsync(transport.Id);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.True(transport.Present);Assert.Equal(deletes,transport.Deletes);
        Assert.Equal(uncertain,result.Exception is SolidRecordReconciliationException);
        if(result.Exception is SolidRecordReconciliationException reconciliation)Assert.Equal(mode=="stale",reconciliation.WriteAcknowledged);
        if(mode=="conflict")Assert.Equal(HttpStatusCode.PreconditionFailed,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
    }
}
