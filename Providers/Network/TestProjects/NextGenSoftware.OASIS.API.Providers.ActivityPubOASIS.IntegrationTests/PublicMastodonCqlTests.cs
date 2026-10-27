using System.Net.Http;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

public class PublicMastodonCqlTests
{
    [Fact]
    public async Task NativeStatusPointThroughSharedLinqMatchesIndependentPublicSource()
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var nativeTransport=new Guard();
        using var native=new HttpClient(nativeTransport){BaseAddress=new Uri("https://mastodon.social"),Timeout=TimeSpan.FromSeconds(15)};

        // Discover one existing public status; no fixture account, token or posting.
        using var accountReply=await native.GetAsync("/api/v1/accounts/lookup?acct=Mastodon",timeout.Token);
        accountReply.EnsureSuccessStatusCode();
        using var account=JsonDocument.Parse(await accountReply.Content.ReadAsStringAsync(timeout.Token));
        var accountId=account.RootElement.GetProperty("id").GetString()!;
        RequireNumericId(accountId);nativeTransport.AccountId=accountId;
        using var statusesReply=await native.GetAsync("/api/v1/accounts/"+accountId+"/statuses?limit=1",timeout.Token);
        statusesReply.EnsureSuccessStatusCode();
        using var statuses=JsonDocument.Parse(await statusesReply.Content.ReadAsStringAsync(timeout.Token));
        var statusId=Assert.Single(statuses.RootElement.EnumerateArray()).GetProperty("id").GetString()!;
        RequireNumericId(statusId);nativeTransport.StatusId=statusId;
        using var originalReply=await native.GetAsync("/api/v1/statuses/"+statusId,timeout.Token);
        originalReply.EnsureSuccessStatusCode();
        using var original=JsonDocument.Parse(await originalReply.Content.ReadAsStringAsync(timeout.Token));

        var providerTransport=new Guard{StatusId=statusId};
        using var provider=new ActivityPubOASIS("https://mastodon.social","",providerTransport);
        var activated=await provider.ActivateProviderAsync();
        Assert.False(activated.IsError,activated.Message);

        // Explicit provider and native key. No timeline filtering, local substitute or implicit activation.
        var dna=new OASISDNA { OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false} } };
        var registry=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        using var output=new StringWriter();
        var result=await CosmicCqlDemo.MastodonPointWalkthrough.RunAsync(
            new HolonManager(provider,dna,registry),provider,statusId,Guid.NewGuid(),output,timeout.Token);
        var row=Assert.Single(result);
        Assert.Equal(statusId,row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal(original.RootElement.GetProperty("uri").GetString(),row.MetaData["MastodonObjectUri"]);
        Assert.Equal(original.RootElement.GetProperty("content").GetString(),row.MetaData["MastodonContentHtml"]);
        Assert.Equal("https://mastodon.social",row.MetaData["MastodonInstance"]);
        Assert.True(row.IsPublic);Assert.Empty(row.Children);
        Assert.Equal(3,nativeTransport.Requests);Assert.Equal(2,providerTransport.Requests);
    }

    private static void RequireNumericId(string id)
        => Assert.True(id.Length is >0 and <=128 && id.All(c=>c>='0'&&c<='9'),"The public fixture must supply a bounded native numeric ID.");

    [Fact]
    public async Task NativeStatusThroughWeb4RequiresAdmissionAndRejectsUnsupportedOrdering()
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        var nativeTransport=new Guard();
        using var native=new HttpClient(nativeTransport){BaseAddress=new Uri("https://mastodon.social"),Timeout=TimeSpan.FromSeconds(15)};
        using var accountReply=await native.GetAsync("/api/v1/accounts/lookup?acct=Mastodon");accountReply.EnsureSuccessStatusCode();
        using var account=JsonDocument.Parse(await accountReply.Content.ReadAsStringAsync());
        var accountId=account.RootElement.GetProperty("id").GetString()!;RequireNumericId(accountId);nativeTransport.AccountId=accountId;
        using var statusesReply=await native.GetAsync("/api/v1/accounts/"+accountId+"/statuses?limit=1");statusesReply.EnsureSuccessStatusCode();
        using var statuses=JsonDocument.Parse(await statusesReply.Content.ReadAsStringAsync());
        var original=Assert.Single(statuses.RootElement.EnumerateArray());
        var statusId=original.GetProperty("id").GetString()!;RequireNumericId(statusId);
        var providerTransport=new Guard {StatusId=statusId};
        using var provider=new ActivityPubOASIS("https://mastodon.social","",providerTransport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        // The controlled host authorizes the exact canonical projection ID;
        // an unbound owner/public policy is not an authoritative server catalogue.
        var canonical=await provider.LoadHolonAsync(statusId,loadChildren:false);
        Assert.False(canonical.IsError,canonical.Message);
        await MastodonWeb4Acceptance.RunAsync(provider,new HolonManager(provider,dna,registry),statusId,original.GetProperty("uri").GetString()!,canonical.Result.Id);
        Assert.Equal(2,nativeTransport.Requests);Assert.Equal(3,providerTransport.Requests);
    }

    [Fact,Trait("Category","ActivityPubCqlInteractiveNative")]
    public async Task ExistingPublicStatusThroughInteractiveNativePointHost()
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        var discovery=new Guard();
        using var native=new HttpClient(discovery){BaseAddress=new Uri("https://mastodon.social"),Timeout=TimeSpan.FromSeconds(15)};
        using var accountReply=await native.GetAsync("/api/v1/accounts/lookup?acct=Mastodon");accountReply.EnsureSuccessStatusCode();
        using var account=JsonDocument.Parse(await accountReply.Content.ReadAsStringAsync());
        var accountId=account.RootElement.GetProperty("id").GetString()!;RequireNumericId(accountId);discovery.AccountId=accountId;
        using var statusReply=await native.GetAsync("/api/v1/accounts/"+accountId+"/statuses?limit=1");statusReply.EnsureSuccessStatusCode();
        using var statuses=JsonDocument.Parse(await statusReply.Content.ReadAsStringAsync());
        var original=Assert.Single(statuses.RootElement.EnumerateArray());
        var statusId=original.GetProperty("id").GetString()!;RequireNumericId(statusId);
        var source=new InteractiveSourceGuard(statusId);
        using var provider=new ActivityPubOASIS("https://mastodon.social","",source);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var canonical=await provider.LoadHolonAsync(statusId,loadChildren:false);Assert.False(canonical.IsError);
        Assert.Equal(original.GetProperty("uri").GetString(),canonical.Result.MetaData["MastodonObjectUri"]);
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        await NextGenSoftware.OASIS.Providers.TestSupport.ProtocolPointWeb4Acceptance.VerifyTeachingHostAsync(
            provider,new HolonManager(provider,dna,registry),Guid.NewGuid(),canonical.Result.Id,statusId,syntheticData:false);
        Assert.Equal(2,discovery.Requests);
        Assert.InRange(source.Reads,3,102); // Activation, canonical read, acceptance query, optional bounded demo clicks.
    }

    private sealed class InteractiveSourceGuard(string statusId):DelegatingHandler(new HttpClientHandler{AllowAutoRedirect=false})
    {
        public int Reads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var uri=request.RequestUri!;
            if(request.Method!=HttpMethod.Get||request.Headers.Authorization!=null||uri.Scheme!="https"||uri.Host!="mastodon.social"||uri.Port!=443||!string.IsNullOrEmpty(uri.UserInfo)
                ||(uri.PathAndQuery!="/api/v2/instance"&&uri.PathAndQuery!="/api/v1/statuses/"+statusId)||++Reads>102)
                throw new InvalidOperationException("Only bounded selected public status GETs are allowed; no posting/authentication/federation.");
            return base.SendAsync(request,token);
        }
    }

    private sealed class Guard : DelegatingHandler
    {
        public string? AccountId,StatusId;
        public int Requests;
        public Guard():base(new HttpClientHandler{AllowAutoRedirect=false}){}
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var uri=request.RequestUri!;
            var allowed=uri.PathAndQuery=="/api/v2/instance"
                ||uri.PathAndQuery=="/api/v1/accounts/lookup?acct=Mastodon"
                ||AccountId!=null&&uri.PathAndQuery=="/api/v1/accounts/"+AccountId+"/statuses?limit=1"
                ||StatusId!=null&&uri.PathAndQuery=="/api/v1/statuses/"+StatusId;
            if(request.Method!=HttpMethod.Get||request.Headers.Authorization!=null||uri.Scheme!="https"
                ||uri.Host!="mastodon.social"||uri.Port!=443||!string.IsNullOrEmpty(uri.UserInfo)||!allowed||++Requests>3)
                throw new InvalidOperationException("This CQL acceptance fixture permits only bounded fixed-origin public GETs.");
            return base.SendAsync(request,token);
        }
    }
}
