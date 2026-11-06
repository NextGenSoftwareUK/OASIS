# Couchbase CQL demo — retained JSON and exact CAS

## Source recovery — 2 November 2026

Named provider/CQL/tests/PS/Python/setup source restored from retained pin237403a75.
Thirty-one controlled cases, producer/consumer compilation and syntax pass using
cached CouchbaseNetClient3.7.1. Original cluster/bucket/data/credentials and accepted
fixtures remain unopened/unverified. Setup source parsed only. Historical native/
browser acceptance below was not replayed; hosted Couchbase is unverified. Native
CAS is preserved exactly as an acknowledgement string, not a conditional-write
contract.


Updated 30 October 2026. Scoped local Community-edition acceptance; full provider/
ORM parity, conditional writes and HyperDrive acceptance remain incomplete.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicCouchbaseLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5128/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both retain Couchbase/Mongo provider labels.
Finish with the local **Stop demo** button and wait for the native post-demo check,
verified fixture cleanup and runner exit. Start one native demo at a time.

The shared [commented C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects explicit provider scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Each source
owns its authoritative catalogue; requested IDs do not grant access.

## Current verified scope

- Actual Couchbase Community8.0.2-5503, original cluster/node/bucket UUIDs, native
  KV/query/index services. Original3records/JSON/exact unsigned CAS remain unchanged
  through restart, federation and native post-pair verification.
- Parameterized read-only SQL++ `USE KEYS $keys` against configured
  bucket._default.holons, timeout15s/native cancellation; at most100 authorized IDs.
  No unrestricted index scan or fallback to legacy loads. Bucket identifiers validated.
- Streamed native row key/stored ID/payload ID/access checks, duplicate/foreign
  rejection and4MiB decoded-payload budget. Terminal native status must succeed
  before rows publish. Stable catalogue order precedes paging; missing/deleted
  exclusion and failure/budget/cancellation behavior covered by controls.
- Save stages detached ID/keys/payload and publishes saved state only after nonzero
  nativeCAS. Exact unsigned64 CAS emitted as invariant decimal string metadata;
  no floating-point/signed conversion. Batch continuation/count controls pass.
  This receipt does not implement observed-token conditional writes or history.
- Activation provisions missing named collections before index DDL, consumes/checks
  DDL completion and activates only on success; deactivation clears state. Existing
  bucket must be provisioned separately. Current run reused its original structure.
- SDK/HTTP5/4, forged actor/unknown preset400, inactive source503 without rows;
  browser independently returned5/4. Unrelated Mongo record excluded.
- Exact `cosmic_cql_mongo_couchbase_demo` identities/collections verified/dropped;
  native container exited0/noOOM/noError, Mongo stopped, pending/ready absent,
 8091/8093/11210/27039/5128 listeners absent. Paired runner builds Mongo before consumer.

## Current evidence

All36 cases passed, zero failures/skips. Paths below are relative to
`C:/Source/OASIS-cosmic/TestResults/CouchbaseCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| couchbase-current-controls.trx |31|83EDB3A94958174D90E1B9FA56CB66ACB0BAAB87F4CD6606BA2EC1793AC847D2|
| native-first.trx |1|72A617644D06D7FC10B8F3152BB44B67C8E93F54647F73395C038C2640BD9CFD|
| native-restart.trx |1|6949F1D501A4986F3011B1B011A19A19814D402D4AEA80B54F25E47926D3A626|
| mongo-init.trx |1|9597683C2A69BA11702CF3D8AD1D653ACE20947F185C7A529FDC18BFE562C124|
| mongo-federation.trx |1|553DC3805D067BD2BA55BC2C8032E711C89ACD7C9032D1D78989A3C85F725BE0|
| native-postpair.trx |1|2838961F0C702FA85577162DA030CC5FB067E1417CCA100FBE442A03D1C61C4E|

Browser evidence: `couchbase-mongo-subset-current.png`. Public CQL catalogue section34
main894b2a2 / Production7453420535 succeeded with local launch instructions, not
hosted Capella connectivity.
Backend remains task branch; no Railway deployment.

## Retention and remaining work

Retain `/var/tmp/oasis-couchbase-cql` data/credentials/IDs/receipts together. All18
top-level metadata/source files unchanged, including3writeACKs and source snapshot
SHA2565F2868AC2F1BE97063A40947FBC72EE6F2E175E333FC05A2770AB9FE0B3D96D2.
Native operational files changed during service use; no whole-directory immutability
claim. Prior completed5TRXs hash-archived; no new setup/IDs/business writes/replay.

Plaintext canonical JSON scope. SDK wire-byte bounding, arbitrary name/description/
metadata/fullSQL++ translation, graph/children/history, conditionalCAS/unknown-write
recovery, migrations, central registration, hosted/TLS/mobile-sync and HyperDrive/
replication/failover/load-balancing/distributed snapshots remain separate work.
[Historical proof](Archive/COUCHBASE_CQL_MVP_PRESERVED_2026-10-30.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).

## WEB4 public profile preparation

The public adapter uses Couchbase's native HTTPS Query Service and shares
the existing bounded SQL++ statement with the owned .NET SDK path.
The public profile sends one read-only statement with explicit keys and
checks the entire response status before publishing rows. It checks an
existing public probe and rejects a selected live record that has become
private. The wire response is bounded to 8 MiB with a 15-second deadline.
This profile requires a Couchbase Server Query Service origin and a
restricted reader. WEB4 registration and live qualification are open.
[Couchbase Query Service API](https://docs.couchbase.com/server/7.6/n1ql-rest-query/index.html).

Provider tests passed 32/32. The HTTP boundary test inspected the actual
JSON statement, keys, read-only setting and Basic authentication header,
then checked private probe rejection and no partial result when a row
became private. Core pin `514b3296db9322a1c8e19094415c11013b620fb4`.

WEB4 registration uses protected `CQL_COUCHBASE_QUERY_` settings with
suffixes ORIGIN, BUCKET, READER_USERNAME, READER_PASSWORD and PROBE_ID.
Use the approved Couchbase Server HTTPS Query Service and a restricted
SQL++ reader for the teaching collection. The HTTP guard requires the
exact compiled statement, read-only flag, 15-second timeout and 1–100
distinct canonical keys. The first reviewed live ID must match the probe.

The protected-settings suite passed all seven tests including Couchbase.
The WEB4 build is in progress. The opt-in integration category
`CouchbaseHostedCqlNative` links the same registration and accepts
`COSMIC_COUCHBASE_HOSTED_CQL_SOURCE` plus the protected settings. Enable
it with `/p:EnableCouchbaseHostedCqlNativeTests=true`. Native and live
acceptance still require an approved TLS source.

For the restricted hosted source, use a reviewed Enterprise Couchbase
Server and a Query Select role limited to the teaching collection.
Community Edition has coarser roles and does not establish that role
boundary. The supported Windows installation uses a system service.
This laptop has no existing Couchbase service or reusable executable,
so native qualification remains tied to an approved source.
[Couchbase role availability](https://docs.couchbase.com/server/7.6/learn/security/authorization-overview.html).

All 10 WEB4 admission tests passed at Core
`0492cec40a87a7ab84ac2ec7718e017ae4463871`. The opt-in native fixture
compiled with zero errors. The native server and live visitor checks
still require the approved teaching source.
