# Local CQL demo address correction — 5 November2026

Invariant: different provider cards must not send users to the same local host
address. Running one demo at a time prevents listener contention, but does not
prevent a card opening another provider's active demo. Fix the allocations.

Live review of the initial QuestDB card exposed its5120overlap with Valkey.
The same inventory identified four overlaps in recent local additions. Preserve
the older providers' addresses and move the recent additions:

| Provider | Conflicting address | Dedicated address | New-address evidence |
|---|---|---|---|
| QuestDB symbols |5120/Valkey|5134|Browser5/4, complete consumer1/1, `interactive-3fe0c37e75ef49ee9843f35d16ba2a94`|
| Meilisearch |5116/DuckDB|5135|Browser full5/provenance/Stop; consumer1/1, `interactive-14258d2da88a402a959c61a4c7ff1f02`|
| Solr |5117/Upstash|5136|Browser full5/provenance/Stop; consumer1/1, `interactive-0435c2021743474ea1afa6b6e92cf277`|
| ArcadeDB |5118/Litestream|5137|Browser full5/provenance/Stop; consumer1/1, `interactive-c0cea86609174244bc4462b9aa74716a`|
| OpenSearch |5119/Memcached|5138|Browser full5/provenance/Stop; consumer1/1, `interactive-c0af47d3e2ff49a9af2e2ad879fba3b7`|

Each phase retains its provider-specific original result/TRX/source-preservation
and shutdown evidence in the admitted runtime cache. No provider reseeding or
native data migration. Exact temporary Mongo fixtures removed after successful
owned checks. Historical evidence at old UI addresses remains historical.
All four scoped consumer rebuilds pass0errors under one provider build lease.
All five corrected consumers pass1/1each with zero skips. Original native source
records remain unchanged and all affected native/Mongo/UI listeners are closed.
`verified-addresses.json` retains the mapping and phase evidence.
Two QuestDB source identity/configuration controls pass.

Website `cql-local-catalogue.test.cjs` reproduces the four remaining clashes
before correction and passes afterward. All113client/catalogue tests pass.
Compact red/green reports and build logs live under
`LocalState/COSMIC/LocalCatalogueAddressCurrent`.

Publication pending: parent task source commit must pin the corrected guides and
QuestDB C# fixture; update all five affected website source links, publish the
scoped website batch, verify Vercel/live bytes and actual card hrefs. Current
published websitebfd1e4b has the initial QuestDB card; corrections are in its
working tree. Do not claim corrected-link publication yet.

Counts remain100local/60hostedHTTP/28hostedbrowser. No hosted API enrollment,
settings changes, new runtime/download or Development deployment.
