# Current provider I/O verification scope

Updated 2026-10-27. [CURRENT_STATUS.md](CURRENT_STATUS.md) is the recovery entry point; [PROVIDER_MATRIX.md](PROVIDER_MATRIX.md) is the per-provider inventory.

## What the evidence proves

| Gate | Recorded scope |
|---|---|
| `core-storage-v2` | 26 sync + 26 async checks covering identity, avatar/detail save/read, graph/metadata/parent queries, batch/import/export, acknowledged deletion and owned-fixture interface absence |
| Native52/52 | Oracle, PostgreSQL, Redis, MongoDB, Neo4j, Azure CosmosDB, Google Firestore, AWSOASIS, DynamoDBOASIS, LocalFile and Azure Blob; local engines/emulators, not hosted certification |
| Controlled52/52 | KeyDB, ValKey, Dragonfly, Garnet; native product runtime/conditional/scan acceptance remains open |
| `capability-v1` | Identity, honest capabilities, activation, read-only service response and deactivation; ActivityPub published5/5 public-read baseline (`00235929a`) |
| Partial/blocked | Solana4/52; SOLID/IPFS/Cosmos/EVM/S3 configuration or runtime gates; most other rows not run |

Deletion checks preserve the shipping contract: valid tombstones or independent absence are allowed as declared, with acknowledged counts and no live/corrupt raw collection result. Optional fixture maintenance does not replace the API under test. Interface absence is not physical backup/ledger erasure.

## Important limits

- No provider is declared 100% across every relevant interface, cloud configuration, IAM, durability, NFT/bridge or federation behavior.
- Detected SDK construction/HTTP calls do not prove a complete implementation. Nine adapters have reviewed source gaps; zero remain source-unclassified.
- ActivityPub is a Mastodon social projection adapter whose generic private record saves are rejected. Social capability evidence is not a 52-check storage pass or full federation certification.
- Ethereum/Arbitrum/Polygon share the Nethereum base; PostgreSQL-compatible SQL facades and Redis-compatible facades share existing policy. Sharing code is not native product verification.
- Explicit root Version selects roots only; children retain independent versions. Metadata may be null. Preserve query/key-exemption behavior while protecting private fields.
- Global HyperDrive failover/load balancing/replication follows all applicable baseline gates. Legacy gaps are documented, not expanded. Depth work remains deferred.

## Next verification

1. Evidence writers migrated (`d70625e86`); keep linked legacy files until each reference is reviewed.
2. ActivityPub published; LocalFile native52/52 completed with source/PDB and empty-current-fixture verification.
3. Recover SOLID protected inputs or keep its configuration gate explicit; continue EVM/remaining priority families without fresh duplicate environments.
4. Run only the relevant bounded native suite using existing cached hosts/runtimes; update registry, matrix and CURRENT_STATUS at the milestone.

Hashes and paths: [EVIDENCE_LOCATIONS.md](EVIDENCE_LOCATIONS.md). Current registered result metadata remains in `CONFORMANCE_RESULTS.json` (its pending updates must be published with their scoped provider work).

Full pre-trim chronology is preserved verbatim in [Archive/HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md](Archive/HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md); it is not a default reading list. [Archive hashes](Archive/ARCHIVE_INDEX.md).
