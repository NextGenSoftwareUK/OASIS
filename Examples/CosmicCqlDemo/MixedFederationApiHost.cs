using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MixedFederationRequest
{
    public string Preset { get; set; } = "";
}

/// <summary>Loopback UI over an explicitly supplied mixed execution; owns no query or ingestion engine.</summary>
public static class MixedFederationApiHost
{
    public static WebApplication Build(
        Func<bool, CancellationToken, Task<OASISResult<CosmicFederationResult>>> execute,
        string address = "http://127.0.0.1:5091")
    {
        var uri = new Uri(address);
        if (uri.Scheme != "http" || uri.Host != "127.0.0.1")
            throw new ArgumentException("Use IPv4 loopback HTTP for the teaching host.");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection();
        builder.WebHost.UseUrls(address);
        var app = builder.Build();
        app.MapGet("/", () => Results.Content(Page, "text/html"));
        app.MapPost("/api/cql/mixed", async (MixedFederationRequest request, CancellationToken cancellation) =>
        {
            if (request.Preset is not ("matching" or "empty"))
                return Results.BadRequest(new { code = "DEMO_PRESET_REQUIRED" });
            var result = await execute(request.Preset == "matching", cancellation);
            if (result.IsError)
                return Results.Json(new { code = result.ErrorCode }, statusCode: 503);
            return Results.Ok(new
            {
                count = result.Result.Rows.Count,
                observedPhysicalRows = result.Result.ObservedInputRows,
                consistency = "MovingProviderReads",
                // Read-level assurance survives an empty result. Numeric slots are
                // strings so JavaScript never rounds a chain position.
                indexedReads = result.Result.IndexedReads.Select(read => new
                {
                    sourceProvider = read.Checkpoint.Source.SourceProvider.ToString(),
                    executionProvider = read.Checkpoint.Source.ExecutionProvider.ToString(),
                    indexId = read.Checkpoint.Source.IndexId,
                    checkpoint = read.Checkpoint.Position.Slot.ToString(),
                    finalizedHead = read.Head.Position.Slot.ToString()
                }),
                rows = result.Result.Rows.Select(row => new
                {
                    provider = row.Selected.Provider.ToString(), id = row.Selected.Row.Id,
                    receipt = row.Selected.IndexedEvidence?.Receipt.CommitReference,
                    versionId = row.Selected.IndexedEvidence?.Receipt.Record.VersionId
                })
            });
        });
        app.MapPost("/demo/shutdown", (IHostApplicationLifetime lifetime) =>
        {
            lifetime.StopApplication();
            return Results.Ok(new { stopping = true });
        });
        return app;
    }

    private const string Page = """
        <!doctype html><html lang="en"><head><meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>CQL · Mongo + Solana</title>
        <style>
          body { max-width:1000px; margin:48px auto; padding:0 28px; font:18px/1.8 system-ui; background:#111827; color:#e5e7eb; }
          section { margin:32px 0; padding:24px; background:#1f2937; border-radius:12px; }
          pre { white-space:pre-wrap; overflow-wrap:anywhere; line-height:1.9; }
          button { padding:12px 20px; margin:8px 12px 8px 0; font:inherit; cursor:pointer; }
        </style></head><body>
        <h1>One CQL query: Mongo + Solana</h1>
        <p>Native Mongo records and the retained Solana coffee record through its explicit SQLite index.</p>
        <section><h2>1. Shared C# query</h2><pre>// One predicate, two explicitly selected providers.
        source.Where(row =&gt; row.Name == selectedName);

        // Separate native adapters and bound indexed readers.
        await manager.ExecuteMixedFederationQueryAsync(plan, nativeAdapters, indexedReaders);</pre></section>
        <section><h2>2. Run the query</h2>
        <button id="matching">Query matching copies</button><button id="empty">Query empty selection</button>
        <p>Matching returns Mongo1 + Solana1. Empty returns0 and still includes index-read assurance.
        Check receipt, version, checkpoint and finalized head. Reads are not a distributed snapshot.</p>
        <p>No chain transaction is submitted. The server owns actor, scope and source/index grants.</p></section>
        <section><h2>3. Inspect provenance</h2><pre id="result" role="status" aria-live="polite">Choose a query.</pre></section>
        <button id="shutdown">Stop demo and clean Mongo fixture</button>
        <script>
          const result=document.getElementById('result');
          async function query(preset) {
            result.textContent='Reading native Mongo and the checked Solana index…';
            try {
              const response=await fetch('/api/cql/mixed',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({preset})});
              result.textContent=JSON.stringify(await response.json(),null,2);
            } catch { result.textContent='The native query could not complete.'; }
          }
          document.getElementById('matching').onclick=()=>query('matching');
          document.getElementById('empty').onclick=()=>query('empty');
          document.getElementById('shutdown').onclick=async()=>{
            await fetch('/demo/shutdown',{method:'POST'});
            result.textContent='Stopping; the runner verifies Mongo fixture cleanup.';
          };
        </script></body></html>
        """;
}
