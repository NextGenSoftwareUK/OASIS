# Current provider I/O verification scope

Updated28October2026. Recover from [CURRENT_STATUS.md](CURRENT_STATUS.md), then query the [matrix](PROVIDER_MATRIX.md); archives are not default reading.

## Evidence scope

| Gate | Verified scope |
|---|---|
| core-storage-v2 |26sync+26async: identity, avatar/detail CRUD, graph/metadata/parent queries, batch/import/export, deletion acknowledgements and exact owned-fixture cleanup |
| Native52/52 |Oracle, PostgreSQL, Redis, MongoDB, Neo4j, CosmosDB emulator, Firestore emulator, AWS alias, DynamoDB, LocalFile, Azure Blob, SQLite, DuckDB, MinIO, Convex; local engines/emulators, not hosted certification |
| Compatible native52/52 |AWSS3OASIS on CI MinIO; hosted AWS/IAM and complete interfaces remain open |
| Recovered native52/52 |SOLID/IPFS compact reports match registry hashes; runtime/key/lifecycle/full-interface acceptance remains separate |
| Controlled52/52 |KeyDB, ValKey, Dragonfly, Garnet, ThreeFold; actual product native gates remain open |
| Non-storage capability-v1 |ActivityPub5/5 real guarded public GET checks;291controlled social cases are separate |
| Partial native |Solana4/52; preserved original history/chunk failure; other rows have explicit matrix gates |

## Limits and next gate

- No provider is declared100% across all relevant interfaces, hosted IAM, durability, NFT/bridge or federation behavior. SDK detection does not prove implementation completeness; nine reviewed adapters remain missing.
- ActivityPub's private generic-record writes are refused; social capability proof does not certify storage/federation. Code sharing and controlled SDK responses are not product-native verification.
- Deletion respects each shipping contract: declared tombstones or independent absence, acknowledged counts, exact fixture-only hard cleanup. Interface absence does not prove physical backup/ledger erasure.
- Explicit Version filters selected roots only; child versions remain independent. Metadata may be null; protect private fields while preserving query semantics.
- Global HyperDrive failover/load balancing/replication follows all applicable baseline gates; legacy gaps are documentation only, depth remains deferred.
- PR502 native MinIO and AWSS3 compatible52 cases passed with empty checks and matching DLL identities. Convex native52 plus revision/paging controls passed locally, records0 and runtime stopped; source matched, immutable release/hosted gates separate. Continue remaining family baselines.

Current hashes/paths: [evidence locations](EVIDENCE_LOCATIONS.md) and CONFORMANCE_RESULTS.json. Complete pre-trim chronology remains verbatim in [Archive/HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md](Archive/HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md), with [archive hashes](Archive/ARCHIVE_INDEX.md).
