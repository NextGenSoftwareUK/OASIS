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

## Verification and limits

Seven new native cases plus existing replica/outbox controls: 31 pass, zero skips. Shared replica/transport controls: 67 pass, zero skips (four new baseline plan cases). Native source graph actually committed through versions 1–4; tests cover sync/async installation, version-5 delivery, exact replay, provider restart, competing clients, child conflict rollback, independent hard delete with history disabled, and unknown receipt rejection.

The first hard-delete test exposed a new implementation defect: the initial receipt fence only inspected replica envelopes. It now also inspects the canonical conditional graph wrapper. The failed proof is retained separately in TestResults; no failing implementation was deployed.

## Still required

1. Equivalent native initialization on MongoDB and Neo4j, with their transaction/concurrency and durable receipt proofs.
2. An application-authorized enrollment coordinator: verify canonical source snapshot and revision, bind source/target identities and expected absence, account for writes that race baseline capture, and record durable transfer intent/receipt before acknowledging enrollment.
3. An authorized, fenced reconciliation/resume protocol for an existing blocked obligation. Baseline initialization alone does not change queue state or retroactively complete an earlier update. Preserve original delivery evidence and independently verify the target before resuming.
4. Operator/UI/API composition and hosted three-database acceptance, including tenant isolation. Never expose the trusted primitive directly as unauthenticated repair.

Hosted queue sequence 6 remains blocked at a version-4 holon. It has not been bootstrapped, skipped, reset or replayed. Actual target lineage still needs inspection before choosing a repair. Hosted Azure remains separately pending/free-only.
