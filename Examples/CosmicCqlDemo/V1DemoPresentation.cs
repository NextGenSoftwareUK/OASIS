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
      <label>Native provider<br><select id="provider"><option>MongoDBOASIS</option>
        <option>SQLLiteDBOASIS</option>
        <option>Neo4jOASIS</option>
        <option value="">Current default (SQLite)</option></select></label>
      <label>Authenticated user<br><select id="actor"><option>Alice</option>
        <option>Bob</option>
        <option>Visitor</option>
        <option>Anonymous</option></select></label>
      <label>Walkthrough<br><select id="example">
        <option value="min">Min / exact selected value</option>
        <option value="max">Max / protected reduction</option>
        <option value="sum">Sum / exact signed total</option>
        <option value="average">Average / Decimal fraction</option>
        <option value="unsignedSum">UInt64 Sum / exact unsigned total</option>
        <option value="unsignedAverage">UInt64 Average / exact Decimal fraction</option>
        <option value="unsignedMeanGroups">UInt64 groups / filter exact Decimal means</option>
        <option value="decimalSum">Decimal Sum / exact price total</option>
        <option value="decimalAverage">Decimal Average / final ties-to-even rounding</option>
        <option value="decimalNumericGroups">Decimal groups / exact totals and rounded means</option>
        <option value="decimalSource">Decimal / exact price range</option>
        <option value="decimalGroups">Decimal / grouped prices</option>
        <option value="doubleSum">Double Sum / round once</option>
        <option value="doubleAverage">Double nullable Average / ties to even</option>
        <option value="doubleNumericGroups">Double groups / rounded totals and means</option>
        <option value="singleSum">Single Sum / round once</option>
        <option value="singleAverage">Single nullable Average / ties to even</option>
        <option value="singleNumericGroups">Single groups / rounded totals and means</option>
        <option value="doubleSource">Double / exact reading range</option>
        <option value="doubleGroups">Double / grouped readings</option>
        <option value="singleSource">Single / binary32 reading range</option>
        <option value="singleGroups">Single / grouped binary32 readings</option>
        <option value="unsignedSource">UInt64 / exact source range</option>
        <option value="unsignedGroups">UInt64 / grouped keys</option>
        <option value="utcSource">UTC dates / source range</option>
        <option value="utcGroups">UTC dates / grouped keys</option>
        <option value="compositionFilter">Composition / filter after source page</option>
        <option value="compositionTake">Composition / page before filter</option>
        <option value="compositionExact">Composition / exact values and result page</option>
        <option value="leftJoin">Left join / unmatched rows and exact values</option>
        <option value="innerJoin">Inner join / matching pairs only</option>
        <option value="groupedAverage">GroupBy / filter exact Decimal means</option>
        <option value="groupedFilter">GroupBy / filter exact totals</option>
        <option value="groupedPage">GroupBy / independent result page</option>
        <option value="groupedOrder">GroupBy / ordered exact totals</option>
        <option value="grouped">GroupBy / exact multiple aggregates</option>
        <option value="projection">Select / detached typed columns</option>
        <option value="longCount">LongCount / complete visible input</option>
        <option value="count">Count / explicit window</option>
        <option value="any">Any / private root</option>
        <option value="page">Visibility before paging</option>
        <option value="nested">Nested metadata + protected OR</option>
        <option value="strings">Ordinal prefix / suffix</option>
        <option value="null">Explicit null / existence</option>
        <option value="children">Visible child existence</option>
        <option value="descendants">Bounded descendants</option>
        <option value="graph">Access-bound child graph</option>
        <option value="precision">Exact Int64 beyond 2^53</option>
        <option value="guid">Typed GUID metadata</option>
        <option value="types">Numeric strings are not integers</option>
        <option value="boolean">Typed Boolean metadata</option>
        <option value="invalid">Unsupported provider rejected</option>
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
    <section class="card"><h2>2. Edit the v1 JSON request</h2><p>POST <code id="route">/api/data/query-cql</code></p><textarea id="query" spellcheck="false" aria-label="CQL JSON request"></textarea></section>
    <section class="card"><h2>3. Inspect the result and settlement</h2><p>Visibility is enforced inside the native plan before sorting and paging.</p><pre id="result">Choose an example, then run it.</pre></section>
  </div>
  <p class="note">Each new intent gets an idempotency key. Repeat demonstrates HTTP 409 without another provider execution. The JSON text is sent intact, preserving exact Int64 literals. Use Finish to stop the host and clean only its owned fixture.</p>

<script>
let session, last;
let queryRoute = "/api/data/query-cql";
const el = id => document.getElementById(id);
const notes = {
  compositionFilter: 'The source page is C-public and D-owned. Filtering the selected Name removes C-public, so D-owned remains. Every selected source cell is validated before any row is removed.',
  compositionTake: 'The same source page is used, but Take(1) runs before Where. C-public is selected then removed: no rows remain. Reordering these stages would change the answer.',
  compositionExact: 'Compare the selected Precision alias to exact Int64 text, order selected names descending, then Skip(1)/Take(1). C-public remains; no JavaScript numeric rounding occurs.',
  leftJoin: 'Join two independently filtered source windows in the selected provider. A left join keeps B-owned and D-owned with hasRight=false and null precision; C-public matches. Both inputs enforce your signed actor. Int64 values remain exact strings.',
  innerJoin: 'Use the same two source windows with an inner join: only C-public matches. Matching happens natively in the selected provider. Limits reject excess work rather than silently truncate results.',

  min: 'Native Min over typed Int64 metadata. Hidden roots contain long.MinValue/long.MaxValue; visibility excludes them for Alice and Visitor before reduction. Exact values remain decimal strings.',
  sum: 'Native Sum over the complete visible input. Int64 output remains exact decimal text; no browser Number conversion.',
  decimalSum: 'Sum the first two visible prices exactly. Source Take runs before the native reduction; the tagged Decimal response preserves the final 10^-28 digit.',
  decimalAverage: 'The mean is halfway between adjacent scale28 Decimal values. Round once at publication using ties-to-even; no source price is converted to binary floating point.',
  decimalNumericGroups: 'Group the first two visible prices, then filter/order their published Decimal total and mean. Exact filter values are JSON strings; resultTake pages groups independently.',
  unsignedSum: 'Sum keeps UInt64 as tagged decimal text. Take1 selects a representable total; summing multiple near-maximum counters correctly returns overflow.',
  unsignedAverage: 'Two visible UInt64 counters produce an exact Decimal half-unit beyond Int64 and browser Number precision. The response stays text.',
  unsignedMeanGroups: 'Choose two source rows before grouping by stock, then filter/order the exact Decimal mean and take a result group. Source take and resultTake have different roles.',
  average: 'Native Average over the first two ordered names. Int64 input returns an exact Decimal fraction as text; the half-unit survives beyond binary64 precision.',
  max: 'Native Max uses the same typed selector and protected source. Choose a provider without changing query semantics; no client entity scan is used.',
  longCount: 'Native LongCount over the complete visible source: Alice 3, Bob 3, Visitor 1. Omitted take has no row-page cap. Int64 values are always decimal strings; keep them as text or BigInt.',
  count: 'Count after Skip 1 / Take 1: Alice and Bob receive Int32 1, Visitor 0. Source windows apply before the terminal.',
  decimalSource: 'Exact Decimal prices differ by 10^-28. Both request bounds and selected prices are strings; source rows are ordered by Name.',
  decimalGroups: 'Group exact Decimal prices, then filter/order selected Price aliases before paging. No floating-point conversion is used.',
  doubleSum: 'Reduce two visible Double readings exactly and round once; nullable Average retains its floating type. Source and result pages are independent.',
  doubleAverage: 'Reduce two visible Double readings exactly and round once; nullable Average retains its floating type. Source and result pages are independent.',
  doubleNumericGroups: 'Reduce two visible Double readings exactly and round once; nullable Average retains its floating type. Source and result pages are independent.',
  singleSum: 'Reduce two visible Single readings exactly and round once; nullable Average retains its floating type. Source and result pages are independent.',
  singleAverage: 'Reduce two visible Single readings exactly and round once; nullable Average retains its floating type. Source and result pages are independent.',
  singleNumericGroups: 'Reduce two visible Single readings exactly and round once; nullable Average retains its floating type. Source and result pages are independent.',
  doubleSource: 'Exact Double readings differ by one binary64 unit. Both request bounds and selected readings are strings; source rows are ordered by Name.',
  doubleGroups: 'Group exact Double readings, then filter/order selected Reading aliases before paging. Values retain their declared binary64 meaning.',
  singleSource: 'Declare Single for binary32 readings. Canonical strings retain their Single precision through filtering and selected output.',
  singleGroups: 'Group binary32 readings and filter/order the selected alias before paging. Use Single when that is the stored domain.',
  unsignedSource: 'UInt64 inputs and outputs stay digit strings. These counters exceed Int64 and JavaScript integer precision; the source projection orders by Name.',
  unsignedGroups: 'Filter exact UInt64 aliases after grouping, then order/page them natively. No Number() conversion or floating-point key is used.',
  utcSource: 'Declare DateTime source predicates with uppercase Z and0..7 fractional digits. These adjacent100ns values retain UTC kind and exact date text; offset/local/over-precision inputs are rejected.',
  utcGroups: 'Group exact UTC timestamps, then filter selected date aliases before ordering/paging. DateTime source and result literals share the same validated UTC parser.',
  groupedAverage: 'Average filters and ordering compare the normalized Decimal published by every provider. Source take2 selects participation; result filters/order/take apply afterward. Decimal thresholds use strings to preserve a half-unit fraction that binary64 loses at this magnitude.',
  groupedFilter: 'Filter selected aggregate aliases before ordering and paging. Int64 result-filter literals use strings to preserve precision; only matching groups consume resultSkip.',
  groupedPage: 'Page ordered aggregate results with resultSkip/resultTake. Complete off-page proof remains validated; maximumGroups bounds all reduced proof, not just the published page.',
  groupedOrder: 'Group visible rows by name, then order selected exact sums descending. resultOrder names output aliases; source ordering remains independent. Adjacent Int64 totals retain precision.',
  grouped: 'Group the first two ordered visible rows by stock. Counts, exact Int64 Sum and Decimal Average reduce natively. Output group limits do not truncate input.',
  projection: 'Select returns ordered column schema and value arrays. Int64 is exact decimal text; nullable metadata stays null. The selected values are detached, with visibility applied before the native page.',
  any: 'Any returns a Boolean. A-private belongs to Bob: Alice and Visitor receive false, Bob true. The query cannot bypass owner/public visibility.',
  children: 'Two independent native EXISTS conditions run before the root page. Alice matches C-public via public and owned siblings. Visitor cannot match the owned child. Private SHARED_WITH metadata grants no access.',
  graph: 'The server applies owner/public access to every child. Alice sees public + owned children; the private branch and its public grandchild are omitted. Parent IDs/role manifests outside the selected view are not public output.',
  page: 'Alice: Skip 1 / Take 1 returns C-public. Her private-inaccessible rows are removed BEFORE paging. Switch provider without changing query semantics.',
  nested: 'London AND stock >= 7 AND (A-private OR D-owned). Alice receives only D-owned: OR cannot escape the server access predicate.',
  strings: 'Ordinal StartsWith("B-") AND EndsWith("owned"). Alice receives B-owned. No culture-dependent evaluation.',
  null: 'IsNull matches a present null member. Exists also matches present null; missing paths satisfy neither. Try changing the operator/path.',
  descendants: 'Bounded depth2/nodes2; no graph output. Private intermediates prune branches before matching or counting.',
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
  if (name === 'children') {
    body.anyChildren = [
      { filters: [{ field: 'Name', operator: 'Equal', value: 'public child' }] },
      { filters: [{ field: 'Name', operator: 'Equal', value: 'owned child' }] }
    ];
    body.take = 1;
  }
  if (name === 'descendants') {
    body.anyDescendants = [{ maximumDepth: 2, maximumNodes: 2, filters: [{ field: 'Name', operator: 'Equal', value: 'owned child' }] }];
    body.take = 1;
  }
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
  // Scalar intent has its own transport. Preserve the selected route with the
  // operation key, so repeating a prior request cannot dispatch to another API.
  queryRoute = '/api/data/query-cql';
  if (['longCount', 'count', 'any'].includes(name)) {
    queryRoute += '/scalar';
    body.operation = name === 'longCount' ? 'LongCount' : name === 'count' ? 'Count' : 'Any';
    delete body.take;
    if (name === 'count') { body.skip = 1; body.take = 1; }
    if (name === 'any') body.filters.push({ field: 'Name', operator: 'Equal', value: 'A-private' });
  }
  if (['min', 'max', 'sum', 'average'].includes(name)) {
    queryRoute += '/scalar';
    body.operation = { min: 'Min', max: 'Max', sum: 'Sum', average: 'Average' }[name];
    body.selection = { metadataPath: ['extremum'], valueType: 'Int64' };
    delete body.take;
    if (name === 'average') { body.orderBy = 'Name'; body.take = 2; }
  }
  if (name === 'decimalSum' || name === 'decimalAverage') {
    queryRoute += '/scalar';
    body.operation = name === 'decimalSum' ? 'Sum' : 'Average';
    body.selection = { metadataPath: ['price'], valueType: 'Decimal' };
    body.orderBy = 'Name';
    body.take = 2;
  }
  if (name === 'decimalNumericGroups') {
    queryRoute += '/grouped';
    body.orderBy = 'Name';
    body.take = 2;
    body.keys = [{ alias: 'stock', metadataPath: ['stock'], valueType: 'Int64' }];
    body.columns = [
      { alias: 'Total', operation: 'Sum', selection: { metadataPath: ['price'], valueType: 'Decimal' } },
      { alias: 'Mean', operation: 'Average', selection: { metadataPath: ['price'], valueType: 'Decimal' } }
    ];
    body.resultFilters = [
      { alias: 'Total', operator: 'Equal', value: '2.0000000000000000000000000005' },
      { alias: 'Mean', operator: 'Equal', value: '1.0000000000000000000000000002' }
    ];
    body.resultOrder = [{ alias: 'Total', descending: true }];
    body.resultTake = 1;
  }
  if (name === 'unsignedSum' || name === 'unsignedAverage') {
    queryRoute += '/scalar';
    body.operation = name === 'unsignedSum' ? 'Sum' : 'Average';
    body.selection = { metadataPath: ['counter'], valueType: 'UInt64' };
    body.orderBy = 'Name';
    body.take = name === 'unsignedSum' ? 1 : 2;
  }
  if (name === 'unsignedMeanGroups') {
    queryRoute += '/grouped';
    body.orderBy = 'Name';
    body.take = 2;
    body.keys = [{ alias: 'stock', metadataPath: ['stock'], valueType: 'Int64' }];
    body.columns = [{ alias: 'Mean', operation: 'Average', selection: { metadataPath: ['counter'], valueType: 'UInt64' } }];
    body.resultFilters = [{ alias: 'Mean', operator: 'Equal', value: '18446744073709551612.5' }];
    body.resultOrder = [{ alias: 'Mean', descending: true }];
    body.resultTake = 1;
  }
  if (name === 'projection') {
    queryRoute += '/projection';
    body.take = 3;
    body.columns = [
      { alias: 'label', field: 'Name' },
      { alias: 'precision', metadataPath: ['precision'], valueType: 'Int64' },
      { alias: 'optional', metadataPath: ['nullable'], valueType: 'Int64', nullable: true },
      { alias: 'city', metadataPath: ['location', 'city'], valueType: 'String' }
    ];
  }
  if (name === 'unsignedSource') {
    queryRoute += '/projection';
    body.take = 2;
    body.filters.push(
      metadata(['counter'], 'UInt64', 'GreaterThanOrEqual', '18446744073709551612'),
      metadata(['counter'], 'UInt64', 'LessThan', '18446744073709551614')
    );
    body.columns = [
      { alias: 'Name', field: 'Name' },
      { alias: 'Counter', metadataPath: ['counter'], valueType: 'UInt64' }
    ];
  }
  if (name === 'unsignedGroups') {
    queryRoute += '/grouped';
    delete body.take;
    body.maximumGroups = 10;
    body.keys = [{ alias: 'counter', metadataPath: ['counter'], valueType: 'UInt64' }];
    body.columns = [{ alias: 'Counter', keyAlias: 'counter' }, { alias: 'Items', operation: 'LongCount' }];
    body.resultFilters = [
      { alias: 'Counter', operator: 'GreaterThanOrEqual', value: '18446744073709551612' },
      { alias: 'Counter', operator: 'LessThan', value: '18446744073709551614' }
    ];
    body.resultOrder = [{ alias: 'Counter', descending: false }];
    body.resultTake = 2;
  }
  if (name === 'decimalSource') {
    queryRoute += '/projection';
    body.take = 2;
    body.filters.push(
      metadata(['price'], 'Decimal', 'GreaterThanOrEqual', '1.0000000000000000000000000002'),
      metadata(['price'], 'Decimal', 'LessThan', '1.0000000000000000000000000004')
    );
    body.columns = [
      { alias: 'Name', field: 'Name' },
      { alias: 'Price', metadataPath: ['price'], valueType: 'Decimal' }
    ];
  }
  if (name === 'decimalGroups') {
    queryRoute += '/grouped';
    delete body.take;
    body.maximumGroups = 10;
    body.keys = [{ alias: 'price', metadataPath: ['price'], valueType: 'Decimal' }];
    body.columns = [{ alias: 'Price', keyAlias: 'price' }, { alias: 'Items', operation: 'LongCount' }];
    body.resultFilters = [
      { alias: 'Price', operator: 'GreaterThanOrEqual', value: '1.0000000000000000000000000002' },
      { alias: 'Price', operator: 'LessThan', value: '1.0000000000000000000000000004' }
    ];
    body.resultOrder = [{ alias: 'Price', descending: false }];
    body.resultTake = 2;
  }
  // Floating aggregate inputs retain their declared binary domain. Keep
  // selected-result thresholds as canonical strings through the JSON editor.
  if (['doubleSum', 'doubleAverage', 'doubleNumericGroups',
       'singleSum', 'singleAverage', 'singleNumericGroups'].includes(name)) {
    const single = name.startsWith('single');
    const valueType = single ? 'Single' : 'Double';
    const metadataPath = [single ? 'singleReading' : 'reading'];
    const grouped = name.endsWith('NumericGroups');
    body.take = 2; // Source window: the first two visible readings by Name.

    if (grouped) {
      queryRoute += '/grouped';
      body.keys = [{ alias: 'stock', metadataPath: ['stock'], valueType: 'Int64' }];
      body.columns = [
        { alias: 'Total', operation: 'Sum', selection: { metadataPath, valueType, nullable: false } },
        { alias: 'Mean', operation: 'Average', selection: { metadataPath, valueType, nullable: true } }
      ];
      body.resultFilters = [
        { alias: 'Total', operator: 'Equal', value: single ? '2.0000005' : '2.000000000000001' },
        { alias: 'Mean', operator: 'Equal', value: single ? '1.0000002' : '1.0000000000000004' }
      ];
      body.resultOrder = [{ alias: 'Mean', descending: true }];
      body.resultTake = 1; // Result page: after grouping and selected filters.
    } else {
      queryRoute += '/scalar';
      body.operation = name.endsWith('Sum') ? 'Sum' : 'Average';
      body.selection = { metadataPath, valueType, nullable: body.operation === 'Average' };
    }
  }
  if (name === 'doubleSource') {
    queryRoute += '/projection';
    body.take = 2;
    body.filters.push(
      metadata(['reading'], 'Double', 'GreaterThanOrEqual', '1.0000000000000004'),
      metadata(['reading'], 'Double', 'LessThan', '1.0000000000000009')
    );
    body.columns = [
      { alias: 'Name', field: 'Name' },
      { alias: 'Reading', metadataPath: ['reading'], valueType: 'Double' }
    ];
  }
  if (name === 'doubleGroups') {
    queryRoute += '/grouped';
    delete body.take;
    body.maximumGroups = 10;
    body.keys = [{ alias: 'reading', metadataPath: ['reading'], valueType: 'Double' }];
    body.columns = [{ alias: 'Reading', keyAlias: 'reading' }, { alias: 'Items', operation: 'LongCount' }];
    body.resultFilters = [
      { alias: 'Reading', operator: 'GreaterThanOrEqual', value: '1.0000000000000004' },
      { alias: 'Reading', operator: 'LessThan', value: '1.0000000000000009' }
    ];
    body.resultOrder = [{ alias: 'Reading', descending: false }];
    body.resultTake = 2;
  }
  if (name === 'singleSource') {
    queryRoute += '/projection';
    body.take = 2;
    body.filters.push(
      metadata(['singleReading'], 'Single', 'GreaterThanOrEqual', '1.0000002'),
      metadata(['singleReading'], 'Single', 'LessThan', '1.0000005')
    );
    body.columns = [
      { alias: 'Name', field: 'Name' },
      { alias: 'Reading', metadataPath: ['singleReading'], valueType: 'Single' }
    ];
  }
  if (name === 'singleGroups') {
    queryRoute += '/grouped';
    delete body.take;
    body.maximumGroups = 10;
    body.keys = [{ alias: 'reading', metadataPath: ['singleReading'], valueType: 'Single' }];
    body.columns = [{ alias: 'Reading', keyAlias: 'reading' }, { alias: 'Items', operation: 'LongCount' }];
    body.resultFilters = [
      { alias: 'Reading', operator: 'GreaterThanOrEqual', value: '1.0000002' },
      { alias: 'Reading', operator: 'LessThan', value: '1.0000005' }
    ];
    body.resultOrder = [{ alias: 'Reading', descending: false }];
    body.resultTake = 2;
  }
  if (name === 'utcSource') {
    queryRoute += '/projection';
    body.take = 2;
    body.filters.push(
      metadata(['observedAt'], 'DateTime', 'GreaterThanOrEqual', '2026-10-24T00:00:00.0000002Z'),
      metadata(['observedAt'], 'DateTime', 'LessThan', '2026-10-24T00:00:00.0000004Z')
    );
    body.columns = [
      { alias: 'Name', field: 'Name' },
      { alias: 'ObservedAt', metadataPath: ['observedAt'], valueType: 'DateTime' }
    ];
  }
  if (name === 'utcGroups') {
    queryRoute += '/grouped';
    delete body.take;
    body.maximumGroups = 10;
    body.keys = [{ alias: 'observed', metadataPath: ['observedAt'], valueType: 'DateTime' }];
    body.columns = [{ alias: 'ObservedAt', keyAlias: 'observed' }, { alias: 'Items', operation: 'LongCount' }];
    body.resultFilters = [
      { alias: 'ObservedAt', operator: 'GreaterThanOrEqual', value: '2026-10-24T00:00:00.0000002Z' },
      { alias: 'ObservedAt', operator: 'LessThan', value: '2026-10-24T00:00:00.0000004Z' }
    ];
    body.resultOrder = [{ alias: 'ObservedAt', descending: false }];
    body.resultTake = 2;
  }
  if (name === 'grouped' || name === 'groupedAverage') {
    queryRoute += '/grouped';
    body.take = 2;
    body.maximumGroups = 10;
    body.keys = [{ alias: 'stock', metadataPath: ['stock'], valueType: 'Int64' }];
    body.columns = [
      { alias: 'Stock', keyAlias: 'stock' },
      { alias: 'Items', operation: 'LongCount' },
      { alias: 'Total', operation: 'Sum', selection: { metadataPath: ['extremum'], valueType: 'Int64' } },
      { alias: 'Mean', operation: 'Average', selection: { metadataPath: ['extremum'], valueType: 'Int64' } }
    ];
  }
  if (name === 'groupedAverage') {
    body.resultFilters = [
      { alias: 'Mean', operator: 'GreaterThanOrEqual', value: '-9007199254740990.5' },
      { alias: 'Mean', operator: 'LessThan', value: '-9007199254740990' }
    ];
    body.resultOrder = [{ alias: 'Mean', descending: false }];
    body.resultTake = 1;
  }
  if (name === 'groupedOrder' || name === 'groupedPage' || name === 'groupedFilter') {
    queryRoute += '/grouped';
    body.maximumGroups = 10;
    body.keys = [{ alias: 'name', field: 'Name' }];
    body.columns = [
      { alias: 'Name', keyAlias: 'name' },
      { alias: 'Total', operation: 'Sum', selection: { metadataPath: ['extremum'], valueType: 'Int64' } }
    ];
    body.resultOrder = [{ alias: 'Total', descending: true }, { alias: 'Name' }];
    if (name === 'groupedPage' || name === 'groupedFilter') { body.resultSkip = 1; body.resultTake = 1; }
    if (name === 'groupedFilter') {
      body.resultFilters = [{ alias: 'Total', operator: 'GreaterThan', value: '-9007199254740991' }];
    }
  }
  if (name === 'leftJoin' || name === 'innerJoin') {
    const provider = body.provider;
    const filters = body.filters;
    for (const key of Object.keys(body)) delete body[key];

    queryRoute += '/join';
    Object.assign(body, {
      provider,
      kind: name === 'leftJoin' ? 'Left' : 'Inner',
      left: { filters },
      right: { filters: [...filters, { field: 'Name', operator: 'Equal', value: 'C-public' }] },
      keys: [{ alias: 'name', left: { field: 'Name' }, right: { field: 'Name' } }],
      columns: [
        { alias: 'name', source: 'Left', selection: { field: 'Name' } },
        { alias: 'precision', source: 'Right', allowMissing: true, selection: { metadataPath: ['precision'], valueType: 'Int64' } },
        { alias: 'present', source: 'RightPresence' }
      ],
      maximumLeftRows: 100,
      maximumRightRows: 100,
      maximumPairs: 1000,
      maximumResults: 25
    });
  }
  if (name === 'compositionFilter' || name === 'compositionTake' || name === 'compositionExact') {
    const provider = body.provider;
    const filters = body.filters;
    for (const key of Object.keys(body)) delete body[key];

    queryRoute += '/composition';
    const where = { operation: 'Where', filters: [{ alias: 'Name', operator: 'NotEqual', value: 'C-public' }] };
    const take = { operation: 'Take', count: 1 };
    const stages = name === 'compositionTake' ? [take, where] : name === 'compositionExact' ? [
      { operation: 'Where', filters: [{ alias: 'Precision', operator: 'Equal', value: '9007199254740993' }] },
      { operation: 'OrderBy', order: [{ alias: 'Name', descending: true }] },
      { operation: 'Skip', count: 1 }, take
    ] : [where, { operation: 'OrderBy', order: [{ alias: 'Precision', descending: true }, { alias: 'Name' }] }, take];

    Object.assign(body, {
      provider,
      source: {
        filters, orderBy: 'Name', skip: 1, take: 2,
        columns: [ { alias: 'Name', field: 'Name' },
          { alias: 'Precision', metadataPath: ['precision'], valueType: 'Int64' } ]
      },
      stages
    });
  }
  el('route').textContent = queryRoute;
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
  const operation = repeat ? last : { body: el('query').value, key: crypto.randomUUID(), actor: el('actor').value, path: queryRoute };
  if (!operation) return;
  const headers = { 'Content-Type': 'application/json', 'Idempotency-Key': operation.key };
  if (session.tokens[operation.actor]) headers.Authorization = 'Bearer ' + session.tokens[operation.actor];
  for (const id of ['run', 'repeat', 'finish']) el(id).disabled = true;
  el('status').textContent = 'Running native query…';
  try {
    const response = await fetch(operation.path, { method: 'POST', headers, body: operation.body });
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
