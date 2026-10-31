using System.Net;
using System.Net.Http.Headers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidCqlPointTests
{
    private static readonly Uri Root=new("https://pod.invalid/demo/");
    private static readonly byte[] Key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();

    [Theory]
    [InlineData("no-tag")] [InlineData("public")] [InlineData("owner")] [InlineData("denied")] [InlineData("deleted")]
    [InlineData("missing")] [InlineData("invalid")] [InlineData("uppercase")] [InlineData("cancelled")]
    [InlineData("inactive")] [InlineData("unsupported")] [InlineData("empty-window")]
    [InlineData("media")] [InlineData("identity")] [InlineData("corrupt")] [InlineData("oversized")]
    [InlineData("denied-http")] [InlineData("server-error")] [InlineData("redirect")]
    [InlineData("inflight-cancel")] [InlineData("target")] [InlineData("budget")]
    public async Task NativePointHasBoundedCanonicalSourceAndFailClosedAccess(string scenario)
    {
        var owner=Guid.NewGuid();var id=Guid.Parse("a1234567-b123-4123-8123-123456789abc");
        var record=new Holon{Id=id,CreatedByAvatarId=owner,IsPublic=scenario is "public" or "no-tag",Name="native-point",MetaData=new(){["SolidSourceETag"]="forged"}};
        record.Children.Add(new Holon{Id=Guid.NewGuid()});
        if(scenario=="deleted")record.DeletedDate=DateTime.UtcNow;
        using var codec=new SolidPrivateRecordCodec(Root,Key);
        var rdf=codec.Encode<IHolon>(record);
        if(scenario=="identity")rdf=codec.Encode<IHolon>(new Holon{Id=Guid.NewGuid()});
        var transport=new Transport(id,rdf,scenario);
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);
        if(scenario!="inactive")Assert.False((await provider.ActivateProviderAsync()).IsError);
        var key=scenario=="invalid"?"invalid-id":scenario=="uppercase"?id.ToString().ToUpperInvariant():id.ToString();
        var target=scenario=="target"?ProviderType.IPFSOASIS:ProviderType.SOLIDOASIS;
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(target).Result)
            .Where(h=>CosmicQueryMetadata.Value<string>(h,SOLIDOASIS.CqlRecordIdKey)==key).Result;
        query=query.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(scenario=="owner"?owner:Guid.NewGuid()).Result).Result;
        if(scenario=="unsupported")query=query.OrderBy(h=>h.Name).Result;
        if(scenario=="empty-window")query=query.Take(0).Result;
        var planned=CosmicQueryPlanner.Plan(query,target,new[]{new CosmicQueryTarget(target,provider.QueryCapabilities)});
        if(scenario=="unsupported"){
            Assert.True(planned.IsError);Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",planned.ErrorCode);Assert.Equal(0,transport.Reads);return;
        }
        Assert.False(planned.IsError,planned.Message);
        using var cancellation=new CancellationTokenSource();if(scenario=="cancelled")cancellation.Cancel();
        var result=await provider.ExecuteCosmicQueryAsync(planned.Result,scenario=="budget"?0:1,cancellation.Token);
        var expected=scenario switch{
            "invalid" or "uppercase"=>"CQL_INVALID_SOURCE_KEY",
            "cancelled" or "inflight-cancel"=>"CQL_QUERY_CANCELLED",
            "inactive"=>"CQL_PROVIDER_INACTIVE",
            "target"=>"CQL_PLAN_TARGET_UNAVAILABLE",
            "oversized" or "budget"=>"CQL_QUERY_LIMIT",
            "media" or "identity" or "corrupt" or "denied-http" or "server-error" or "redirect"=>"CQL_QUERY_FAILED",
            _=>null
        };
        if(expected!=null){Assert.True(result.IsError);Assert.Equal(expected,result.ErrorCode);Assert.Null(result.Result);Assert.False(result.IsLoaded);}
        else{
            Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsLoaded);
            if(scenario is "public" or "owner" or "no-tag"){
                var row=Assert.Single(result.Result);Assert.Equal(id,row.Id);Assert.Empty(row.Children);
                Assert.Equal(id.ToString(),row.ProviderUniqueStorageKey[ProviderType.SOLIDOASIS]);
                Assert.Equal(codec.Resource<IHolon>(id).AbsoluteUri,row.MetaData["SolidRecordResource"]);
                if(scenario=="no-tag")Assert.False(row.MetaData.ContainsKey("SolidSourceETag"));
                else Assert.Equal("\"native-revision\"",row.MetaData["SolidSourceETag"]);
            }else Assert.Empty(result.Result);
        }
        Assert.Equal(scenario is "invalid" or "uppercase" or "cancelled" or "inactive" or "target" or "budget" or "empty-window"?0:1,transport.Reads);
        if(scenario=="public"){
            var shared=await CosmicQueryExecutor.ExecuteAsync(planned.Result,new ICosmicQueryStorageProvider[]{provider});
            Assert.False(shared.IsError,shared.Message);Assert.Equal(id,Assert.Single(Assert.Single(shared.Result.Providers).Rows).Id);
        }
    }

    private sealed class Transport(Guid id,string rdf,string scenario):HttpMessageHandler
    {
        public int Reads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Assert.Equal(HttpMethod.Get,request.Method); // No CQL mutations or container enumeration.
            if(request.RequestUri==Root)return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            using var codec=new SolidPrivateRecordCodec(Root,Key);Assert.Equal(codec.Resource<IHolon>(id),request.RequestUri);
            Reads++;
            if(scenario=="inflight-cancel")throw new OperationCanceledException();
            var status=scenario switch{"missing"=>404,"denied-http"=>403,"server-error"=>500,"redirect"=>302,_=>200};
            var body=scenario=="oversized"?new string('x',4*1024*1024+1):scenario=="corrupt"?"broken RDF":rdf;
            var response=new HttpResponseMessage((HttpStatusCode)status){Content=new StringContent(body,System.Text.Encoding.UTF8,scenario=="media"?"text/plain":"text/turtle")};
            if(scenario!="no-tag")response.Headers.ETag=new EntityTagHeaderValue("\"native-revision\"");
            return Task.FromResult(response);
        }
    }
}
