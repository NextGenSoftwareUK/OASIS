# COSMIC ORM implementation roadmap

Date: 19 October 2026. Design and acceptance plan. The first tracking/save-ownership batch is implemented and locally verified; see [current status and test evidence](IMPLEMENTATION_STATUS.md). Later stages remain open.

Read with the [audit](COSMIC_ORM_AUDIT_2026-10-19.md) and [evidence inventory](COSMIC_ORM_SOURCE_EVIDENCE.md). This plan deliberately reuses shared graph planning, provider checked writes and hosted/Edge synchronization rather than adding competing persistence paths.

## Scope clarification — 19 October 2026

User direction: implement and certify Hyperdrive V2 only. Record legacy Hyperdrive gaps in [the legacy inventory](LEGACY_HYPERDRIVE_GAPS.md); do not repair or refactor legacy routing/fan-out as part of this goal. Shared COSMIC ORM and provider contracts remain in scope where required by V2. Historical legacy test evidence is retained, but additional legacy certification is not an acceptance gate.

## Current acceptance checkpoint — 20 October 2026

The complete scope below remains active. [Implementation status](IMPLEMENTATION_STATUS.md) summarizes verified foundations and remaining work for all eight stages. Selected tracking/concurrency, graph ownership and search fixes have native evidence; they do not close the full roadmap. Per-save committed deltas and complete CQL have not been delivered; the initial predicate compiler is documented in Stage 8.

Detailed sections preserve dated checkpoints. Treat earlier counts and remaining-work notes as historical where superseded by [integration evidence](INTEGRATION_BOUNDARY.md). Legacy implementation is excluded by user direction; shared provider fixes needed for V2 remain in scope.

## Required invariants

1. One logical edit produces one acknowledged logical revision. Replicas preserve that revision's identity; retries do not invent edits.
2. An unchanged tracked aggregate produces no provider write, audit/version change or replication event.
3. Failed preparation or a definitely rejected write does not publish committed state into the caller. Unknown outcomes retain the original operation identity and reconciliation evidence.
4. Conditional save/delete atomically checks the expected persisted revision at the authoritative store.
5. When durable replication is promised, primary state and its outbox obligation commit together. Replica acknowledgement is idempotent and target-specific.
6. Reads satisfy a declared freshness/authority policy before latency/cost ranking.
7. Authorization, tenant and data-placement requirements constrain every route and retry.
8. Every requested guarantee is supported explicitly or rejected before mutation.

## Stage 1 — Repair entity lifecycle and tracking

Manager preparation follow-on: [ordered enter/exit frames](CORE_GRAPH_PREPARATION.md) remove mutual recursion, preserve encryption/context order and check revision overflow. Core1079/Shared732/SQLite705 pass with deep public V2 routes. Later [typed ownership and encryption-context evidence](TYPED_CHILD_OWNERSHIP.md) advances local collection/acknowledgement correctness, with latest Core1128 passing. Save publication now retains submitted effective encryption settings across configuration mutation during I/O. Persisted key context, generic-load decryption, role hydration and declared arbitrary callback/constructor failure semantics remain open; earlier checkpoints below are historical.

Lifecycle follow-on: [Core graph lifecycle](CORE_GRAPH_LIFECYCLE.md) now uses heap traversal and single child/source captures, with Core1073/Shared732/SQLite701 passing and both Core TFMs building. Remaining concrete owners: mutually recursive manager audit/encryption preparation and physical Children versus typed semantic AllChildren tracking/undo. These must be fixed before closing universal graph lifecycle acceptance.

Owners: HolonBase, Holon partials, HolonManager preparation/save/load and canonical serialization helpers.

- Define a persistent projection, excluding runtime services/events, Original, temporary caches and cyclic navigations. Include declared custom properties and graph membership according to the aggregate boundary.
- Add a detached tracking entry: original committed snapshot, current instance, original revision token and state (`Detached`, `Unchanged`, `Added`, `Modified`, `Deleted`). State is local; persisted existence is resolved by the authoritative provider.
- Capture the baseline after successful hydration and commit. Do not serialize tracking state or persist snapshots recursively.
- Detect changes before audit/version preparation. Cover mutable nested values, equal-value replacements and child membership. Reject cycles where unsupported; avoid unbounded traversal.
- Correct notification timing. Suppress dirty notifications during hydration and baseline acceptance without hiding user edits made during a pending save.
- Replace mutation-and-restore preparation with an operation-local write plan. Preserve original caller values on failure. Publish acknowledged values through a full explicit mapping contract.
- Remove the manager's unconditional change-detection returns once tests establish correct behavior. Keep an explicit opt-in force-write operation separate from ordinary Save.

Acceptance: a load is clean; no-op save issues zero I/O; editing any persistent scalar/custom field/nested value is detected; reverting values becomes clean; child add/remove/reparent follows declared rules; checkChildren=false excludes child-only edits. New empty-ID metadata-only objects save correctly. Preparation, encryption, provider rejection and cancellation leave caller state correct. Notifications expose the new value. Original never aliases the live graph.

Regression matrix: sync/async, generic/non-generic, object convenience/manager entry points, single/batch, Hyperdrive V2. No need to reproduce every permutation in every provider suite: centralize pure tracking tests and use representative route contract cases.

## Stage 2 — Specify undo, concurrency and transactions

Proposed APIs below are names for discussion, not existing methods:

| Operation | Meaning | Required behavior |
|---|---|---|
| RejectChanges | Discard local unsaved edits | Restore tracked baseline including collection membership; no remote writes |
| AcceptChanges | Accept an acknowledged baseline | Normally internal after verified commit; never imply a remote write |
| Reload | Fetch authoritative state | Explicit conflict behavior if local edits exist |
| RestoreVersion | Restore historical content | Conditional new revision, audit actor/reason, history retained |
| SaveChanges | Save tracked aggregate/unit of work | Declared provider-local atomicity or structured partial outcomes |
| Compensate | Counteract a committed external action | Separate durable business operation with its own status |

Introduce expected revision and stable logical operation ID in the write contract. Reuse native CAS/checks where implemented; unsupported providers must return a precise unsupported-guarantee error. Separate logical revision from native transaction hash and provider key. Return conflict data sufficient to choose reject, reload, merge or authorized overwrite; never silently merge money, balances or ownership.

Define transaction capabilities per provider and operation. Start with atomic writes within one authoritative store; add distributed sagas for multi-provider business processes. Keep transaction scopes bounded. No global rollback promise across heterogeneous chains.

Acceptance: two clients load revision N; one commits N+1 and the other receives conflict without overwrite. Conditional delete cannot remove a newer record. Same operation replay returns the same commit. A failed atomic batch commits nothing; best-effort mode returns exact per-item acknowledgements. Restoring version N creates a newer revision and preserves all prior retained versions.

## Stage 3 — Certify history, mapping and graph semantics

### Child-holon load/save/query certification — added 19 October 2026

Typed semantic ownership checkpoint (20 October): [explicit collection capture and restoration](TYPED_CHILD_OWNERSHIP.md) now aligns tracking, save planning and Shared write planning. Core1,092, STAR8 and SQLite709 tests pass at their recorded checkpoints, including native V2 typed-child saves and local STAR body/core/space membership and subscription restoration. Durable relationship-role hydration, Mongo/Neo typed native cases and broader query certification remain open.

Neo4j native follow-on: [hydration and depth frontiers](NEO4J_CHILD_HYDRATION.md) now use explicit query frames,64-bit current/history offsets and a centralized pre-query frontier. Final190 native/130 unit cases pass. Historical graph coherence, broader manager/generic/query/visibility/cancellation routes and all-provider conformance remain open.

SQLite native follow-on: [repository hydration](SQLITE_CHILD_HYDRATION.md) now uses heap traversal and64-bit parent offsets, accepting the full nonnegative current-depth range. Full701 native cases pass with42 new boundary/deep graph/cycle/visibility cases. Neo4j-specific hydration/depth and broader manager/generic/history/query/all-provider certification remain open.

Shared traversal checkpoint: [write planning/read projection](SHARED_GRAPH_TRAVERSAL.md) now use explicit heap stacks and64-bit internal depth arithmetic. Full Shared723 passes with deep graph/cycle/frontier/order tests. Provider-specific hydration/mapping/history/manager recursion and native parent-depth offsets remain open; this is not end-to-end unlimited-depth certification.

Audit and fix child loading, saving and querying in every storage provider, beginning with **MongoDB, SQLite and Neo4j**. Identify the BootLoader-selected implementation when several adapters exist; do not certify an unused implementation by mistake. Extend the same conformance suite and evidence matrix to the remaining providers after these three.

Define traversal once at the WEB4 contract boundary and test native provider behavior against it. Cover `loadChildren`, `saveChildren`, `recursive`, `maxChildDepth` (including zero/unlimited and invalid negative values), `currentChildDepth`/legacy spelling aliases, `continueOnError`, `loadChildrenFromProvider`/`ChildrenFromProvider`, and `saveChildrenOnProvider`. Verify direct provider calls as well as HolonManager using Hyperdrive V2, generic/non-generic and sync/async overloads. Each advertised parameter must affect behavior as specified or fail explicitly before I/O if unsupported.

Use graphs with multiple roots, siblings, children and grandchildren, mixed holon types and metadata, empty collections and records beyond the selected depth. Verify inclusive/exclusive depth boundaries, immediate children when recursion is disabled, no unintended descendants in results or writes, stable parent identity, selected graph acknowledgements, and no changes to unselected descendants. Exercise parent-ID/provider-key and metadata/query routes, filtering/order/pagination over children, deleted children, missing references, corrupt records, duplicate identities, cycles and reparenting. Distinguish unlinking a child from deleting it.

For saving, verify create/update/mixed graphs, parent-before-child identity allocation, transaction/partial-failure behavior, selected-child concurrency conditions and rollback. For loading, verify clean tracking only after the requested graph is hydrated, cancellation, bounded traversal, and current versus coherent historical graph semantics. Include CQL relationship traversal and predicates in the same matrix when CQL is implemented. Query limits must apply to declared graph/query semantics, not silently truncate descendants.

The [retained tombstone identity contract](SOFT_DELETE_READ_CONTRACT.md) now aligns MongoDB with SQLite/Neo4j: ID/key reads and linked graph snapshots resolve retained tombstones. Mongo unit140/native64 and four selected MongoDB/Neo4j worker cases pass. Explicit visibility for discovery/parent/metadata/CQL queries and broader mixed-state/historical graphs still requires implementation and certification before closing this stage.

Acceptance: MongoDB, SQLite and Neo4j each have an explicit parameter/operation capability table with native test evidence; every supported combination matches the shared contract, unsupported combinations produce precise errors, and the same suite is applied to all remaining providers. Source inspection or mocked calls alone do not count as native-provider certification.

Build a maintained machine-readable provider capability matrix covering create/update/read/delete, exact history, metadata queries, parent traversal, graph atomicity, export/import, conditional writes, deduplication, cancellation and durable fan-out support. Include active BootLoader-selected implementation, runtime/configuration flags and native contract/ABI version.

Use explicit capability values: implemented and verified, implemented but unverified, unsupported, and not applicable. Store evidence pointers and test dates. Provider count is not a substitute for route coverage.

Unify typed codecs; cache descriptors only after correctness. Distinguish current aggregate reads from coherent historical graph snapshots. Define version retention and what happens when history settings change. Use UTC consistently for audit and delivery timestamps.

Acceptance: deterministic cross-provider round trips including precision, nulls, byte arrays, nested collections and custom types; exact-version misses never return latest; unsupported history fails clearly; parent/child version policy is explicit; soft/hard delete respects declared history and chain limits; corrupt data fails visibly.

## Stage 4 — Close durable delivery gaps

Wire admission checkpoint: the [committed graph decoder](COMMITTED_GRAPH_WIRE_CONTRACT.md) now rejects coercible scalar types, alternate GUID forms and incorrectly cased fields before typed conversion or native I/O. Existing producer format, receipt fingerprints and empty-actor host-policy semantics are retained. Full Shared713 passes; native configured-host2 passes with the initial strict decoder. Payload/graph/queue resource bounds and authenticated admission remain open.

Current startup/security checkpoint: [strict ONODE catalog configuration](ONODE_COSMIC_REPLICATION_CONFIGURATION.md) is implemented and native-tested. Shared initialization is awaited before registry lookup, exact capabilities/grants are checked, native stores captured and workers joined on shutdown. Authorization freezes the entire envelope before policy invocation; asynchronous replacement cannot change the authorized write. Full Shared692, configured native-host2 and full ONODE build pass. Earlier paragraph counts below are historical checkpoints. Default CRUD enrollment, authenticated tenant/record/encryption admission, physical binding verification, manifests/migration, crash recovery and queue limits remain open.

Current receiver prerequisite: MongoDB, SQLite and Neo4j now implement explicit committed-replica intents under their existing native transaction owners. [MongoDB receiver](MONGO_COMMITTED_REPLICAS.md) includes exact embedded audit precision, captured graph routing, source/local head checks and causal-majority uncertain-commit verification. Actual SQLite/MongoDB receiver directions and the explicit [hosted graph adapter](HOSTED_COMMITTED_GRAPH_DELIVERY.md) now pass four native combinations. All six directed MongoDB/SQLite/Neo4j [explicit outbox-worker paths](OUTBOX_PROVIDER_DIRECTIONS.md) now have native sync/async evidence. Provider-wide/default CRUD enrollment and crash/retention/security acceptance below remain open. Receiver implementation alone does not complete this stage.

SQLite now implements explicit [primary/outbox enrollment](ATOMIC_HOLON_OUTBOX.md) under its existing graph transaction, including manager/V2 routing. SQLite [delivery leases](HOLON_OUTBOX_DELIVERY_LEASES.md) now implement ordered per-target claims and token-fenced outcomes. The explicit [bounded dispatcher](HOLON_OUTBOX_DISPATCHER.md) consumes SQLite claims with renewal and native SQLite-to-MongoDB completion proof. MongoDB now implements [atomic primary enrollment](MONGO_PRIMARY_OUTBOX.md) under its existing transaction owner. MongoDB [delivery leasing/inspection](MONGO_OUTBOX_DELIVERY.md) now implements the recovery-capable store and actual MongoDB-to-SQLite worker path. Neo4j now implements [atomic primary enrollment](NEO4J_PRIMARY_OUTBOX.md), with 117 unit and 170 native cases passing. Neo4j [delivery leasing/inspection](NEO4J_OUTBOX_DELIVERY.md) now passes its four focused native cases, the full 174-case native regression and actual Neo4j-to-SQLite worker path (128 unit cases also pass). ONODE host configuration/readiness, durable target/catalog resolution and crash/security acceptance remain open.

Explicit [Generic Host/catalog integration](COSMIC_HOLON_REPLICATION_HOST.md) now automatically schedules configured routes, preserves independent target progress and joins shutdown. Versioned route IDs bind source/store/endpoint/policy descriptors; every target requires an explicit authorization policy. Shared 653 and two native host cases pass, including expired-grant blocking without target writes. ONODE configuration/factory/readiness, persisted catalog manifests, tenant/record/encryption authorization, default/global enrollment and operator target/grant migration remain open.

[Native-head readiness](COSMIC_REPLICATION_READINESS.md) is now implemented for the three selected stores and configured host. A durable reconciliation head cannot appear healthy merely because claim returns no work, including after service restart. Shared656/SQLite658/Mongo141-unit+64-native/Neo129-unit+6-delivery+2-host cases pass. The ONODE-specific readiness endpoint is now [wired and HTTP-tested](ONODE_COSMIC_REPLICATION_STARTUP.md), and the application builds. Enabled configuration/provider factory, initialization/lifetime and default enrollment remain open.

Choose one authoritative commit/outbox owner for each durable mode. Reuse IHyperDriveSyncStateStore, hosted fan-out stores, leases and target idempotency contracts. Connect ordinary CRUD only where primary+outbox atomicity can actually be enforced. Avoid a second post-write enqueue that leaves a crash window.

SQLite now persists [typed native recovery evidence](HOLON_OUTBOX_RECOVERY_EVIDENCE.md) atomically with fenced outcomes and supports read-only inspection; actual crash-before-outcome journaling and authenticated reconciliation remain open.

Persist operation ID, logical version, payload/schema version, target set, target acknowledgements, attempt/lease state and reconciliation evidence. Require ordered application for the same record; do not allow an older queued update to overwrite a newer one. Retain tombstones until the replay horizon is safe. Define poison-operation handling and repair tooling. Configuration changes need target-set versioning and a backfill strategy.

Expose `CommittedPrimary`, `PendingReplication`, `Replicated`, `SubmittedOnChain`, `Confirmed`, `Finalized`, `Conflict`, `ReconciliationRequired` and terminal failure as appropriate. These are proposed semantic states; map them onto existing contracts rather than duplicating state machines unnecessarily.

Acceptance: crash immediately after primary commit, worker restart, lease expiry, duplicate delivery, out-of-order updates, target outage, target-set change, expired offline grant, deletion versus delayed edit, and accepted native write with lost response. Original transaction must be reconciled without an unsafe second spend.

## Stage 5 — Deliver controlled migration

Build a migration job over existing import/export and domain backfill primitives. Explicitly distinguish logical data copy from value transfer or smart-contract migration.

Stages: preflight/capability and permission checks → snapshot → target transform → paged copy → normalized content/hash/count verification → incremental catch-up → controlled cutover → observation → explicit source retirement. Keep durable checkpoints, rejects and per-record audit evidence. Preserve identities and define version/key lineage. Define how direct legacy writes are captured during migration and how rollback of the cutover works before irreversible source retirement.

Acceptance: resume after interruption without duplication; detect incompatible schemas and unsupported fields before cutover; retain consistent relationships; prove deletion catch-up; reconcile live writes; compare source/target semantic data; reverse direction using the same verified process. Existing Mongo backfill CLI is a building block, not proof of this full workflow.

## Stage 6 — Add gas-aware and user-aware placement

Prerequisite: Stage 4 durable intent handling, plus exact distinction between a data record and a settlement command.

Policy inputs: supported operation and asset/contract context, permitted providers/chains, trust/finality requirements, data residency, replica freshness, live fee quote age/currency, maximum cost, latest execution deadline, user consent and latency SLO. Use measured request-path latency and explicit endpoint identity; report unknown metrics instead of ranking defaults as measured facts.

For deferred settlement, persist the authorized intent atomically with its local business record, reserve funds or enforce an explicit credit-risk model, and queue execution. Revalidate quote, nonce, balance, deadline and authorization before signing/submission. Track native identity through finality and reorg handling. Chain selection must be limited to semantically equivalent permitted execution targets. Moving JSON to a cheaper database does not execute the original transaction.

Acceptance: high fees cause durable deferral; fee drop resumes; prolonged high fees hit a visible deadline; budget change/authorization revocation prevents execution; native timeout triggers reconciliation; duplicate worker does not double-submit; failed/reverted transactions retain fee evidence; stale pricing cannot approve an oversized spend.

## Release and operational gates

Start with a deliberately small certified set: one authoritative transactional Web2 provider, one durable Edge store, and one specifically chosen chain/provider pair. Select from current maintained environments and release gates rather than installing new copies. Expand only after equivalent contract suites pass.

Measure p50/p95/p99 latency by operation/provider, source-to-replica lag, oldest pending operation, recovery duration, stale/conflicting reads, uncertain commits, fees actually incurred, and data verification failures. Define read/write availability separately and publish the tested failure model.

Run focused pure tests first, then controlled route tests, then native integration/fault tests using existing reusable environments. Bound .NET workers with `/m:2 /nr:false /p:UseSharedCompilation=false`; retain compact evidence and clean owned fixtures only after processes stop and targets are verified. Respect the current provider release-gate ledger; do not sweep concurrent unverified changes into a release.

Done means the declared guarantees pass through the real public route and active provider implementation, including restart/failure cases. A TODO removed, a mock pass or a version field populated is not sufficient acceptance evidence.

## Stage 7 — Per-save deltas

Prerequisite checkpoint: [relationship membership comparison](RELATIONSHIP_DELTA_PREREQUISITE.md) now compares immutable manifests without inferring deletion from unlinking. Core1,135 passes. This is not committed Save evidence; all transaction, revision, authorization, persistence and replay acceptance below remains open.

Requested 19 October 2026. Expose immutable, version-linked change sets for each acknowledged Save: added/removed/modified fields and metadata entries, nested value changes, child additions/removals/reparenting, soft/hard deletion and restoration. Distinguish a collection removal from deletion of a persisted entity. Include previous/new values where authorized, logical operation ID, actor, authoritative provider and previous/committed revision. Do not include unsaved edits made while a write is pending in that write's committed delta.

Generate deltas from the same canonical before/after commit values that own concurrency and history; do not infer them from UI notifications. No-op saves have empty deltas and perform no write. Persist durable deltas atomically with the authoritative commit when history/audit is requested, with explicit redaction/encryption and retention policy. Replayed operation IDs return the original delta, not a new change event. Provide preview of local pending changes separately from committed evidence.

Acceptance: scalar/custom/metadata/nested/graph edits, explicit null versus missing, collection order, delete versus unlink, concurrent edits, rejected writes, uncertain acknowledgement and idempotent replay all produce the correct authorized result. A failed conditional write must never advertise a committed delta.

## Stage 8 — CQL (COSMIC Query Language)

Initial implementation: [the CQL predicate compiler](CQL_EXPRESSION_CONTRACT.md) now freezes typed scalar/metadata comparisons and boolean composition into an immutable representation without provider I/O or arbitrary client execution. Typed immutable query composition and explicit current/named/all-eligible scope are also implemented; pure scope/capability planning is implemented; nineteen cases pass in Core1,308. security integration, native execution and all remaining acceptance below are still required.

Requested 19 October 2026. Build a typed LINQ/EF-style query API over WEB4, with a provider-neutral expression/AST contract suitable for later textual CQL. Support composed predicates (AND/OR/NOT, equality/ranges/inclusion/null tests and metadata paths), ordering, projection, paging, aggregates and relationship queries according to explicitly declared capabilities. Replace the limits of exact metadata selectors without concatenating provider-native query strings from user input.

Provider scope is explicit: current default, named provider(s), or all eligible providers. Default to the current provider to avoid surprising cross-network scans; applications may configure all-provider scope deliberately. Apply actor/tenant/visibility, residency, encryption-query limitations, cost limits and timeouts before choosing execution targets. Plan pushdown against adapter capabilities; bounded client evaluation must be an explicit opt-in with visible limits, never a silent substitute for unsupported server execution.

For federated execution define identity/version deduplication, provenance, authority/freshness selection, global sort/paging, partial failures and cancellation. Return a query plan/explanation describing targets and pushdown/client work. Do not call a union of independently paged stores globally ordered or snapshot-consistent. No implicit writes, settlement or cross-provider joins with hidden unbounded work.

Acceptance: equivalent supported queries produce equivalent typed results on certified adapters; unsupported expressions fail before provider I/O; hostile predicates cannot inject native queries; multi-provider duplicate/conflicting revisions and partial failures are explicit; pagination is stable under the declared consistency policy; ownership filtering cannot be removed by projections or client evaluation. Document runnable examples and the capability matrix before declaring CQL complete.
