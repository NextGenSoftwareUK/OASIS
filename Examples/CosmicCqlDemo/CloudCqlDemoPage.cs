namespace CosmicCqlDemo;

/// <summary>A teaching client for the real WEB4 cloud routes, with no query engine of its own.</summary>
public static class CloudCqlDemoPage
{
    public static string Html => """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>CQL · Azure Cosmos DB</title>
  <style>
    :root { color-scheme: dark; font-family: system-ui, sans-serif; }
    body { max-width: 1100px; margin: 48px auto; padding: 0 24px; line-height: 1.8; background: #101722; color: #e8edf5; }
    h1, h2 { line-height: 1.4; margin-top: 32px; }
    .scope { padding: 24px; background: #192637; border-left: 4px solid #6cd6bc; }
    .buttons { display: flex; flex-wrap: wrap; gap: 14px; margin: 24px 0; }
    button { padding: 12px 20px; font: inherit; background: #193a39; color: white; border: 1px solid #6cd6bc; border-radius: 8px; cursor: pointer; }
    button:disabled { opacity: .5; }
    pre { padding: 24px; overflow: auto; background: #080e17; border-radius: 8px; line-height: 1.8; }
    code { font-family: Consolas, monospace; }
  </style>
</head>
<body>
  <h1>CQL over Azure Cosmos DB</h1>
  <p class="scope"><strong>Local native emulator · controlled identity and usage receipts.</strong><br>
    These requests execute the actual WEB4 controller, HolonManager and Cosmos SDK.
    The server authorizes one private coffee record from a four-record fixture; other records remain invisible.
    This demonstrates the cloud provider contract locally. Hosted Azure, production JWT, durable billing,
    conditional saves and full EF parity are separate acceptance gates.</p>

  <h2>1. Start with C#</h2>
  <p>The shared <code>CloudRowsWalkthrough.cs</code> example shows explicit provider scope,
    actor access, nested metadata, projection and native cardinality. Its host supplies the activated adapter.</p>
  <pre><code>// Build a deferred query over the trusted host's provider and actor scope.
var source = CosmicQuery&lt;Holon&gt;.Create(scope).Result
    .WithReadAccess(access).Result
    .Where(h =&gt; h.IsActive &amp;&amp;
        CosmicQueryMetadata.Value&lt;string&gt;(h, "fixture") == fixtureTag).Result;

var rows = CosmicLinq.Create(manager, adapter, source, maximumResults: 10).Result;

// The provider executes ordering and paging; no local Enumerable substitute.
var page = await rows.OrderBy(h =&gt; h.Id).Take(10).ToCosmicListAsync();

// Each operation is an independent read, without a cross-request snapshot.
var count = rows.LongCount();</code></pre>

  <h2>2. Try the WEB4 requests</h2>
  <p>Choose an operation to inspect its JSON and real HTTP response. The host owns access and prices;
    provider selection never grants access. Exact cardinality travels as a string.</p>
  <div class="buttons">
    <button data-query="rows">Load rows</button>
    <button data-query="projection">Select name</button>
    <button data-query="LongCount">LongCount</button>
  </div>
  <p id="status" role="status">Ready. Each button makes one independent native read.</p>
  <h2>Request</h2>
  <pre id="request">Choose a query above.</pre>
  <h2>Response</h2>
  <pre id="response">The response will appear here.</pre>
  <h2>3. Finish the demo</h2>
  <p>Shutdown stops the teaching host, then verifies exact ETag fixture cleanup and stops the emulator.
    If cleanup fails, the durable journal is preserved for reconciliation.</p>
  <button id="shutdown">Stop and clean up</button>
  <script>
    const buttons = [...document.querySelectorAll('button')];
    const status = document.getElementById('status');
    const filters = [{ metadataPath: ['fixture'], scalar: 'String', operator: 'Equal', value: 'cosmic-cloud-rows-demo' }];
    // Automatic fixture/HTTP acceptance owns startup. Do not let a user query
    // race its exact receipt assertions before the teaching session is ready.
    buttons.forEach(button => button.disabled = true);
    async function waitUntilReady() {
      status.textContent = 'Waiting for native acceptance checks…';
      try {
        for (let attempt = 0; attempt < 60; attempt++) {
          const response = await fetch('/demo/health');
          if (response.ok) {
            buttons.forEach(button => button.disabled = false);
            status.textContent = 'Ready. Each button makes one independent native read.';
            return;
          }
          if (response.status !== 503) throw new Error('Readiness HTTP ' + response.status);
          await new Promise(resolve => setTimeout(resolve, 250));
        }
        throw new Error('Native acceptance readiness deadline exceeded. Check the runner.');
      } catch (error) {
        status.textContent = String(error);
      }
    }
    waitUntilReady();

    async function run(kind) {
      buttons.forEach(button => button.disabled = true);
      const request = { provider: 'AzureCosmosDBOASIS', orderBy: 'Id', filters };
      let route = '/api/data/query-cql';

      if (kind === 'LongCount') {
        route += '/scalar';
        request.operation = kind;
      } else {
        request.take = 10;
        if (kind === 'projection') {
          route += '/projection';
          request.columns = [{ alias: 'name', field: 'Name' }];
        }
      }

      document.getElementById('request').textContent = route + '\n\n' + JSON.stringify(request, null, 2);
      status.textContent = 'Executing a native Cosmos DB read…';
      try {
        const response = await fetch(route, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: 'Bearer controlled-cloud-cql' },
          body: JSON.stringify(request)
        });
        const body = await response.json();
        document.getElementById('response').textContent = JSON.stringify(body, null, 2);
        status.textContent = 'HTTP ' + response.status;
      } catch (error) {
        status.textContent = String(error);
      } finally {
        buttons.forEach(button => button.disabled = false);
      }
    }

    document.querySelectorAll('[data-query]').forEach(button => {
      button.addEventListener('click', () => run(button.dataset.query));
    });

    document.getElementById('shutdown').addEventListener('click', async () => {
      buttons.forEach(button => button.disabled = true);
      try {
        const response = await fetch('/demo/shutdown', { method: 'POST' });
        if (!response.ok) throw new Error('Shutdown HTTP ' + response.status);
        status.textContent = 'Shutdown requested. Check the runner for verified cleanup.';
      } catch (error) {
        status.textContent = String(error);
        buttons.forEach(button => button.disabled = false);
      }
    });
  </script>
</body>
</html>
""";
}
