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
            await VerifyNativeMongoFederationAsync(mongoRoot,provider,ids,dna,registry);
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

    private static async Task VerifyNativeMongoFederationAsync(string root,ActivityPubOASIS protocol,string[] keys,
        NextGenSoftware.OASIS.API.DNA.OASISDNA dna,NextGenSoftware.OASIS.API.Core.Managers.ProviderManager registry)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database="cosmic_cql_protocol_federation_v1";
        var state=Path.Combine(root,"LocalState/COSMIC/ProtocolMongoCurrent");Directory.CreateDirectory(state);
        var journal=Path.Combine(state,"fixture.pending.json");
        var reconcile=Environment.GetEnvironmentVariable("OASIS_PROTOCOL_MONGO_RECONCILE")=="1";
        Assert.False(File.Exists(journal+".next"),"Preserve interrupted journal publication.");
        Assert.Equal(reconcile,File.Exists(journal));
        var client=new MongoClient(uri);
        using(var names=await client.ListDatabaseNamesAsync())
            Assert.Equal(reconcile,(await names.ToListAsync()).Contains(database));
        var actor=Guid.NewGuid();
        var records=Enumerable.Range(0,3).Select(index=>new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,
            IsPublic=true,Name="Mongo protocol federation "+index,
            MetaData=new(){[ActivityPubOASIS.CqlStatusKey]=index<2?keys[index]:"unselected-fixture-key"}}).ToArray();
        if(reconcile)
        {
            using var retained=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.Equal(database,retained.RootElement.GetProperty("database").GetString());
            Assert.Equal(keys,retained.RootElement.GetProperty("keys").EnumerateArray().Select(key=>key.GetString()));
            actor=retained.RootElement.GetProperty("actor").GetGuid();
            var ids=retained.RootElement.GetProperty("ids").EnumerateArray().Select(id=>id.GetGuid()).ToArray();
            Assert.Equal(3,ids.Length);Assert.Equal(3,ids.Distinct().Count());
            for(var index=0;index<records.Length;index++){records[index].Id=ids[index];records[index].CreatedByAvatarId=actor;}
        }
        else
        {
        await using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            await JsonSerializer.SerializeAsync(file,new{database,actor,ids=records.Select(row=>row.Id).ToArray(),keys,phase="before-create"});file.Flush(true);
        }
        }
        var mongo=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri,database){IsVersionControlEnabled=false};
        var active=false;
        try
        {
            var activation=await mongo.ActivateProviderAsync();Assert.False(activation.IsError,activation.Message);active=true;
            if(!reconcile)foreach(var record in records){var saved=await mongo.SaveHolonAsync(record,saveChildren:false);Assert.False(saved.IsError,saved.Message);}
            using var output=new StringWriter();
            var result=await CosmicCqlDemo.NativeKeySetWalkthrough.RunFederatedAsync(
                new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(protocol,dna,registry),mongo,protocol,keys,actor,output);
            Assert.Equal(5,result.ObservedInputRows);Assert.Equal(5,result.Rows.Count);
            Assert.Equal(3,result.Rows.Count(row=>row.Selected.Provider==ProviderType.ActivityPubOASIS));
            Assert.Equal(records.Take(2).Select(row=>row.Id).Order(),result.Rows.Where(row=>row.Selected.Provider==ProviderType.MongoDBOASIS).Select(row=>row.Selected.Row.Id).Order());
            Assert.DoesNotContain(result.Rows,row=>row.Selected.Row.Id==records[2].Id);
            foreach(var record in records){var read=await mongo.LoadHolonAsync(record.Id,loadChildren:false);Assert.False(read.IsError);Assert.Equal(actor,read.Result.CreatedByAvatarId);Assert.Equal(record.Name,read.Result.Name);}
            // This database was absent before the journaled writes. Verify its entire
            // physical record set and collection ownership before exact fixture teardown.
            var native=client.GetDatabase(database);
            using(var collections=await native.ListCollectionNamesAsync())Assert.Equal(new[]{"Avatar","AvatarDetail","CosmicHolonOutbox","Holon"},(await collections.ToListAsync()).Order());
            foreach(var empty in new[]{"Avatar","AvatarDetail","CosmicHolonOutbox"})
                Assert.Equal(0,await native.GetCollection<BsonDocument>(empty).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(3,await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.False((await mongo.DeActivateProviderAsync()).IsError);active=false;
            await client.DropDatabaseAsync(database);
            using(var names=await client.ListDatabaseNamesAsync())Assert.DoesNotContain(database,await names.ToListAsync());
            var report=Path.Combine(root,"TestResults/DemoBreadthCurrent/native-mongo-mastodon-federation.json");
            await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new{observedRows=5,mongoRows=2,mastodonRows=3,unselectedMongoExcluded=true,exactCleanupVerified=true}));
            File.Delete(journal);
        }
        finally{if(active)Assert.False((await mongo.DeActivateProviderAsync()).IsError);}
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
