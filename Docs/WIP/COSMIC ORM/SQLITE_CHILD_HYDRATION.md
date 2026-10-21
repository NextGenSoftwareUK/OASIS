# SQLite child hydration and depth boundaries

Working checkpoint, 19 October 2026. Full SQLite native suite passes701 cases, zero failures/skips. Exact reproduction and binary evidence are recorded in VERIFICATION.md.

The SQLite repository previously rejected current depth int.MaxValue to avoid overflow in four checked parent-query offsets. That rejection also affected valid requests with child loading disabled. Depth is a nonnegative position, so the owner now accepts the full public integer range and computes parent-root offsets using 64-bit arithmetic. The root returned by a parent query is one edge after the supplied current depth. Maximum depth zero remains unlimited; a finite maximum at or below that root's depth selects no grandchildren. Nonrecursive parent queries still return immediate children only.

`AttachLoadedChildren` now uses heap frames and path exit markers instead of recursive calls. It retains native-row mapping at the selected node, child order, path-scoped cycle rejection and the existing DeletedDate child predicate. It does not invent a depth cap, skip corrupt selected nodes or change identity reads of retained tombstones. Source rows and caller graphs remain unchanged. The same repository owner serves ID/key, single/collection, sync/async entry points; no alternate hydration path was added.

| Native coverage | Scope |
|---|---|
|32 parent boundary cases|ID/key, sync/async, child loading on/off, recursion on/off, maximum zero/int.MaxValue, current depth int.MaxValue|
|4 collection boundary cases|Sync/async, maximum zero/int.MaxValue, current depth int.MaxValue|
|2 deep graph cases|Real SQLite save and sync/async identity load of 2048 linked records, followed by a depth-one read|
|2 cycle/frontier cases|Direct native corruption creates a selected cycle; unlimited load rejects it while a bounded frontier excludes the back-edge|
|2 deleted-child cases|Parent hydration excludes retained deleted children and their descendants; direct identity read still returns the tombstone|

All 36 new boundary cases first failed against the prior producer. The final full701-case suite passes after the fix, including both deep graphs and four cycle/visibility cases. This is native repository coverage, not proof that all generic DTO, manager, historical, query, cancellation or cross-provider graph routes support arbitrary depth. The [Neo4j counterpart](NEO4J_CHILD_HYDRATION.md) now passes its current/history frontier and native hydration coverage. Broader generic/manager/history/query and all-provider parameter certification remain open. Legacy Hyperdrive is unchanged.
