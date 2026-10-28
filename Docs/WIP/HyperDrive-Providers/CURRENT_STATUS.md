# Current provider status

Updated28October2026. Read this first, then query relevant matrix rows; do not load archives by default.

## Phase and rules

- Breadth first: real provider implementations and canonical core-storage-v2 (26checks per sync/async mode), approximately one day per family. Non-storage uses capability-v1, not storage tests.
- All applicable provider baselines precede global failover/load balancing/replication verification, then priority-provider depth. No Polkadot/NEAR/SQLite depth now.
- Use C:/Source/OASIS, fix/hyperdrive-v2-providers; never recreate deleted checkout. No new clones/worktrees/environments; reuse cached runtimes.
- Stage exact owned files, preserve concurrent dirty work/submodules, no force pushes/attribution trailers. COSMIC is isolated in OASIS-cosmic; contact only for overlapping Core/Shared public API work.
- Integrate scoped verified batches to Development once/twice daily; fetch first, verify submodule commits remotely and manifest/gitlink agreement. Never deploy unrelated/unverified changes just to synchronize.
- New raw evidence under TestResults; compact hash-bound summaries only are tracked. Maintain resource ledger, ten recent entries, byte-preserved archive rollovers.

## Evidence now

| State | Scope |
|---|---|
| Native core-storage-v2 52/52 | OracleDB, PostgreSQL, Redis, Mongo, Neo4j, CosmosDB emulator, Firestore emulator, AWS alias, DynamoDB, LocalFile, Azure Blob, SQLite, DuckDB |
| Native compatible52/52 | Shipping AWSS3OASIS on existing CI MinIO; independent empty-bucket checks passed; hosted AWS/IAM open |
| Recovered native52/52 summaries | SOLID/IPFS reports recovered from isolated checkout with exact registry hashes/contracts; original failed reports preserved, lifecycle/full-interface gates open |
| Controlled52/52 only | KeyDB, ValKey, Dragonfly, Garnet; product-native shared-suite runs pending |
| Non-storage ActivityPub | Native public-GET capability5/5, controlled291; full social operations open |
| Other controlled cases | Firebase23, Appwrite19, PocketBase24, Convex20; these do not certify native storage |
| Partial native | Solana4/52; original referenced history unavailable, unresolved outcomes preserved |

229 matrix projects:56non-storage/173storage-specialized. SDK-source-unverified0, reviewed missing9; client detection is not implementation completeness. No overall100% claim.

## Exact next actions and gates

1. DuckDB thin shared-record adapter passes52/52 native plus6controls, zero current rows, default soft deletion preserved; see DUCKDB_STORAGE_BASELINE.md. Continue remaining SQL/document family baselines; FlyIO/Nile already reuse PostgreSQL. Neon10controls and MotherDuck10controls only, hosted native open; MotherDuck now shares DuckDB transport/record policy (SQL_FAMILY_BASELINE.md). Fauna default host does not resolve; self-hosted source exists. Convex companion modules and REST family shared-record consolidation remain implementation gates.
2. Firebase canonical binding builds;23controls pass,2native cases skip without configuration. One RTDB4.11.2 JAR cached (34,495,935 B); execution policy rejected loopback19000 launch. No retry/bypass, owner file or listener; native run remains open.
3. S3 CI40731869610/job122970030713 passed52/52 and both post-suite bucket-empty checks. See S3_COMPATIBLE_STORAGE_BASELINE.md. PR501 remains draft; broader CI/deployment gates must pass independently.
4. Oracle cloud credentials/bucket/ADB access absent at last check. EVM signer/ABI/runtime and Cosmos bridge inputs remain gates; reuse existing resources before any installation. Keep original stores/keys/outcomes intact.
5. Development f8e872a4f merged as3f9ae8664. Core319bd96 includes7c45e2c: per-avatar request routing, owned-write outbox, fault gates and native-import contracts now present. Follow Docs/Devs/HYPERDRIVE_PER_AVATAR_SETTINGS.md and NATIVE_DATA_IMPORT.md; schema-unsafe holon properties must be excluded from GraphQL. Local changes preserved; no new all-provider routing proof claimed.

## Key commands

```powershell
git status --short
git fetch origin
python -B Scripts/Providers/update_provider_matrix.py
python -B Scripts/Providers/test_provider_matrix.py
python -B Scripts/Providers/test_conformance_reports.py
```

Wrap producer/consumer builds once in Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1; use cached restores and bounded workers. Native runners require original configuration/report review; do not reset failed fixtures to obtain a pass.

## Recovery links

[Matrix](PROVIDER_MATRIX.md), [gates](HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md), [inventory](ENVIRONMENT_INVENTORY.md), [cleanup](WORKSPACE_CLEANUP_LEDGER.md), [I/O scope](HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md), [evidence](EVIDENCE_LOCATIONS.md), [document family](DOCUMENT_FAMILY_BASELINE.md).

Provider-specific audits hold details; [archive index](Archive/ARCHIVE_INDEX.md) is historical lookup only. Update at each milestone; status under100lines, each summary under150, entire default recovery set about300lines.
