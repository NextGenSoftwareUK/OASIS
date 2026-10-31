# Hosting a query-only CQL source

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
