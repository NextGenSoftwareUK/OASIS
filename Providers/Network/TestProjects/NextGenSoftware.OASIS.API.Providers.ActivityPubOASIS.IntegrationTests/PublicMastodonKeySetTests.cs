using System.Net.Http;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

public sealed class PublicMastodonKeySetTests
{
    [Fact]
    public async Task NativeBatchFiltersSeveralExistingPublicStatusesWithoutPointFanout()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var discovery = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            { BaseAddress = new Uri("https://mastodon.social"), Timeout = TimeSpan.FromSeconds(15) };

        // Read existing public data only. No account, token, posting or local replacement store.
        using var accountReply = await discovery.GetAsync("/api/v1/accounts/lookup?acct=Mastodon", timeout.Token);
        accountReply.EnsureSuccessStatusCode();
        using var account = JsonDocument.Parse(await accountReply.Content.ReadAsStringAsync(timeout.Token));
        var accountId = account.RootElement.GetProperty("id").GetString()!;
        Assert.True(accountId.All(char.IsAsciiDigit));
        using var statusesReply = await discovery.GetAsync("/api/v1/accounts/" + accountId + "/statuses?limit=3", timeout.Token);
        statusesReply.EnsureSuccessStatusCode();
        using var statuses = JsonDocument.Parse(await statusesReply.Content.ReadAsStringAsync(timeout.Token));
        var originals = statuses.RootElement.EnumerateArray().ToArray();
        Assert.Equal(3, originals.Length);
        var ids = originals.Select(status => status.GetProperty("id").GetString()!).ToArray();
        Assert.All(ids, id => Assert.True(id.Length is > 0 and <= 128 && id.All(char.IsAsciiDigit)));
        var path = "/api/v1/statuses?" + string.Join("&", ids.Select(id => "id%5B%5D=" + id));
        var guard = new BatchGuard(path);
        using var provider = new ActivityPubOASIS("https://mastodon.social", "", guard) { EnableCqlStatusKeySets = true };
        Assert.False((await provider.ActivateProviderAsync()).IsError);

        // Contains captures an explicit native IN filter, then applies a bounded visible page.
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ActivityPubOASIS).Result)
            .Where(row => ids.Contains(CosmicQueryMetadata.Value<string>(row, ActivityPubOASIS.CqlStatusKey))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ActivityPubOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ActivityPubOASIS, provider.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        var result = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: timeout.Token);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(ids, result.Result.Select(row => row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]));
        for (var index = 0; index < ids.Length; index++)
        {
            Assert.Equal(originals[index].GetProperty("uri").GetString(), result.Result[index].MetaData["MastodonObjectUri"]);
            Assert.Equal(originals[index].GetProperty("content").GetString(), result.Result[index].MetaData["MastodonContentHtml"]);
            Assert.True(result.Result[index].IsPublic);
        }
        Assert.Equal(1, guard.BatchReads);

        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
        { HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
          StorageProviders = new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings { LogSwitchingProviders = false } } };
        var registry = new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna)
        { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        if(Environment.GetEnvironmentVariable("OASIS_PROTOCOL_MONGO_ROOT") is string mongoRoot)
            await NextGenSoftware.OASIS.Providers.TestSupport.NativeProtocolMongoAcceptance.RunAsync(mongoRoot,provider,ids,Guid.NewGuid(),dna,registry);
        await using var app = CosmicCqlDemo.ProtocolPointApiHost.BuildKeySet(
            new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(provider,dna,registry),provider,
            Guid.NewGuid(),result.Result.ToDictionary(row => (string)row.MetaData[ActivityPubOASIS.CqlStatusKey],row => row.Id),"http://127.0.0.1:0");
        await app.StartAsync(timeout.Token);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var browser = new HttpClient { BaseAddress = new Uri(address) };
            Assert.Contains("CQL multi-record native demo",await browser.GetStringAsync("/",timeout.Token));
            using var forged = await browser.PostAsJsonAsync("/api/cql/keyset",new { nativeKeys=ids, actorId=Guid.NewGuid() },timeout.Token);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest,forged.StatusCode);
            using var invalid = await browser.PostAsJsonAsync("/api/cql/keyset",new { nativeKeys=new[]{"unowned"} },timeout.Token);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest,invalid.StatusCode);
            using var reply = await browser.PostAsJsonAsync("/api/cql/keyset",new { nativeKeys=ids },timeout.Token);
            var json = await reply.Content.ReadAsStringAsync(timeout.Token);
            Assert.True(reply.IsSuccessStatusCode,json);
            using var response = JsonDocument.Parse(json);
            Assert.Equal(3,response.RootElement.GetProperty("count").GetInt32());
            Assert.Equal(Environment.GetEnvironmentVariable("OASIS_PROTOCOL_MONGO_ROOT")==null?2:3,guard.BatchReads);
        }
        finally { await app.StopAsync(); }

        if(Environment.GetEnvironmentVariable("OASIS_CQL_POINT_SERVE")=="1")
        {
            await using var interactive = CosmicCqlDemo.ProtocolPointApiHost.BuildKeySet(
                new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(provider,dna,registry),provider,
                Guid.NewGuid(),result.Result.ToDictionary(row => (string)row.MetaData[ActivityPubOASIS.CqlStatusKey],row => row.Id));
            Console.WriteLine("Multi-record demo: http://127.0.0.1:5088/ ; POST /demo/shutdown to stop.");
            await interactive.RunAsync();
        }
    }

    private sealed class BatchGuard(string path) : DelegatingHandler(new HttpClientHandler { AllowAutoRedirect = false })
    {
        public int BatchReads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var uri = request.RequestUri!;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Null(request.Headers.Authorization);
            Assert.Equal("https", uri.Scheme); Assert.Equal("mastodon.social", uri.Host); Assert.Equal(443, uri.Port);
            if (uri.PathAndQuery != "/api/v2/instance")
            {
                Assert.Equal("/api/v1/statuses",uri.AbsolutePath);
                var allowed = Uri.UnescapeDataString(path.Split('?')[1]).Split('&').ToHashSet(StringComparer.Ordinal);
                var selected = Uri.UnescapeDataString(uri.Query.TrimStart('?')).Split('&');
                Assert.InRange(selected.Length,1,allowed.Count);
                Assert.Equal(selected.Length,selected.Distinct(StringComparer.Ordinal).Count());
                Assert.All(selected,term => Assert.Contains(term,allowed));
                Assert.InRange(++BatchReads,1,102);
            }
            return base.SendAsync(request, token);
        }
    }
}
