# Current provider status
The baseline table below is from 28 October 2026. Read PROVIDER_COMPLETION_CONTINUATION.md for later evidence and ACTIONS-FOR-DAVID.md in Release and Status for hosted fixture requirements.

10 November priority registration checkpoint. Neon, Convex, Supabase and SQL Server registration changes are on integration/priority-provider-registration at c5cc955689. The current HyperDrive branch at 97a7c75829 compiled the BootLoader and its project-reference graph locally with 0 errors and 2,139 warnings. The hosted WEB4 build and isolated real-backend checks remain open. Subsequent BootLoader changes preserve startup and registration errors for sync and async activation callers and await async startup in async registration. Compile and live checks for those changes remain open. See the [owner action list](../Release%20and%20Status/ACTIONS-FOR-DAVID.md) for the candidate build and fixture steps. Local builds are authorized with immediate owned-output cleanup recorded in the [cleanup ledger](WORKSPACE_CLEANUP_LEDGER.md). The subsequent error-propagation build lost its session before a final result was recovered. Its 3.56 GB artifact directory was removed under the build lease, and compilation remains unverified.

Public provider count audit (10 November). The matrix lists 234 shipping provider projects (178 storage or specialized storage and 56 non-storage). Alternate implementations count separately. NFT Founders main commit e5dbb6b is live at founders.oasisomniverse.one with 234 provider projects. OASIS Omniverse main commit f793074 is live at www.oasisomniverse.one with 234 provider projects. Separate Web4Site main commit e086f21 is live at web4.oasisomniverse.one with the updated inventory and registry copy. The DevPortal sandbox references 98 providers in its metered AI agent section, a separate gateway count. The oasisweb4.com homepage and resources show 234 provider projects, and the registry derives its displayed total from its entries. Verified all three oasisweb4.com pages with fresh HTTP 200 responses after main commit 6c5452e. Reconcile the directory entries to the matrix and use the inventory wording where publishing the project count. Live acceptance remains separately evidenced. Local builds and downloads may run under the existing resource rules, with generated outputs and disposable downloads cleaned and logged after use.

## Phase and rules

Active checkpoint, 10 November 2026. Current HyperDrive verification is split between retained native provider evidence and open isolated fixture gates. Development contains the scoped SQL Server avatar/history and graph-read batch at af621f3c3e. Development also contains the concurrent avatar and avatar-detail history regressions at 2a91bda8eb. Both passed separately on LocalDB and compiled together with Development Core 0fc929336 (zero errors). BigQuery hosted lifecycle verification awaits its isolated fixture. HyperDrive routing verification follows provider acceptance.

- Breadth first: real provider implementations and canonical core-storage-v2 (26checks per sync/async mode), approximately one day per family. Non-storage uses capability-v1, not storage tests.
- All applicable provider baselines precede global failover/load balancing/replication verification, then priority-provider depth. No Polkadot/NEAR/SQLite depth now.
- Use the existing C:/Source/OASIS2-hyperdrive worktree on work/hyperdrive-provider-completion. No new clones/worktrees/environments; reuse cached runtimes.
- Stage exact owned files, preserve concurrent dirty work/submodules, no force pushes/attribution trailers. Preserve the separate COSMIC/CQL checkout and branch.
- Check Development every 30 minutes during active work. Batch verified Development pushes under the repository deployment cadence. Fetch first, verify submodule commits remotely and check manifest/gitlink agreement. Never deploy unrelated/unverified changes just to synchronize.
- Before taking on a provider, check both this directory and the main checkout COSMIC ORM status and provider handoffs for active ownership. Update this checkpoint and affected provider evidence, matrix, handoff, and owner-action entries when ownership, acceptance, prerequisites, or integration changes.
- New raw evidence under TestResults; compact hash-bound summaries only are tracked. Maintain resource ledger, ten recent entries, byte-preserved archive rollovers.

Development now includes the COSMIC five-reader WEB4 batch at 111165cf8b. The HyperDrive branch merged the batch at 2fa940c4b8. Core fast-forwarded from 14bfd34f6 to 59595871, retaining the task commits, and the Railway manifest validates against the resulting gitlinks. EdgeSQLite local file-backed enumeration passed, and the provider matrix carries nine focused checks.

Development includes the Garnet and ClickHouse fixture guards at e1e3e9e06a. The Garnet Linux checkpoint skipped on Windows and all four ClickHouse storage smoke tests skipped without the writable fixture. These checks cover fixture admission. Prior native lifecycle receipts remain separate.

Neo4j promotion review: the task diff contains broad canonical, V2 and Aura changes. Preserve the canonical 52/52 receipt, with separate live gates for V2 activation and Aura lifecycle. This session has no Neo4j fixture configuration and no local Bolt listener on 7687 or on the retained fixture port 17687. The inventory identifies the latter as a shared Podman volume. No WSL distribution is registered on this host, and WSL reports that virtualization support is unavailable. Keep this larger provider batch separate from the verified Garnet fixture guard.

ClickHouse storage hard-delete changes are on the HyperDrive branch at e02321178e. The focused regression compiled and skipped. Live mutation acknowledgement requires the isolated writable fixture in Actions for David; COSMIC retains its separate read-only teaching source. The isolated CI ClickHouse job already supplies CLICKHOUSE_CONN; CI job 129955743319 on e1e3e9e06a started the disposable server, then failed three COSMIC fixture tests and skipped four storage tests. Development 9419d463f4 adds the write opt-in and selects only the storage class in that matrix row. [ClickHouse job 129973616147](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42857272015/job/129973616147) passed all four native storage smoke cases against the disposable ClickHouse server. The two load cases only assert that a result wrapper exists; this receipt does not prove saved-record readback. The workflow-only repair is separate from the hard-delete CI candidate 8052e10782. The candidate is published; GitHub denied draft PR creation (403), so its native job has not run.

Development 8ab80a5390 records both native SQLite import acknowledgement passes. Development d31cd59ee1 also records the child-role results. The HyperDrive branch synchronized at 6bb00758ce and retains the additional EdgeSQLite evidence. Eight file-backed SQLite checks passed for ordinary child-role persistence, replacement, publication and rollback. The focused test and ordinary writer sources already match Development. Solana storage remains partial at 4/52 with a missing memo chunk. COSMIC public read evidence covers a separate path, and the memo acknowledgement change awaits a native validator.

IPFS smoke-test fixture selection now requires explicit IPFSOASIS_TEST_API and compiles. The pending shipping activation diff publishes its client only after the daemon version probe succeeds. That change stays separate pending a live Kubo check. HyperDrive is synchronized with Development 63f2568e14.

Development a78e7ebaf9 replaces the Neon and Convex mock integration factories with guarded native harnesses and updates six inventory documents. Neon compiled and both cases skipped without hosted configuration. Convex compiled and retains its 52 local native passes. Both hosted acceptance gates remain open.

### Priority hosted gates, 10 November

| Provider | Evidence to carry forward | Next acceptance gate |
|---|---|---|
| Neon | Hosted conformance project compiled. | Isolated hosted Neon database for native sync and async lifecycle. |
| Convex | Real local Convex engine passed 26/26 in each mode, with cleanup. | Dedicated hosted companion deployment for activation and guarded writes. |
| Supabase | Hosted suite compiled and was discovered. COSMIC separately records local PostgREST demo evidence. | Dedicated hosted Supabase project for lifecycle, pagination and avatar/detail acknowledgement. |

The current process exposes none of the hosted fixture variables listed in [Actions for David](../Release%20and%20Status/ACTIONS-FOR-DAVID.md). Keep fixture access there and preserve existing native receipts. COSMIC tracks separate teaching-source acceptance for Neon (direct compute Npgsql reader), Convex (indexed exact-ID companion) and Supabase (bounded PostgREST reader). Preserve those results in the COSMIC scope. HyperDrive requires its own disposable writable fixtures for the lifecycle runs above. The same separation applies to BigQuery, where COSMIC requires a managed read-only teaching fixture.

## Evidence now

| State | Scope |
|---|---|
| Native core-storage-v2 52/52 | OracleDB, PostgreSQL, Redis, Mongo, Neo4j, CosmosDB emulator, Firestore emulator, AWS alias, DynamoDB, LocalFile, Azure Blob, SQLite, DuckDB, MinIO, Convex, Upstash |
| Native compatible52/52 | Shipping AWSS3OASIS on existing CI MinIO; independent empty-bucket checks passed; hosted AWS/IAM open |
| Recovered native52/52 summaries | SOLID/IPFS reports recovered from isolated checkout with exact registry hashes/contracts; original failed reports preserved, lifecycle/full-interface gates open |
| Controlled52/52 baseline | KeyDB, ValKey, Dragonfly and ThreeFold; consult the provider matrix for later native evidence. |
| Garnet native split evidence | Official Garnet 2.2.5 passed targeted lifecycle and cleanup checks across several runs. Full provider acceptance and a single registered native shared-suite report remain open. |
| Non-storage ActivityPub | Native public-GET capability5/5, controlled291; full social operations open |
| Other controlled cases | Firebase23, Appwrite19, PocketBase24, Convex24; these do not certify native storage |
| Partial native | Solana4/52; original referenced history unavailable, unresolved outcomes preserved |
The current matrix covers 234 shipping projects, including 56 non-storage and 178 storage or specialized-storage projects. The count includes the five new COSMIC reader packages; live acceptance remains scoped per provider.

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

Public count audit (10 November): Development 7fdf47792a carries the verified 234-project inventory and oasisweb4.com page source. Separate Web4Site and Omniverse source counts remain stale and branch creation is blocked (Actions for David). No matching OASIS-wide numeric count was found in the inspected Founders homepage/FAQ/Learn pages, OPORTAL React entry pages, or Docs and Learn homepages. Deeper pages and deployed revisions remain outside this check.

IPFS integration readiness (10 November): 346cd105a9 compiled under the provider build lease. A trial merge against Development 88cfc84a82 succeeded. Its intervening changes are confined to two CQL diagnostic files. The fixture guard is queued for a scoped batch under the repository's Development cadence. Live Kubo verification remains open.

Public count follow-up (10 November): OurWorldSite f70ae42 and OASISOmniverseSite db838f0 update homepage inventory copy to 234 provider projects on their respective work/provider-inventory-count branches. Omniverse 6c5d2a3 calculates the WEB4 catalogue count from its listed rows. A Node check of the page script returned 214 catalogue entries. Live deployments remain unverified. Separate Web4Site is still outstanding, and its local COSMIC checkout has uncommitted changes.

Separate Web4Site source follow-up (10 November): 4f823fd and 6e0cd9a update homepage and README inventory copy and derive the catalogue count from listed rows (214 verified in Node). The temporary site checkout was removed. Live deployment remains unverified.

Neon console harness fa22560447 compiled under the provider build lease with 0 errors and 1136 warnings. It requires explicit disposable Neon configuration. Hosted lifecycle verification remains open.

Convex console harness 1a0f239a77 compiled under the provider build lease with 0 errors and 1139 warnings. The harness requires an explicit disposable deployment URL and deploy key. Hosted verification remains open.

Supabase console harness d81549b200 compiled under the provider build lease with 0 errors and 1141 warnings. It requires an explicit disposable project URL and API key. Hosted verification remains open.

Supabase console failure exit status bbb1e1418b compiled with 0 errors and 1139 warnings. Hosted runtime verification remains open.

Development 510eea95e9 integrates the five-file Neon, Convex, Supabase and IPFS fixture batch, including the compiled Supabase exit-code fix. The push preserved the cross-checkout build lease and passed the Railway manifest check. No new live provider acceptance was established.

Neon ONODE registration: the BootLoader now binds Neon settings and the explicit connection override to the shipping Npgsql adapter. The cross-checkout build lease was busy, so compilation and hosted ONODE activation remain open.

Convex and Supabase BootLoader bindings construct the shipping clients from protected DNA settings. ConnectionString supplies the service URL and DeployKey or ApiKey supplies the corresponding credential. Compile and live ONODE activation remain open.

BootLoader at 5655afe141 compiled the Neon, Convex and Supabase shipping registrations with 0 errors and 2113 warnings. Live ONODE activation still requires the isolated hosted fixtures.

Scoped BootLoader candidate a70a6d5cb4 built against its Development dependency tree with 0 errors and 2123 warnings. Live registration and activation remain open.

BootLoader hot-activation error propagation (1a01cf27aa) compiled with 0 errors and 2130 warnings. Runtime verification through the shipping registration entry points remains open.

IPFS integration cleanup and SDK reference (defcb2ade3, bf041d53ae) compiled with 0 errors and 1148 warnings. The direct pinned IPFS Core reference resolved the client assertion compilation error. Live Kubo verification remains open.

Scoped IPFS teardown candidate 1f1021ec6a built against Development Core e5802022e with 0 errors and 1139 warnings. Live Kubo verification remains open.

Development 1f1021ec6a contains the compiled IPFS round-trip test teardown. HyperDrive synchronized at 1e7be4a153 with Core e5802022e and the matching Railway manifest pin. The IPFS runtime activation change still awaits live Kubo verification.

Combined BootLoader candidate 6b07640979 compiled with pinned Core e5802022e, 0 errors and 2142 warnings. Covers Neon, Convex and Supabase registration plus hot-activation error propagation. Live ONODE verification remains open.

ONODE single-provider registration fix ef90fa5ef1 remains on the task branch. The WebAPI build was stopped for disk recovery and its generated outputs were cleaned. Compilation and live endpoint verification are open. Development has no overlapping change to ProviderController.cs.

GitHub denied draft creation for ONODE candidate 9fd03698d3 (HTTP 403). The owner list tracks remote CI access. No local build or download was started.

Candidate 9fd03698d3 also keeps Convex and Supabase credentials bound to their configured service URLs. Earlier build evidence covers the BootLoader before this guard and the ONODE endpoint change. Both follow-ups await remote compilation and live verification.

Remote ONODE candidate cefa7d754a extends the compiled BootLoader baseline with credential binding and single/batch registration result handling. Live endpoint verification must cover an invalid batch rejected before registration and a valid mixed-outcome batch reporting failed provider types without exposing provider objects. No candidate CI run exists yet. Local builds remain deferred for disk recovery.

Candidate cefa7d754a passed in-memory Roslyn 5.0 syntax parsing for all four changed C# files. This check produced no build outputs. Type checking, full compilation and live verification remain open.

Current remote verification candidate: 8099fb6697 includes the Supabase production redirect guard. Its five C# files passed in-memory syntax parsing and all three new BootLoader references resolved. Development remains at 1f1021ec6a. The remote build and hosted activation gates remain open.

Remote registration candidate 8b9d00b0ccc2ec16bbca66988132ab0d3856d935 includes rejection of instances that ProviderManager does not register. Full compilation and live registration remain open.

SQL Server BootLoader binding 30c5798dbf uses the shipping SQLServerDBOASIS constructor and OASIS.StorageProviders.SQLServerDBOASIS.ConnectionString, with the existing explicit connection override. Syntax parsing and project reference resolution passed. Compilation and live registration remain open; retained native storage receipts carry forward.

TON harness commit 59fe0eb2b2 removes the legacy EVM constructor call that throws during aggregate harness startup. TonNativeLifecycleTests retains the local TVM fixture coverage. Aggregate harness compilation, hosted TON storage and hosted lifecycle verification remain open.
