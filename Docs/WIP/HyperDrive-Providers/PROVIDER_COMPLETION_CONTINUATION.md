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
