# Hosting a query-only CQL source

## Try the live examples

Open the [WEB4 sandbox](https://web4.oasisomniverse.one/web4-sandbox.html), start a
free demo session and select **CQL Playground**. Choose **ActivityPubOASIS**, then
**Read three public Mastodon posts** or **Filter to two public Mastodon posts**
and press **Run query**. The editor shows the actual JSON request, and the table
shows the native provider and returned records. Public posts can change or disappear;
the displayed count always comes from the real response.

These public-source reads need no Mastodon account or local service. Your WEB4
session stays with WEB4. The separate local teaching demos remain linked below
the hosted controls. MongoDB, SQLite and Neo4j examples query your visible holons;
save your own records in the COSMIC ORM tab before querying an empty account.

## Configure a source

`PREPARED_STORAGE_ADMISSION_JSON` supports an explicit `queryOnlyProviders` list.
Use it for unsigned CQL read sources that must not become ordinary holon storage.
Omitting it preserves existing admission behaviour.

This minimal **read-only admission** example does not configure provider hosting,
authentication, session quotas or native connectivity; those remain host-owned:

```json
{
  "nativeProviders": [],
  "ordinaryProviders": [],
  "queryOnlyProviders": ["ActivityPubOASIS"],
  "queryTariffs": [
    { "provider": "ActivityPubOASIS", "costUsd": 0, "catalogueVersion": "hosted-read-v1" }
  ],
  "storageTariffs": [],
  "routes": [
    {
      "method": "POST",
      "path": "/api/data/query-cql",
      "policy": {
        "meterCategory": "api.request",
        "reservedUnits": 1,
        "estimatedCostUsd": 0,
        "fixedCostUsd": 0,
        "pricingCatalogueVersion": "hosted-read-v1",
        "requiresProviderMeasurement": true,
        "maximumProviderAttempts": 1,
        "maximumNativeFeeCostUsd": 0
      }
    }
  ]
}
```

For a mixed host retain the existing `ordinaryProviders`, write routes and write
tariffs. Add the new read source only to `queryOnlyProviders` and `queryTariffs`.
Do not fabricate write tariffs to satisfy ordinary-provider startup validation.
Every query-only source needs an explicit query tariff; a zero service price still
requires quota admission and provider-attempt measurement.

The lists must be unique, concrete and non-overlapping. Query-only sources cannot
also have native signers or ordinary storage tariffs. The factory captures the
lists, so later mutation of input arrays does not expand permission.

`PreparedStorageExecution` enforces the prohibition at both storage preflight and
physical dispatch, before provider invocation or outbox preparation. It also holds
for SDK construction with optional storage pricing disabled. Ordinary point loads,
writes and replication calls do not inherit permission from CQL-only enrollment.
Existing ordinary-provider permissions remain separate.

This is an unsigned CQL admission contract, not a native chain signing grant or a
declaration of full provider support. Provider registration, adapter capabilities,
read visibility, bounded query presets and deployed live acceptance are still
required. Query tariffs currently accept the existing explicitly supported read
profiles; this setting does not automatically enable every provider enum.

See [ActivityPub host configuration](ACTIVITYPUB_HOST_CONFIGURATION.md).

## Source enrollment and discovery

The WEB4 host registers explicitly query-only sources in the background, without
changing its default storage provider or failover/replication lists. It requires
`ICosmicReadOnlyQueryActivation`: ordinary activation is not used as a substitute,
because some providers provision storage during activation. Verification receives
a linked30-second deadline; the host awaits the provider's owning request.

Authenticated `GET /api/data/query-cql/providers` reports tariff-backed sources,
availability, query-only status, native features and current federation support.
Query-only sources remain unavailable until verification completes, even if an
older registration has its active flag set. Failure reports a redacted source
error; it never switches to another provider. A missing admission factory yields
an empty catalogue, not implicit provider enrollment.

Discovery is a readiness observation, not native-query/deployment acceptance or a
grant to run every native feature through every HTTP operation. The ordinary
three-provider federation contract remains explicit until separately expanded.

## Public ready-to-run examples

Set `CQL_HOSTED_QUERY_EXAMPLES_JSON` to a provider-keyed object of example arrays.
Each entry has `id`, `label` and a bounded row-query `request` whose `provider`
matches that key. [The ActivityPub configuration](../../Scripts/Sandbox/cql-activitypub-public-examples.json)
contains full/subset public-source requests. Its contents are public: never put
credentials, private source keys or private record data into this configuration.

The host captures the JSON, rejects ambiguous/unknown fields and compiles it
without performing native reads. Before publishing examples in discovery, it
plans them against the actual adapter's capabilities. Unsupported examples produce
an explicit unavailable result; no replacement query is generated. Request bodies
remain editable in the browser, and the real authenticated request still receives
the normal visibility, capability, usage and output checks.

The CQL tab discovers sources from the authenticated API and loads their example
bodies through its fixed WEB4 query endpoint. It never forwards the session bearer
to a native provider. Discovery failure disables execution; local developer links
remain separate. Public posts can change or disappear, so actual responses own
the displayed row count. Configuration/planning is not deployed native acceptance.
