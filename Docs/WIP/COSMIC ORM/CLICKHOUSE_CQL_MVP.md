# ClickHouse CQL demo — retained FINAL query scope

Updated 30 October 2026. Scoped local acceptance; full ORM/provider parity,
cross-client conditional writes and HyperDrive acceptance remain incomplete.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicClickHouseLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5129/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both retain ClickHouse/Mongo provider labels.
Finish with the local **Stop demo** button and wait for the native post-demo check,
verified fixture cleanup and runner exit. Start one native demo at a time.

The shared [commented C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects explicit provider scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Each source owns
its authoritative catalogue; requested IDs do not grant access.

## Current verified scope

- Actual open-source ClickHouse26.8.36.3, authenticated HTTPS/private CA on18443;
  original server/database/table UUIDs and3rows retained. Original JSON hashes and
  exact native UInt64 versions unchanged across restart/federation/post-pair check.
- Parameterized UUID IN query on `oasis.holons FINAL`; async ADO open/read/cancel,
  native execution/row/byte limits. FINAL resolves ReplacingMergeTree versions;
  it does not establish conditional CAS, atomic transactions or a shared snapshot.
- At most100 authorized IDs; native/payload identity/access/duplicate checks,
  cumulative4MiB decoded payload budget, stable catalogue order before paging and
  no partial result after stream failure. SDK wire-byte limiting remains unverified.
- Detached save stages caller ID/keys/payload until synchronous parameterized INSERT
  completes (`async_insert=0`). Only then publish saved state/count; batch stop/
  continue/errors/counts covered. Unsupported history mode rejects before writes.
  Current save fault controls inject IO, not full native unknown-outcome recovery.
- Native3/filter2/missing0/page1/budget checks; SDK/HTTP5/4, forged actor/unknown
  preset400, inactive source503 without rows; browser independently returned5/4.
  Unrelated Mongo record excluded. No new business saves or source replacement.
- Exact `cosmic_cql_mongo_clickhouse_demo` identities/collections verified/dropped;
  native container exited0/noOOM/noError, Mongo stopped, pending/ready absent,
 18443/27039/5129 listeners absent. Runner builds Mongo before paired consumer.

## Current evidence

All33 cases passed, zero failures/skips. Paths below are relative to
`C:/Source/OASIS-cosmic/TestResults/ClickHouseCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| clickhouse-current-controls.trx |28|050437DFAE6E05555BAD00082D18D510E0CDFE634A6A34F3998076B10CAFC673|
| native-first.trx |1|1CFBA4A516ADF4B78573DD6127A7EAC9521AA94760909B8477AE40AA842EAF09|
| native-restart.trx |1|70C2731F1F67F268B7CB46FA77125C57AADC22B61E404B85DC5A3B641A8E97FE|
| mongo-init.trx |1|767D9F2CD536505920CBA68FA10FA980868CDA847EF58887891E41406D15F57F|
| mongo-federation.trx |1|0BAF23CEE272EB91438683B45E2FBFAFBFB7032C57FDF23BA56303037B7AC5B7|
| native-postpair.trx |1|77A3E37E7B4CD6064AD46697F74963B0D8AA36B4C343454993B7F9AB8E6F345C|

Browser evidence: `clickhouse-mongo-subset-current.png`. Public CQL section35
main5916e6e / Production7453484627 succeeded with local launch instructions, not
hosted ClickHouse connectivity.
Backend remains task branch; no Railway deployment.

## Retention and remaining work

Retain `/var/tmp/oasis-clickhouse-cql` source/data/private CA/TLS/credentials/ACKs
together. All21 top-level metadata/source and TLS files unchanged; source snapshot
SHA256AE8F4C6873914811BECC6F2399AC9189A8EF2E0A1A40DC4AD763A8C23A0281EF.
Native operational files changed; no whole-directory immutability claim. Prior
completed5TRXs hash-archived; setup helper definitions imported without setup replay.

Canonical JSON is plaintext. Millisecond default versions can tie under concurrent
writes; version ordering/CAS/history/transaction/replication semantics remain open.
General name/description/metadata queries, children/graphs/migrations, unknown-write
recovery, central registration, hosted configuration, SDK wire bounds, full EF/
NHibernate parity and HyperDrive/failover/load-balancing/distributed snapshots need
separate acceptance. [Historical proof](Archive/CLICKHOUSE_CQL_MVP_PRESERVED_2026-10-30.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).
