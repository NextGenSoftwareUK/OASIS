# Neo4j atomic primary outbox

Updated 19 October 2026. Local implementation; 170 native and 117 unit cases pass. This is not a release or deployment claim.

## Transaction invariant

An explicitly enrolled conditional graph mutation, its exact operation receipt and one frozen obligation per target share the existing Neo4j graph transaction. Enrollment uses `ConditionalHolonPlan.CreateReplicating`; source identity and ordinal target set bind the operation fingerprint. Existing unenrolled fingerprints are unchanged. Replayed receipts return before counter allocation or enrollment.

The payload is captured from the verified native acknowledgement and selected graph edges before commit, including deletion acknowledgements. The caller graph is never reread to build delivery payloads. The existing transaction owner retains commit, rollback, reconciliation and cleanup handling.

A uniquely identified `CosmicHolonOutboxSequence` node serializes range allocation. Every obligation has its own global sequence. Allocation rejects invalid/negative counters, overflow and regression behind retained obligations; it never repairs a damaged counter silently. Counter updates and queue inserts roll back with the graph and receipt. Contention remains an explicit native transaction error; no hidden retry was added.

Activation now verifies seven native uniqueness guarantees by actual label/property shape: holon identity, receipt operation, history identity, replica head, sequence owner, outbox sequence and outbox operation/target. Two guarantees on the same outbox label are counted separately. Equivalent composite property ordering is accepted. Constraint-name collisions cannot substitute a different guarantee.

## Evidence and open work

Provider build and existing 117 unit cases pass. The native suite adds synchronous/asynchronous graph enrollment tests: an owned conflicting second target forces rollback of the graph, receipt, earlier obligation and counter; the same request succeeds after removing that owned fault. It also checks selected child payloads, per-target sequences, fresh-provider receipt replay, target reorder equivalence and changed/plain enrollment rejection. Native uniqueness tests cover all seven constraints.

The full native suite passed all 170 cases with no failures/skips; see VERIFICATION.md. The final focused rerun passed both cases after adding payload-digest and no-sequence-consumption replay assertions. Neo4j [claim/renew/finish/inspection delivery storage](NEO4J_OUTBOX_DELIVERY.md) now passes its four focused native cases, including actual Neo4j-to-SQLite worker delivery. Automatic hosting, authorized target/source/tenant context, recovery administration, process-loss certification, retention limits and broader roadmap gates remain open.
