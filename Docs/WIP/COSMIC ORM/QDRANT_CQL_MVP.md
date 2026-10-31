# Qdrant CQL demo — retained points and payload retrieval

Updated 30 October 2026 (America/Cancun). Scoped local standalone acceptance;
hosted cloud, semantic similarity and full ORM/provider parity remain unverified.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicQdrantLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5131/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both retain Qdrant/Mongo provider labels. Finish
with the local **Stop demo** button and wait for native post-demo verification,
fixture cleanup and runner exit. Start one native demo at a time.

The shared [commented C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects explicit provider scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Each source owns
its authoritative catalogue; requested IDs do not grant access.

## Current verified scope

- Actual Qdrant1.16.3 standalone, authenticated HTTPS/private CA on16333;
  original source inode/raft peer/container/image/mount preserved. Three original
  GUID points/payload JSON/128-dimensional placeholder zero vectors/native collection
  config hash unchanged through actual restart and post-pair verification.
- One native POST retrieves admitted point IDs with payload and `with_vector=false`;
  no scroll/search/full-store predicate fallback. Maximum100 authorized IDs,
 15s deadline/cancel, streamed4MiB body limit before parsing, strict HTTP/native
  status/identity/payload/access/duplicate validation, stable paging and no partial rows.
- Native3/filter2/missing0/page1/budget/schema/receipt checks pass. Controls also
  cover deleted/corrupt rows, cancellation, wire/result limits and failed/queued/
  incomplete acknowledgements. Retained native fixture has3live points.
- Detached save stages caller ID/keys/payload until `wait=true` returns completed
  operation; exact unsigned64 operation ID (including0/max) preserves precision as
  decimal string metadata. Batch continuation/count controls pass. Receipt is not CAS.
- Collection activation distinguishes404 from denied/failed reads, validates
  native vector dimensions/distance and creation acknowledgement; state follows lifecycle.
- SDK/HTTP5/4; forged actor/unknown preset400, inactive source503 without rows;
  browser independently returned5/4. Unrelated Mongo record excluded.
- Exact `cosmic_cql_mongo_qdrant_demo` identities/collections verified/dropped;
  native exited0/noOOM/noError, Mongo stopped, pending/ready absent,
 16333/27039/5131 listeners absent. No business writes/vector rewrites/setup replay.

## Current evidence

All43 cases passed, zero failures/skips. Paths below are relative to
`C:/Source/OASIS-cosmic/TestResults/QdrantCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| qdrant-current-controls.trx |38|00C40C056A19BFF5EA630FEB68149AA95A631523FD98BF6DF8DDA5A7DD386129|
| native-first.trx |1|AE0BE03F340C232BD2DEF4B85E866F65AEB20B47F8C90338BB89FD43AD0027B5|
| native-restart.trx |1|E532EBF60434369642D757FE332BFF5EAA605A0BC54C36A90420C7E95A401F2D|
| mongo-init.trx |1|2CCE77F6822D35AB68DE894732CF64956ECFC048056DC06ABE665CCA4D14E220|
| mongo-federation.trx |1|91E08E8BB2A193D541B3D9B05C7EB55A44028A42AA1C4440C195352BED2DAFF5|
| native-postpair.trx |1|6F1ED1EB1823614F03E36081E7D92D39E9FCA5090C4BF9FA1C194B4585D858D0|

Browser evidence: `qdrant-mongo-subset-current.png`. Public CQL section37 main
aaad860 / Production7453625960 succeeded with local launch instructions, not hosted
Qdrant connectivity.
Backend remains task branch; no Railway deployment.

## Retention and remaining work

Preserve `/var/tmp/oasis-qdrant-cql` points/vectors/config/credentials/CA/TLS/receipts.
All22 top-level metadata/source/TLS files unchanged; original source snapshot SHA256
FE816F279F46A933945FF7F3F7E110CCF9FB4CD7E43EC20531EE1FE27BFE50A7.
Sparse files total482,560,525logicalB but434,176allocatedB at this verification;
logical length is not disk usage. Prior completed5TRXs hash-archived. Removed
95,582,317B of completed private test-bin copies; runner rebuilds integration bin,
unit-test runs rebuild unit bin using retained obj/cache. Source/evidence untouched.

Canonical JSON is plaintext. Atomic conditional writes, history/children/migrations/
replication/unknown-write recovery, general name/description/metadata queries,
central registration, legacy lookup/search/delete HTTP/size audit, semantic search,
hosted/distributed snapshots and full HyperDrive/parity remain separate work.
[Historical proof](Archive/QDRANT_CQL_MVP_PRESERVED_2026-10-30.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).
