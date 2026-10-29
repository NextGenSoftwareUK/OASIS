using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace CosmicCqlDemo;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class IdentityFederationRequest
{
    public string Preset { get; set; } = "";
}

/// <summary>Loopback teaching host; production authentication belongs to WEB4.</summary>
public static class IdentityFederationApiHost
{
    public static WebApplication Build(
        HolonManager manager,
        ICosmicQueryStorageProvider mongo,
        ICosmicQueryStorageProvider secondary,
        Guid actor,
        IEnumerable<Guid> selectedIds,
        IEnumerable<Guid> readableMongoIds,
        IEnumerable<Guid> readableSecondaryIds,
        string address = "http://127.0.0.1:5090")
    {
        var uri = new Uri(address);
        if (uri.Scheme != "http" || uri.Host != "127.0.0.1")
            throw new ArgumentException("This teaching host requires IPv4 loopback HTTP.", nameof(address));

        // Capture trusted fixture/catalogues once. Browser input selects only a
        // named preset and cannot grant access, choose a provider or change actor.
        var ids = selectedIds.ToArray();
        var mongoIds = readableMongoIds.ToArray();
        var secondaryIds = readableSecondaryIds.ToArray();
        if (ids.Length != 3 || ids.Distinct().Count() != 3)
            throw new ArgumentException("The synthetic demo requires three distinct selected IDs.");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection();
        builder.WebHost.UseUrls(address);
        var app = builder.Build();

        var page = Page.Replace("OracleDBOASIS", secondary.QueryProviderType.ToString(), StringComparison.Ordinal)
            .Replace("Oracle", IdentityFederationWalkthrough.DisplayName(secondary), StringComparison.Ordinal);
        if (secondary.QueryProviderType == NextGenSoftware.OASIS.API.Core.Enums.ProviderType.AWSOASIS)
            page = page.Replace("Read synthetic holons from two native databases through HolonManager.",
                "Read synthetic holons from MongoDB and DynamoDB Local through HolonManager.", StringComparison.Ordinal)
                .Replace("Stop demo and clean fixtures", "Stop demo (keep reusable AWS seed)", StringComparison.Ordinal);
        if (secondary.QueryProviderType is NextGenSoftware.OASIS.API.Core.Enums.ProviderType.EthereumOASIS
            or NextGenSoftware.OASIS.API.Core.Enums.ProviderType.PolygonOASIS
            or NextGenSoftware.OASIS.API.Core.Enums.ProviderType.ArbitrumOASIS)
            page = page.Replace("Read synthetic holons from two native databases through HolonManager.",
                "Read synthetic holons from MongoDB and local EVM chain31338 through HolonManager.", StringComparison.Ordinal)
                .Replace("Stop demo and clean fixtures", "Stop demo (keep reusable " + IdentityFederationWalkthrough.DisplayName(secondary) + " seed)", StringComparison.Ordinal);
        app.MapGet("/", () => Results.Content(page, "text/html"));
        app.MapPost("/api/cql/federation", async (IdentityFederationRequest request, CancellationToken cancellation) =>
        {
            var selection = request.Preset switch
            {
                "all" => ids,
                "subset" => ids.Take(2).ToArray(),
                _ => null
            };
            if (selection == null) return Results.BadRequest(new { code = "DEMO_PRESET_REQUIRED" });

            try
            {
                // The same C# walkthrough used by the SDK acceptance executes the
                // native providers; no HTTP-specific query engine or local scan.
                var result = await IdentityFederationWalkthrough.RunAsync(
                    manager, mongo, secondary, selection, mongoIds, secondaryIds, actor, TextWriter.Null, cancellation);
                return Results.Ok(new
                {
                    count = result.Rows.Count,
                    observedPhysicalRows = result.ObservedInputRows,
                    consistency = "MovingProviderReads",
                    rows = result.Rows.Select(row => new
                    {
                        provider = row.Selected.Provider.ToString(),
                        id = row.Selected.Row.Id,
                        name = row.Selected.Row.Name
                    })
                });
            }
            catch (InvalidOperationException)
            {
                // Never expose connection details or return the successful source
                // as a partial replacement when the other native source failed.
                return Results.Json(new { code = "DEMO_NATIVE_QUERY_FAILED" }, statusCode: 503);
            }
        });

        app.MapPost("/demo/shutdown", (IHostApplicationLifetime lifetime) =>
        {
            lifetime.StopApplication();
            return Results.Ok(new { stopping = true });
        });
        return app;
    }

    private const string Page = """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>CQL - Mongo + Oracle</title>
          <style>
            body { margin: 0; background: #111827; color: #e5e7eb; font: 18px/1.7 system-ui; }
            main { max-width: 1000px; margin: 48px auto; padding: 0 28px 60px; }
            section { margin: 32px 0; padding: 24px; background: #1f2937; border-radius: 12px; }
            h1, h2 { line-height: 1.3; } h2 { font-size: 22px; }
            pre { white-space: pre-wrap; overflow-wrap: anywhere; line-height: 1.9; }
            button { margin: 8px 12px 8px 0; padding: 12px 20px; font: inherit; cursor: pointer; }
            #result { min-height: 100px; } code { color: #93c5fd; }
          </style>
        </head>
        <body><main>
          <h1>One CQL query. Mongo + Oracle.</h1>
          <p>Read synthetic holons from two native databases through HolonManager.</p>
          <section>
            <h2>1. Select records with C#</h2>
            <pre><code>// Scope explicitly selects MongoDBOASIS and OracleDBOASIS.
        // Each provider receives its own trusted authorization catalogue.

        source.Where(row =&gt; selectedIds.Contains(row.Id));

        // HolonManager executes both native reads and preserves provider copies.
        await manager.ExecuteFederationQueryAsync(plan, adapters);</code></pre>
          </section>
          <section>
            <h2>2. Run the native query</h2>
            <p>All records: Oracle 3 + Mongo 2 = 5 physical rows.<br>
               Subset: Oracle 2 + Mongo 2 = 4 physical rows.</p>
            <button id="all">Query all records</button>
            <button id="subset">Query two-record subset</button>
            <p>Copies retain their source provider. Reads may observe different moments;
               this demo does not claim a distributed snapshot.</p>
          </section>
          <section>
            <h2>3. Inspect provider provenance</h2>
            <pre id="result" role="status" aria-live="polite">Choose a query above.</pre>
          </section>
          <button id="shutdown">Stop demo and clean fixtures</button>
        </main>
        <script>
          const result = document.getElementById('result');
          async function query(preset) {
            result.textContent = 'Reading Mongo and Oracle...';
            try {
              const response = await fetch('/api/cql/federation', {
                method: 'POST', headers: {'Content-Type':'application/json'},
                body: JSON.stringify({preset})
              });
              result.textContent = JSON.stringify(await response.json(), null, 2);
            } catch { result.textContent = 'The native query could not complete.'; }
          }
          document.getElementById('all').onclick = () => query('all');
          document.getElementById('subset').onclick = () => query('subset');
          document.getElementById('shutdown').onclick = async () => {
            await fetch('/demo/shutdown', {method:'POST'});
            result.textContent = 'Demo stopping; the runner verifies fixture cleanup.';
          };
        </script>
        </body></html>
        """;
}
