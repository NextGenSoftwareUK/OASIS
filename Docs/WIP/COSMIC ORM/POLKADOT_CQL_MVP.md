# Polkadot CQL MVP

Updated 29 October 2026. **Adapter controls verified; native and paired demo pending.**

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

These controls do not prove native query execution or demo readiness.

## Next acceptance steps

1. Reuse the retained Substrate 0.42.0 runtime, specification and original ledger;
   verify original contract identity without resetting historical state.
2. Journal an owned three-record encrypted seed before submitting any writes;
   prove native full/subset queries return three/two and preserve revisions.
3. Add Mongo-paired C# and HTTP presets, verify five/four results in the browser,
   then enroll the provider in the demo launcher.
4. Stop owned services and validate temporary Mongo cleanup while retaining the
   original chain, seed, encryption key and signed preparations for reuse.

The pinned Node SDK was restored offline from the existing npm cache. Resources
and retention rules are in [the ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
Return to [current status](CURRENT_STATUS.md) for the breadth-first queue.
