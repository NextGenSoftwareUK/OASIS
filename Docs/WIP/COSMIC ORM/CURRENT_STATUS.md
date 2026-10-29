# Current COSMIC ORM / CQL status

Updated29October2026. Read first after compaction. Full programme remains incomplete.

## Workspace and rules

- Work/build/test only C:/Source/OASIS-cosmic, parent/Core branch work/cosmic-cql.
- Priority: HyperDriveV2 replication/failover/load balancing first; then ORM/CQL parity for MongoDB, SQLite, Neo4j, IPFS, Azure Cosmos, AWS, Oracle, SOLID and ActivityPub. Preserve Solana demos; expand others later.
- Work only on red checklist cells; freeze green features/cursors absent a reproducible bug.
- Entity-selected All/element/numeric LINQ depth is paused. Large POCO/migrations and untagged parity work stay deferred until demo acceptance; do not silently resume them.
- HyperDriveV2 only; legacy gaps documented. No paid cloud provisioning/upgrade.
- [Checklist](CQL_PARITY_CHECKLIST.md), [launcher](CQL_DEMO_LAUNCHER.md), [manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md).

Development sync29October includes upstream parentc8a381dd2/Core0240771 (attached-avatar visibility, encrypted saved connections, DNA/API docs and pin-hook tooling). Core mergeaa47e48 preserves task changes;4462Core/19DNA/1shippingGraphQL tests pass, plus affected priority-provider/BootLoader/WEB4 builds. STAR35896e8 retained. [Compatibility scope](DEVELOPMENT_SYNC_2026-10-28.md). No full native recertification/deployment claim.

Source-bound receipt capability now builds on SQLite/Mongo/Neo without changing historical intent. Core4462/Neo619 controls and SQLite2 native copied-file cases pass; native Mongo2/Neo2 historical/bound receipt cases now also pass. Full migration preflight remains red. See [migration checkpoint](V2_LINEAGE_MIGRATION.md).

## External prerequisite and exact next step

1. Hosted Azure Cosmos is the remaining external prerequisite in the current breadth queue. User has no existing free subscription. The no-subscription sandbox was rechecked and still shows Under Construction.
2. User explicitly chose **leave hosted Azure pending**. Do not repeat signup requests or provision an account. The last official free-account route reached blank Microsoft sign-in. No account/resource/subscription created and no terms accepted. Official page discloses a possible temporary$1 card-verification hold; never choose pay-as-you-go. [Admission and next gates](CQL_HOSTED_AZURE_FREE_ADMISSION.md).
3. Hosted opt-in read runner now builds with strict non-secret configuration and explicit -Mongo/-Serve wiring;68controlled CQL tests pass, plus local SDK/HTTP1/1regression/exact cleanup. Hosted auth/config presence rechecked:false/false. The same signup/auth prerequisite remains an actual external blocker after preparation; do not claim completeness. Actual hosted SDK/HTTP/browser/Mongo and free-account verification remain red. Hosted prerequisite implemented:Cosmos ExistingResourcesOnly activation validates pre-provisioned database/container identity/partition without creation or provisioning fallback;6new cases/52affected CQL tests pass. Native hosted proof remains red. After an authenticated genuinely free account is available:verify spending protection/zero-cost configuration, prepare a dedicated journaled hosted fixture, then prove native SDK/HTTP/browser and Mongo pairing against the real hosted endpoint with exact cleanup. Emulator evidence cannot satisfy this.
4. Demo breadth is verified within the table below; freeze green demos absent a relevant change/bug. Latest Oracle+Mongo SDK/HTTP1/1 regression is recorded in the [presenter run-through](CQL_DEMO_RUNTHROUGH.md); it preceded publication of the catalogue foundation and is not a deployment certification.
5. HyperDrive checkpoint: native destination catalogues implemented for SQLite/Mongo/Neo; protected recovery preserves complete bindings. Core8917a52/4440Coretests includes node-list isolation and failed-primary-confirmation propagation;2concurrent scoped execution cases and native primary-only witness1/1 pass. Six Mongo/SQLite worker cases prove restart/new-save during metadata outage. Neo cold-new-save metadata-outage1/1 and6admission regressions also pass. Three separate process phases prove a SQLite save while the Mongo destination process is stopped. Active-delivery outage1/1 additionally proves pending retry/order, same-data recovery on attempts2/1 and stable target revisions; reviewed original-outcome reconciliation1/1 and3phase regression pass. Configured one-attempt native exhaustion1/1 proves terminal blocking and retained cause after recovery (shipping default10/system-clock/native SQLite->Mongo schedule now passes1/1 in9m8s; other pairs remain open). Concurrent first-proposal cases pass on all3native databases (catalogue suites Mongo6/SQLite6/Neo2); synchronized client invocation, not forced internal-read interleaving. [Scope, hashes and commands](DURABLE_ROUTE_DESCRIPTORS.md).
6. **Exact next step:** selected-record source/target migration preflight using verified original receipts, then guarded operator execution and remaining concurrent-change/SQLite-Mongo hard-delete audit gates. Neo reader now passes native6/6 (including live and hard-deleted audits), Neo616/Shared1343/Core4449controls. Strict snapshots remain default; Neo explicitly refuses that guarantee and requires read-committed opt-in, reported in the result. Mongo head reader now passes native1/1, modernMongo628/Shared1340controls and native lifecycle2/catalogue6 regressions; original setup failure reconciled1/1 without replay. Coref317bfa defines bounded head evidence; SQLite read-only native head audit passes1/1, fullSQLite1825native/227unit, Shared1338 and Core4449 pass. Hard-delete audit and concurrency native gates remain separate. [V2 lineage audit](V2_LINEAGE_MIGRATION.md) now inventories one physical-source-bound page without claims/state changes:11focused/1327Shared/6nativeNeo admission tests pass; target adoption/rebind is still red. Neo/SQLite native routing1/1 now proves42strategy reads,3sandbox failovers and1peer native read plus hosted delivery/restart/idempotence. It does not prove Neo process outage or privacy ACLs. Native Mongo/SQLite strategy eligibility42reads (7strategies x3entry points x2plans) passes inside1scenario; Free filters Mongo before scoring, Gold permits both, configured list unchanged. Native default/preferred-authority sync/async/explicit read outage proof now passes1/1, including unchanged receiver lifecycle and original-outcome reconciliation1/1. Coreed83223 fixes the reproduced preferred-authority false absence in all3failover forms;9focused/4449full Core tests pass, both Core targets build. Native MongoDown->SQLite sync/async/explicit reads plus4round-robin Mongo/SQLite reads now pass1scenario; [routing acceptance](V2_ROUTING_ACCEPTANCE.md) owns evidence/limits. Neo->SQLite native write-lock/new-save/order/restart acceptance now passes1/1 in8s. Remaining replication scope includes other-pair outage acceptance and audited operator/V2 lineage migration; preserve the deployment gate and never reset/retarget old obligations silently. Full nine-provider HyperDrive/ORM/CQL acceptance remains red; hosted Azure stays deferred. Development enrollment/primary-confirmation fixes are already integrated.
7. Remaining full ORM correctness/child graphs/history/migration/placement acceptance is preserved in the [roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md); no programme completion claim.

## Verified demo breadth

| Scope | Verified evidence and limits |
|---|---|
| SQLite/MongoDB/Neo4j | Original12investor rows green;34native entity/reselection/cardinality cases/database.681SDK queries/138HTTP requests/30save checks in grouped demo. Full EF/NHibernate parity remains red. |
| AWS + Mongo | Red: AWS/DynamoDB native CQL execution is not yet admitted; C#/HTTP/browser/Mongo presets remain to implement. Existing storage tests do not prove CQL. [Admission plan](CQL_DEMO_EXPANSION_ROADMAP.md). |
| Oracle + Mongo | Native ID membership, trusted authorization,5physical rows/4subset, C#/HTTP/browser, exact cleanup.473Oracle unit tests. Latest hub SDK/HTTP1/1pass on mergedCore. [Proof](CQL_ORACLE_DEMO_ADMISSION.md). No OCI hosting claim. |
| Local Cosmos + Mongo | C#/HTTP/browser5rows/4subset, provider provenance/policy checks and exact cleanup on mergedCore. [Proof](CQL_MONGO_COSMOS_VERIFICATION.md). Hosted Azure, numeric metadata and conditional lifecycle remain open. |
| IPFS/SOLID/ActivityPub + Mongo | Native3-record/2-record-subset membership; paired SDK/HTTP/browser5/4rows and protocol-only2. Controlled tests195/393/321 respectively. [Profiles](CQL_PROTOCOL_MULTI_RECORD.md), [browser proof](CQL_PROTOCOL_BROWSER_VERIFICATION.md). Bounded key membership is not arbitrary content querying. |
| Solana + Mongo | Native Mongo1/retained Solana1 via explicit SQLite index;0empty retains assurance. Original chain receipt/version, checkpoint/head preserved. C#/HTTP/browser and exact Mongo cleanup pass; no source writes/reseed. [Proof](CQL_MONGO_SOLANA_NATIVE_VERIFICATION.md). Local validator, not hosted chain. |
| Central launcher |8single profiles/16plans and6paired profiles/12plans;4unsupported-pair/3archive rejections. Discovery starts no services. Oracle live dispatch passed. [Guide](CQL_DEMO_LAUNCHER.md). |

Oracle, mixed Solana and local Cosmos screenshots inspected/readable. Protocol screenshots were previously unavailable; functional browser evidence is separate. No persistent screenshot archive was created.

## Other correctness evidence / open scope

- Reproduced/fixed sync+async post-commit wake observer exceptions erasing the native acknowledgement. Four focused tests/4325Core pass; original receipt/message/warnings retained, later subscribers still invoked. Cored980bdf pushed. Restart/delivery and automatic-route authorization remain separate open acceptance.
- Three-database save demos prove dirty/no-op ownership, generated IDs, optimistic conflicts and committed per-save deltas within their linked checklist scopes.
- Shared mixed federation preserves native/indexed separation, protected failure, budgets and read-level assurance;4298Core tests passed before Development integration.
- Development routing/replication integration builds both Core targets and WEB4;4308full Core tests pass. This is compatibility evidence, not certification of every new replication/runtime scenario.
- Original Solana historical root/chunk recovery remains red; no backup known. Preserve ledger/keys/index/acknowledgements; never reset/replay to conceal this.
- Production hosted auth/durable settlement/scaling, incremental ingestion transport and broad cross-provider snapshots remain open. Never claim100%uptime or universal EF parity from these demos.
- [Deferred parity source](DEFERRED_PARITY_HANDOFF.md) remains separate; no All/element/numeric continuation without user direction.

## Git/publication and resources

- Latest Development953cb7968/Core261fce1 merged into task branch (Core merge c1b8bd8);4377Core tests pass, including new native-connection and replica-read controls. Per-avatar guide is now present. See [integration report](DEVELOPMENT_SYNC_2026-10-28.md). Earlier Developmentf8e872a4f/Core319bd96 integrated into task branch; merged Coree6bc667127e2177a41e0a30a191faa1b9c3834f7 integrated; later native-import ownership fix Core7d05bdb138df39db7b6010272188464c63184558 pushed/remote verified. Fixed incoming SettingsManager Enum qualification for netstandard2.1.4308Core tests/1shipping GraphQL schema test pass; SQLite/Neo/BootLoader/WEB4 builds pass.
- Last Development deployment batch:parent858edd8280294d748e0e4999e39c0a141c5b6042/Corebd86a37fa1df02d50e5d2d0e5ad4bd76cce1d26d. Later verified work stays on task branch until a scoped daily integration batch.
- Native SQLite import/save/CQL demo now passes:3created/3unchanged/1updated, stable per-avatar private IDs and both-direction visibility/source preservation/exact cleanup. Root mapper fix preserves save-owned metadata;4311Core tests/1native integration and real console command pass. Run Scripts/TestHosts/Run-CosmicNativeImportCqlDemo.ps1 -ArchiveCompletedDemo. SQLite native source -> Mongo target now also proves the same workflow in a native test/console command;6owned target holons verified/removed. Run the same command with -Mongo. Reverse/Neo/chain/encryption/replication migration gates remain open.
- Current manifest/gitlink point to Coreaa47e48 (includes merged Development sandbox/DNA changes); native import mapping IDs include avatar identity and saved per-avatar mappings. Native reader runtime proofs from the incoming programme are separate from this compatibility check.
- Native evidence is milestone-specific; latest refresh ran Core/GraphQL/build compatibility, not every native provider. Existing demo proofs retain their earlier revision scope.
- [Resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) owns exact paths/hashes/lifecycle. Reuse SDK10.0.401, caches and retained services. No new installations for demo milestones.
- Docs/Devs/NATIVE_DATA_IMPORT.md is present. HYPERDRIVE_PER_AVATAR_SETTINGS.md is now present after the latest Development merge.
- Raw logs/TRX/JSON go only to worktree TestResults. Persistent identities and pending journals stay LocalState/COSMIC; failed/unknown outcomes require reconciliation, never replay.
- Latest owned Mongo/Neo services stopped and native fixture journals absent after verified tests. Exact test-owned SQLite files removed. Inspect live state before reuse/cleanup;historical status is not process ownership.

## Key commands

```powershell
git status --short
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Oracle -Federated -ShowPlan
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SolanaMetadata -Federated -ArchiveCompletedDemo
& Scripts/TestHosts/Test-CosmicCqlDemoHubRouting.ps1
```

One build lease, /m:2 /nr:false /p:UseSharedCompilation=false. Commit named files only.
Push/verify Core first, then matching parent gitlink/manifest. Validate manifest after commit.
Fetch/inspect before daily integration; preserve concurrent changes. No force push.
