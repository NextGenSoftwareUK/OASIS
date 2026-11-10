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
