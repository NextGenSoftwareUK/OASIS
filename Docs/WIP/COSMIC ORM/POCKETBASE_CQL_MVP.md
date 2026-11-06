# PocketBase CQL demo — retained native REST source

## Source recovery — 2 November 2026

Named provider/CQL/reconciliation exception/tests/PS/Python source restored from
retained pin237403a75. Forty-six controlled cases, producer/consumer compilation
and syntax pass. Original runtime/collections/account/credentials/fixtures remain
unopened/unverified; no native provisioning or replay. Historical native/browser
proof below was not repeated. Unknown write outcomes retain the proposed logical
identity for explicit reconciliation, not automatic retry. Hosted/conditional-write
acceptance is not inferred.


Updated 30 October 2026. Current local demo acceptance; full ORM/provider parity
and hosted PocketBase service access remain incomplete.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicPocketBaseLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5124/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both preserve PocketBase/Mongo provider labels.
Finish with the local **Stop demo** button and wait for the runner to exit after
verified cleanup. Start one Linux Mongo demo at a time.

The shared [commented C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects an explicit provider scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Authoritative
access catalogues belong to each source; selecting an ID does not grant access.

## Current verified scope

- Real pinned PocketBase0.40.6 REST service on loopback18090 and retained database;
  original4records/3live/1deleted survive actual service restart.
- Native `oasis_id` membership filter with deletion exclusion and stable ordering;
  paging after missing/deleted exclusion. Maximum100 authorized IDs; one bounded
  native response with4MiB streamed-body limit, complete page/count/identity checks,
  cancellation, budget overflow and unsupported-shape errors without partial rows.
- Canonical reader shared by point/parent/all-record/CQL reads; payload/projection
  identity agreement, tombstone handling and malformed/duplicate result rejection.
- Save stages a detached payload and proposed ID/key; caller identity/provider key,
  IsSaved and SavedCount publish only after matching native acknowledgement.
  A dispatched uncertain mutation returns a reconciliation error, never silent replay.
  Current save fault/success controls are synthetic HTTP tests; this retained-source
  integration did not issue new business saves or deletes.
- SDK/HTTP5/4 and malformed/forged requests400, inactive source503 without rows;
  browser independently returned5/4. Unrelated Mongo record excluded.
- Exact owned `cosmic_cql_mongo_pocketbase_demo` identities/collections verified and
  removed; owned PocketBase SIGINT exit0 and Mongo exit0, pending/active/ready absent,
 18090/27039/5124 listeners absent after teardown.

## Current evidence

All45 cases passed, no failures/skips. Files are relative to
`C:/Source/OASIS-cosmic/TestResults/PocketBaseCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| pocketbase-current-controls.trx |41|759BD21AF914EA04052DAB8F4EB22B71CA2E2D6CDEE834317AC5190B4DD880BF|
| native.trx |1|E1FAEDE4D1475784D488E0F6DD7A0884897E15EAD2B61C95F9B1FDA8EB03EADC|
| restart.trx |1|87130874D5AED6D2CEB130380F3A5C0359ECADB2C4153504C7D93F89BB538F59|
| mongo-init.trx |1|0D4CF340FE557E1B935004F7E148D9FB9C0F75A412E4EEB331B2AEB3578D17B6|
| mongo-federation.trx |1|78FA7C59D94767BACCB3B4F36A27D836D66663C3912E8A0979B8AAAAC59CEE9B|

Browser evidence: `pocketbase-mongo-subset-current.png`. Public CQL section30 main
32fba52 / Production7453220231 is a local launch catalogue, not a hosted PocketBase
connection. Backend changes remain task branch, not a Railway deployment.

## Source retention and remaining limits

Preserve `/var/tmp/oasis-pocketbase-cql` database/password/identity/ACKs together.
Exact native record/projection/payload baseline remains
9B37B1F59EDCAB41FE0A26A834A3C5CF56E9C8907FDA17D09340B19B5ECCA771.
All non-database source files remained byte-identical; native data/auxiliary SQLite
files legitimately changed with authentication/service metadata. No whole-database
byte-immutability claim. Prior completed TRXs hash-archived, unresolved outcomes refuse replay.

Canonical stored JSON is plaintext. General name/description/metadata predicates,
graph/conditional/history semantics, central registration/hosted admission,
HyperDrive replication/failover/load-balancing and distributed snapshots remain
separate acceptance work. Read-time restart proves this retained fixture, not the
full failure matrix. [Historical evidence](Archive/POCKETBASE_CQL_MVP_PRESERVED_2026-10-30.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).

## Development WEB4 candidate

Set the explicit HTTPS `CQL_POCKETBASE_QUERY_ORIGIN`, a protected restricted
`CQL_POCKETBASE_QUERY_TOKEN` and an existing public
`CQL_POCKETBASE_QUERY_PROBE_ID`. The guarded route sends only bounded
filtered GETs to the dedicated teaching collection `oasis_holons`.
The native reader uses its own token and collection access rules. Keep the
existing administrator credentials and retained source separate.

Use `TestCategory=PocketBaseHostedCqlNative` after a reviewed HTTPS source
is available, with `COSMIC_POCKETBASE_HOSTED_CQL_SOURCE` pointing to the
catalogue with `Actor`, three `LiveIds`, `DeletedId` and `MissingId`.
Use one live ID as the probe. All 26 controlled provider checks and eight
WEB4 admission checks passed. The native HTTPS fixture compiled and awaits
the reviewed service.
