# Core graph lifecycle and save-plan ownership

Working checkpoint, 19 October 2026. Both Core target frameworks build; full Core1,073, Shared732 and SQLite701 suites pass, zero failures/skips; final evidence is recorded in VERIFICATION.md.

The tracking and save-plan owners now use explicit heap stacks for baseline acceptance, change detection, reject planning, staged graph construction and acknowledgement enumeration. Internal traversal levels are 64-bit. Depth-first node/root ordering is retained so operation-derived new identities keep their existing index meaning. Failed staging still disposes every acquired save lease.

Baseline acceptance captures each selected physical child collection once and uses that same capture for both stored membership and descendant acceptance. Previously a changing getter could put a child in the parent's baseline without accepting that child. Save staging similarly serializes each source once, deriving both initial values and the detached staged object from those same bytes. Previously two serialization passes could observe different persistent values. Two deterministic red tests reproduced those inconsistencies against the previous producer.

Reject planning walks the accepted membership, including descendants temporarily removed from the live graph. It materializes all required baseline snapshots before publishing any restoration; a missing accepted descendant therefore fails before changing earlier nodes. Existing root-only versus child-inclusive behavior is retained. Acknowledgement traversal still validates/materializes selected identities before publishing source state, and pending-edit/publication semantics are unchanged.

Seven new Core cases cover the two capture defects, 4096-node accept/detect/reject with both child scopes, missing-baseline preflight at depth 2,048, 2,048-node staging/replay identity ordering and publication, and release of all leases after late repeated-node rejection. Core net10.0 and netstandard2.1 build successfully. These are lifecycle-owner tests, not a remote transaction, durable retry journal or proof that every manager route can handle arbitrary depth.

## Remaining public-route gaps

- The [manager preparation follow-on](CORE_GRAPH_PREPARATION.md) now replaces that mutual recursion with enter/exit frames and verifies1,024-node public V2 native save/load/no-op routes. This supersedes the earlier preparation finding, while arbitrary typed constructors/callbacks and aggregate mappings remain separate gates.
- Save-plan selection retains the existing semantic `AllChildren` contract, while physical membership tracking/undo uses `Children`. Actual STAR CelestialSpace/CelestialBody/Core overrides combine other typed collections. Change detection, undo and commit membership for those aggregates need one explicit mapping/ownership contract and tests; replacing AllChildren with Children would discard supported semantic children.
- Multi-node acceptance/publication and arbitrary user-defined property setters/notifications need their declared failure behavior audited separately. This change preserves existing scalar callbacks; it does not promise atomic rollback of arbitrary user code.
- Historical graph coherence, generic/query/visibility/cancellation and all-provider conformance, durable recovery/security/migration, committed deltas and CQL remain open. Legacy Hyperdrive routing is unchanged.
