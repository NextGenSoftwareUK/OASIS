using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace CosmicCqlDemo;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ProtocolPointRequest
{
    public string NativeKey { get; set; } = "";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ProtocolKeySetRequest
{
    public string[] NativeKeys { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Loopback teaching host over one explicitly selected demonstration record.
/// Production WEB4 authentication and billing are tested through its separate controller.
/// This host never accepts an actor, provider, price or arbitrary source from the browser.
/// </summary>
public static class ProtocolPointApiHost
{
    public static WebApplication Build(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        Guid actor,
        Guid fixtureId,
        string nativeKey,
        string address = "http://127.0.0.1:5088",
        bool syntheticData = true)
    {
        var provider = adapter.QueryProviderType;
        var metadataKey = provider switch
        {
            ProviderType.IPFSOASIS => "IPFSRecordCid",
            ProviderType.SOLIDOASIS => "SolidRecordId",
            ProviderType.ActivityPubOASIS => "MastodonStatusId",
            _ => throw new ArgumentException("Select an enrolled native point profile.")
        };

        if (actor == Guid.Empty || fixtureId == Guid.Empty || string.IsNullOrWhiteSpace(nativeKey) || nativeKey.Length > 128)
            throw new ArgumentException("An explicit source record and actor are required.");
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "http" || uri.Host != "127.0.0.1")
            throw new ArgumentException("Use an explicit IPv4 loopback HTTP listener.");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection();
        builder.WebHost.UseUrls(address);
        var app = builder.Build();

        // The server owns the actor and exact readable record set. Browser values cannot expand them.
        var access = CosmicQueryReadAccess.AuthoritativeRecords(actor, provider, new[] { fixtureId }).Result;
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(access)
            .Where(holon => CosmicQueryMetadata.Value<string>(holon, metadataKey) == nativeKey);
        if (source.IsError) throw new InvalidOperationException(source.Message);

        app.MapGet("/", () => Results.Content(Page(provider.ToString(), nativeKey, syntheticData), "text/html"));
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "ready", provider = provider.ToString(), syntheticData,
            profile = "One native record point; no ordering, scalar or general metadata queries"
        }));

        app.MapPost("/api/cql/point", async (ProtocolPointRequest request, CancellationToken cancellation) =>
        {
            if (!string.Equals(request.NativeKey, nativeKey, StringComparison.Ordinal))
                return Results.BadRequest(new { code = "DEMO_FIXTURE_KEY_REQUIRED" });

            // Deferred LINQ uses the shared planner/executor and the supplied native adapter.
            var deferred = CosmicLinq.Create(manager, adapter, source.Result, maximumResults: 1);
            if (deferred.IsError) return Results.BadRequest(new { code = deferred.ErrorCode });
            var result = await deferred.Result.ToCosmicListAsync(cancellation);
            if (result.IsError) return Results.Json(new { code = result.ErrorCode }, statusCode: 503);

            return Results.Ok(new
            {
                provider = provider.ToString(), count = result.Result.Count,
                rows = result.Result.Select(row => new
                {
                    row.Id, row.Name, nativeKey,
                    sourceResource = row.MetaData.TryGetValue("SolidRecordResource", out var resource) ? resource :
                        row.MetaData.TryGetValue("MastodonObjectUri", out var original) ? original : nativeKey
                })
            });
        });

        app.MapPost("/demo/shutdown", (HttpContext context, IHostApplicationLifetime lifetime) =>
        {
            context.Response.OnCompleted(() => { lifetime.StopApplication(); return Task.CompletedTask; });
            return Results.Ok(new { status = "stopping" });
        });
        return app;
    }

    /// <summary>Serve a bounded native membership demo over an exact server-owned fixture set.</summary>
    public static WebApplication BuildKeySet(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        Guid actor,
        IReadOnlyDictionary<string,Guid> records,
        string address = "http://127.0.0.1:5088",
        ICosmicQueryStorageProvider? mongo = null,
        IReadOnlyCollection<Guid>? mongoRecords = null)
    {
        var descriptor = adapter.QueryCapabilities.StringKeySetLookup
            ?? throw new ArgumentException("Explicit native key-set enrollment is required.", nameof(adapter));
        if (actor == Guid.Empty || records.Count < 2 || records.Count > descriptor.MaximumKeys
            || records.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value == Guid.Empty))
            throw new ArgumentException("Supply a bounded multi-record native fixture and actor.", nameof(records));
        if (!Uri.TryCreate(address,UriKind.Absolute,out var listener)
            || listener.Scheme != "http" || listener.Host != "127.0.0.1")
            throw new ArgumentException("Use an IPv4 loopback HTTP listener.", nameof(address));

        // Freeze server ownership. Browser input can narrow this set but cannot change
        // provider, actor or authoritative readable identities.
        if (mongo != null && (mongo.QueryProviderType != ProviderType.MongoDBOASIS
            || mongoRecords == null || mongoRecords.Count < 1 || mongoRecords.Count > 40
            || mongoRecords.Any(id => id == Guid.Empty) || mongoRecords.Distinct().Count() != mongoRecords.Count))
            throw new ArgumentException("Mongo federation requires exact server-owned record IDs.", nameof(mongoRecords));
        var mongoIds = mongoRecords?.ToArray();
        var metadataKey = descriptor.MetadataKey;
        var owned = new Dictionary<string,Guid>(records,StringComparer.Ordinal);
        var access = CosmicQueryReadAccess.AuthoritativeRecords(actor,adapter.QueryProviderType,owned.Values).Result;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection();
        builder.WebHost.UseUrls(address);
        var app = builder.Build();

        bool Allowed(string[]? keys) => keys != null && keys.Length > 0 && keys.Length <= owned.Count
            && keys.Distinct(StringComparer.Ordinal).Count() == keys.Length
            && keys.All(key => key != null && owned.ContainsKey(key));
        app.MapGet("/", () => Results.Content(KeySetPage(adapter.QueryProviderType.ToString(),owned.Keys.ToArray(),mongo != null),"text/html"));
        app.MapGet("/health", () => Results.Ok(new { status = "ready", profile = "bounded-native-key-membership", provider = adapter.QueryProviderType.ToString(), records = owned.Count }));
        app.MapPost("/api/cql/keyset", async (ProtocolKeySetRequest request,CancellationToken cancellation) =>
        {
            var keys = request.NativeKeys;
            if (!Allowed(keys))
                return Results.BadRequest(new { code = "DEMO_FIXTURE_KEYS_REQUIRED" });

            var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
                .WithReadAccess(access)
                .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row,metadataKey)));
            if (source.IsError) return Results.BadRequest(new { code = source.ErrorCode });
            var query = CosmicLinq.Create(manager,adapter,source.Result,maximumResults:descriptor.MaximumKeys);
            if (query.IsError) return Results.BadRequest(new { code = query.ErrorCode });
            var result = await query.Result.ToCosmicListAsync(cancellation);
            if (result.IsError) return Results.Json(new { code = result.ErrorCode },statusCode:503);
            return Results.Ok(new
            {
                provider = adapter.QueryProviderType.ToString(), count = result.Result.Count,
                rows = result.Result.Select(row => new { row.Id, row.Name, nativeKey = row.MetaData[descriptor.MetadataKey] })
            });
        });
        if (mongo != null)
        {
            var policies = new[]
            {
                new CosmicFederationReadPolicy(adapter.QueryProviderType,access),
                new CosmicFederationReadPolicy(ProviderType.MongoDBOASIS,
                    CosmicQueryReadAccess.AuthoritativeRecords(actor,ProviderType.MongoDBOASIS,mongoIds!).Result)
            };
            app.MapPost("/api/cql/federation", async (ProtocolKeySetRequest request,CancellationToken cancellation) =>
            {
                var keys = request.NativeKeys;
                if (!Allowed(keys)) return Results.BadRequest(new { code = "DEMO_FIXTURE_KEYS_REQUIRED" });
                var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS,adapter.QueryProviderType).Result)
                    .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row,metadataKey)));
                if (source.IsError) return Results.BadRequest(new { code = source.ErrorCode });
                var options = CosmicFederationOptions.Create(take:80,maximumInputRows:80,maximumRowsPerProvider:40,
                    maximumProviderAttempts:2,maximumDurationMilliseconds:30000).Result;
                var adapters = new[] { mongo,adapter };
                var targets = adapters.Select(target => new CosmicQueryTarget(target.QueryProviderType,target.QueryCapabilities)).ToArray();
                var plan = CosmicFederationPlanner.Plan(source.Result,options,ProviderType.MongoDBOASIS,targets,policies);
                if (plan.IsError) return Results.BadRequest(new { code = plan.ErrorCode });
                var result = await manager.ExecuteFederationQueryAsync(plan.Result,adapters,cancellation);
                if (result.IsError) return Results.Json(new { code = result.ErrorCode },statusCode:503);
                return Results.Ok(new
                {
                    providers = adapters.Select(target => target.QueryProviderType.ToString()),
                    count = result.Result.Rows.Count, observedPhysicalRows = result.Result.ObservedInputRows,
                    consistency = result.Result.Consistency.ToString(),
                    rows = result.Result.Rows.Select(row => new { provider = row.Selected.Provider.ToString(),
                        row.Selected.Row.Id, row.Selected.Row.Name, copies = row.Copies.Select(copy => copy.Provider.ToString()) })
                });
            });
        }
        app.MapPost("/demo/shutdown", (HttpContext context,IHostApplicationLifetime lifetime) =>
        {
            context.Response.OnCompleted(() => { lifetime.StopApplication(); return Task.CompletedTask; });
            return Results.Ok(new { status = "stopping" });
        });
        return app;
    }

    private static string KeySetPage(string provider,string[] keys,bool federation) => """
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>CQL multi-record native demo</title>
<style>body{max-width:960px;margin:40px auto;padding:24px;font:18px/1.8 system-ui}pre{white-space:pre-wrap;background:#eee;padding:24px;line-height:1.9}label{display:block;margin:16px 0;overflow-wrap:anywhere}button{padding:14px 24px;margin:18px 12px 18px 0}</style></head>
<body><h1>CQL multi-record native demo</h1>
<p>Provider: <strong>PROVIDER</strong>. Select native records, then run the same CQL membership query through HolonManager and LINQ.</p>
<pre>// 1. Explicit native provider and server-owned read policy.
// 2. Capture selected native keys as an IN filter.
.Where(row =&gt; keys.Contains(
    CosmicQueryMetadata.Value&lt;string&gt;(row, metadataKey)))

// 3. Execute through the native adapter; display canonical identities.
await query.ToCosmicListAsync(cancellationToken);</pre>
<p>This profile supports bounded key membership. Content predicates and general query parity are not implied. The server fixes the actor, provider and allowed identities.</p>
<div id="keys"></div><button id="run">Run selected-record query</button>FEDERATIONBUTTON<pre id="result">Select two or more records to demonstrate a collection query.</pre>
<script>
const keys=KEYJSON;
for(const key of keys){const label=document.createElement('label');const box=document.createElement('input');box.type='checkbox';box.checked=true;box.value=key;label.append(box,document.createTextNode(' '+key));document.getElementById('keys').append(label);}
const runQuery=async(endpoint)=>{const nativeKeys=[...document.querySelectorAll('input:checked')].map(box=>box.value);const response=await fetch(endpoint,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({nativeKeys})});document.getElementById('result').textContent=JSON.stringify(await response.json(),null,2);};
document.getElementById('run').onclick=()=>runQuery('/api/cql/keyset');
const federated=document.getElementById('federation');
if(federated)federated.onclick=()=>runQuery('/api/cql/federation');
</script></body></html>
""".Replace("PROVIDER",HtmlEncoder.Default.Encode(provider)).Replace("KEYJSON",JsonSerializer.Serialize(keys)).Replace("FEDERATIONBUTTON",federation ? "<button id=\"federation\">Run Mongo + protocol query</button>" : "");

    private static string Page(string provider, string key, bool syntheticData) => """
<!doctype html>
<html lang="en">
<head><meta charset="utf-8"><title>COSMIC CQL native point demo</title>
<style>body{max-width:850px;margin:50px auto;font:18px/1.65 system-ui;padding:20px}input{width:95%;padding:12px}button{margin:22px 0;padding:12px 24px}pre{white-space:pre-wrap;background:#eee;padding:24px}</style></head>
<body><h1>COSMIC CQL native point demo</h1>
<p>Provider: <strong>PROVIDER</strong></p>
<p>Current native record. This local teaching host reads one selected record through HolonManager and LINQ. Actor and provider are selected by the server.</p>
<p>SOURCETYPE</p>
<p>General filtering, ordering and aggregation require a separately supported query profile. Production WEB4 authentication and billing are separate.</p>
<label>Native record key <input id="key" readonly value="RECORDKEY"></label>
<button id="run">Run native CQL query</button>
<pre id="result">Click Run to query the native backend.</pre>
<script>document.getElementById('run').onclick=async()=>{const r=await fetch('/api/cql/point',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({nativeKey:document.getElementById('key').value})});document.getElementById('result').textContent=JSON.stringify(await r.json(),null,2);};</script>
</body></html>
""".Replace("SOURCETYPE", syntheticData ? "Source: owned synthetic fixture." : "Source: existing public record; no remote writes.").Replace("PROVIDER", HtmlEncoder.Default.Encode(provider)).Replace("RECORDKEY", HtmlEncoder.Default.Encode(key));
}
