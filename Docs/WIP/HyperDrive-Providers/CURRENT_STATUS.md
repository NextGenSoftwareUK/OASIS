# Current provider status

11 November IPFS independent writers. [GitHub Actions](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43009364727) at caa7f39b1c exercises two provider instances with separate DNA objects and the same file. One instance publishes before the other begins writing, then both submit saves concurrently. A fresh provider must recover all eight acknowledged IDs. This is a new native scenario; the accepted single-instance and restart scenarios are excluded.

11 November IPFS concurrent publication. [GitHub Actions](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43008085065) at 1c822023e3 runs eight overlapping holon saves against a disposable Kubo repository and requires a fresh provider to load the exact acknowledged IDs from persisted DNA. Source review found shared catalogue and root updates across awaited operations. The baseline test failed after native record acknowledgement with collection mutation and DNA file sharing errors. Task revision 40cdab6b44 serializes publication and catalogue reads within a provider instance and restores its previous in-memory state when pointer persistence fails. The [corrected run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43008574348) passed. A fresh provider loaded all eight acknowledged records from persisted DNA. Development 2c11a120ae contains the verified IPFS sources and both tests. The previously accepted activation and restart cases were excluded from the concurrent run. Independent provider instances and processes require separate verification.

11 November IPFS restart verification. [GitHub Actions](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43007340696) at 2182db5690 runs two test-host processes against the same disposable repository, with a real Kubo daemon stop and restart between them. The write phase creates two encrypted holon versions through the shipping provider. The read phase reloads the persisted DNA and checks both versions. Both phases passed. The first Kubo process stopped before the second started, and the read phase recovered the current and historical versions. This verifies a fresh repository through a graceful daemon restart; historical fixture recovery and concurrent publication remain open.

11 November IPFS native activation CI. Task revision cb967faad2 starts the guarded sync and async activation cases on an isolated GitHub Windows runner with the checksum-pinned Kubo 0.43.1 release. The [run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43006107859) checked out the recorded provider and Libraries dependencies. The first run downloaded and initialized Kubo, then its PowerShell RPC probe returned HTTP 403 before the provider test. The CI probes now use the same explicit non-browser User-Agent as the retained fixture runner. The [corrected run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43006440924) passed both synchronous and asynchronous activation and deactivation cases against the pinned Kubo 0.43.1 daemon at fdac8a7b81. Development c686a8618d contains the identical provider and integration test files.

11 November IPFS activation verification. The official Kubo v0.43.1 release API lists the Windows amd64 archive at 42,170,789 bytes with SHA-256 c25871516dec4d97ce1d160a26c1e3fdad294cb0eea065c0c0343bcfca86c524. The distribution host timed out. The proposed isolated Windows probe using the official GitHub release was rejected before launch by automatic policy, so no native activation result was produced for 736c2fda51. The prior 52/52 storage evidence remains valid. The retained repository and protected key needed for the historical continuation remain in the existing owner action.

11 November SQL Server avatar wallets. The shipping provider now loads interface-shaped wallet records without changing the stored JSON schema. The native regression exposed loss of the getter-only native transaction ID with interface-only deserialization. The corrected reader reconstructs the concrete transaction and preserves its native identity. The LocalDB 17.0.4025.3 run passed 1/1, with zero skips. It covered current avatar loading after a version update and historical point and collection loads with the original wallet and transaction. Development 400b42ef36 contains the tested provider and integration test. Full provider acceptance remains open.

11 November live site recheck. WEB6 main 8f4f68e removes the remaining 222-storage-provider claims on the homepage and both Holonic Braid whitepaper pages. The public pages returned HTTP 200 with the corrected COSMIC copy and the 234-project OASIS inventory reference. OASISOmniverseSite main 3ede46f updates the public README to 234 shipping provider projects. Fresh checks found 234 distinct projects in both WEB4 registries and current counts on the Omniverse homepage, NFT Founders, One World, Our World, OASIS Docs and OASIS Learn. The published OPORTAL and /devportal routes returned HTTP 200. The inspected portal source has no OASIS-wide provider total.

11 November MongoDB graph transaction. The shipping provider passed on a fresh one-member MongoDB 8.2.1 replica set. A native validator rejected the child with code 121. Independent inspection found zero holon rows and both caller IDs remained empty. The corrected graph committed two rows, published both IDs and loaded the same child through the provider. This run verifies the provider's graph transaction on a replica-set primary. Development 1afe6f7923 carries the matching native test.

11 November MongoDB metadata queries. The shipping provider saved three holons to a fresh MongoDB 8.2.1 standalone server. Synchronous and asynchronous metadata queries both returned the exact expected GUID sets and loaded counts for All and Any. The test used the installed MongoDB binary on a disposable loopback port and left the historical conformance fixture untouched. Development 88dcb9ada1 carries the matching source and test.

11 November Polygon PoS wallet balance. The synchronous shipping blockchain provider interface returned a positive native POL balance through unsigned Nethereum at PublicNode's Polygon PoS endpoint. The shared wallet reader verified chain 137 and did not activate contract storage or use a signer. Development e5530b0914 carries the matching shared reader and both the Polygon and Arbitrum tests. Public-network storage lifecycle remains open.

11 November Arbitrum One wallet balance. The shipping Web3Core wallet path now uses an unsigned Nethereum client without activating contract storage. The focused live test read positive ETH on Arbitrum One without caller-context posts and rejected that endpoint when configured for chain 137. This verifies the shared read implementation on the Arbitrum V2 facade. Public-network storage lifecycle verification remains open.

11 November SQLite wallet encryption. The shipping WalletManager generated a local private key and recovery phrase using an ephemeral DNA encryption key. The shipping provider saved the encrypted wallet in a disposable SQLite file. An independent SQLite query found the ciphertext and neither plaintext value. After reopening, WalletManager recovered the same values on a detached response and the loaded wallet retained ciphertext. Development 4489034aa1 carries the matching native test. This covers local wallet encryption and persistence through the tested key configuration.

11 November Convex response deadline. The shipping HTTP adapter now applies the configured client timeout while reading the response body. A real loopback TCP/HTTP test stalled a successful readiness response after its headers, verified that the body timed out, and completed a subsequent probe on the same backend instance. Development 2e2a4dc0d3 carries the verified transport and readiness fixes. Hosted companion acceptance remains open.

11 November Supabase credential isolation. The shipping PostgREST adapter attaches each project's API key to its own requests and no longer puts credentials on the shared client's default headers. Hosted origins require HTTPS and explicit local PostgREST can still use loopback HTTP. The 34 request and storage tests passed. The five metadata cases then passed with an explicit offline constructor after removing their reflective factory. Dedicated hosted verification remains open.

11 November Polkadot Asset Hub wallet balance. The synchronous shipping wallet method passed through @polkadot/api 17.0.2 against the public HTTPS RPC. It verified the Asset Hub genesis, read a positive finalized treasury DOT balance and posted no continuations to the caller context. No signer or storage contract was configured. Development abf589b08a carries the matching wallet fix and regression test. Shared storage lifecycle verification remains open.

11 November TRON mainnet wallet balance. The synchronous shipping wallet call passed using PublicNode's TLS full and solidity gRPC endpoints. The SDK verified both genesis responses, read a positive solidified TRX balance, and posted no continuations to the caller context. No signer or storage activation was used. Development 6ce625714f carries the matching wallet fix and regression test. The separate writable contract lifecycle still requires the dedicated non-mainnet fixture.

11 November Solana ledger recovery. The history probe now checks the expected genesis before querying any signature and includes the verified genesis in its output. Six protocol controls passed. A read-only mainnet check found a finalized public transaction at slot 468063580 and rejected the retained validator's genesis against that same endpoint before reading history. The original Solana storage fixture remains unavailable and its 4/52 result is unchanged.

11 November Cosmos Hub wallet balance. The synchronous shipping wallet method passed a live read of the distribution account through CosmJS 0.39.0. It returned a positive ATOM balance on cosmoshub-4 with no signer, storage activation or posts to the caller synchronization context. Development f4d314f7a3 carries the scoped wallet fix with matching shipping provider, SDK bridge and shared dependencies. The prior contract storage result remains 0/52 before native dispatch and the retained wasmd fixture remains open.

11 November matrix reconciliation. Scoped notes from 35 provider rows now live in the conformance registry so they survive regeneration. The regenerated matrix preserves all 234 project identities, all prior row notes and the registered conformance metadata. Scoped native checks and interrupted runs remain documented in the notes for providers without a registered suite result.

11 November NEAR public wallet balance. Both shipping wallet entry points passed against the public NEAR mainnet RPC through the pinned near-api-js 7.3.1 bridge. The bridge verified network identity and read the finalized balance for `near`. The tests required a positive amount and confirmed storage stayed inactive with no signer or storage contract. Development ae885fc092 carries the scoped wallet fix and regression test. The wallet sources, bridge and Core revision matched the tested path. The SDK service differs only by the task branch's independent avatar deletion method. The existing mainnet results were reused. Dedicated writable lifecycle acceptance remains open.

The baseline table below is from 28 October 2026. Read PROVIDER_COMPLETION_CONTINUATION.md for later evidence and ACTIONS-FOR-DAVID.md in Release and Status for hosted fixture requirements.

10 November priority registration checkpoint. Neon, Convex, Supabase and SQL Server registration changes are on integration/priority-provider-registration at c5cc955689. The current HyperDrive branch at 97a7c75829 compiled the BootLoader and its project-reference graph locally with 0 errors and 2,139 warnings. The hosted WEB4 build and isolated real-backend checks remain open. Subsequent BootLoader changes preserve startup and registration errors for sync and async activation callers and await async startup in async registration. Those error-propagation changes compiled at 820bebbecf with 0 errors and 2,139 warnings. The missing-DNA regression at b0d32552f0 passed both activation cases and failed both direct registration cases. The direct methods now initialize their result before startup so the error handler can populate it. At a41551c70f, all four missing-DNA regression cases passed through the sync and async registration and activation entry points. Development now includes the two-file error-propagation batch at 9b87fda434. The exact Development candidate passed the same four cases and cleaned its isolated artifacts. Development CI run 42966121834 passed BootLoader restore, build and test in job 130343662692. Solution Integrity job 130343662547 failed the existing project coverage check, including COSMIC readers and provider test projects. The Railway dependency pin job passed. Development 9b8a30a65c enrolled the four Qdrant and Weaviate storage test projects in the full solution. MSBuild solution configuration validation passed and the coverage checker reported 12 remaining projects outside any solution. Provider execution remains open. Live backend registration checks remain open. The isolated test artifacts were removed. See the [owner action list](../Release%20and%20Status/ACTIONS-FOR-DAVID.md) for the candidate build and fixture steps. Local builds are authorized with immediate owned-output cleanup recorded in the [cleanup ledger](WORKSPACE_CLEANUP_LEDGER.md). The interrupted verification was reconciled and cleaned. The replacement build at 820bebbecf passed, with artifact cleanup confirmed by its receipt and an independent path check.

Public provider count audit (11 November). The inventory lists 234 shipping provider projects: 178 storage and specialized storage, and 56 non-storage. Alternate implementations count separately. Both public WEB4 registries were checked live and each contains 234 distinct project names. Web4Site main 7dbc4b2 updates the registry and the ONET statistics label. oasisweb4.com main d696c8e updates its registry. OASISOmniverseSite main b26c27f removes the remaining 222-project references. Web6Site main 0531a9b updates the homepage and both Holonic Braid whitepaper pages. OneWorldSite main dd00232 updates its provider badge. Fresh HTTP checks verified the changed copy, the existing 234 count on NFT Founders, Our World and the oasisweb4.com homepage and resources, and the public OPORTAL, Docs and Learn homepages responded. The DevPortal sandbox uses a separate AI gateway count. Provider backend acceptance is tracked separately in the matrix.

Public documentation follow-up (11 November). [OASIS Docs Provider Overview](https://docs.oasisomniverse.one/providers/overview) at 61e1323 and [OASIS Learn Providers & Production](https://learn.oasisomniverse.one/tutorials/08-providers-production) at 3120b29 now display 234 shipping provider projects and link to the verification matrix. Both pages returned HTTP 200 and the new copy after publication. OPORTAL React and the DevPortal source contain no OASIS-wide provider total. The generated inventory passed `Scripts/Providers/update_provider_matrix.py --check`. The site changes required no local build, download or retained test artifact.

11 November SQL Server BootLoader probe at c277943300 passed against the isolated LocalDB instance. BootLoader registered, activated and deactivated SQLServerDBOASIS. SQL Server reported six tables created through activation. The fixture, instance and build outputs were removed. The isolated SQL Server binding then passed the same activation checks on exact Development candidate c39eba5cd1 and was fast-forwarded into Development. The disposable LocalDB instance and fixture are absent. Hosted WEB4 activation remains open. Development BootLoader CI job 130371952550 passed on this revision. Solution Integrity job 130371952583 failed.

11 November EVM evidence reconciliation. Ethereum, Arbitrum and Polygon each retain 52/52 native core-storage-v2 checks through shipping factories and isolated Hardhat contracts at 1ed77c18d6. Both original reports per provider matched the existing hashes and recorded complete fixture cleanup. The conformance registry now uses those reports and links the historical v1 configuration failures. Development 94fe19c958 contains the verified evidence and all three displayed totals. The HyperDrive branch merged that Development revision at a507fbfbe5, preserving a single SQL Server registration. Public-network acceptance remains open. Solana remains at 4/52 while its original validator ledger and protected test wallet are unavailable.

11 November Neo4j Aura metadata. The native driver writes metadata and its exact lookup tokens in the holon upsert transaction. Point and collection reads use the same projection, including typed single-key, all-of and any-of queries. The guarded lifecycle case compiled and skipped without the dedicated Aura fixture. Hosted verification remains open.

11 November Supabase storage. PostgREST storage awaits no longer capture the caller context used by the synchronous entry points. Graph saves preserve fields captured from derived holons and retain the staged identity and provider keys. All 39 existing unit tests passed, including the exact decimal payload regression. Dedicated hosted lifecycle verification remains open.

11 November Convex companion readiness. A failed or canceled probe no longer permanently blocks the existing provider instance. Concurrent callers share the protocol verification but can cancel their waits independently. Hosted recovery verification remains open.

11 November Neon connection policy. Neon endpoints with inline ports or in explicit host lists now receive Npgsql VerifyFull. A single endpoint can be mapped to its pooler without changing its port. Explicit host lists retain their order, ports and endpoint names. All 13 configuration and provider unit tests passed at e4e9605424. Development bbb4706313 carries the matching Neon source and test. Hosted sync and async lifecycle verification still requires the dedicated Neon database.

11 November Garnet evidence. Verified the hashes of both original native checkpoints and three focused sync/async result pairs, including the parented-child cleanup. The [split evidence](NativeEvidence/garnet-split-storage-v2.json) now records their exact scope and outcomes. The controlled suite and full native acceptance remain separate. Development records the same split evidence at 371f3d5dc4.

11 November TRON lifecycle admission. The guarded native runner now rejects mainnet by network label, numeric chain ID in decimal or hexadecimal, or the known mainnet genesis hash before it creates reports or connects. It also rejects malformed chain IDs and genesis hashes. Five admission tests passed and the native theory skipped without the dedicated writable fixture. COSMIC read-only TRON access is separate.

11 November EOSIO public balance. The shipping EosClient passed a live positive-balance read against the Vaulta chain. It checked the chain identity and decoded one positive EOS amount from eosio.token for eosio.saving. The node reported head block 530137880 during identity validation. The exact two-file Development candidate 16d22e842e passed the same public check at head block 530138483 and was fast-forwarded into Development. No signer or transaction was used. The EOSIO wallet balance facade subsequently passed both sync and async live checks with no signer and French locale settings. Both cases also passed on the exact Development candidate 3f5d781f29, which was fast-forwarded into Development. Signed storage and the remaining wallet interfaces still require verification.

11 November Hedera wallet balance. The synchronous shipping wallet entry point read a positive HBAR balance for 0.0.3 from the live testnet mirror. The caller synchronization context received no continuations and storage activation remained false. Development a7522f4286 carries the identical verified provider source and focused test. The separate funded storage test source remains owned by its existing workstream. Funded storage and remaining wallet operations still require their own checks.

11 November ActivityPub synchronous reads. The shipping adapter passed a live Mastodon status and reply read while a tracking caller context received zero continuations. The initial public post reported 20 replies and exposed 19 direct replies, which the existing completeness check rejected. The retained complete public thread then passed. The direct-provider test configuration passed. Development fcf2a522e6 carries the same verified adapter, test and direct-provider project configuration. The default CQL-linked integration build has missing references for unrelated providers. Authenticated search and owned writes remain open.

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

IPFS smoke-test fixture selection now requires explicit IPFSOASIS_TEST_API and compiles. The pending shipping activation diff publishes its client only after the daemon version probe succeeds. The live Kubo check subsequently passed and the verified change is now in Development c686a8618d. HyperDrive is synchronized with Development 63f2568e14.

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

SQL Server BootLoader binding 30c5798dbf uses the shipping SQLServerDBOASIS constructor and OASIS.StorageProviders.SQLServerDBOASIS.ConnectionString, with the existing explicit connection override. The registration binding compiled with the BootLoader graph at 97a7c75829 and 820bebbecf. Live registration remains open. The host has LocalDB 17.0.4025.3 and both retained test database files, but the MSSQLLocalDB instance directory is absent and the CLI reports the automatic instance is not created. Preserve the retained database files during fixture recovery. Existing native storage receipts carry forward.

TON harness commit 59fe0eb2b2 removes the legacy EVM constructor call that throws during aggregate harness startup. TonNativeLifecycleTests retains the local TVM fixture coverage. Aggregate harness compilation, hosted TON storage and hosted lifecycle verification remain open.

11 November SQL Server BootLoader probe at a5fcf80fcc: restore and build passed. The isolated LocalDB instance started and the fixture database was created. The harness stopped at passive boot, before activation and schema verification. Source review found the fixture used the rejected literal V2 where the DNA loader requires OASISHyperDrive2. The runner now uses the accepted value and explicitly disables automatic routing in the V2 policy. Live rerun remains open.
