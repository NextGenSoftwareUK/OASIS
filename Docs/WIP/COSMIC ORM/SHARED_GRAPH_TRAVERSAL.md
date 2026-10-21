# Shared graph traversal ownership

Working implementation checkpoint, 19 October 2026. Final regression evidence is recorded in VERIFICATION.md: Shared723, SQLite659, Neo4j180 and MongoDB64 pass, zero failures/skips.

`HolonGraphWritePlan` and `HolonGraphProjection` now use explicit heap stacks instead of recursively calling themselves for each selected child. Unlimited depth does not impose one CLR call frame per selected edge. No artificial depth cap or silent truncation was added. Memory remains proportional to the selected graph/frontier; resource admission limits remain a separate open requirement.

Write planning retains root order and depth-first preorder. Nodes still have unique references/identities across the selected aggregate, parent identity checks, operation-local allocated identities and immutable selected-child IDs. Source children are captured once when selected, including empty membership, and pushed in reverse order so processing preserves caller order. Unselected child getters are not read. Snapshot/intent capture and its strict metadata admission are unchanged. The plan performs no provider I/O and publishes no allocated identities into caller objects.

Read projection preserves clone isolation, child order and path-scoped cycle detection. Exit frames remove an identity only after its selected descendants finish. A cycle outside the selected frontier remains outside that read. Embedded children and provider-indexed relationships use the same iterative projection. Each call owns its traversal state.

Internal depth arithmetic uses 64-bit values. Public nonnegative depth parameters remain 32-bit. Parent-query roots start one edge after the caller's current depth, including when that value is int.MaxValue; this offset must not overflow or wrap into a different traversal selection. Nonrecursive parent queries return immediate children without grandchildren. Zero maximum remains unlimited.

Ten new Shared cases cover a 4096-node unlimited write plan, 12000-node unlimited read projections through both edge sources, four parent-query offset combinations at int.MaxValue, two cycle/frontier cases, and root/sibling preorder with repeated-node rejection. The full Shared suite passes723 cases, zero failures/skips.

This certifies these shared traversal owners, not unlimited-depth operation through every ORM route. Provider-specific hydration, entity mapping, historical graph reconstruction and manager tracking can have separate recursive paths. The [SQLite follow-on](SQLITE_CHILD_HYDRATION.md) now removes its recursive hydration and checked parent-offset workaround, with701 native cases passing. The [Neo4j follow-on](NEO4J_CHILD_HYDRATION.md) also fixes native recursion and current/history depth offsets/frontiers, with190 native/130 unit cases passing. Broader manager/generic/history/query paths still need their own certification before universal child-depth acceptance. Graph query visibility, unlink/reparent/history coherence and all-provider capability coverage also remain open. Legacy Hyperdrive is unchanged.


Core follow-on: [tracking/save-plan lifecycle](CORE_GRAPH_LIFECYCLE.md) now uses heap traversal too, with Core1,073/Shared732/SQLite701 suites passing. Manager audit/encryption preparation still recurses, and typed semantic AllChildren aggregates need consistent tracking/undo ownership; no universal deep public-route claim is made.
