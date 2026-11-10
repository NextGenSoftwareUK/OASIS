# Provider completion continuation

2026-11-09. Isolated worktree C:/Source/OASIS2-hyperdrive, branch work/hyperdrive-provider-completion, based on Development 871e3d7184. The previous provider branch had already been merged into Development and its remote head is no longer advertised. Start from CURRENT_STATUS.md and PROVIDER_MATRIX.md. Treat their evidence as historical until matched to the current provider and dependency revisions.

1. Verify the current matrix against the production provider registrations and each provider's advertised interfaces. Preserve separate states for missing code, controlled tests, native conformance and full interface coverage.
2. Continue SQL and document provider baselines. Review the Neon native gate and the missing Convex server functions first. Use one reusable test environment and run native lifecycle conformance through the shipping adapters. Record external access decisions in the existing David action list when needed.
3. After provider acceptance, exercise HyperDrive auto failover, auto replication and auto load balancing across eligible providers with real backends. Verify operation outcomes, persistence after failover and replication convergence. Use capability checks for non-storage providers.
4. Commit scoped verified batches on the task branch. Keep the COSMIC/CQL checkout and its pending changes separate.

## First-provider evidence, 2026-11-09

- Neon now has a dedicated sync/async native suite entry point. A real Neon endpoint and a disposable oasis_provider_test database are required. No connection was configured in this process. The test checkpoints fixture identities before dispatch.
- Convex ships the internal oasis/records companion. The provider matrix records 52/52 historical native core-storage cases. The existing runner expects a retained Ubuntu WSL installation and protected key. This Windows host reports no installed WSL distributions. Full advertised interface acceptance remains open.
- Supabase already has a native CQL fixture runner using PostgREST and a dedicated test identity. Its evidence covers query and restart behavior. The provider matrix has no shared core-storage lifecycle run. The existing Linux runner also needs a reusable runtime on this host.
- Priority re-audit includes Solana, MongoDB, SQLite, Neo4j, Supabase, SQL Server, Oracle, AWS, IPFS, NEAR, Cosmos, Azure, Google, Polkadot, ActivityPub, SOLID, Polygon, Arbitrum, TRON, EOSIO, TON and Hashgraph.

### Supabase lifecycle findings

The retained COSMIC fixture provisions only public.oasis_holons and grants its API role SELECT, INSERT and UPDATE. Its retained source hash and data are used for restart evidence. Preserve that fixture. A separate lifecycle test database on the same reusable service must provision public.oasis_avatars and public.oasis_avatar_details, and provide an atomic hard-delete contract for avatar and details. The core suite also requires exact-fixture cleanup for holon tombstones. The shipping adapter now validates avatar mutation acknowledgements and reports confirmed soft holon deletion. These edits are awaiting build and real-backend checks.

### Priority evidence triage

- Native core-storage v2 baselines exist for MongoDB, SQLite, primary Neo4j, Oracle DB, AWS DynamoDB Local, Azure Cosmos DB, Azure Storage, Firestore emulator, IPFS and SOLID. Their scope is the shared storage contract. Hosted accounts, distinct provider variants and additional advertised interfaces need separate evidence.
- ActivityPub's native capability baseline covers public reads. Full social writes and delivery remain separate gates.
- Solana has 4/52 native cases and a retained missing-memo-chunk failure. Preserve those fixture identities for root-cause investigation. Polygon and Arbitrum have configuration-only historical failures. Cosmos chain, unlike Azure Cosmos DB, has a blocked native run.
- Supabase and SQL Server have no shared storage baseline in the matrix. NEAR and Polkadot have operation-specific evidence but no recorded shared baseline. TRON, EOSIO, TON and Hashgraph likewise need provider-specific interface review and native evidence.
- The matrix used to count commented-out throws as stubs. The scanner now strips comments before counting. Remaining candidates still require source and registration review.

### Local Convex Windows backend and dependency checks

- Convex companion typechecked with TypeScript 5.9.3 using the installed Node 24.22.1 runtime. The pinned Convex CLI 1.48.0 deployed the companion to its official Windows engine, precompiled-2026-10-22-1447815, bound to loopback port 13210. The authenticated native list returned an empty store and a complete cursor. An identical unauthenticated request returned a Convex error envelope. Neither probe wrote records.
- The synthetic fixture storage key is retained outside Git under the Windows user's protected OASIS provider test settings. The CLI deployment state and administrator identity remain local. Keep both available until any partial fixture is reconciled.
- Neon now compiles against the locally merged Core revision. The two native tests were discovered and skipped without a configured Neon endpoint. Supabase compilation reached its MongoDB integration dependency and failed on the absent ICosmicQueryEntityProjectedAllStorageProvider interface. Published Core commit 21884e7 is the single missing commit on that branch and a merge preview is clean. Keep the build inputs frozen until Convex releases its lease before evaluating that Core change.

- The Windows Convex run recorded async 26/26 with an independently empty table. Sync advanced to 22/26 in the durable pending report and stopped while publishing the report. Concurrent report inspection on Windows may have interfered with the rename. That cause remains unconfirmed. Preserve both report versions.
- The native read-only reconciliation passed and matched all five remaining encrypted records to the sync fixture. Revision-checked cleanup is running against the same store. A clean recovery is separate from a completed sync run.
- The matrix tests initially passed 12/13. Polygon ID's added public schema reader invalidates its older hash-bound negative audit. The corrected targeted test passes and confirms HTTP source evidence. Regeneration is running.

### Convex Windows rerun

The revision-checked cleanup passed and independently confirmed an empty store. The first-run reports and cleanup intent were archived under TestResults/ProviderConformanceCurrent/ConvexOASIS-windows-first-run. The real Windows engine then passed both shared suite modes, native revision conflicts and 105-record pagination. The passing binary cohort used Core 7f722cbdc68bc56fcbc3af1dc6f1b750ebd31ae7. The subsequent Supabase build uses Core 14bfd34f6e34d2be9f4bdec34b3431b8ce356fe5. Hosted and broader interface gates remain open.

- Convex async native report: 26/26, fixture cleanup confirmed. SHA-256 AB642C2D43B04073DE18C9B358B342CEA84FDE3D0850C44861351E85B02E8485.
- Convex sync native report: 26/26, fixture cleanup confirmed. SHA-256 B1F705B189A097999369964E10D52834F8D5D1494CE1C212B2CF4A917880AF3A.

### Matrix and SQL Server verification

- Regenerated matrix commit 974243149b. Full inventory regression rerun passed 13/13.
- SQL Server LocalDB 17.0.4025.3 created the dedicated oasis_hyperdrive_provider_test database on MSSQLLocalDB. Independent SQL inspection showed zero user tables before provider activation. The COSMIC instance was not used. Native suite build is pending.
- The Supabase integration dependency exposed a parent merge conflict in Mongo replica method signatures and outbox construction. The scoped repair keeps both native destination binding and initialization contracts. Build and native Mongo verification are still required.

- Supabase native lifecycle tests compiled after the Core and Mongo dependency repairs. Both modes were discovered and skipped without a dedicated hosted project. The credential and soft-delete regression cases are running separately.
- SQL Server native sync and async first runs reached avatar-save, with one confirmed avatar row per report ID. The provider had omitted save acknowledgement despite the committed write. The two exact owned rows were reconciled and removed in one transaction. Original reports and cleanup intent are under TestResults/ProviderConformanceCurrent/SQLServerDBOASIS-avatar-save-first-run. The provider now checks affected-row counts for avatar saves and returns read acknowledgements. These changes await the clean LocalDB rerun.

### Development integration cadence

David requested two to three scoped, verified integration batches per day. Fetch Development before each batch and review the current shared head and dependency pins. Keep the unpublished Core revision and unfinished provider changes out of Development until the matching dependency is available remotely and the affected checks pass. Preserve the current COSMIC/CQL checkout.

- Development received scoped inventory and cleanup registration merge 8216f17f45. Core-dependent changes remain on the provider branch.
- Reuse existing passing native evidence for unchanged scope. Run tests for missing coverage and changes requiring proof. Do not repeat accepted suites as a routine step.
- Retire owned disposable test outputs and downloads when verification finishes. Preserve compact reports, pending fixture evidence and reusable pinned runtimes. The attempted bulk cleanup of four completed test projects was blocked by command policy and did not reclaim space.
- SQL Server's second run passed activation, avatar save, ID and username in both modes before failing email read acknowledgement. The email path now shares the existing SQL point lookup and reports one loaded row. Both owned fixture rows were removed in one guarded transaction, and reports were archived under TestResults/ProviderConformanceCurrent/SQLServerDBOASIS-avatar-email-run.

### SQL Server native lifecycle progress

- Both retained legacy avatar-detail rows passed the focused shipping read after the concrete canonical decoder change. The original failed reports and exact cleanup intent are under TestResults/ProviderConformanceCurrent/SQLServerDBOASIS-detail-read-run.
- The graph fix built and both native modes passed through import. The first failing check was owner-export. Its old JSON predicate matched zero rows, while the typed creator predicate matched exactly five rows per fixture owner. A focused shipping export test reads the retained fixtures without replaying writes. Until it passes, preserve all retained rows and reports.

SQL Server completed the remaining native core-storage lifecycle checks on the same retained fixtures. The split evidence retains the original stop and separate targeted passes. No uninterrupted 52/52 run is claimed. An independent LocalDB query confirmed all three fixture tables are empty. Other SQL Server interfaces remain outside this result.

- SQL Server async mode: 17 initial shared checks before owner export. Focused owner export and retained deletion continuation passed. Report SHA-256 0EA0DD5CA8C16B88843AD74E6707D854998FF48CEED567E38540CFFF1DC59C78. Journal SHA-256 3C5C893F9A22C5C23E830A8F8D11602841A0BA556C72F8CF2F0B1D06F1DBCA16.
- SQL Server sync mode: 17 initial shared checks before owner export. Focused owner export and retained deletion continuation passed. Report SHA-256 4CE7902410A64E70551872F21F508EE5B74B8C6054FB0F908829D75531A5B698. Journal SHA-256 A8B9A62BA99F4A5CEDC4DCF61DDCC427AF34E44445D636319E17992DD7EA0878.

### Solana memo acknowledgement

The shipping Solana provider builds with a finalized memo read-back check before each chunk can be referenced and before the record commit can be acknowledged. The change preserves uncertain signatures for reconciliation. It has no new native runtime evidence and does not explain the historical absent memo. Preserve the original ledger if recovered. The owner access action is in the central list.

### Neo4j V2 entry point

V2 now delegates to the canonical Neo4j provider and official driver, with its existing provider identity and storage format. The canonical provider's bound-replica merge conflict was repaired using its existing head validator. The V2 identity test and focused provider inventory assertion passed. The native activation smoke test compiled and skipped without a dedicated Bolt endpoint. The historical canonical storage suite was not rerun. Native V2 activation and the changed replica paths remain open.

### Supabase graph and detail storage

Supabase uses one PostgREST upsert for the selected graph and validates the exact acknowledged row set before publishing IDs. Uncertain responses preserve all proposed IDs. Avatar detail uses the canonical concrete serializer. The two changed-path soft-delete regressions passed and the provider compiled. Hosted graph, detail and full lifecycle verification require the dedicated Supabase project listed in Actions for David.

### Hedera evidence review

Existing Hedera public mirror results verify bounded read-only account metadata. They do not prove mutable testnet storage or asset operations. The historical prepared-read run stopped at its testnet funding gate. The original deployment and pending outcomes need reconciliation before using the retained testnet identity. No balances, credentials or native writes were replayed during this review.

### TRON and EOSIO evidence review

TRON's retained public evidence verifies bounded solidified block headers and the documented public query scope. EOSIO/Vaulta has accepted hosted read-only account queries. Neither result exercises the credentialed holon write path. EOSIO holon save acknowledgement now includes the persisted row count and preserves exceptions. Its persistence logs omit serialized record contents. Native write, graph, and broader interface acceptance are still open.
The focused EOSIO native integration test built and discovered one case on .NET 10. It skipped without an explicitly configured Antelope fixture (0 passed, 0 failed, 1 skipped). This proves test discovery and the fixture gate only.
The EOSIO build for aa79255dc1 passed with zero errors. A read-only probe loaded the compiled shipping EosClient and submitted two currency-balance requests to the retained Vaulta RPC for eosio / eosio.token. Both decoded successfully to zero rows (symbols A and ZZZZZZZ). This verifies native JSON transport and empty-array decoding. Nonempty results, provider facade behavior, and signed writes remain separate gates.
EOSIO activation now validates the native endpoint chain ID before initializing signing repositories. The provider built with zero errors. The compiled async activation path rejected a mismatching configured chain against the live Vaulta endpoint with the expected chain mismatch error, using no signer and sending no transactions. Matching-chain signed activation and the synchronous runtime path remain open.

### TRON native storage evidence reconciliation

The earlier public-read summary did not include the separate local native storage cohort. The retained [TRON native report](../COSMIC%20ORM/TRON_CQL_MVP.md) documents three encrypted synthetic holons persisted through the shipping provider, an initial lost-record failure after restart, and a corrected restart with all three records, a bounded two-record query and an empty missing-record query. The report identifies the original TRX and SHA-256. Preserve this historical scope without replay. It does not establish current binary parity, full shared lifecycle coverage, or hosted signed-write acceptance. The obsolete integration assertions removed in 062fcefab7 expected storage to be unsupported and do not represent that native suite.
On the Windows provider worktree host, the TRON local node ports 50051, 50052 and 9090 had no listeners and the retained lifecycle contract-address artifact was absent from this worktree. No ledger was started or recreated. Reuse the guarded retained runtime once it is available and select only missing lifecycle coverage.

### TON shipping scope

The registered TON provider is a storage provider backed by the local native TVM emulator and the shared encrypted record implementation. Its legacy EVM constructor rejects use explicitly. Public TON header metadata has a separate read-only source. The project named TON integration tests checks offline provider metadata only. Keep existing native TVM evidence separate from those checks. Public-network storage transport, wallet and NFT capabilities remain outside the current shipping implementation and cannot be inferred from TVM or public-header results.
The registered TONOASIS provider passed 26/26 async shared storage lifecycle checks on the pinned local TVM engine. Its terminal checkpoint confirms exact fixture cleanup and has SHA-256 999172CA532100458472F2366AE23B70ED68DA255427AB00DA3496F89A09CA6E. The separate sync case is still running. This is local native execution, with public-network transport still open.
TON native TVM lifecycle finished 2/2 modes passed, zero failures/skips, 10m16s. Sync passed 26/26 with fixture cleanup confirmed. Sync report SHA-256 783D448D3AD632AAACD1DD1055AEF9372EECC8071AED46A1CB8C90E5DF737A6A. Combined sync and async: 52/52 through the registered shipping provider.
The TON lifecycle acceptance used Core 14bfd34f6e34d2be9f4bdec34b3431b8ce356fe5 in the provider worktree. Development still pinned Core 9c2882e51b8517d37b2293e4beeaf815003e7649 at the integration review. Keep the existing native result attached to its tested revision when integrating the inventory.

### NEAR native lifecycle admission

The existing signed-write tests use the shipping NEAROASIS provider. The retained runner requires an RPC URL, chain ID, contract ID, and protected signer path, and launches its test tooling through WSL. The current shell has no NEAROASIS test settings. Existing operation-specific reports remain valid within their recorded scope. The next shared-suite run needs an isolated writable contract with exact fixture cleanup and must preserve retained CQL state.
The focused shared NEAR lifecycle test compiled and discovered both modes. Both skipped because OASIS_NEAR_LIFECYCLE_REPORT_ROOT and a dedicated writable fixture were not configured. No native pass or fixture mutation is claimed.

### Polkadot retained native storage scope

The [Polkadot native evidence](POLKADOT_PROVIDER_NATIVE_TESTS.md) records ten original finalized operations through the public C# avatar, detail and holon storage path, with encrypted-cell verification and final empty storage. The original run failed only at its terminal journal comparison. The corrected read-only recovery passed against the same ten originals and issued zero fresh acknowledgements. Retain that scope and do not replay it. A current shared lifecycle result and broader graph/version coverage are still missing.
The guarded Polkadot runner requires the existing TestResults/PolkadotCurrent environment before acquiring its native lease. That directory is absent in this worktree. The tracked completion evidence remains intact. Do not recreate or replay the accepted deployment to fill the shared-suite gap.

### Cosmos SDK restore and retained node

The broken dependency link was absent in this worktree. A lockfile restore installed 32 packages and ran the repository postinstall. Node 24 loaded both pinned CosmJS 0.39.0 clients. The retained runner requires Ubuntu under WSL. This host has no installed distributions. Native dispatch, contract state and cleanup were not exercised.

### Polygon and Arbitrum lifecycle preflight

The revised EVM runner stops before the build when the retained loopback RPC setting is missing and selects only the shared lifecycle cases. No listener was found on the checked local EVM ports 8545, 8546, or 18545. The existing owner decision on the lost original EVM authority stays open. Do not treat a newly initialized chain as recovery of the old ledger.

### BigQuery hosted lifecycle gate

BigQuery integration tests now require an explicitly dedicated project, dataset and report directory. Both sync and async shared lifecycle cases compiled and were discovered, then skipped without the fixture. The run did not submit a BigQuery job. Hosted test-resource admission remains tracked in Actions for David.
Google Cloud synchronous activation now waits on context-independent provider awaits while still requiring the SDK resource probes. The provider build passed with zero errors. The standard Firebase emulator cache on this host has no Firestore artifact. The prior 52/52 lifecycle was not repeated and the changed activation path still needs runtime verification.

### Development integration

Development advanced to 9131d651e3 with the EVM fixture preflight and IPFS dead-comment cleanup only. The push validated the existing Railway manifest pins. Provider runtime changes and native acceptance evidence remain tied to their tested task-branch revisions. The scoped batch did not run or establish new backend acceptance.

### Azure native and hosted scope

The Azure Blob and Azure Cosmos DB inventory entries each retain 52/52 shared lifecycle checks against local emulators through the shipping SDK paths. The Azure Blob runner also independently checked empty containers after fixture cleanup. Hosted Cosmos is explicitly deferred in Actions for David. Preserve that decision and the existing local evidence without rerunning the accepted suites.

### Oracle Database and OCI Object Storage

Oracle Database retains its 52/52 native shared lifecycle result through the managed Oracle client. OCI Object Storage is a separate provider. Its existing OracleLiveProviderAcceptance entry point uses the shipping provider and real ObjectStorageClient with a dedicated prefix. The 155 SDK controls recorded in ORACLE_CLIENT_PRIORITY.md do not exercise a hosted OCI bucket. Preserve the existing hosted access decision and do not rerun the database baseline.

### Google Maps rendering boundary

The Google Maps facade now rejects unexecuted operations and builds. Its legacy Core map contract differs from the object-based contract used by GOMap and MapManager. GOMap owns its renderer and directions client. Its straight-line fallback cannot serve as Google route evidence. A complete implementation still needs the Google renderer and directions transport bound to the actual caller, with a native result verified before reporting success.

### Convex companion package verification

A local Debug NuGet pack from existing build output contained all seven declared companion files, including the schema and record functions. The temporary package was removed after inspection. The hosted activation check compiled and skipped without the dedicated HTTPS deployment. Hosted IAM and the hosted lifecycle remain open.

### SQLite provider and Edge state store

SQLLiteDBOASIS retains its 52/52 native shared storage result through Microsoft.Data.Sqlite. The separately named EdgeSQLiteOASIS package contains EdgeSQLiteSyncStateStore and a synchronization journal. It is a runtime dependency rather than a second IOASISStorageProvider entry point. Its eventual verification belongs to the Edge runtime and synchronization paths. Do not rerun the canonical SQLite baseline to qualify that separate package.

### Valkey shared lifecycle gate

The dedicated Valkey integration cases compiled and discovered sync and async schedules. Both skipped without OASIS_VALKEY_LIFECYCLE_REPORT_ROOT and a dedicated Valkey endpoint. The cases require a real valkey_version response before writes. Keep the retained CQL fixture and controlled family result separate from the pending native shared lifecycle.
Garnet shared lifecycle sync and async cases compiled and were discovered. Both skipped without the dedicated endpoint and OASIS_GARNET_LIFECYCLE_REPORT_ROOT. The new preflight requires native garnet_version before writes. Existing CQL and controlled evidence were not replayed.

### Garnet Windows native partial run

Garnet 2.2.5 ran on a disposable loopback-only Windows instance. Both schedules reached 18/26 and stopped because the harness did not supply the exact hard-delete callback required after a default soft deletion. Async report SHA-256 EF1B9AD5B745217601C1D5C47973793246B6BE3768B7D079D090C046BCFAC61B. Sync report SHA-256 EA165F8144C51CD729C353B090A9C49D3D567E80B7D56238D4C6A92D528C57DF. The server stopped and its in-memory state cannot be resumed. The correction wires the existing public hard-delete methods into the Garnet and Valkey harnesses. Verify the missing deletion checks on an isolated fixture without replaying the accepted operations.

### Garnet focused native cleanup (2026-11-09)

Official Garnet 2.2.5 on a disposable Windows loopback instance passed both focused cleanup cases (async and sync). Each checked exact tombstone removal, point and list absence, avatar and detail removal, database emptiness after cleanup, and deactivation. Previously verified lifecycle cases were not replayed. This is split evidence, with full provider acceptance still open. Build exited 0. The runner stopped its server. `TestResults/GarnetProviderLifecycle/cleanup.trx` SHA-256 is `3858EB4BE631B070BAB45C2A7C057B3EA2C30133D134297F16634831B068E240`. Automatic approval review blocked the combined command containing disposable tool cleanup, so the runtime and runner still need cleanup.

Follow-up: the temporary runner was removed. Automatic approval review rejected recursive removal of the dedicated Garnet runtime directory separately, reporting a policy block. The preserved lifecycle report stopped at child fixture hard cleanup. The focused run covers single-holon hard cleanup plus avatar cleanup and deactivation. Default deletion and exact hard cleanup of the original root, both batch records, and imported record still need native evidence in both modes. Seed these fixtures for the remaining checks without replaying the 18 verified cases per mode.

Remaining Garnet fixture verification passed 2/2 against the official Garnet 2.2.5 loopback process. Both modes verified shipping default deletion and exact cleanup of the root, imported holon, and two batch holons, with point and collection absence and an empty native database. Earlier passing cases were excluded. The evidence remains split across runs. TestResults/GarnetProviderLifecycle/remaining.trx SHA-256 9C63F510B692D0ED0F9BEA2CCF01C5061685A7AD878873A9E9E648F5E8264B2F. The build succeeded with zero errors.

### NEAR deletion acceptance boundary

Commits d91eeec6e7, 00d3bbac5a and e7cf7d0dd6 restore deletion acknowledgements for the default holon, avatar and explicit holon methods. The relevant provider and integration builds passed with zero errors. There is no new chain result. Default holon deletion writes a tombstone hidden by normal reads; shared-suite absence alone cannot establish physical cleanup. Hard avatar deletion currently deletes avatar versions and the current avatar, leaving the separately keyed detail and its versions. The only checked-write contract source in this checkout is under Scripts/TestHosts/near-local/contract. Its delete_record ABI removes one key per call. Acceptance needs a released contract operation that removes the avatar and detail family consistently, followed by a focused test of current and historical absence on an authorized writable contract. Carry the prior encrypted write/delete and NFT receipts forward without replay.

The local atomic avatar cleanup draft compiled on the C# side with zero errors. The pinned contract dependency installation stopped during the near-sandbox postinstall with "Unsupported platform: Windows_NT x64", before the WASM compiler ran. Use the checked-in lockfile and Node 22.22.2 or newer on a supported Linux runner. The draft scans contract keys in reverse order and still needs gas measurements and verification of current and historical absence alongside an unrelated retained record. Automatic approval review rejected recursive removal of the disposable TestResults/NearAvatarContractBuild directory. No prior native cohort was replayed.

### Polkadot storage coverage carried forward

PolkadotPublicStorageNativeTests and the retained public-storage completion report cover async avatar save and ID/username/email lookups, avatar-detail save and username/email lookups, a single holon save and ID/metadata queries, physical ciphertext checks, default holon deletion, and hard avatar cleanup. The completed run verifies original dispatch receipts and an empty native contract after cleanup. Remaining shared-suite coverage includes the synchronous entry points, avatar-detail ID lookup, graph and parent queries, holon update/readback, batch save with null metadata, import, owner export, and result flags/counts where the original test only checked payloads. Use distinct fixture identities with the existing prepared-submission journal for those cases. Do not invoke the completed write cohort again.

SQLite focused regression: both modes of BoundReplicaChecksNativeIdentityBeforeWriteAndReceiptReplay passed against real SQLite (2 passed, 0 failed, 0 skipped). The repaired provider exposes its existing bound replica and head audit interfaces. The earlier shared storage baseline was excluded. This is provider-level evidence only.

Development batch preflight: origin/Development 9131d651e3 pins Core 9c2882e51b8517d37b2293e4beeaf815003e7649, which does not declare the bound replica or replica head audit interfaces. The successful focused SQLite run used task Core 14bfd34f6e34d2be9f4bdec34b3431b8ce356fe5. Defer the SQLite batch until it can be verified with a compatible Development Core pin and matching Railway manifest. Do not advance the shared Core pin solely to publish this repair.

AWS S3 dependency repair: e5a0ddf982 restores AWSS3OASIS inheritance from the existing S3StorageProviderBase, matching the canonical S3 transport consolidation. The Polkadot integration build compiled the restored provider with zero errors. The earlier 52/52 MinIO-compatible evidence covers the shared S3 lifecycle and was not replayed. The guarded AWS IAM/bucket transport run and hosted lifecycle still require the dedicated AWS fixture in Actions for David. Development's shared S3 source matches this task branch, but the successful build used the newer task Core pin. Development-specific build verification remains open.

SOLID synchronous activation: provider fix 2a70473362 built with zero errors. Direct integration build (/p:SolidDirectProviderOnly=true) built with zero errors and discovered SynchronousActivationCompletesWithoutPumpingCallerContext. This focused read-only check requires OASIS_SOLID_ACTIVATION_POD set to the retained loopback Pod root on port 13000. The endpoint did not respond on this host. Default WebAPI/CQL integration compilation still requires its unrelated registration dependencies. Prior native storage evidence was not replayed.

Hedera shipping receipt check passed (1/1, no skips) for SDK transaction ID 0.0.10993779@1791929787.100278783 and net fee 1259196297 tinybars. The initial mirror-formatted ID was rejected before lookup. The corrected run read the same retained receipt, with no signing, activation or submission. Funded storage and consensus reserve checks remain open.

### Neo4j package evidence boundaries

The canonical Neo4j provider carries forward its recorded 52/52 native storage result. Neo4jOASIS2 is a sealed package entry point whose constructor forwards host and credentials to the canonical provider. Its outstanding native check is package resolution and activation through that entry point against the retained Neo4j service. The existing lifecycle evidence applies to the inherited implementation. The Aura package has its own provider partials for activation, reads and writes, and requires independent acceptance. Its runtime DLL reference in the STAR console template makes it relevant to the shipped package audit.

Aura now publishes its SDK driver only after connectivity succeeds and closes rejected connections. Synchronous entry points share the same lifecycle as asynchronous entry points. The Aura project built under the provider lease with zero errors and two warnings. The retained Neo4j Podman host is unavailable on this Windows installation (WSL_E_DISTRO_NOT_FOUND), so live activation remains open.

Aura avatar deletion is a separate shipping gap. Its ID overload runs an unconditional DELETE in a read transaction, ignores softDelete and treats an empty cursor as success. Native acceptance must establish the deployed Avatar key and deletion marker schema, then verify soft deletion, exact hard deletion acknowledgement and missing-record behavior in both entry points. The canonical Neo4j lifecycle report does not exercise these Aura overrides.

Aura avatar saves now share a single SDK write transaction keyed by the string GUId used by Aura lookups. Success follows transaction acknowledgement and returns the saved avatar. The build passed with zero errors and two warnings. Native create/update and historical data checks remain open, including records lacking GUId and uniqueness on the deployed graph.

Aura GUID reads now use one SDK query in both entry points and reconstruct the stored avatar identity and profile fields. A missing row returns an unloaded result, and duplicate GUID rows surface an error. The provider build passed with zero errors and two warnings. Live GUID readback and the other selectors remain open.

Aura avatar deletion now executes one SDK write transaction for all four selectors and both calling modes. It rejects ambiguous active matches before mutation, marks a soft-deleted avatar with IsDeleted, and detaches relationships when hard-deleting the selected Avatar node. Primary avatar lookups and listing hide the marker. Provider build passed with zero errors and two warnings. Native Cypher execution, linked avatar-detail lifecycle and retained provider-key schema remain open.

Aura avatar-detail saves now use a single SDK write transaction keyed by GUId in both calling modes, update the same four persisted profile fields, and return the detail after acknowledgement. Build passed with zero errors and two warnings. Live detail create/update, full payload round-trip and historical identity verification remain open.

Aura detail GUID, username and email lookups now return the stored identity and the four fields written by the detail save path. Both calling modes report missing and duplicate matches consistently. Build passed with zero errors and two warnings. Native detail lookup remains open.

Aura avatar writes now persist the provider key selected by the shipping lookup and delete queries. A saved key is preserved across updates, with the supplied Neo4j key or GUID used to initialize records that lack it. Single-avatar reads and saves return the actual stored key. Build passed with zero errors and two warnings. Native key round-trip, duplicate-key handling and compatibility with existing records remain open.

### Supabase collection pagination

Avatar and detail lists now read successive PostgREST pages. Holon storage and the bounded CQL ID selector share a reader that checks exact totals, contiguous ranges and canonical IDs across pages. Provider builds passed with zero errors and four warnings. No retained standalone PostgREST executable or test-result fixture was found on this Windows host. The earlier CQL query and restart evidence carries forward for its original scope. Live pagination still needs the dedicated hosted fixture, including a server row cap below the requested page size and more records than that cap.

All three guarded read-only ListingReturnsEveryPreseededIdentityAcrossPages cases build and are discoverable. Set OASIS_SUPABASE_PAGINATION=1 with the protected hosted fixture settings. Set OASIS_SUPABASE_PAGINATION_IDS, OASIS_SUPABASE_PAGINATION_AVATAR_IDS, and OASIS_SUPABASE_PAGINATION_DETAIL_IDS to the paths of the corresponding JSON arrays of all eligible synthetic GUIDs, at least 101 distinct IDs per case. Run separately from the empty-project lifecycle checks. Each case compares exact identities and count without altering the fixture.

NEAR hard-delete scaling review: checked-record.ts admits a positive archive version suffix but does not enforce contiguous versions, and the current record is opaque ciphertext. The contract cannot safely infer all historical keys from the head or stop at the first missing version. The draft reverse scan handles sparse historical keys but still requires a measured gas limit. An indexed design must account for existing archives before replacing that scan.

KeyDB guarded native lifecycle: both modes built and were discovered. OASIS_KEYDB_LIFECYCLE_REPORT_ROOT and KEYDB_CONN must point to a dedicated fixture and fresh report root. INFO Server executable must identify keydb-server before writes. Prior native CQL evidence carries forward. No lifecycle test ran on this host.

Dragonfly native lifecycle requires DRAGONFLY_CONN and a fresh OASIS_DRAGONFLY_LIFECYCLE_REPORT_ROOT. The guard requires the native dragonfly_version field before writes. Provider and integration builds passed, and both lifecycle modes were discovered. Prior native query and snapshot results were not replayed.

Garnet split-evidence audit: the initial 18/26 reports reached child hard cleanup after checking the soft-deleted child was excluded from its parent. The focused hard cleanup used an unparented holon. The remaining fixture to qualify is exact hard cleanup of a previously parented child, with native absence of both the child and its relationship index and preservation of the parent. The standalone cleanup result does not establish that graph-specific invariant.

Focused Garnet child-index test ran against official 2.2.5. Initial test incorrectly queried the logical index key. The corrected physical-key probe confirmed the index exists, then the async case stopped at default deletion of a batch-seeded fixture and the sync case was refused by the empty-database guard. Physical-key run TRX SHA-256 07EA90014C7420DE2C078F08AC0B77B131C6520AEC355E0B6737176C8C135C8D. The next test uses the original shared-suite graph seeding route and reports the delete result message. The child hard-cleanup gate remains open.

Garnet child relationship hard cleanup passed in both modes (2 passed, 0 failed, 0 skipped) on official Windows Garnet 2.2.5 with Lua transaction mode enabled. The earlier graph-seeded attempt failed because Lua was disabled. Both passing cases verified native encrypted index existence before deletion and absence after it, child absence, intact parent readback, and an empty database after cleanup. TRX SHA-256 11B1607FBE767844858E7D2FB812202FEF1838873E17660DA21ADBD4222BDDC9. The owned server stopped. No prior passing cohort was replayed. This closes the graph-specific split lifecycle gap, with broader provider acceptance still open.

Development preflight (2026-11-10): fetched 9131d651e3. Garnet production code and the shared key-value storage source match Development; the task branch adds the README, focused tests and evidence. The native test build still uses task Core 14bfd34f6e, while Development pins 9c2882e51b. Carry the passing native evidence forward. A Development-pinned consumer build is still required before promoting the test batch, and the unrelated NEAR draft is excluded.

Garnet promotion source check: the shared key-value project references IHolonReplicaHeadAuditStorageProvider and IConditionalHolonReceiptEvidenceStorageProvider. Neither declaration exists at Development Core 9c2882e51b. Matching Garnet and shared provider source is therefore insufficient to reproduce the tested build with those pins. Keep the verified result and defer the dependent batch until the compatible shared dependency is integrated.

TON public-storage implementation boundary: the registered constructor accepts only TonSandboxStorageOptions and wraps TonSandboxStorageBackend behind the shared private record layer. Its legacy three-string overload rejects the EVM configuration. The distinct public header source is read-only. A public storage implementation needs a native contract and authenticated transport that can resolve and verify the configured contract identity, submit writes, observe finalized outcomes, and perform persistent exact-key and collection reads. The shared encrypted record layer should remain the logical storage owner. Carry forward the local TVM passes and public-header read evidence in their separate scopes.

Supabase hosted avatar follow-up: 97c3bd3474 and eb09b3751c extend exact PostgREST pagination and ambiguity checks to avatar and detail reads. 0f36710e55 validates requested GUIDs; c122ea8ed8 validates all written selector fields; f2c6ebad1c distinguishes an empty deletion acknowledgement from an unconfirmed mutation. JSON null now fails row-array decoding. Hosted verification needs multi-page avatar and detail fixtures, duplicate username/email under a one-row cap, exact write readback, and soft-delete/missing/hard-delete outcomes. The existing holon pagination test covers holons only. No dedicated hosted configuration was present in this session.

### Aura native verification handoff

The Aura-only implementation now uses the official driver for point and parent holon reads, typed collection reads, GUID upserts, batch saves, import and exact-node deletion. The shared search evaluator runs over the Aura projections. Activation attempts uniqueness constraints for holon, avatar and detail GUIDs in the designated database. The provider and guarded integration project built with zero errors. The project includes AvatarLifecycleUsesPersistedIdentityAndDeletionMode and HolonLifecycleUsesPersistedParentAndProviderKey. Neither has a hosted result. Run these named tests on the disposable Aura database described in Actions for David. Preserve the canonical Neo4j evidence separately. Constraint enforcement under concurrent saves, complete payload and recursive hierarchy behavior still need evidence.

### Pinata hosted verification handoff

Holon, avatar and avatar-detail saves now assign missing GUIDs in the uploaded payload before publishing them to callers. Direct and service uploads validate returned CIDs, and the service validates CIDs before unpinning. Provider and integration builds passed. HolonRoundTrip_UsesAuthenticatedPinataApi now requires a generated GUID and checks both GUID and CID readback before exact fixture unpin. No Pinata credentials or write opt-in were configured for this check. Use the existing Pinata hosted fixture action. Avatar/detail native readback and durable mutable identity, encryption, graph and version semantics remain separate acceptance gaps.

MongoDB metadata scope: 8c6ec18c2f changes the repository overload without a session parameter. The shipping facade uses the separate session-bound query path. MongoMetadataIntentTests cover that facade with controlled boundaries and cannot verify the changed overload. Preserve the native 52/52 baseline in its original scope. An installed mongod process is active, but its fixture ownership was not established and no requests were sent to it.

SQL Server LocalDB literal search passed on MSSQLLocalDB 17.0.4025.3 against the existing oasis_hyperdrive_provider_test fixture (1 passed, 0 failed, 0 skipped). The shipping provider matched only the synthetic name containing literal percent and underscore characters, reported one record, and acknowledged hard cleanup of both synthetic records. No prior lifecycle cases were replayed.

SQL Server promotion preflight: fetched Development 1d913e5701. The tested task-branch provider differs from Development across save, lookup and deletion paths as well as search (114 additions, 81 deletions in SQLServerDBOASIS.cs). The focused LocalDB pass verifies the task-branch cohort. A search-only promotion needs its own Development-pinned fixture result; the wider batch needs a separate scope review. No deployment was triggered.

SQL Server LocalDB MetadataQueriesMatchLiteralPropertyNames passed (1 passed, 0 failed, 0 skipped) against the existing dedicated fixture with provider change 271989d5f8. The shipping single-key and all-of multi-key paths returned the exact synthetic row with a dotted literal property name. Exact hard cleanup succeeded and the LocalDB instance stopped. Prior checks were excluded.

SQL Server historical storage boundary: SaveHolonAsync writes every graph node in one SqlTransaction, but MERGE overwrites DataJson in OASISHolons keyed by Id. There is no archive table in the provider. Implement version snapshots inside that same transaction, keyed by logical holon ID and positive version, with a uniqueness constraint and explicit handling of an existing version. Preserve only snapshots actually present for older databases. Before enabling historical selectors, define soft-delete visibility and hard-delete archive cleanup, and verify that a rolled-back graph changes neither current rows nor historical rows. The current-only metadata guard remains in place.

SQL Server HolonHistoryRetainsSupersededSnapshot passed on dedicated LocalDB (1 passed, 0 failed, 0 skipped). The shipping provider archived version 1 when version 2 committed, read version 1 by GUID and version 2 by provider key, then acknowledged hard deletion. A native query confirmed zero archive rows for the fixture. LocalDB stopped. Historical metadata and rollback checks remain open.

SQL Server MetadataReadsSelectArchivedSnapshot passed on the dedicated LocalDB fixture (1 passed, 0 failed, 0 skipped). Both shipping metadata overloads selected the archived value at version 1; the current view excluded it after version 2 committed. Hard cleanup left zero archive rows for the fixture, and LocalDB stopped. Earlier tests were excluded.

SQL Server StaleChildRollsBackParentAndArchive passed on the dedicated LocalDB fixture (1 passed, 0 failed, 0 skipped). A stale child caused the whole graph save to fail. The parent retained version 1 and its original name, and a native query found zero archive rows for that parent. Both fixture records were hard-deleted and LocalDB stopped.

SQL Server HistoricalCollectionsUseStoredParent passed on dedicated LocalDB (1 passed, 0 failed, 0 skipped). Version 1 listed the moved child under its original parent by GUID and in the full historical collection. Current listings excluded the old parent and found the child under its new parent by provider key. The child fixture was hard-deleted and LocalDB stopped.

SQL Server SearchSelectsArchivedHolonText passed on dedicated LocalDB (1 passed, 0 failed, 0 skipped). The shipping search found literal archived text at version 1 and excluded it from the current view after version 2 committed. Hard cleanup left zero archive rows for the fixture, and LocalDB stopped. Earlier cases were excluded.

SQL Server OwnerExportReturnsRequestedSnapshot passed on dedicated LocalDB (1 passed, 0 failed, 0 skipped). The same creator ID returned the archived name at version 1 and the updated name in the current export. Hard cleanup left zero archive rows for the fixture, and LocalDB stopped. Earlier checks were excluded.

SQL Server DuplicateAvatarSelectorsRejectOwnerExport initially failed because email lookup omitted AmbiguousIdentity. After preserving that code, the focused LocalDB case passed (1 passed, 0 failed, 0 skipped). Both username and email exports rejected duplicate active avatars with no export payload. The test hard-deleted the two fixtures and the owned LocalDB instance stopped.

SQL Server LocalDB passed DuplicateAvatarSelectorsRejectDeletion (1 passed, 0 failed, 0 skipped). On the dedicated fixture, username, email and provider-key selectors each rejected ambiguous soft and hard deletion with zero deletions. Both synthetic avatars remained readable and were hard-deleted by ID during teardown. The test stopped its LocalDB instance after the run.

SQL Server LocalDB passed DuplicateAvatarDetailSelectorsRejectLookup (1 passed, 0 failed, 0 skipped). The dedicated fixture returned each detail by ID, rejected duplicate username and email selectors with AmbiguousIdentity, and hard-deleted both synthetic avatars during teardown. LocalDB was stopped after the run.

SQL Server LocalDB provider-key hard deletion passed (1 focused case, 0 failed, 0 skipped). The shipping provider deleted each synthetic avatar using the storage key returned by SaveAvatarAsync. That key is a GUID, matching the load contract. Username and email ambiguity remain covered by the dedicated selectors. The LocalDB instance was stopped.

SQL Server LocalDB passed AvatarListsValidateStoredIdentity (1 passed, 0 failed, 0 skipped). Both shipping list methods returned the valid synthetic row and returned an error without a partial result after its payload identity was replaced. Teardown hard-deleted the fixture avatar and detail and stopped LocalDB.

SQL Server LocalDB passed HolonListValidatesStoredIdentity. The shipping all-holon list returned the valid synthetic row, then rejected the mismatched payload without a partial result. The test restored the payload for provider teardown and stopped LocalDB. The one corrupted fixture row from the initial run was removed by exact ID before retrying.

### Next scoped Development batch

SQL Server LocalDB verification supports the task-branch range 1c2970fde0 through 4e92f5a638 for the owner selector, avatar deletion/detail/list, and holon snapshot fixes. This evidence covers the isolated LocalDB paths. Keep hosted provider and HyperDrive acceptance open. Development already received the SQLite and Garnet batch at 1d913e5701 on November 10. Recheck the remote and select only verified changes at the next integration window.

The task process did not expose Neon, Supabase, or Convex hosted fixture settings. The existing owner entries remain the source for provisioning these fixtures. Convex local evidence carries forward without a replay.

Neon hosted conformance test project built successfully with the revised failure checkpoints (0 errors, 1143 warnings). This is compile evidence. Hosted sync and async acceptance still require the dedicated Neon fixture.
