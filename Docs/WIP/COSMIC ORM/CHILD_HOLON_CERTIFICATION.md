# Child-holon load, save and query certification

Updated 19 October 2026. Initial native traversal corrections are verified; full provider/operation certification remains in progress. This is a working matrix, not a claim that every provider or parameter combination is certified.

Latest Neo4j verification: **117 unit and 163 native tests pass**, zero failures/skips. This includes four committed-replica sequences, sixteen transaction-outcome and eight parent-assignment cases; earlier counts below are historical checkpoints. See [verification](VERIFICATION.md#committed-replica-receiver-checkpoint--19-october-2026).

## Shared traversal contract

- A direct root load starts at depth zero. Loading children without recursion includes one edge; recursion can continue until the maximum depth. Zero means unlimited. A negative depth is invalid even when child loading is disabled or no records match.
- A parent lookup has already traversed an edge when it returns its immediate children. Those result records are at `currentChildDepth + 1`. `recursive: false` returns them without grandchildren; `maxChildDepth: 1` at current depth zero likewise excludes grandchildren. The limit controls hydration below query results; the parent query still returns its matching immediate children.
- Ordinary collection/metadata query roots start at the supplied current depth. They are not implicitly parent-lookup results. Depth limits do not change the meaning of the root filter.
- `loadChildren: false` never hydrates descendants. `saveChildren: false` does not write descendants. Provider-handling flags are execution hints and must not make the selected save graph disappear. Providers that own atomic graph persistence must acknowledge every selected record.
- Saving a selected graph and replacing all relationship membership are distinct contracts. A missing child must not be treated as an implicit hard delete. Unlink/reparent semantics, retained embedded snapshots and graph history require separate certification.

## Root causes corrected

| Layer | Defect | Correction and evidence |
|---|---|---|
| SQLite parent GUID/key reads | Returned children started at depth zero; nonrecursive/depth-one queries hydrated grandchildren | Count parent lookup results at current depth + 1 and stop descendant hydration when recursion is disabled; 24 native boundary cases cover sync/async, GUID/key, recursion and depths 0/1/2, including an offset current depth |
| SQLite read validation | Negative depth could pass when children were disabled or the result was empty | Validate traversal input before querying, across the ten root/parent/collection repository read entry points; two additional native cases |
| MongoDB parent collection projection | Same parent-result off-by-one error in both embedded and provider-linked hydration | Explicit parent-result mode in shared `HolonGraphProjection`; ten shared projection tests and four native Mongo cases exercise both representations and all boundary combinations |
| MongoDB selected save graph | Default `saveChildrenOnProvider: false` omitted descendants from native writes/acknowledgements | Select by `saveChildren`, recursion and depth; native graph transaction owns persistence and acknowledgement for both provider-handling flag values. Validation now follows selected traversal rather than rejecting unselected descendants |
| Neo4j runtime selection | BootLoader referenced the older Aura adapter instead of the canonical native-tested adapter | BootLoader references canonical Neo4j project and a production factory; factory tests verify adapter type and settings, and the BootLoader build passes |
| Neo4j connection URI | Canonical adapter rebuilt secure/routing URIs as invalid Bolt host strings and mishandled bare host:port | Preserve all six supported driver schemes, explicit ports and IPv6 hosts; reject malformed explicit configuration. Supported schemes follow the [official .NET driver documentation](https://neo4j.com/docs/dotnet-manual/current/connect-advanced/) |

Neo4j's existing parent-depth behavior agrees with the shared contract. Four new native cases confirm GUID/key, sync/async, both child-source flags, recursion, depths 0/1/2 and offset current depth; its full native suite passes 127 cases. Unit/factory/URI suite passes 103 cases. The old Aura sources are retained, but BootLoader no longer selects them. Existing legacy records are not silently converted or migrated by this change; deployments need a verified data migration if those records lack the canonical native snapshot schema.

## Retained tombstone identity reads

MongoDB now agrees with SQLite/Neo4j that an exact current ID/key resolves a retained tombstone. Its linked graph cursor includes tombstoned descendants rather than changing root visibility based on the child-source hint. Four new native cases cover GUID/key × sync/async and 24 traversal combinations per case (child loading, source hint, recursion and depths 0/1/2). Full Mongo native 64 and unit 140 cases pass. See the [read contract](SOFT_DELETE_READ_CONTRACT.md) for compatibility and still-open discovery/history/mixed-state graph gates.

## Initial traversal evidence (historical)

| Check | Evidence |
|---|---|
| SQLite full native suite, including 26 new traversal cases | 573 passed, zero failed/skipped; `TestResults/sqlite-regression.trx` |
| Shared storage suite, including 10 new projection cases | 456 passed, zero failed/skipped; `TestResults/shared-storage.trx` |
| Neo4j unit/factory/URI suite | 103 passed, zero failed/skipped; `TestResults/neo4j-unit.trx` |
| Neo4j full native suite | 127 passed, zero failed/skipped; `C:/Source/OASIS/TestResults/Neo4jCurrent/neo4j-native-current.trx` |
| MongoDB unit suite | 68 passed, zero failed/skipped; `TestResults/mongo-unit.trx` |
| MongoDB focused native traversal suite | 4 passed, zero failed/skipped; `C:/Source/OASIS/TestResults/SubscriptionUsageCurrent/mongo-cosmic-children-current.trx` |
| MongoDB wider selected native transaction suite | 52 passed, zero failed/skipped; `C:/Source/OASIS/TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx` |
| BootLoader production build | Succeeded, zero errors; `bootloader-build.log` |

All native cases reused the retained SQLite fixture, Neo4j container and MongoDB 8.3.11 replica environment. The Mongo fixture uses one fixed database with an exclusive file lease, refuses a pre-existing database and removes only its verified owned fixture on success. Failed/uncertain fixtures remain available for inspection. No new runtime, download, clone or duplicate environment was created.

## Remaining certification

1. Manager-mediated child traversal and baseline establishment, including every generic/non-generic legacy/V2 path and both child-source flags; direct-provider passing results do not prove the manager routes.
2. Metadata, search and CQL relationship predicates, filtering, stable ordering/paging and projection with descendant hydration. Validate unsupported combinations before I/O rather than scanning silently.
3. Bounded and root-only updates to existing graphs: unchanged descendants, embedded snapshot retention versus live linked data, child membership removal, explicit unlink/reparent, and duplicate/cyclic/missing/deleted records.
4. Coherent historical aggregates versus independently versioned descendants, strict versus `continueOnError` partial reads, structured partial failures, cancellation and bounded native query work.
5. Native cross-client conditional graph writes and receipt recovery for MongoDB/Neo4j, plus graph-local transaction guarantees and provider capability reporting. SQLite's conditional implementation is documented separately.
6. Extend the same executable conformance matrix to every remaining provider, naming the BootLoader-selected adapter and retaining per-operation evidence. Hosted credentials, production migration and external-chain finality remain independent verification gates.

Use the [roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) for complete acceptance, and [conditional writes](CONDITIONAL_WRITES.md) for revision and receipt guarantees.

## Fresh-reader and routing follow-up

MongoDB unit coverage is now 70 passing cases, including concrete Holon discriminator registration before any write and nested embedded-child serialization without hydrated parent navigation. The complete 12-case MongoDB/SQLite routing matrix passes; parent collection assertions account for returned immediate children starting at depth one. These checks strengthen the initial provider evidence, but do not certify all manager query routes, child unlink semantics, historical graphs or every provider. Those remain roadmap acceptance gates.

## V2 root deletion boundary

V2 soft deletion now explicitly selects only the root for both loading and saving. Four native SQLite cases (sync/async, Guid/provider key) prove root tombstone revision advancement while the complete child row and concurrency token stay unchanged. Full SQLite suite: 580 passed. Sixteen core manager cases also verify detached source ownership and a single captured authority. Root-only embedded-child preservation in MongoDB/Neo4j and deletion/history propagation remain open provider-certification requirements.

## MongoDB root-only write preservation

Single-record updates with `saveChildren:false` now retain the exact observed embedded `Children` BSON within the native whole-document compare-and-swap. The excluded children are not traversed or written; the acknowledgement remains root-only. A concurrent native change fails the same CAS rather than combining a stale root replacement with newer membership.

The four native traversal cases now also submit unrelated child data to a root-only update, assert exact preserved embedded BSON and byte-identical standalone descendant documents, then verify both embedded and provider-derived graph loads. All four pass; the larger selected native transaction suite passes 52 cases. Mongo unit suite passes 71 cases.

The owned `AvatarDetail.Inventory` member has a dedicated canonical value serializer: navigation is excluded for inventory ownership without globally removing graph Children from Holon/InventoryItem class maps. A unit case verifies that the same InventoryItem retains selected children when used as an embedded graph node, and that a typed quantity filter still renders through the member serializer. Native inventory round-trip assertions remain intact and pass.

An initial field lookup in the root-only patch incorrectly searched declared members for inherited Children. The test rejected that update before dispatch and retained its three-record synthetic fixture. Independent native reconciliation verified names, logical IDs, parent links, embedded membership, hashes and empty other collections before removing only `cosmic_child_traversal_fixture`; no save was replayed. The inherited-member lookup is corrected. The original failed reports are retained once under `TestResults/SubscriptionUsageCurrent/`.

### Graph boundary finding — corrected by the following batch

Root-only single-record preservation does not yet prove bulk/depth-limited writes. `MongoDBOASIS.Graph.cs` currently builds selected embedded children and serializes frontier nodes without preserving their unselected persisted descendants. The transaction-owned correction and native evidence are recorded below. Explicit unlink/reparent semantics also remain open; do not equate child omission with authorized deletion.

## MongoDB depth-limited and bulk graph preservation

The shared write plan now records `SelectsChildren` for each selected node. This distinguishes an explicitly selected empty membership from children excluded by `saveChildren`, recursion or the current/max-depth boundary. Six unit cases verify the distinction, including initial depth equal to maximum depth. No source graph membership is changed by planning.

MongoDB's existing import transaction now stages all selected records and native observations before a graph projection hook runs, before the first native write. At every excluded boundary, the projection copies the exact observed child BSON into both the node's native document and its embedding in selected ancestors. Unknown native fields and exact numeric BSON survive. The acknowledgement is prepared separately from the selected entities and therefore does not claim to have saved unselected descendants.

The four native traversal cases now exercise six direct bounded-save configurations (single/bulk, recursive/nonrecursive, finite depth and nonzero initial depth), bulk root-only and max-depth-equals-initial-depth writes, plus V2 manager saves. They assert that grandchild documents remain byte-identical, embedded frontier BSON retains an injected unknown native field containing integer `9007199254740993`, both graph-loading modes retain the descendants, and direct acknowledgements exclude unselected nodes. V2 manager saves retain the caller's excluded grandchild edit as pending while committing selected root/child edits.

Verification: **484 shared unit tests**, **71 MongoDB unit tests**, **52 selected MongoDB native transaction cases**, and the latest four extended native traversal cases pass without failures/skips. The full native transaction run preceded the final addition of manager assertions; the subsequent four-case run verifies those assertions on the same implementation. No extra MongoDB environment or download was created; owned port 27029 has no listener after completion.

Still open: explicit unlink/reparent persistence and native/embedded membership agreement, full history semantics, conditional writes for MongoDB/Neo4j, broader typed/query/filter combinations, all-provider certification, and V2 durable tombstone/outbox delivery. This batch preserves excluded descendants; it does not declare those other requirements complete.

## MongoDB native graph acknowledgements and local revisions

The graph transaction now prepares every selected native record's identity, immutable audit data and provider-local token before materializing acknowledgements and serializing embedded descendants. Returned child DTOs therefore include the same physical revision tokens that were persisted. This corrects the earlier preparation order in which submitted child snapshots were embedded before native ownership fields were known.

The extended native cases compare each selected child acknowledgement token against the physical child document, ensure ordinary writes preserve logical version IDs, and prove local tokens rotate without mutating submitted observation maps. The existing unselected descendant byte/BSON checks and V2 pending-edit assertions still pass. Current MongoDB suites: 52 native selected transaction cases, 72 unit cases. Atomic expected-token/receipt writes are now implemented in the following conditional batch.

## Conditional graph acceptance in MongoDB

MongoDB conditional writes compare the physical token for every selected node before any mutation, and commit the graph and receipt together in the existing transaction. The native cases verify that a stale selected child rejects the entire root/child write, while a forced receipt insertion failure rolls back preceding native record writes. Exact replay returns the original selected graph after subsequent writes. V2 manager cases confirm clean acknowledged state and retained pending state on a stale competing client.

The full selected native suite now passes 54 cases; MongoDB unit suite passes 75. Explicit unlink/reparent semantics, coherent historical graphs, remaining provider coverage, native process/crash reconciliation and durable tombstone fan-out remain open.

## Neo4j native revision and root-only child isolation

The four native traversal cases now verify that a root-only update rotates only the root's physical token, preserves its supplied logical version when history is disabled, and leaves child/grandchild snapshot JSON and physical tokens unchanged. Root hydration still retains native descendant edges after the update. Removing the physical token in the owned fixture models a legacy row: reads expose zero, and the next write issues a new token despite spoofed submitted metadata, without changing that submitted observation.

Neo4j unit suite: 108 passed (five new physical-token mapping cases). Full native suite: 127 passed, no failures/skips. Conditional writes are still unsupported pending identity/receipt uniqueness constraints and atomic token/graph/receipt ownership. Explicit unlink/reparent and historical-graph consistency remain open.

## Neo4j native identity prerequisite

The provider now verifies/provisions unique holon IDs and unique receipt operation IDs during activation. Two real concurrent-create tests prove the server prevents duplicate identities rather than relying on an application pre-read or process-local lock. Full Neo4j native suite now passes 129 cases, including the earlier traversal, token, history and timeout cases; unit suite passes 112. Conditional graph mutations still need one transaction owner for expected tokens, history, edges and receipts before the capability is enabled.

## Neo4j conditional graph checkpoint

The final 134-case native suite includes four sync/async × history-on/off conditional theory cases. Stale selected children reject the whole graph without changing the root; version exhaustion rolls back parent history and the receipt claim. V2 manager conflicts preserve pending caller state, and root-only deletion retains unselected child snapshots. Independent receipt inspection after a real commit and an injected application acknowledgement exception precedes exact replay/publication. This does not certify actual wire interruption or process crash. Explicit unlink/reparent semantics, coherent historical graph reconstruction and all-provider certification remain open.

## Neo4j selected-child parent assignment

The native graph transaction now projects the `ParentHolonId` of every selected node, including a child submitted as a root. Previously only children nested in submitted parent collections updated incoming edges: an isolated child save could persist a new parent ID while retaining the old edge, causing parent reads to fail or return incorrect membership.

Setting a selected child's parent to a locally present parent moves its incoming edge; setting it to zero unlinks it. Both happen in the same transaction as the child's snapshot, physical revision, optional history and conditional receipt. The operation preserves unselected descendant snapshots and tokens and does not rewrite parent snapshots. A parent's accepted record token does not stand for an aggregate membership revision. Saving an empty/omitted child collection is not an implicit unlink/delete request; the child must be selected and its parent ID changed explicitly.

Cycle validation follows incoming parent relationships, avoiding a scan of a root's entire descendant tree. A cycle crossing unselected descendants rejects and rolls back the selected write, archive and receipt. Local relationships project locally present parents; a logical parent may live on another provider. This batch does not materialize remote parents, backfill edges when an absent parent arrives later, or repair already inconsistent stored graphs. Those remain explicit migration/reconciliation requirements.

Eight new native theory cases cover sync/async, ordinary/conditional writes and history on/off: move, unlink, parent GUID query, unchanged parent/grandchild snapshots, omitted-child preservation, descendant-spanning cycle rejection and receipt/history rollback. MongoDB embedded membership, SQLite relationship parity, manager-specific relationship publication and coherent historical graph reconstruction still need certification. Legacy Hyperdrive routing was not changed.

## Neo4j graph rollback outcome ownership

Parent-cycle rejection now requires both unchanged native snapshot/history/receipt state and a definite (non-reconciliation) error after acknowledged rollback. Native graph writes share explicit commit/rollback state across ordinary and conditional calls. Known committed selected-graph acknowledgements survive transaction/session disposal failures; single and batch wrapper counts/warnings are verified by the new ordinary-outcome fault cases. This does not change selected graph membership or claim aggregate-parent concurrency.

## Committed replica graph ownership

Four new native sync/async × history-on/off sequences use actual SQLite committed root/child graphs and the canonical Neo4j receiver. Source logical/audit identity is preserved; selected native graph/head/receipt changes share one transaction. Out-of-order/local conflicts and changed acknowledgement payloads reject atomically. Root-only soft/hard deletion preserves child snapshots, while durable replica-head tombstones retain deleted-root lineage. Exact old receipt replay does not resurrect a physically deleted root. These tests establish selected current-graph replica application, not coherent historical graph reconstruction or primary outbox delivery. See [committed replica contract](COMMITTED_REPLICAS.md).

## SQLite additional relationship-value mapping

Additional parent IDs/lists and declared child ID/cache values now survive SQLite round trips through the common persistent-value extension. Four raw single/batch sync/async cases verify the values without persisting excluded navigation or unselected child records. These values do not replace primary-parent/child graph ownership and do not certify unlink/reparent or coherent graph history. The full SQLite suite passes 597 cases, and four richer cross-provider replica sequences preserve an additional parent ID. See [mapping boundaries](SQLITE_PERSISTENT_VALUES.md).

## SQLite replica and captured-topology checkpoint

The shared plan now records selected child IDs during its original traversal. Conditional/replica plans and SQLite acknowledgements use those immutable IDs, avoiding another caller collection read. A shared changing-getter case and a native during-I/O caller-mutation case verify selection stays intact. SQLite receiver sequences preserve source root/child logical versions and root-only deletion isolation under the graph/head/receipt transaction. Both rich provider directions pass; current suites are 616 SQLite native, 165 Neo4j native and 510 shared. Ordinary parent-cycle/unlink and coherent graph-history certification remain open; this checkpoint does not equate child omission with unlink.

## MongoDB embedded audit and captured topology checkpoint

The Mongo receiver checkpoint preserves exact creation/modification/deletion timestamps in embedded canonical Core children and native child rows, including derived Core inventory values. Date query fields retain native BSON date types. Additional parent IDs and declared child IDs now survive common entity mapping. Single saves capture and validate once before choosing the native single-record or multi-record transaction owner. Ordinary graph assembly uses the planner's captured SelectedChildIds rather than rereading caller membership. A native sync/async getter-changing probe proves exactly one caller child read and persistence of that original child. See [Mongo receiver evidence and limits](MONGO_COMMITTED_REPLICAS.md). These corrections do not certify all historical graphs, unlink/reparent policies, typed queries or every provider/depth parameter combination.


## Shared traversal stack and depth arithmetic

[Shared write planning/read projection](SHARED_GRAPH_TRAVERSAL.md) now use explicit heap stacks while preserving preorder, one selected-child capture, identity/parent validation and path-scoped read cycle checks. Internal64-bit depths prevent parent-query offset overflow at int.MaxValue. Full Shared723 passes, including10 new deep graph/depth/cycle/order cases. This closes recursion in these two owners only. SQLite AttachLoadedChildren/entity mapping, checked repository parent offsets, Neo4j native/historical hydration and32-bit parent offsets, manager tracking and other provider-specific traversal still require direct correction/certification. Final native regressions pass SQLite659, Neo4j180 and MongoDB64 (zero failures/skips), with exact hashes and retained-fixture reconciliation recorded in VERIFICATION.md; a green existing shallow suite is not proof of arbitrary-depth end-to-end behavior.


## SQLite native hydration and full-range depth

[SQLite child hydration](SQLITE_CHILD_HYDRATION.md) now accepts every nonnegative public current-depth value, including int.MaxValue, and computes parent offsets using64-bit arithmetic. The repository no longer rejects maximum depth as a workaround. Existing hydration now uses explicit heap frames with path exit markers, preserving child order, native mapping, selected-cycle rejection and the existing deleted-child predicate. No cap or truncation is introduced.

All36 new boundary cases first failed against the old producer. Final full701 native cases pass, zero failures/skips, including42 additions:32 parent combinations (ID/key, sync/async, child loading/recursion flags, bounded/unlimited maximum), four collection offsets, two2048-node native graph round trips, two selected-cycle/frontier and two deleted-child/retained-ID cases. Negative argument coverage remains green. Exact producer/consumer hashes and logs are in VERIFICATION.md. Neo4j hydration/depth, broader generic/manager/history/query/cancellation routes and all-provider parameter certification remain open. Historical shared-traversal paragraphs above describe the earlier checkpoint; SQLite's checked-offset/hydration findings are now superseded for these repository routes.


## Neo4j native hydration and full-range frontiers

[Neo4j child hydration](NEO4J_CHILD_HYDRATION.md) now uses explicit asynchronous query frames and64-bit current/history offsets. The centralized hydration owner checks the frontier before issuing a child query, fixing one-extra-level collection/metadata reads as well as parent overflow. Native row order, repeated identities/parent-edge validation and partial-read error handling are retained.

Final190 native cases and130 units pass, zero failures/skips. Ten added cases cover eight sync/async × ID/key × current/historical roots, child-source/load/recursion flag combinations, bounded/unlimited maxima, metadata/collection frontiers and two1024-node graph round trips. All eight boundary cases first reproduced the old failures. An older one-extra-level test assumption and its acknowledged-read fixture cleanup were corrected; exact retained data/report were archived and conditionally reconciled without broad reset. See VERIFICATION.md for hashes. Historical roots still include current descendants by the existing policy; coherent historical graphs, manager/generic/query/visibility/cancellation routes and all-provider conformance remain open. Earlier SQLite/Shared paragraphs listing Neo recursion/offsets describe superseded checkpoints, not new unresolved findings for this owner.


## Core lifecycle and staging traversal

[Core lifecycle/save-plan owners](CORE_GRAPH_LIFECYCLE.md) now avoid recursive accept/detect/reject/stage/ack calls. Baseline membership/descendant acceptance reuse one captured physical child list, and staged/initial persistent values derive from one source serialization. Two red tests reproduce the prior mismatches. Seven new cases cover4096-node lifecycle scope/undo,2048-node staging/publication/deterministic identity order, missing-baseline preflight and complete lease release after late rejection. Core net10/netstandard builds pass; full Core1073, Shared732 and SQLite701 suites pass, zero failures/skips.

This does not close the remaining mutual recursion between PrepareHolonForSaving and SetParentIdsForHolon, or the mismatch between physical Children tracking and semantic AllChildren selection in actual STAR typed aggregates. Those are next correctness owners, not optional certification paperwork. Generic/history/query/visibility/cancellation/all-provider and wider roadmap acceptance remains open.


## Manager preparation and public V2 deep graph routes

[Manager preparation](CORE_GRAPH_PREPARATION.md) now uses an explicit enter/exit stack. Parent identity/hierarchy and audit/custom metadata are established before descendants; ancestors remain available and unencrypted during child extraction, then staged navigation is stripped and metadata encrypted on exit. The version counter is checked; exhaustion fails before provider save execution without publishing staged values.

Core1079, Shared732 and SQLite705 pass, zero failures/skips; both Core TFMs build. New unit coverage verifies2048-node public V2 saves and encryption ordering. Four native cases verify1024-node public V2 save/load/no-op paths (sync/async × provider-child flag), correct version1/actor/parent identities, untouched caller navigation and clean hydration. Earlier paragraphs identifying manager preparation recursion are superseded for this owner. Typed AllChildren/Children membership tracking/undo, arbitrary callbacks/constructors, historical graph coherence and broader generic/query/visibility/cancellation/all-provider conformance remain open.
