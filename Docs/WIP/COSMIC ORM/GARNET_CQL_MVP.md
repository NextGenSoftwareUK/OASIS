# Garnet CQL demo — admitted generation2

Updated 30 October 2026. Current scoped demo acceptance; full ORM/provider parity
and recovery of the first failed source remain unverified.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicGarnetLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5123/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both keep Garnet/Mongo provider labels. Finish with
the local **Stop demo** button and wait for verified fixture cleanup and runner
exit. Start one Linux Mongo demo at a time.

The shared, commented [C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects an explicit provider scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Trusted access
catalogues belong to each source; caller-selected IDs do not grant access.

## Current verified scope

- Pinned Microsoft Garnet2.2.5 runtime, owned loopback socket and native product
  version; encrypted Redis-protocol executor with distinct Garnet CQL authority.
- Original admitted generation2:3 live records/1 tombstone; bounded authorized ID
  equality/membership/Boolean selection, missing/deleted exclusion and subsequent
  paging; cancellation, result budgets and unsupported-shape rejection.
- Original encrypted string values, keys, seeds, admission, config, checkpoint files
  and acknowledgements unchanged through queries and actual checkpoint restart.
  No new SAVE, business-write replay, empty probe or fixture admission was run.
- SDK/HTTP5/4, forged actor/unknown preset400, inactive source503 without rows;
  browser independently returned5/4. Unrelated Mongo record excluded.
- Exact `cosmic_cql_mongo_garnet_demo` identities/collections verified before drop;
  pending/active/ready files absent and16392/27039/5123 listeners absent afterward.
  Garnet SIGTERM exit-15 follows an acknowledged checkpoint; this is not a
  clean-dispose or uncheckpointed crash-durability claim.
- First failed fixture refused before startup; all18 original file hashes unchanged.
  Its pending/active receipts remain evidence, not proof of a currently live process.

## Current evidence

12 MSTest cases and6 lifecycle controls passed, no failures/skips; one additional
original-fixture refusal check passed. Files are under
`C:/Source/OASIS-cosmic/TestResults/GarnetCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| garnet-current-controls.trx |8|F25CC02B9D0B9201EF8A69ABFB99ED8A5E5B88BB128B9F792B40F652570BEE5E|
| garnet-g2-cql-native.trx |1|CA44DAA6B6022EAA5B7767F6D77D14D9BE8105031B5B3CBADB5ABDCD52A05D9E|
| garnet-g2-cql-restart.trx |1|F82A1A4D1F8FC862818B88312E31186A31A2737ACE7B262B1F20AA80162A5088|
| garnet-g2-mongo-init.trx |1|76C0F786D9336E4A38B40099D915064E7573096D7BD64CD36E1AFC7B3EDF8BA1|
| garnet-g2-mongo-federation.trx |1|AFCDC5B27B1C20671FEC5360E360676E01A04ACDE9413468B072219FB332161D|
| current-lifecycle-controls.log |6|6C094240B5050C73C20CE1379B42EDD2E7D075D408C843B7903D85ACC15B5093|
| current-original-refusal.json |1|CC2DE0A0304DB45178BFE1C371FB9F1F9A527DA45A2FEDE4E1B56A88610A04EA|

Browser evidence: `garnet-mongo-subset-current.png`. Public CQL section29 main
f7d8392 / Production7453175882 publishes the local launch catalogue, not hosted
Garnet connectivity. Backend remains task branch; no Railway deployment.

## Retention and remaining work

Retain `/var/tmp/oasis-garnet-cql/generations/2` and its parent protected credentials
together. Seed SHA2562B7DFBA20505855D5B37D59BD7C89578697F2114CCF5173F58B616FD96FA9799
unchanged. Prior verification receipt is hash-archived; checkpoint payloads unchanged.
The first source lost uncheckpointed in-memory payloads historically; generation2
uses separate admitted IDs and does not recover that loss.

Catalogue maximum100 and decoded4MiB budget after native reads; general name/text/
metadata queries, graph/conditional/history semantics, central registration,
external configuration/atomicity admission, AOF/crash recovery, HyperDrive/cluster/
failover and distributed snapshots remain separate work. Original native profile
uses Lua and LuaTransactionMode; protocol compatibility alone certifies none of these.
[Historical proof](Archive/GARNET_CQL_MVP_PRESERVED_2026-10-30.md),
[first-source failure](Archive/GARNET_CQL_PRE_GENERATION2_2026-10-29.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).
