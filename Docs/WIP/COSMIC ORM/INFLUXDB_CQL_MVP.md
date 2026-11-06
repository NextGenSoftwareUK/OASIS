# InfluxDB 2.x CQL demo — retained Flux latest heads

## Source recovery — 2 November 2026

The named provider/compiler/write-ownership/test/launcher closure is restored.
34 controlled tests pass (zero failures/skips), with provider and native-consumer
builds and launcher/Python syntax checks passing. Evidence: local
`LocalState/COSMIC/LauncherRecoveryCurrent/influxdb-recovery-evidence.json`;
original TRX SHA256 `c6f2b1c11571c9d60183bcbe8d4e788aa1c178de7e8856b70202a3eee3bf2b1c`.
No original service, credential, data or accepted native fixture was opened.
The native results below are historical; current hosted availability is unverified.
Scope is InfluxDB 2.x Flux bounded GUID queries, not InfluxDB 3 or full ORM parity.

Updated 30 October 2026 (America/Cancun). Scoped local OSS2.7.12/Flux acceptance;
InfluxDB3, hosted cloud and full ORM/provider parity remain unverified.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicInfluxDbLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5130/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both retain InfluxDB/Mongo provider labels and show
**Future Espresso**. Finish with the local **Stop demo** button and wait for native
post-demo verification, fixture cleanup and runner exit. Start one native demo at a time.

The shared [commented C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects explicit provider scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Each source
owns its authoritative catalogue; requested IDs do not grant access.

## Current verified scope

- Actual InfluxDB OSS2.7.12/SDK5.1.0 with authenticated HTTPS/private CA,18086;
  original org/bucket UUIDs/infinite retention. Six retained points:3normal,
  future latest head,2tied-head controls. Three admitted logical records returned;
  separately requested ambiguous control ID explicitly rejects, not arbitrary choice.
- Flux constrains measurement/IDs/fields before pivot, groups by ID and joins max
  timestamps back to original rows, preserving ties for duplicate rejection. Native
  UTF8/`${...}` echo, future-head visibility,3/filter2/missing0/page1/budget verified.
- Maximum100 authorized IDs, streamed DTO completeness/key/payload/access checks,
  duplicate/tie rejection, aggregate4MiB decoded budget,15s deadline/cancellation,
  stable order before paging and no partial rows after later native failure.
  SDK wire-byte bounding and full absolute timestamp endpoints remain unverified.
- Detached save stages ID/keys/payload until native HTTP write acknowledgement;
  only then publish caller saved state/count. Batch continuation/counts controlled;
  history mode explicitly rejected. Save fault controls inject IO, not native CAS proof.
- SDK/HTTP5/4; forged actor/unknown preset400, inactive source503 without rows;
  browser independently returned5/4. Unrelated Mongo record excluded.
- Original point JSON/timestamps unchanged through actual restart and post-pair
  check. Exact `cosmic_cql_mongo_influxdb_demo` identities/collections verified/dropped;
  native gracefulSIGINT exit0/noOOM/noError, Mongo stopped, pending/ready absent,
 18086/27039/5130 listeners absent. No setup replay/TLS bypass/business writes.

## Current evidence

All39 cases passed, zero failures/skips. Paths below are relative to
`C:/Source/OASIS-cosmic/TestResults/InfluxDbCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| influxdb-current-controls.trx |34|C5D5AEA3E0F704914013DE690E1BE134645583AA86A72F880E4EDD68958973A7|
| native-first.trx |1|2308B88AAB6CDF8E0812FDFB39D4525596B279D5A6F623C64E0006A0C4B6B166|
| native-restart.trx |1|D24B805D6276D6F2EFAAF739860C281A55CB2727AD5FFA54B0FE5F0D8AC7C310|
| mongo-init.trx |1|9835702B7B40F60993176ACDBEA280D806673DCE0AE7C1811FA9C084F7BA84B5|
| mongo-federation.trx |1|0D7AC6616590C4F51FABF8FC9CE552D596D118E031364161892885C515FF02DE|
| native-postpair.trx |1|B49368A2B2A559F001F0F70C0231ADBB501CD60E3058282902E2997BE47049F0|

Browser evidence: `influxdb-mongo-subset-current.png`. Public CQL section36 main
dc76a57 / Production7453546339 succeeded with local launch instructions, not hosted
cloud connectivity.
Backend remains task branch; no Railway deployment.

## Retention and remaining work

Preserve `/var/tmp/oasis-influxdb-cql` points/credentials/token/CA/TLS/receipts together.
All27 top-level metadata/source/TLS files unchanged; original source snapshot SHA256
6E8A4EFDC485BC778F5ABD6A54926B7A3AA4EB88C6D12D42DCCE59502661DA23.
Native operational files changed; no whole-directory immutability claim. Prior
completed5TRXs hash-archived. Removed105,599,039B of completed private unit/integration
bin copies after verified teardown. The demo runner rebuilds its integration bin;
unit-test runs rebuild their own bin using retained obj/cache.

Canonical JSON is plaintext. Append timestamps/tag changes do not implement CAS/
version-control/history/children/transactions/replication/migration guarantees.
General name/description/metadata queries, central registration, timestamp endpoints,
avatar/legacy lookup/delete audit, hosted configuration and HyperDrive/failover/
load-balancing/distributed snapshots remain separate work. Retention must be checked.
[Historical proof](Archive/INFLUXDB_CQL_MVP_PRESERVED_2026-10-30.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).

## WEB4 public query profile preparation

All 35 provider tests passed, including a public probe and rejection of
a selected record whose public flag was removed. The official InfluxDB
SDK uses the configured HTTP transport for the public profile. WEB4
accepts the exact generated Flux identity query for the configured bucket,
using the configured HTTPS origin and organization. The guard applies an
8 MiB wire limit before SDK CSV parsing. The provider retains its decoded
4 MiB limit. This profile targets InfluxDB 2.x with Flux enabled.

Protected settings use `CQL_INFLUXDB_QUERY_` with suffixes ORIGIN,
ORGANIZATION, BUCKET, READER_TOKEN and PROBE_ID. The token must have read
access to the teaching bucket. The protected settings preparer binds the
probe to the first reviewed live ID. All six settings tests passed.
The WEB4 transport tests are running.
[Official SDK query path](https://github.com/influxdata/influxdb-client-csharp/blob/v5.1.0/Client/QueryApi.cs).

A separate disposable Windows native fixture is prepared for the pinned
InfluxDB OSS 2.7.12 executable. It will test the actual WEB4 query guard,
Flux parser and read-only bucket token over hostname-verified loopback
TLS, including full/subset/paging, missing and deleted records, and
unchanged original points. This fixture uses new synthetic data. The
retained Linux source is separate.

The corrected WEB4 admission passed 8/8 at Core
`514b3296db9322a1c8e19094415c11013b620fb4`. The official SDK used the
guard on activation and reactivation. The Windows native fixture passed
1/1 against InfluxDB 2.7.12. It checked full/subset/paging, missing and
deleted exclusion, a denied native write with the scoped reader and
unchanged native points. The first observation combined different Flux
field types. The next used a moving time range. The final observer
separates fields and uses a fixed range. Original run logs remain
in the ledger paths. Native TRX SHA256
`69c7090531263272fea65e574ceacc030ebde1da59bb5d93fde942b6c285b1ad`.

To repeat from the repository root, build the integration project with
`/p:EnableInfluxDbWindowsPublicCqlNativeTests=true` under the shared
build lease, then run `python -B Scripts/TestHosts/run-cosmic-influx-windows-public.py`.
It expects the verified runtime receipt and refuses to reuse the evidence
directory. Hosted source and live visitor qualification remain open.
