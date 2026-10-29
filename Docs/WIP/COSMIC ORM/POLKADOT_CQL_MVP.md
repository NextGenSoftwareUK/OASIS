# Polkadot CQL MVP

Updated 29 October 2026. **Local native, C#, HTTP and browser MVP verified.**

## Implemented scope

The concrete Polkadot provider implements `ICosmicQueryStorageProvider`. It admits
one explicit Polkadot target and a trusted, server-owned record catalogue, then
compiles captured identity predicates into bounded native contract-key reads.
Supported predicates are ID equality/membership, boolean constants and AND/OR;
pages use deterministic identity order. Selection is limited to 100 keys.

Reads use the shipping encrypted backend and the shared canonical `RecordKey`
builder. Native contract/genesis/code identity checks and encryption remain owned
by that backend. The adapter validates returned holon identities and read access,
hides deleted records and publishes no partial results on failure or cancellation.
The canonical response budget is 4 MiB. Reads may observe different blocks.

Name/metadata predicates, arbitrary ordering, child expansion, unbounded scans,
common-block snapshots and full ORM/HyperDrive parity are not supported by this
MVP. Unsupported queries fail admission rather than scanning the contract.

## Evidence

- Provider build: 0 errors, 8 warnings.
- `PolkadotCqlTests`: **7 passed, 0 failed, 0 skipped**.
- Cases prove captured-key immutability and catalogue intersection, empty
  selection, wrong authority, unsupported shapes/bounds, inactive-provider
  rejection and cancellation before native execution.
- Report: `TestResults/PolkadotCqlCurrent/polkadot-cql-controls.trx`.
- SHA256: `F4B73C6794C88CCE845DEA8F1C0B1E869412E3A44ED64177320F73AC00A432C0`.

Native acceptance now passes **1/1, zero skips, 3m47s** on the retained Substrate
0.42.0 node. Three owned encrypted holons return Espresso/Latte/Tea; captured
membership returns Espresso/Latte. Canonical source contents, including versions,
remain unchanged after CQL reads. Each signed seed write was flushed before
broadcast and independently checked for finalized success and matching effect.

- Initial native report: `TestResults/PolkadotCqlCurrent/polkadot-cql-native.initial.trx`.
- SHA256: `8A9934BAC49900930D02D279E4390E37677A0AD03B427609376DAA0BAF266FBD`.
- Seed: `LocalState/COSMIC/PolkadotCql/demo-seed.json`.
- Seed SHA256: `F5038F40B99B3A0B8F64B3EEFF931F2426DCCC459AE54AB7B3D7DD864A497FE8`.
- Persistent state: 25,164 bytes across seed, DPAPI key, three signed preparations,
  three matching receipts and environment lock. No credentials are in this report.
- Owned node PID8173 stopped; ports19944/19945 independently verified closed.

The initial runner readiness timeout preserved the same live node. It was resumed
with explicit original PID validation, without restarting or repeating writes.
Logs remain under `TestResults/PolkadotCqlCurrent/`. Full restart/reuse acceptance
also passes **1/1, zero skips, 1m36s**, using the same manifest hash and exactly the
same three signed originals. No reseeding or further signatures. Owned node9184
stopped and both ports closed.

Restart report: `TestResults/PolkadotCqlCurrent/polkadot-cql-native.trx`, SHA256
`5CA51481D491E74FBC370666AF681EC6E724B33900978660F877760B20993734`.

## Mongo-paired demo

Paired acceptance passes **1/1, zero skips, 2m12s**. The common HolonManager C#
walkthrough and HTTP host return five physical rows for all records, four for the
subset, preserving provider provenance. Forged actor/unknown preset return 400;
an inactive Polkadot source returns 503 without partial rows. The paired provider
has no signing key and reuses the original seed. Actual browser clicks verified
both counts and source labels.

- Report: `TestResults/PolkadotCqlCurrent/polkadot-mongo-federation.trx`.
- SHA256: `1553083001B4FC47843F2DB37E11305E5F697998FA7ECC5513762790BA69558D`.
- Screenshot: `TestResults/PolkadotCqlCurrent/polkadot-mongo-browser.jpg`.
- SHA256: `A9812207D531FEF2FEFB28545EA43693B5F61D4BB87E7D578409F73BD8766784`.
- Exact owned Mongo fixture `cosmic_cql_mongo_polkadot_demo` validated and dropped;
  ready/pending markers removed. Ports19944/19945/27029/5102 closed afterward.
- Original seed hash unchanged; exactly three original preparations retained.
- Hub discovery: 8 single/16 plans, 13 paired/26 plans, 12 paired rejections,
  3 archive rejections; no listener changes. The verifier now reports measured
  counts rather than its stale hard-coded summary.

This closes the scoped local MVP, not public-network or full-parity acceptance.
Next provider: NEAR, then the remaining all-provider breadth queue.

The pinned Node SDK was restored offline from the existing npm cache. Resources
and retention rules are in [the ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
Return to [current status](CURRENT_STATUS.md) for the breadth-first queue.

```powershell
Scripts/TestHosts/Run-CosmicPolkadotCqlNative.ps1

# Interactive Mongo + Polkadot demo; open http://127.0.0.1:5102/.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Polkadot -Federated

# SDK/HTTP acceptance and cleanup without leaving the host open.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Polkadot -Federated -VerifyOnly
```
