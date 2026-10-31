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
