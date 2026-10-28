# Current COSMIC ORM / CQL status

Updated28October2026. Read first after compaction. Full programme remains incomplete.

## Workspace and rules

- Work/build/test only C:/Source/OASIS-cosmic, parent/Core branch work/cosmic-cql.
- Work only on red checklist cells; freeze green features/cursors absent a reproducible bug.
- Entity-selected All/element/numeric LINQ depth is paused. Large POCO/migrations and untagged parity work stay deferred until demo acceptance; do not silently resume them.
- HyperDriveV2 only; legacy gaps documented. No paid cloud provisioning/upgrade.
- [Checklist](CQL_PARITY_CHECKLIST.md), [launcher](CQL_DEMO_LAUNCHER.md), [manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md).

## External prerequisite and exact next step

1. Hosted Azure Cosmos is the remaining external prerequisite in the current breadth queue. User has no existing free subscription. The no-subscription sandbox was rechecked and still shows Under Construction.
2. User explicitly chose **leave hosted Azure pending**. Do not repeat signup requests or provision an account. The last official free-account route reached blank Microsoft sign-in. No account/resource/subscription created and no terms accepted. Official page discloses a possible temporary$1 card-verification hold; never choose pay-as-you-go. [Admission and next gates](CQL_HOSTED_AZURE_FREE_ADMISSION.md).
3. Hosted opt-in read runner now builds with strict non-secret configuration and explicit -Mongo/-Serve wiring;68controlled CQL tests pass, plus local SDK/HTTP1/1regression/exact cleanup. Hosted auth/config presence rechecked:false/false. The same signup/auth prerequisite remains an actual external blocker after preparation; do not claim completeness. Actual hosted SDK/HTTP/browser/Mongo and free-account verification remain red. Hosted prerequisite implemented:Cosmos ExistingResourcesOnly activation validates pre-provisioned database/container identity/partition without creation or provisioning fallback;6new cases/52affected CQL tests pass. Native hosted proof remains red. After an authenticated genuinely free account is available:verify spending protection/zero-cost configuration, prepare a dedicated journaled hosted fixture, then prove native SDK/HTTP/browser and Mongo pairing against the real hosted endpoint with exact cleanup. Emulator evidence cannot satisfy this.
4. Next unblocked acceptance:immutable endpoint/store/policy binding in automatic enrollment/receiver validation and operator handling of old unbound routes. All3native identity providers now implemented:Neo3cases preserve its complete opaque ID and captured storage-driver URI;16Core controls/4341fullCore and SQLite7/Mongo10compatibility pass. StoreId is now a bounded native string;GUID constructor/fingerprint bytes remain compatible. Cluster/home-database/rebinding acceptance remains broader. SQLite prerequisite passes7native/7contract cases and4332Core tests;Mongo now passes4identity+6atomic native cases using read-only native collection UUID capture from the actual repository handle;capture is explicit and not yet consumed by automatic routes. Durable identity row is atomic,not replaced if corrupt/empty;native-address fingerprint distinguishes a copied endpoint. Full binding acceptance remains open. Automatic WEB4 now uses the shared policy receiver;default admission validates committed actor/selected ownership,host DI may supply a full stricter policy. Six controls/1278Shared tests and3native shipping cases pass(default delivery,denied grant,expired grant). Denials retain original payload/primary and block in reconciliation across restart with no replica. Tenant/encryption/residency grants and durable physical binding remain open. Neo→SQLite crash proof predates this policy wiring;ordinary restart was rerun successfully. Hosted Azure stays pending and LINQ depth paused.
5. Remaining full ORM correctness/replication/history/migration/placement acceptance is preserved in the [roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md); this phase does not declare it complete.

## Verified demo breadth

| Scope | Verified evidence and limits |
|---|---|
| SQLite/MongoDB/Neo4j | Original12investor rows green;34native entity/reselection/cardinality cases/database.681SDK queries/138HTTP requests/30save checks in grouped demo. Full EF/NHibernate parity remains red. |
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
- Current parent manifest/gitlink point to Corec1b8bd8; native import mapping IDs include avatar identity and saved per-avatar mappings. Native reader runtime proofs from the incoming programme are separate from this compatibility check.
- Native evidence is milestone-specific; latest refresh ran Core/GraphQL/build compatibility, not every native provider. Existing demo proofs retain their earlier revision scope.
- [Resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) owns exact paths/hashes/lifecycle. Reuse SDK10.0.401, caches and retained services. No new installations for demo milestones.
- Docs/Devs/NATIVE_DATA_IMPORT.md is present. HYPERDRIVE_PER_AVATAR_SETTINGS.md is now present after the latest Development merge.
- Raw logs/TRX/JSON go only to worktree TestResults. Persistent identities and pending journals stay LocalState/COSMIC; failed/unknown outcomes require reconciliation, never replay.
- Last owned native services stopped and journals absent. Inspect actual state before reuse; a historical report alone does not prove today's process state.

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
