# ClickHouse CQL MVP — controls/native/restart/paired/browser verified

Updated 30 October 2026. Green means the bounded identity demo scope only.

## Implemented scope

ClickHouseOASIS now implements ICosmicQueryStorageProvider using the shared bounded
identity compiler, bound to ClickHouse authority. Trusted complete catalogue at most
100 GUIDs; identity equality/membership/Boolean composition and stable paging.
Arbitrary predicates, metadata ordering and child graphs reject during admission.

The native reader uses one parameterized UUID IN query against oasis.holons FINAL,
with asynchronous ADO open/read/cancellation, execution timeout and native result
row/byte limits. FINAL applies ReplacingMergeTree version resolution. It does not
establish atomic conditional writes or a distributed snapshot. The stream checks
native identity/payload identity/read authority/duplicates before publishing rows,
enforces a cumulative4MiB decoded payload budget and never publishes partial rows
after native terminal failure. SDK wire-byte limiting remains unverified separately.

SaveHolon stages identity/copied provider keys/JSON until the native INSERT completes;
only then publishes caller ID/keys and IsSaved/SavedCount. Typed UUID/String/UInt8
parameters replace interpolated SQL for this save boundary. Explicit async_insert=0
requires synchronous insertion acknowledgement. No SDK-specific affected-row count
is assumed. HolonManager retains ownership of change-tracking acceptance.
Batch saves honor continueOnError and return acknowledged counts and real failures.
Failed activation clears stale active state; deactivation clears it too.

The existing history flag had no implementation. Holon saves and CQL now explicitly
reject true before native IO. Other avatar/general load/delete/history paths remain
an independent audit gap; they are not silently used by the new CQL adapter.

## Proving controls

28passed/0failed/0skipped: metadata4, compiler/admission9, saveownership6,
streaming query9. Provider and unit builds pass; final restores cache-only.
Tests cover authority mismatch, unsupported predicates/graphs, missing/invalid
plans/budgets, pre/active cancellation, identity/payload/duplicate/foreign corruption,
stream terminal failure/no partial output, stable paging, decoded budget stopping
enumeration, empty/no IO and missing records. Save tests exercise failed new/existing
identity, incomplete acknowledgement, published payload equality, unsupported history
and batch stop/continue/counts. IO is injected; these are not native backend tests.

[Compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.UnitTests/ClickHouseCqlTests.cs),
[query tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.UnitTests/ClickHouseNativeQueryBoundaryTests.cs),
[save tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.UnitTests/ClickHouseSaveOwnershipTests.cs).
Raw TestResults/ClickHouseCqlCurrent/controls.trx SHA256
`76d15bd6e1265641f29897760bdb3662a6916ccc05e22cde0721e88460d73954`.
Stale GetProviderVersion test removed rather than inventing a shipping API.

## Exact next step

Continue red providers, next InfluxDB capability/native-source audit. Preserve this
scope's acceptance; no green hardening without a reproduced bug.

## Native and Mongo-paired proof

Official free open-source ClickHouse26.8.36.3 on authenticated HTTPS/private CA,
loopback18443. Original serverUUID a0075f10-54e2-4d09-babf-a54889fe062a, database/table
UUIDs, actor/3IDs, JSON hashes and exact native UInt64 versions survive actual restart.
Native3/filter2/missing0/page1/budget and typed parameter transport/schema provisioning
pass. No SDK/runtime/TLS bypass. Private source /var/tmp/oasis-clickhouse-cql retained;
18,509,680B logical after interactive verification. Own node stopped0/noOOM/noError.

Launch `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider ClickHouse -Federated`
from the task worktree; add `-VerifyOnly` for bounded checks. Browser5143. The C#
example `Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs` uses HolonManager's
actual executor and explicit per-provider read catalogues. HTTP/browser all returns
ClickHouse3+Mongo2=5physicalrows; subset2+2=4. Copies retain provider provenance.
Forged actor/unknown preset400 and inactive provider503/no partial rows pass.

Bounded and interactive runners each passed five phases: native-first,
native-restart, mongo-init, mongo-federation and native-postpair. Browser all5/subset4
verified. Hub8single/16plans,52paired/104plans,90pairedrejections,3archive pass,
listeners unchanged. Exact temporary cosmic_cql_mongo_clickhouse_demo verified/dropped,
pending/ready cleared, native/Mongo/demo services stopped; original source preserved.
Raw interactive evidence in TestResults/ClickHouseCqlCurrent (not committed):
- native-first.trx SHA256 `b3388f7db6c046d1419ffd3fb9c7f8ef3727e4fddda1d024600abe9886710953`
- native-restart.trx SHA256 `f1742cfdba7206fd907cc1472567319c6e9f79ce65b60c8c84f3a52fcb177460`
- mongo-init.trx SHA256 `bb27410f7f8056037f1d2b4124cc2e8cfe05b1453ce70bac059b64c53aacd13a`
- mongo-federation.trx SHA256 `1d7027f800c069074055ef9650eae3956e33b6ba7a2ec168fece0b3cc0c24a28`
- native-postpair.trx SHA256 `a4ba2ed26b3ca0da8545028b7fda8e80d61ebcf0248bee7716dd6bc875990ae6`
Browser screenshot browser.jpg160,150B, runner86934terminal0. Native state and
credentials are retained outside TestResults; only logs/TRX/images are regenerable.

Current ReplacingMergeTree versions default to server milliseconds, so concurrent
writes can tie; broader source CAS/history/transaction/replication/child/migration
semantics remain open. This bounded demo is not full EF/NHibernate parity.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Environment creation refuses existing state. Its first executable check followed an
absolute image symlink into the host, before any native/config/credential creation.
Image-relative resolution fixes it; explicit original-intent readback resumed only
that pre-config stage. No source/container replacement, native mutation replay or
baseline replacement. Vendor image/private CA/config/credentials are reused.
[Official installation](https://clickhouse.com/docs/install/docker).
