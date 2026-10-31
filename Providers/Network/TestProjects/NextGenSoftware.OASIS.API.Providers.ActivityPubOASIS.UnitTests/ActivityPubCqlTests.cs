using System.Net;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubCqlTests
{
    private const string Status="""{"id":"123","uri":"https://remote.invalid/statuses/123","content":"<p>Hello</p>","created_at":"2026-01-02T03:04:05Z","account":{"id":"42","acct":"alice"},"visibility":"public"}""";
    private static CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(
        CosmicQueryScope.Named(ProviderType.ActivityPubOASIS).Result)
        .Where(h=>CosmicQueryMetadata.Value<string>(h,ActivityPubOASIS.CqlStatusKey)=="123").Result;
    private static CosmicQueryPlan<Holon> Plan(ActivityPubOASIS provider,CosmicQuery<Holon>? query=null)
    {
        var result=CosmicQueryPlanner.Plan(query ?? Query(),ProviderType.ActivityPubOASIS,
            new[]{new CosmicQueryTarget(ProviderType.ActivityPubOASIS,provider.QueryCapabilities)});
        Assert.False(result.IsError,result.Message);return result.Result;
    }

    [Fact]
    public async Task SharedExecutorReadsOnlyTheNativePointAndPublishesCanonicalVisibilityAndProvenance()
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var query=Query().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var result=await CosmicQueryExecutor.ExecuteAsync(Plan(provider,query),new ICosmicQueryStorageProvider[]{provider});
        Assert.False(result.IsError,result.Message);
        var row=Assert.Single(Assert.Single(result.Result.Providers).Rows);
        Assert.Equal("123",row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal("123",row.MetaData[ActivityPubOASIS.CqlStatusKey]);
        Assert.Equal("https://mastodon.invalid",row.MetaData["MastodonInstance"]);
        Assert.True(row.IsPublic);Assert.Empty(row.Children);
        Assert.Equal(1,transport.StatusReads);Assert.Equal(0,transport.Mutations);
    }

    [Fact]
    public async Task ExplicitActivationAndAdmissionPrecedeEveryPointRequest()
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        Assert.Equal("CQL_PROVIDER_INACTIVE",(await provider.ExecuteCosmicQueryAsync(Plan(provider))).ErrorCode);
        Assert.Equal(0,transport.Calls);
        var unsupported=Query().Where(h=>h.IsPublic).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",CosmicQueryPlanner.Plan(unsupported,ProviderType.ActivityPubOASIS,
            new[]{new CosmicQueryTarget(ProviderType.ActivityPubOASIS,provider.QueryCapabilities)}).ErrorCode);
        Assert.Equal(0,transport.Calls);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task EmptyPointWindowsRequireNoStatusOrTimelineRead(bool skip)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var query=skip ? Query().Skip(1).Result : Query().Take(0).Result;
        var result=await provider.ExecuteCosmicQueryAsync(Plan(provider,query));
        Assert.False(result.IsError,result.Message);Assert.Empty(result.Result);Assert.True(result.IsLoaded);
        Assert.Equal(0,transport.StatusReads);
    }

    [Theory]
    [InlineData(404,false)] [InlineData(401,true)] [InlineData(403,true)] [InlineData(500,true)] [InlineData(302,true)]
    public async Task NativeAbsenceIsDistinctFromDeniedFailedOrRedirectedReads(int status,bool error)
    {
        var transport=new Transport{StatusCode=(HttpStatusCode)status};using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.ExecuteCosmicQueryAsync(Plan(provider));
        Assert.Equal(error,result.IsError);
        if(error){Assert.Null(result.Result);Assert.False(result.IsLoaded);}else Assert.Empty(result.Result);
        Assert.Equal(1,transport.StatusReads);Assert.Equal(0,transport.Mutations);
    }

    [Theory]
    [InlineData("private")] [InlineData("direct")] [InlineData("unknown")]
    public async Task PrivateAndUnverifiedVisibilityCannotBecomePublicCqlRows(string visibility)
    {
        var transport=new Transport{Body=Status.Replace("\"public\"","\""+visibility+"\"")};
        using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.ExecuteCosmicQueryAsync(Plan(provider));
        Assert.Equal("CQL_VISIBILITY_VIOLATION",result.ErrorCode);Assert.Null(result.Result);
    }

    [Theory]
    [InlineData("wrong-id")] [InlineData("malformed")] [InlineData("oversized")]
    [InlineData("duplicate-id")] [InlineData("duplicate-visibility")]
    [InlineData("oversized-stream")]
    public async Task InvalidOrOversizedSourceNeverReturnsPartialRows(string kind)
    {
        var body=kind switch {"wrong-id"=>Status.Replace("\"id\":\"123\"","\"id\":\"different\""),
            "malformed"=>"not-json",
            "duplicate-id"=>Status.Replace("\"id\":\"123\"","\"id\":\"different\",\"id\":\"123\""),
            "duplicate-visibility"=>Status.Replace("\"visibility\":\"public\"","\"visibility\":\"private\",\"visibility\":\"public\""),
            _=>new string('x',1024*1024+1)};
        var transport=new Transport{Body=body,StreamPayload=kind=="oversized-stream"};using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.ExecuteCosmicQueryAsync(Plan(provider));
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsLoaded);
        Assert.Equal(1,transport.StatusReads);
    }

    [Fact]
    public async Task CompleteAuthoritativeCatalogueCanExcludeAPublicNativeRecord()
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var access=CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),ProviderType.ActivityPubOASIS,Array.Empty<Guid>()).Result;
        var result=await provider.ExecuteCosmicQueryAsync(Plan(provider,Query().WithReadAccess(access).Result));
        Assert.False(result.IsError,result.Message);Assert.Empty(result.Result);
    }

    [Fact]
    public async Task CancellationStopsTheSameNativeOperationWithoutRetryOrPublication()
    {
        var transport=new Transport{WaitForCancellation=true};using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        using var cancellation=new CancellationTokenSource();
        var operation=provider.ExecuteCosmicQueryAsync(Plan(provider),cancellationToken:cancellation.Token);
        await transport.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        var result=await operation;
        Assert.Equal("CQL_QUERY_CANCELLED",result.ErrorCode);Assert.Null(result.Result);
        Assert.Equal(1,transport.StatusReads);Assert.Equal(0,transport.Mutations);
    }

    private sealed class Transport : HttpMessageHandler
    {
        public int Calls,StatusReads,Mutations;
        public string Body=Status;
        public HttpStatusCode StatusCode=HttpStatusCode.OK;
        public bool WaitForCancellation;
        public bool StreamPayload;
        public TaskCompletionSource<bool> Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;if(request.Method!=HttpMethod.Get)Mutations++;
            Assert.Equal("mastodon.invalid",request.RequestUri!.Host);
            if(request.RequestUri.AbsolutePath=="/api/v2/instance")
                return new(HttpStatusCode.OK){Content=new StringContent(ActivityPubInstanceContractTests.InstanceJson)};
            Assert.Equal("/api/v1/statuses/123",request.RequestUri.PathAndQuery);
            StatusReads++;Started.TrySetResult(true);
            if(WaitForCancellation)await Task.Delay(Timeout.Infinite,token);
            return new(StatusCode){Content=StreamPayload ? new UnlengthContent(Body) : new StringContent(Body)};
        }
    }

    private sealed class UnlengthContent(string body) : HttpContent
    {
        protected override bool TryComputeLength(out long length){length=0;return false;}
        protected override async Task SerializeToStreamAsync(Stream stream,TransportContext? context)
            => await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(body));
    }
}
