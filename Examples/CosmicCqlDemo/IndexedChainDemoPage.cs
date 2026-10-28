namespace CosmicCqlDemo;

/// <summary>Teaching UI for the existing WEB4 indexed routes; it owns no transport or query engine.</summary>
public static class IndexedChainDemoPage
{
    public static string Html => """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>CQL · Solana indexed queries</title>
  <style>
    :root { color-scheme: dark; font-family: system-ui, sans-serif; }
    body { max-width: 1120px; margin: 48px auto; padding: 0 24px; line-height: 1.7; background: #101722; color: #e8edf5; }
    h1, h2 { line-height: 1.3; margin-top: 36px; }
    .scope { padding: 20px 24px; border-left: 4px solid #6cd6bc; background: #192637; }
    .buttons { display: flex; flex-wrap: wrap; gap: 12px; margin: 28px 0; }
    button { font: inherit; padding: 12px 20px; border-radius: 8px; border: 1px solid #6cd6bc; background: #193a39; color: white; cursor: pointer; }
    button:disabled { opacity: .5; cursor: wait; }
    pre { padding: 24px; overflow: auto; background: #080e17; border: 1px solid #344253; border-radius: 10px; line-height: 1.8; tab-size: 4; }
    code { font-family: Consolas, monospace; }
    #status { min-height: 2em; }
    a { color: #8fe7d4; }
  </style>
</head>
<body>
  <h1>CQL over Solana, executed by an explicit SQLite index</h1>
  <p class="scope"><strong>Local validator · declared source range · controlled demo identity/billing.</strong><br>
    These queries use the acknowledged synthetic coffee record. No transaction is submitted.
    Rows and projections validate their source records; cardinality authenticates the index snapshot
    and checks its checkpoint against an independently observed finalized head.
    This is not a hosted-chain, production-authentication or full EF-parity claim.<br>
    This teaching session uses a startup index snapshot with a 500-slot maximum lag.
    Freshness errors remain explicit; ongoing ingestion is a remaining roadmap item.</p>

  <h2>1. Choose a query</h2>
  <p>Each button calls the actual WEB4 controller through its stock reservation and measured-storage pipeline.
    The server owns the actor, source/index binding, freshness grant and tariffs.</p>
  <div class="buttons">
    <button data-query="rows">Load rows</button>
    <button data-query="projection">Select columns</button>
    <button data-query="Count">Count</button>
    <button data-query="LongCount">LongCount</button>
    <button data-query="Any">Any</button>
    <button data-query="empty">Count empty window</button>
  </div>
  <p id="status" role="status">Ready. Select a query to inspect its request and full provenance.</p>

  <h2>2. Inspect the HTTP request</h2>
  <pre id="request">No query sent yet.</pre>

  <h2>3. Read the result and source evidence</h2>
  <p>Check <code>provider</code>, <code>executionProvider</code>, <code>assurance</code>,
    <code>checkpoint</code> and <code>finalizedHead</code>. LongCount values and slots are strings
    so JavaScript preserves their exact integer precision.</p>
  <pre id="response" aria-live="polite">Results appear here.</pre>

  <h2>The equivalent C# pattern</h2>
  <p>The query has an explicit indexed source and actor read grant before it becomes IQueryable.
    See <code>ChainIndexedWalkthrough.cs</code> for the complete runnable setup and evidence terminals.</p>
  <pre><code>// The source/index requirement and read grant are supplied by the server.
var source = CosmicQuery&lt;Holon&gt;
    .Create(CosmicQueryScope.Named(ProviderType.SolanaOASIS).Result)
    .WithIndexedSource(requirement).Result
    .WithReadAccess(access).Result;

// The native index executes the filter, ordering and source window.
var query = CosmicLinq.CreateIndexed(manager, index, observer, source).Result;

var coffees = query
    .Where(holon =&gt; holon.Name == "COSMIC native chain coffee")
    .OrderBy(holon =&gt; holon.Name);

// Omitted Take counts the entire eligible source, rather than a row page.
var count = coffees.LongCount();
var any = coffees.Any();</code></pre>

  <h2>Stop cleanly</h2>
  <p>POST <code>/demo/shutdown</code> to finish the run, preserve its report and stop the owned validator.
    The acknowledged source record, index and keys remain available for reuse.</p>
  <button id="shutdown">Stop demo and validator</button>

  <script>
    const buttons = [...document.querySelectorAll('[data-query]')];
    const status = document.getElementById('status');

    async function run(kind) {
      buttons.forEach(button => button.disabled = true);
      const scalar = !['rows', 'projection'].includes(kind);
      const route = scalar ? '/api/data/query-cql/indexed/scalar' : '/api/data/query-cql/indexed';
      const query = {
        filters: [{ field: 'Name', scalar: 'String', operator: 'Equal', value: 'COSMIC native chain coffee' }],
        orderBy: 'Name'
      };

      if (scalar) query.operation = kind === 'empty' ? 'Count' : kind;
      else query.take = 10;
      if (kind === 'empty') query.take = 0;

      const body = { alias: 'solana-native-demo', query };
      if (kind === 'projection') body.columns = [
        { alias: 'label', field: 'Name' },
        { alias: 'revision', field: 'Version' }
      ];

      document.getElementById('request').textContent = 'POST ' + route + '\n\n' + JSON.stringify(body, null, 2);
      status.textContent = 'Executing native indexed query…';
      try {
        const response = await fetch(route, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer controlled-native-test',
            'Idempotency-Key': 'interactive-' + crypto.randomUUID()
          },
          body: JSON.stringify(body)
        });
        const result = await response.json();
        document.getElementById('response').textContent = JSON.stringify(result, null, 2);
        status.textContent = 'HTTP ' + response.status + ' · settlement: ' + (response.headers.get('X-OASIS-Usage-Settlement') || 'see error');
      } catch (error) {
        status.textContent = 'Query failed: ' + error.message;
      } finally {
        buttons.forEach(button => button.disabled = false);
      }
    }

    buttons.forEach(button => button.addEventListener('click', () => run(button.dataset.query)));
    document.getElementById('shutdown').addEventListener('click', async () => {
      const response = await fetch('/demo/shutdown', { method: 'POST' });
      status.textContent = response.ok ? 'Shutdown requested. Reports will be retained.' : 'Shutdown failed: HTTP ' + response.status;
    });
  </script>
</body>
</html>
""";
}
