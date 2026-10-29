# Polkadot CQL MVP

Updated 29 October 2026. **Adapter controls and native query verified; paired demo pending.**

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
stopped and both ports closed. C#/HTTP/browser federation is not yet verified.

Restart report: `TestResults/PolkadotCqlCurrent/polkadot-cql-native.trx`, SHA256
`5CA51481D491E74FBC370666AF681EC6E724B33900978660F877760B20993734`.

## Next acceptance steps

1. Add Mongo-paired C# and HTTP presets, verify five/four results in the browser,
   then enroll the provider in the demo launcher.
2. Stop owned services and validate temporary Mongo cleanup while retaining the
   original chain, seed, encryption key and signed preparations for reuse.

The pinned Node SDK was restored offline from the existing npm cache. Resources
and retention rules are in [the ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
Return to [current status](CURRENT_STATUS.md) for the breadth-first queue.

```powershell
Scripts/TestHosts/Run-CosmicPolkadotCqlNative.ps1
```
