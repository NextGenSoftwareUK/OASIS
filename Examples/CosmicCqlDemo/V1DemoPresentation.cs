using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CosmicCqlDemo;

// Presentation for the existing verified native fixture. This file implements
// no query execution, authentication, pricing or provider-selection engine.
public sealed class V1DemoSession
{
    public required string FixtureTag { get; init; }

    public required Guid[] Ids { get; init; }

    public required IReadOnlyDictionary<string, string> Tokens { get; init; }

    public bool Ready { get; set; }

    public TaskCompletionSource Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public static class V1DemoPresentation
{
    public static void Map(WebApplication app, V1DemoSession session)
    {
        app.MapGet("/demo", () => Results.Content(Page, "text/html"));

        // Synthetic fixture tokens exist only for this loopback host's lifetime.
        // Readiness files and transcripts contain no tokens or signing secrets.
        app.MapGet("/demo/session", () => session.Ready
            ? Results.Json(new { session.FixtureTag, session.Ids, session.Tokens })
            : Results.Json(new { message = "Initial verification is running. Reload shortly." }, statusCode: 503));

        app.MapPost("/demo/finish", () =>
        {
            session.Ready = false;
            session.Finished.TrySetResult();
            return Results.Ok(new { message = "Finishing requests, checking settlement and cleaning owned data." });
        });

    }

    private const string Page = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>CQL v1 · Native provider demo</title>
  <style>
    :root { color-scheme: dark; font-family: system-ui, sans-serif; }
    body { max-width: 1200px; margin: 36px auto; padding: 0 24px; line-height: 1.75; background: #101723; color: #e7edf7; }
    h1 { margin-bottom: 4px; } h2 { font-size: 19px; margin-top: 0; }
    p { max-width: 95ch; } a { color: #89c7ff; }
    .card { background: #182334; border: 1px solid #34445b; border-radius: 12px; padding: 24px; margin: 24px 0; }
    .controls { display: flex; flex-wrap: wrap; gap: 24px; align-items: end; }
    label { display: block; font-weight: 600; } select, button { font: inherit; padding: 10px 14px; border-radius: 7px; }
    select { min-width: 170px; } button { cursor: pointer; background: #285d91; color: white; border: 1px solid #7aa5d1; }
    button:disabled { opacity: .5; cursor: wait; } .secondary { background: #253349; }
    .columns { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
    textarea, pre { box-sizing: border-box; width: 100%; font: 14px/1.8 Consolas, monospace; tab-size: 2; background: #0e1622; color: #e5edfa; border: 1px solid #40516a; padding: 20px; border-radius: 8px; }
    textarea { height: 560px; resize: vertical; } pre { height: 560px; margin: 0; overflow: auto; white-space: pre-wrap; overflow-wrap: anywhere; }
    #status { margin: 18px 0; font-weight: 600; } .note { color: #b6c8df; } code { color: #abd9ff; }
    @media(max-width: 850px) { .columns { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <h1>CQL v1 — one query language, three native databases</h1>
  <p class="note">MongoDB · SQLite · Neo4j. Shipping WEB4 query code, real native reads and a real durable Mongo outbox. Synthetic identities and a controlled billing authority; no production account or charge.</p>

  <div class="card">
    <h2>1. Choose a provider, identity and example</h2>
    <div class="controls">
      <label>Native provider<br><select id="provider"><option>MongoDBOASIS</option><option>SQLLiteDBOASIS</option><option>Neo4jOASIS</option><option value="">Current default (SQLite)</option></select></label>
      <label>Authenticated user<br><select id="actor"><option>Alice</option><option>Bob</option><option>Visitor</option><option>Anonymous</option></select></label>
      <label>Walkthrough<br><select id="example">
        <option value="page">Visibility before paging</option><option value="nested">Nested metadata + protected OR</option>
        <option value="strings">Ordinal prefix / suffix</option><option value="null">Explicit null / existence</option>
        <option value="graph">Access-bound child graph</option><option value="precision">Exact Int64 beyond 2^53</option><option value="guid">Typed GUID metadata</option>
        <option value="types">Numeric strings are not integers</option><option value="boolean">Typed Boolean metadata</option><option value="invalid">Unsupported provider rejected</option>
      </select></label>
    </div>
    <p id="explanation"></p>
    <p class="note">Alice owns <code>B-owned</code> and <code>D-owned</code>. Bob owns <code>A-index</code>, <code>A-private</code> and globally public <code>C-public</code>. Visitor sees only C-public. Sharing-index metadata does not grant access.</p>
  </div>

  <div class="controls">
    <button id="run" disabled>Run new query</button><button id="repeat" class="secondary" disabled>Repeat last operation key</button>
    <a href="/swagger" target="_blank" rel="noopener">Open Swagger</a>
    <button id="finish" class="secondary">Finish demo &amp; clean up</button>
  </div>
  <p id="status" role="status">Loading the synthetic session…</p>
  <div class="columns">
    <section class="card"><h2>2. Edit the v1 JSON request</h2><p>POST <code>/api/data/query-cql</code></p><textarea id="query" spellcheck="false" aria-label="CQL JSON request"></textarea></section>
    <section class="card"><h2>3. Inspect the result and settlement</h2><p>Visibility is enforced inside the native plan before sorting and paging.</p><pre id="result">Choose an example, then run it.</pre></section>
  </div>
  <p class="note">Each new intent gets an idempotency key. Repeat demonstrates HTTP 409 without another provider execution. The JSON text is sent intact, preserving exact Int64 literals. Use Finish to stop the host and clean only its owned fixture.</p>

<script>
let session, last;
const el = id => document.getElementById(id);
const notes = {
  graph: 'The server applies owner/public access to every child. Alice sees public + owned children; the private branch and its public grandchild are omitted. Parent IDs/role manifests outside the selected view are not public output.',
  page: 'Alice: Skip 1 / Take 1 returns C-public. Her private-inaccessible rows are removed BEFORE paging. Switch provider without changing query semantics.',
  nested: 'London AND stock >= 7 AND (A-private OR D-owned). Alice receives only D-owned: OR cannot escape the server access predicate.',
  strings: 'Ordinal StartsWith("B-") AND EndsWith("owned"). Alice receives B-owned. No culture-dependent evaluation.',
  null: 'IsNull matches a present null member. Exists also matches present null; missing paths satisfy neither. Try changing the operator/path.',
  precision: '9007199254740993 is preserved as an exact Int64. The client sends the original text without converting it to JavaScript Number.',
  guid: 'GUID metadata accepts canonical D literals with GUID case normalization. Alice receives B-owned.',
  types: 'mixedInteger contains the string "7" on D-owned. Integer equality returns B-owned and C-public, excluding the numeric string.',
  boolean: 'Boolean equality is typed. enabled=true returns B-owned, C-public and D-owned to Alice.',
  invalid: 'HTTP v1 chooses one eligible concrete provider. All is deliberately rejected before any provider query.'
};

function preset() {
  if (!session) return;
  const name = el('example').value;
  const body = { provider: el('provider').value, filters: [], orderBy: 'Name', take: 10 };
  const metadata = (path, scalar, operator, value) => ({ metadataPath: path, scalar, operator, value });
  body.filters.push(metadata(['fixture'], 'String', 'Equal', session.fixtureTag));
  if (name === 'graph') {
    body.filters.push({ field: 'Name', operator: 'Equal', value: 'C-public' });
    body.take = 1;
    body.graph = { recursive: true, maxChildDepth: 2, childType: 'All', maximumNodes: 8, maximumPayloadBytes: 1048576, consistency: 'ProviderDefault' };
  }
  if (name === 'page') { body.skip = 1; body.take = 1; }
  if (name === 'nested') {
    body.filters.push(metadata(['location', 'city'], 'String', 'Equal', 'London'), metadata(['stock'], 'Integer', 'GreaterThanOrEqual', 7));
    body.anyOf = [{ field: 'Name', operator: 'Equal', value: 'A-private' }, { field: 'Name', operator: 'Equal', value: 'D-owned' }];
  }
  if (name === 'strings') body.filters.push({ field: 'Name', operator: 'StartsWith', value: 'B-' }, { field: 'Name', operator: 'EndsWith', value: 'owned' });
  if (name === 'null') body.filters.push({ metadataPath: ['nullable'], operator: 'IsNull' });
  if (name === 'precision') body.filters.push(metadata(['precision'], 'Integer', 'Equal', '__EXACT_INT64__'));
  if (name === 'guid') body.filters.push(metadata(['guid'], 'Guid', 'Equal', session.ids[2].toUpperCase()));
  if (name === 'types') body.filters.push(metadata(['mixedInteger'], 'Integer', 'Equal', 7));
  if (name === 'boolean') body.filters.push(metadata(['enabled'], 'Boolean', 'Equal', true));
  if (!body.provider) delete body.provider;
  if (name === 'invalid') body.provider = 'All';
  el('query').value = JSON.stringify(body, null, 2).replace('"__EXACT_INT64__"', '9007199254740993');
  el('explanation').textContent = notes[name];
}

// Format JSON lexically. Parsing numbers and reserializing would round Int64 data.
function pretty(text) {
  let result = '', depth = 0, quoted = false, escaped = false;
  const newline = () => '\n' + '  '.repeat(depth);
  for (const c of text) {
    if (quoted) { result += c; if (escaped) escaped = false; else if (c === '\\') escaped = true; else if (c === '"') quoted = false; continue; }
    if (c === '"') { quoted = true; result += c; }
    else if (c === '{' || c === '[') { depth++; result += c + newline(); }
    else if (c === '}' || c === ']') { depth--; result += newline() + c; }
    else if (c === ',') result += ',' + newline();
    else if (c === ':') result += ': ';
    else if (!/\s/.test(c)) result += c;
  }
  return result;
}

async function run(repeat) {
  const operation = repeat ? last : { body: el('query').value, key: crypto.randomUUID(), actor: el('actor').value };
  if (!operation) return;
  const headers = { 'Content-Type': 'application/json', 'Idempotency-Key': operation.key };
  if (session.tokens[operation.actor]) headers.Authorization = 'Bearer ' + session.tokens[operation.actor];
  for (const id of ['run', 'repeat', 'finish']) el(id).disabled = true;
  el('status').textContent = 'Running native query…';
  try {
    const response = await fetch('/api/data/query-cql', { method: 'POST', headers, body: operation.body });
    last = operation;
    el('result').textContent = pretty(await response.text());
    el('status').textContent = 'HTTP ' + response.status + ' · settlement: ' + (response.headers.get('X-OASIS-Usage-Settlement') || 'not settled by this response') + ' · key: ' + operation.key;
  } catch (error) { el('status').textContent = 'Outcome uncertain: ' + error.message + '. Retain key ' + operation.key; last = operation; }
  finally { for (const id of ['run', 'repeat', 'finish']) el(id).disabled = false; }
}

el('provider').onchange = preset;
el('example').onchange = preset;
el('run').onclick = () => run(false);
el('repeat').onclick = () => run(true);
el('finish').onclick = async () => {
  for (const id of ['run', 'repeat', 'finish']) el(id).disabled = true;
  await fetch('/demo/finish', { method: 'POST' });
  el('status').textContent = 'Demo finished. The runner verifies settlement and removes its owned fixture.';
};
fetch('/demo/session').then(async response => {
  if (!response.ok) throw new Error((await response.json()).message);
  session = await response.json(); preset(); el('run').disabled = false;
  el('status').textContent = 'Ready. Choose MongoDB, SQLite or Neo4j and run a query.';
}).catch(error => { el('status').textContent = error.message; });
</script>
</body>
</html>
""";
}
