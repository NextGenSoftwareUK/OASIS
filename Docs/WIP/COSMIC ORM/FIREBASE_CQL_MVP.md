# Firebase RTDB CQL demo — retained export restoration

Updated 30 October 2026. Scoped local Realtime Database emulator acceptance,
separate from Firestore; hosted Firebase and full ORM/provider parity remain open.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicFirebaseCqlNative.ps1 -Federated -Serve
```

Open http://127.0.0.1:5126/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both retain Firebase/Mongo provider labels.
Finish with the local **Stop demo** button and wait for verified fixture cleanup
and runner exit. Start one native demo at a time.

The shared [commented C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects explicit Mongo/Firebase scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Each source
owns its trusted access catalogue; selected IDs do not grant authorization.

## Current verified scope

- Official RTDB emulator4.11.2/JDK21.0.12, loopback19091, namespacecosmic-cql-demo;
 256MiB/twoCPU JVM. Explicit namespace is encoded on reads/writes/queries.
- RTDB has no native IN: bounded union of indexed `$key` equality REST requests,
  at most100 authorized IDs. Strict object/null/identity checks, missing/deleted
  exclusion before paging, aggregate4MiB bound, cancellation and no partial rows
  after later-read or result-budget failure. Reads may observe different moments.
- Activation probes reserved empty-GUID record through REST; state follows lifecycle.
- Detached save projection removes native null/empty nodes before dispatch; exact
  native acknowledgement precedes caller ID/key/IsSaved/SavedCount publication.
  Unsupported numeric roundtrips reject before dispatch; uncertain dispatched
  results carry reconciliation contract. No acknowledgement repair or automatic replay.
  Soft delete writes canonical DeletedDate through the same save owner.
- Actual original export restored into two empty owned emulator processes. Native
 3live/1deleted, filtered2 and page1; no new IDs or business-save replay.
  Public reads denied; emulator owner header confined to this local fixture.
- SDK/HTTP5/4, forged actor/unknown preset400, inactive source503 without rows;
  browser independently returned5/4. Unrelated Mongo record excluded.
- Exact `cosmic_cql_mongo_firebase_demo` identities/collections verified and removed;
  original checkpoint rechecked before termination,19091/27029/5126 stopped and
  pending/active/ready absent. All11 retained source files byte-identical.

## Current evidence

50 non-native query/ownership/read/provider controls plus3 native integration cases
passed, zero failures/skips. Full `FirebaseStorageConformanceTests` deliberately
excluded and remains unverified. Paths below are relative to
`C:/Source/OASIS-cosmic/TestResults/FirebaseCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| firebase-current-controls.trx |50|AE2FA3B314502D9BC7DF35265EFDC129BF4925496538A9A7B66118848989DF84|
| native.trx |1|465709E2580D0219D64CD7B550F63126678F12F4A20E639C1DE29AA35FD959F3|
| restart.trx |1|354CD1DB307BED2A2F6743A0D4A4CCF90D3AEAFF9FCC227400DA0E493186EEC2|
| mongo-federation.trx |1|3643E3E24926A1AFD14231692A93CA8796F50CC249FBF5DBB9EE2CD150D53315|

Browser evidence: `firebase-mongo-subset-current.png`. Public CQL section32 main
02f4ebf / Production7453302521 publishes a local launch catalogue, not hosted
Firebase connectivity. Backend remains task branch; no Railway deployment.

## Retention and remaining limits

Preserve `LocalState/COSMIC/FirebaseCql` original11files/9,823B including checkpoint,
IDs, native baseline and acknowledgements. Original logical source hash remains
6222B4436A54A485F172E2D34413A248C0ED67C7E8B738DC20271C0101FEA343.
Prior completed3TRXs hash-archived. This volatile emulator restores an export;
process termination after checkpoint verification does not prove native disk flush,
uncheckpointed crash recovery or hosted durability.

Plaintext native JSON scope. Hosted credentials/rules, general name/description/
metadata predicates, full numeric/array/default/graph roundtrips, child/depth/history,
atomic concurrency, avatar/detail ownership, central registration, full storage
conformance and HyperDrive/failover/load-balancing remain separate acceptance work.
[Historical proof](Archive/FIREBASE_CQL_MVP_PRESERVED_2026-10-30.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).
