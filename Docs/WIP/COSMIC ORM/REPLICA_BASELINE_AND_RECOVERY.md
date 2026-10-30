# Replica baseline and recovery status

Updated 30 October 2026. X10a/X10b remain incomplete; this is native implementation evidence, not hosted repair approval.

## Implemented: explicit SQLite initial baseline

The trusted native capability `ICommittedHolonReplicaInitializationStorageProvider` accepts a committed source snapshot through `InitializeCommittedHolonReplica` / `InitializeCommittedHolonReplicaAsync`. The calling application must authorize the transfer and verify the source snapshot. No HTTP endpoint, automatic delivery permission or source-authentication policy is introduced by this interface.

- Uses the existing atomic SQLite write owner and an IMMEDIATE writer reservation.
- Can preserve a committed version greater than 1, including logical IDs, audit and selected graph edges. Generates target-owned provider revisions.
- Requires a nonempty actor and a Save snapshot; rejects tombstones and incoherent/uncommitted source content.
- Fences every selected target node against current data, replica heads, archived revisions and retained write receipts. Existing conditional and replica receipt envelopes are inspected explicitly; unknown envelope shapes prevent initialization.
- A conflict anywhere rejects the complete graph, receipt and replica heads atomically. Concurrent initializations have one winner.
- A separate fingerprint domain binds initialization receipts. Normal sequential delivery cannot impersonate initialization or reuse its operation ID.
- Exact initialization replay returns its original acknowledgement, including after restart or subsequent versions, without rewinding current data.
- Normal delivery still requires version 1 on an uninitialized target and an exact predecessor thereafter. No implicit baseline or lineage relaxation was added.

Source: [capability](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Providers/ICommittedHolonReplicaInitializationStorageProvider.cs), [shared plan](../../../Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/CommittedHolonReplicaPlan.cs), [SQLite transaction](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/Persistence/Repositories/HolonRepository.Conditional.cs), [SQLite baseline fence](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/Persistence/Repositories/HolonRepository.Replica.cs).

## Implemented: MongoDB native baseline

MongoDB implements the same trusted capability and shared initial-baseline plan. Its existing snapshot transaction owns graph, replica heads and exact receipt. Every atomic graph mutation now updates a persistent `CosmicHolonLineageFence` for each selected ID in deterministic order, including hard deletes; initial baseline installation requires that fence to have been absent. This makes competing writers conflict natively rather than trusting an unlocked missing-row snapshot. Positive operation replay remains read-only and does not touch fences.

Atomic receipts now contain explicit `HolonIds` membership. Initialization refuses retained receipts with absent, empty or malformed membership with `COSMIC_REPLICA_BASELINE_RECONCILIATION_REQUIRED`; it does not infer absence from historical receipt JSON. An audited migration of older receipts remains required before initialization in such a database. Current rows and replica heads still prevent installation. Mongo history is embedded in its current record, which the current-row fence protects. Legacy non-atomic repository mutations are outside this V2 primitive's concurrency guarantee; no legacy implementation was changed.

Two native sync/async scenario runs pass on the reused Mongo replica set. Each actually commits a source graph through versions 1–4, installs/reopens/replays its baseline, delivers version 5 without rewinding on replay, races two initializers, rejects independent child data atomically, rejects reinitialization after an independent hard delete, and refuses an unknown historical receipt. Eight shipping sandbox billing lifecycle cases also pass against this changed atomic owner.

Source: [Mongo capability](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoDBOASIS.Replica.cs), [transaction owner](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/HolonRepository.Import.cs), [lineage fences](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/HolonRepository.Replica.cs), [native scenarios](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/MongoReplicaInitializationTests.cs). Runner: `wsl -d Ubuntu -u dellams -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/run-mongo-replica-initialization-linux.py`, after bounded builds under the worktree lease.
## Verification and limits

Seven new native cases plus existing replica/outbox controls: 31 pass, zero skips. Shared replica/transport controls: 67 pass, zero skips (four new baseline plan cases). Native source graph actually committed through versions 1–4; tests cover sync/async installation, version-5 delivery, exact replay, provider restart, competing clients, child conflict rollback, independent hard delete with history disabled, and unknown receipt rejection.

The first hard-delete test exposed a new implementation defect: the initial receipt fence only inspected replica envelopes. It now also inspects the canonical conditional graph wrapper. The failed proof is retained separately in TestResults; no failing implementation was deployed.

## Still required

1. Equivalent native initialization on Neo4j, plus audited Mongo historical-receipt membership migration. Existing Mongo/SQLite primitives do not establish end-to-end enrollment acceptance.
2. An application-authorized enrollment coordinator: verify canonical source snapshot and revision, bind source/target identities and expected absence, account for writes that race baseline capture, and record durable transfer intent/receipt before acknowledging enrollment.
3. An authorized, fenced reconciliation/resume protocol for an existing blocked obligation. Baseline initialization alone does not change queue state or retroactively complete an earlier update. Preserve original delivery evidence and independently verify the target before resuming.
4. Operator/UI/API composition and hosted three-database acceptance, including tenant isolation. Never expose the trusted primitive directly as unauthenticated repair.

Hosted queue sequence 6 remains blocked at a version-4 holon. It has not been bootstrapped, skipped, reset or replayed. Actual target lineage still needs inspection before choosing a repair. Hosted Azure remains separately pending/free-only.
