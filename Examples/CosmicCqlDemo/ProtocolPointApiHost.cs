using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
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

/// <summary>
/// Loopback teaching host over one explicitly owned synthetic fixture.
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
        string address = "http://127.0.0.1:5088")
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
            throw new ArgumentException("An explicit synthetic fixture and actor are required.");
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

        app.MapGet("/", () => Results.Content(Page(provider.ToString(), nativeKey), "text/html"));
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "ready", provider = provider.ToString(), syntheticData = true,
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

    private static string Page(string provider, string key) => """
<!doctype html>
<html lang="en">
<head><meta charset="utf-8"><title>COSMIC CQL native point demo</title>
<style>body{max-width:850px;margin:50px auto;font:18px/1.65 system-ui;padding:20px}input{width:95%;padding:12px}button{margin:22px 0;padding:12px 24px}pre{white-space:pre-wrap;background:#eee;padding:24px}</style></head>
<body><h1>COSMIC CQL native point demo</h1>
<p>Provider: <strong>PROVIDER</strong></p>
<p>Current native record. This local teaching host reads one owned synthetic fixture through HolonManager and LINQ. Actor and provider are selected by the server.</p>
<p>General filtering, ordering and aggregation require a separately supported query profile. Production WEB4 authentication and billing are separate.</p>
<label>Native record key <input id="key" readonly value="RECORDKEY"></label>
<button id="run">Run native CQL query</button>
<pre id="result">Click Run to query the native backend.</pre>
<script>document.getElementById('run').onclick=async()=>{const r=await fetch('/api/cql/point',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({nativeKey:document.getElementById('key').value})});document.getElementById('result').textContent=JSON.stringify(await r.json(),null,2);};</script>
</body></html>
""".Replace("PROVIDER", HtmlEncoder.Default.Encode(provider)).Replace("RECORDKEY", HtmlEncoder.Default.Encode(key));
}
