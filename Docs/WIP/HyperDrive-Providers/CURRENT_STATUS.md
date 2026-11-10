# Current provider status
The baseline table below is from 28 October 2026. Read PROVIDER_COMPLETION_CONTINUATION.md for later evidence and ACTIONS-FOR-DAVID.md in Release and Status for hosted fixture requirements.

## Phase and rules

Active checkpoint, 10 November 2026. SQL Server is the current HyperDrive focus. Development contains the scoped SQL Server avatar/history and graph-read batch at af621f3c3e. The next batch covers concurrent avatar and avatar-detail history. Both cases passed separately on LocalDB and compile together with Development Core 0fc929336 (zero errors). The one-file batch is staged as 2a91bda8eb for the next scoped integration check. BigQuery hosted lifecycle verification awaits its isolated fixture. HyperDrive routing verification follows provider acceptance.

- Breadth first: real provider implementations and canonical core-storage-v2 (26checks per sync/async mode), approximately one day per family. Non-storage uses capability-v1, not storage tests.
- All applicable provider baselines precede global failover/load balancing/replication verification, then priority-provider depth. No Polkadot/NEAR/SQLite depth now.
- Use the existing C:/Source/OASIS2-hyperdrive worktree on work/hyperdrive-provider-completion. No new clones/worktrees/environments; reuse cached runtimes.
- Stage exact owned files, preserve concurrent dirty work/submodules, no force pushes/attribution trailers. Preserve the separate COSMIC/CQL checkout and branch.
- Check Development every 30 minutes during active work and integrate scoped verified batches; fetch first, verify submodule commits remotely and manifest/gitlink agreement. Never deploy unrelated/unverified changes just to synchronize.
- Before taking on a provider, check both this directory and the main checkout COSMIC ORM status and provider handoffs for active ownership. Update this checkpoint and affected provider evidence, matrix, handoff, and owner-action entries when ownership, acceptance, prerequisites, or integration changes.
- New raw evidence under TestResults; compact hash-bound summaries only are tracked. Maintain resource ledger, ten recent entries, byte-preserved archive rollovers.

### Priority hosted gates, 10 November

| Provider | Evidence to carry forward | Next acceptance gate |
|---|---|---|
| Neon | Hosted conformance project compiled. | Isolated hosted Neon database for native sync and async lifecycle. |
| Convex | Real local Convex engine passed 26/26 in each mode, with cleanup. | Dedicated hosted companion deployment for activation and guarded writes. |
| Supabase | Hosted suite compiled and was discovered. COSMIC separately records local PostgREST demo evidence. | Dedicated hosted Supabase project for lifecycle, pagination and avatar/detail acknowledgement. |

The current process exposes none of the hosted fixture variables listed in [Actions for David](../Release%20and%20Status/ACTIONS-FOR-DAVID.md). Keep fixture access there and preserve existing native receipts. COSMIC also lists a managed BigQuery read-only fixture requirement. Keep that scope separate from HyperDrive's writable lifecycle dataset.

## Evidence now

| State | Scope |
|---|---|
| Native core-storage-v2 52/52 | OracleDB, PostgreSQL, Redis, Mongo, Neo4j, CosmosDB emulator, Firestore emulator, AWS alias, DynamoDB, LocalFile, Azure Blob, SQLite, DuckDB, MinIO, Convex, Upstash |
| Native compatible52/52 | Shipping AWSS3OASIS on existing CI MinIO; independent empty-bucket checks passed; hosted AWS/IAM open |
| Recovered native52/52 summaries | SOLID/IPFS reports recovered from isolated checkout with exact registry hashes/contracts; original failed reports preserved, lifecycle/full-interface gates open |
| Controlled52/52 only | KeyDB, ValKey, Dragonfly, Garnet and ThreeFold; product-native shared-suite runs pending |
| Non-storage ActivityPub | Native public-GET capability5/5, controlled291; full social operations open |
| Other controlled cases | Firebase23, Appwrite19, PocketBase24, Convex24; these do not certify native storage |
| Partial native | Solana4/52; original referenced history unavailable, unresolved outcomes preserved |
229 matrix projects:56non-storage/173storage-specialized. SDK-source-unverified0, reviewed missing9; client detection is not implementation completeness. No overall100% claim.

## Exact next actions and gates

1. DuckDB thin shared-record adapter passes52/52 native plus6controls, zero current rows, default soft deletion preserved; see DUCKDB_STORAGE_BASELINE.md. Continue remaining SQL/document family baselines; FlyIO/Nile already reuse PostgreSQL. Neon10controls and MotherDuck10controls only, hosted native open; MotherDuck now shares DuckDB transport/record policy (SQL_FAMILY_BASELINE.md). Fauna default host does not resolve; self-hosted source exists. Convex now supplies its companion and native52/52; other REST shared-record consolidation remains an implementation gate.
2. Firebase canonical binding builds;23controls pass,2native cases skip without configuration. One RTDB4.11.2 JAR cached (34,495,935 B); execution policy rejected loopback19000 launch. No retry/bypass, owner file or listener; native run remains open.
3. Dynamo relocation evidence revalidated: pinned JAR hash matches; both aliases52/52, five tables empty, compacted DB43,008B, evidence folder under200MB. Scripts already pushed asf446b70a2; current summaries now under TestResults. S3 CI40731869610/job122970030713 passed52/52 and both post-suite bucket-empty checks. See S3_COMPATIBLE_STORAGE_BASELINE.md. PR501 merged externally at7b834548e; later work is not covered by that merge. Shared S3 base serves AWSS3/MinIO/ThreeFold; MinIO native52 and AWSS3 compatible52 verified in CI40754776833/job123050672281.51family/41AWS/60shared/5QSS controls pass; full/hosted gates remain (S3_COMPATIBLE_STORAGE_BASELINE.md).
4. Oracle cloud credentials/bucket/ADB access absent at last check. EVM signer/ABI/runtime and Cosmos bridge inputs remain gates; reuse existing resources before any installation. Keep original stores/keys/outcomes intact.
5. Development 953cb7968 synchronized for PR502. Published Core261fce1 pin/manifest matched; local Core checkout remains owner-held e46ce28 with dirty files preserved, so CI must establish the new source cohort. Core includes7c45e2c: per-avatar request routing, owned-write outbox, fault gates and native-import contracts now present. Follow Docs/Devs/HYPERDRIVE_PER_AVATAR_SETTINGS.md and NATIVE_DATA_IMPORT.md; schema-unsafe holon properties must be excluded from GraphQL. Local changes preserved; no new all-provider routing proof claimed.

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

## Reading the current registry

The 32 registered report paths and SHA-256 hashes matched in the HyperDrive worktree. The registry now includes TON's 52/52 native lifecycle result. Polygon, Arbitrum and Cosmos blockchain still have configuration-only rows in that registry. Their separate evidence, where present, must be assessed on its own scope. SQL Server has split LocalDB evidence in PROVIDER_COMPLETION_CONTINUATION.md. ActivityPub's 5/5 row covers its public-read capability suite. These different scopes must remain distinct when selecting the remaining native checks.
