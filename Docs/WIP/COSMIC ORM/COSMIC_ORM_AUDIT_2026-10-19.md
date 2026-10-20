# COSMIC ORM implementation audit

Date: 19 October 2026. Status: source audit and implementation proposal; not a production certification.

Implementation update: this document preserves the **pre-implementation audit findings**. The first tracking/save-state batch has since been implemented and tested. Read [current finding status and verification](IMPLEMENTATION_STATUS.md) before treating the findings below as unresolved defects.

## Assessment

COSMIC has a substantial foundation: a common holon identity and object model, simple object and manager CRUD, custom-property mapping, hierarchical data, provider-independent results, version fields, export/import, and HyperDrive routing. Its most compelling proposition is application data portability with explicit choices about performance, durability and decentralization.

It is not yet a complete EF-style tracking/unit-of-work system. The most immediate gaps are actual snapshot tracking, save-state ownership, undo semantics, uniform concurrency contracts, and clear delivery guarantees across the direct CRUD and hosted synchronization paths. HyperDrive already contains meaningful recovery and synchronization infrastructure; this should be reused rather than replaced.

Recommended positioning: **a provider-independent persistence and synchronization layer for hybrid Web2/Web3 applications**. Keep COSMIC ORM as the product name, while explaining that its scope extends beyond relational mapping. A portable data record is not automatically a portable token, payment, smart contract or trust guarantee.

Companion documents:

- [Implementation roadmap and acceptance criteria](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md).
- [Commercial positioning and use cases](COSMIC_ORM_USE_CASES_AND_POSITIONING.md).
- [Source evidence and TODO inventory](COSMIC_ORM_SOURCE_EVIDENCE.md).

## Scope and evidence limits

Inspected the working source in `OASIS Architecture/NextGenSoftware.OASIS.API.Core`, including all Holon partials, HolonBase/AuditBase, HolonManager CRUD/helper files, IOASISStorageProvider, the V2 router, provider selection/metrics, synchronization contracts and fan-out handler. Also inspected shared graph planning, representative provider implementation locations, migration tooling, test-source inventories, and current HyperDrive release-gate notes.

Parent HEAD at audit: `39f667ac7ac4e7b9f21b683062a75d90ded611c7`. Core submodule HEAD: `632ccc9bf337e65ba5b139ba8f3da6432a092945`. Both repository and submodules contain concurrent local changes. Findings concern the inspected working tree, not just these commits. Existing changes were preserved; this audit adds documentation only.

No builds, automated suites, provider services, chain transactions or failure drills were executed for this audit. Existing test files establish coverage intent, not fresh passing results. Historical evidence in release notes remains attributed to those notes. Provider inspection is representative, not a new exhaustive certification of every provider/method/configuration combination. Absence findings below mean no common implementation was located in the inspected ORM surface; they do not deny specialized implementations elsewhere.

## Architecture and present capabilities

| Layer | Present implementation | Boundary to make explicit |
|---|---|---|
| Holon object API | `Load`, `Save`, `Delete`, async/generic variants and children APIs | Active-record convenience through managers; not an isolated EF DbContext |
| Object model | GUID identity, metadata, provider keys, audit/version fields, parent/child references | Persistent state and runtime navigation/tracking state are intertwined |
| Mapping | `[CustomOASISProperty]` extraction and typed hydration | Type fidelity and serializer behavior require contract tests |
| HolonManager | Single/batch CRUD, metadata queries, parent traversal, file wrapper, provider transfer | Legacy and V2 execution paths differ |
| WEB4 provider abstraction | Common IOASISStorageProvider operations and OASISResult | Interface membership does not establish uniform atomicity/history/query semantics |
| HyperDrive V2 | Provider selection, failover classification, mutation replication and diagnostics | Direct replication is attempted in the request; not universal durable eventual delivery |
| Hosted/Edge sync | Device sequences, version bases, checkpoints, conflicts, durable fan-out contracts, leases, offline grants | Separate protocol and provider capabilities; deployment and adapter coverage matter |
| History | Version/link fields, version-selecting reads, provider-specific history work | Record history, chain transaction history and graph snapshots are different guarantees |
| Migration | SendHolon, import/export and a Mongo hosted-domain backfill tool | No demonstrated universal migration orchestrator with cutover/reconciliation |

## Prioritized findings

Severity: P1 blocks a reliable advertised ORM guarantee; P2 limits correctness or product completeness in a defined scenario; P3 is maintainability or efficiency. Findings marked **confirmed** follow directly from the cited source. **Contract gap** identifies an absent common guarantee, not a claim that all providers fail.

### C01 — Change detection is explicitly bypassed (P1, confirmed)

`HolonManager-Private.Replication.cs`, `HasHolonChanged` and `HasAnyHolonsChanged`, set `HasAnyHolonsChanged = true` and immediately return. The intended comparison code is unreachable. Legacy save helpers invoke these methods, so merely finishing Holon.HasHolonChanged would not activate write suppression. V2 SaveHolon routes prepared saves without this comparison.

Impact: unchanged saves can issue provider writes, increment versions, trigger replication and incur costs. Fix detection before save preparation, with an explicit force-write option if needed; remove the bypass only once tracking is correct. An unchanged save must leave version, audit, provider traffic and replication unchanged.

### C02 — Original is an alias, not a snapshot (P1, confirmed)

Load methods repeatedly assign `result.Result.Original = result.Result` or `holon.Original = holon`. Both references observe the same mutations. `PrepareHolonForSaving` then clears Original. There is no independent persisted baseline for structural diffing or undo.

Fix: capture a detached immutable snapshot of the canonical persistent projection after successful hydration/commit. Exclude Original, events, services and navigation cycles. A shallow clone is insufficient for nested metadata, lists and child values. Do not repurpose the existing import/write-attempt snapshot as a tracking snapshot without specifying its lifetime and content.

### C03 — Holon comparison and notifications are incomplete (P1, confirmed)

`Holon.Part1.cs:HasHolonChanged(bool checkChildren)` never uses checkChildren, omits MetaData/content comparison and custom/child state, compares some dictionaries by reference, repeats one provider-type comparison, and ends with `return Id != Guid.Empty`. Thus an unchanged persisted holon reports changed; a fresh empty-ID object with only untracked values can report unchanged. C01 currently masks some downstream effects.

HolonBase marks Name and Description dirty, but most properties are auto-properties and nested dictionaries/lists are not comprehensively tracked. Those two setters notify before updating their backing field, so a subscriber reading the property sees the old value.

Fix: define equality for the persistent model, honor graph options, track additions/removals/reparenting, and notify after assignment. Start with snapshot diffing; optimize with notifications only after parity tests. Derived properties and mutable metadata need explicit value comparers.

### C04 — Save preparation mutates caller state before durable acknowledgement (P1, confirmed)

`PrepareHolonForSaving` assigns identity/audit fields, increments version, creates VersionId, clears Original, extracts/encrypts metadata, sets parent IDs and removes navigation references before provider execution. V2 preparation calls this same routine and catches preparation exceptions without restoring those mutations. Provider-level detached planning in shared code does not undo earlier manager mutation.

Impact: rejected/failed preparation or provider I/O can leave the caller with advanced versions, missing references or encrypted metadata. Retrying can appear to be a new logical edit. Preallocated IDs also take the manager's modification branch even though the comment correctly assigns durable existence resolution to the provider.

Fix: create one detached persistence plan. Provider/native persistence owns existence resolution and atomic revision commit; publish committed identity/audit/version values to the caller only after acknowledgement. A caller may preallocate identity, but allocation must not imply persistence. Unknown native outcomes must retain reconciliation evidence and must not publish a fictitious successful state.

### C05 — Generic ORM rollback is not implemented as a clear contract (P1, contract gap)

No common `AcceptChanges`, `RejectChanges`, unit-of-work rollback or restore-version workflow was located in Holon/HolonManager. Bridge compensation and HyperDrive configuration rollback elsewhere are different operations. `RestoreCelesialBodies` restores temporarily removed navigation references; it is not persisted data rollback.

Specify four distinct operations: discard unsaved edits; abort an uncommitted local transaction; restore historical data as a new version; compensate an externally committed operation. None can erase a finalized blockchain transaction. See roadmap for proposed APIs and acceptance tests.

### C06 — Revision fields do not establish universal optimistic concurrency (P1, contract gap)

The common StorageOperationRequest exposes Version and payload but no explicit general ExpectedVersion/ETag condition for CRUD writes. Manager version increments alone cannot prevent two clients overwriting the same revision. Provider-specific checked writes and sync BaseVersionId/conflict responses already exist; do not claim concurrency is absent everywhere.

Unify a conditional-write contract for save and delete: expected revision, atomic comparison, committed revision and structured conflict. Reject unsupported requested guarantees before writes. EF's concurrency-token model is a useful reference: the database checks the original token in the update condition ([Microsoft guidance](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)).

### C07 — Batch/graph atomicity is not a uniform guarantee (P1 for atomic-save claims, contract gap)

Batch/graph APIs expose continueOnError, recursion and provider-side child flags. Shared HolonGraphWritePlan already validates null/repeated/cyclic nodes, identity duplication and parent conflicts using detached snapshots. That is good reusable work, but planning is not an atomic distributed commit.

Offer explicit `AtomicWithinProvider` versus `BestEffort` semantics and per-node outcomes. Reject atomic mode on unsupported providers. A cross-provider operation needs a durable saga or acknowledgement policy; a transaction on one provider does not cover other providers/chains.

### C08 — Direct replication and durable eventual delivery are different (P1 for eventual-delivery claims, confirmed boundary)

V2 `RouteRequestAsync` awaits `ReplicateSuccessfulMutationAsync` after primary success. That routine loops configured replicas, records diagnostics and adds warnings for failures. It does not itself establish an atomic durable outbox with the primary commit. A process death between commit and replication, or an offline replica, needs a repair mechanism beyond that request.

Separately, SyncInterfaces defines durable local outboxes, hosted fan-out leases, device sequencing and idempotent replication targets; hosted dispatcher/state-store implementations exist. Reuse and connect these capabilities where guaranteed eventual replication is requested. A source contract alone is not proof that every CRUD provider is enrolled. The located explicit idempotent-target implementations include EdgeSQLiteSyncStateStore and HoloEdgeMutationRepository; this is not evidence of coverage across all chains.

Require per-target status, idempotent operation IDs, ordering, retry policy, retained terminal errors and reconciliation. Primary success with replica warnings must remain distinguishable from globally synchronized success.

### C09 — Legacy hard-delete replication can stop before all replicas (P1, confirmed)

In the legacy GUID DeleteHolon path, the replication loop replaces the result with a replica result and breaks on the first successful secondary deletion. With multiple secondaries, later replicas may retain data. Do not extrapolate this defect to V2's separate replication loop.

Fix the legacy invariant if that mode remains supported: all configured targets receive the tombstone/delete, with primary acknowledgement retained and per-target results. Add partial failure and three-provider tests. Define tombstone retention so delayed devices cannot resurrect deleted data. Immutable/public-chain deletion must be described in provider-specific terms.

### C10 — Historical versions require a provider/route capability matrix (P1 for universal-history claims, contract gap)

Version and PreviousVersionId fields plus a version parameter are not sufficient proof of retained history, historical metadata queries, export or delete behavior. Current HyperDrive release gates explicitly describe scoped native history work with remaining wrapper/hosted/V2 certification gaps. Never turn those notes into an all-provider pass claim.

Specify exact-version NotFound versus unsupported, latest semantics, retained versions, restore behavior, version lineage across replication, and history retention when settings change. Historical parent/current-child reads may be intentional, but must not be advertised as a point-in-time graph. Add a coherent graph-snapshot option if required.

### C11 — Provider transfer does not honor its documented replication override (P2, confirmed)

`HolonManager.SendHolon<T>(..., bool autoReplicate = false)` documents an override, but its body does not use autoReplicate. It calls ordinary SaveHolon, returns the loaded source object, and sets IsSaved after checking IsError rather than explicitly validating a destination acknowledgement. Transfer also passes through ordinary save preparation, potentially creating another revision.

Define copy versus move versus replica versus history-preserving migration. Carry per-operation replication policy through the authoritative route and return destination commit evidence. Do not change global configuration temporarily to implement the override.

### C12 — Mapping has divergent conversion paths (P2, confirmed)

Holon.Part3.SetProperties copies only selected members; CustomKey is commented out, and common fields such as IsPublic are not included in that method. Sharing some mutable references does not guarantee the receiving object has every persisted field. Its MapMetaData has hand-coded conversions and unresolved type TODOs; the float branch passes Convert.ToDouble to reflection rather than producing a Single value. Manager mapping separately supports complex JSON with case-insensitive options. Trace callers before assigning runtime impact to this private mapping path.

Unify mapping with one explicit canonical codec and cached/generated metadata descriptors. Prove round trips for decimal/double precision, nullable/enums, UTC timestamps, byte arrays, lists, dictionaries, custom subclasses and both serializer boundaries. Reflection optimization is secondary to correctness.

### C13 — Runtime state and serialization ownership need separation (P2, confirmed risk)

The manager stores temporary parent/core references in instance dictionaries keyed by holon ID and removes them during restoration. The singleton convenience path can share a manager. Overlapping saves for the same ID can contend for these temporary entries; this is a concurrency risk inferred from the structure, not a reproduced race.

Use operation-local immutable persistence projections rather than destructive navigation stripping and restoration caches. V2 provider execution already avoids globally changing CurrentStorageProvider during normal routing; preserve that improvement. Legacy current-provider switching and static object convenience calls still need a concurrent-use contract.

### C14 — Authorization and encryption boundaries must be explicit (P1 before sensitive-data deployments, contract gap)

Manager collection visibility filtering deliberately returns unfiltered data when avatarId is empty; some convenience reads do not accept an actor. ONODE has HolonAccess/HolonWriteOwnership helpers and associated tests. Therefore do not infer public API exposure from the manager alone, but also do not treat the manager as a universally enforcing authorization boundary.

Persist through an explicit actor/tenant context; establish which trusted internal calls may omit it. Test the same policy through HTTP, GraphQL, RPC, direct SDK and synchronization. Metadata encryption intentionally leaves queryable system/additional keys plaintext, while `data` is excluded from metadata encryption. Do not advertise whole-record confidentiality from this metadata feature. Test key rotation, override/read compatibility, query leakage, failure publication and replica key access.

### C15 — Cache lifecycle is incomplete as an ORM contract (P2, inspection finding)

HolonManager has a mutable `_allHolonsCache`, one-hour TTL and ClearCache. Inspected load code populates it; no save/delete ClearCache calls were located in the manager. This does not prove stale reads: the inspected load path does not clearly consume a cache hit. Clarify actual consumers before introducing invalidation patches.

Either remove unused caching or define scoped keys for provider, tenant, actor, filters and version, immutable entries and post-commit invalidation. Do not introduce a shared unkeyed identity map by extending this field.

### C16 — Geographic/cost optimization is narrower than the proposed promise (P2, confirmed boundary)

V2 calls ProviderManager's selection, not the standalone ProviderSelector. ProviderManager geographic routing ranks supplied NetworkLatency with a default when data is absent; cost routing ranks supplied TotalCost/CostPerOperation, also with defaults. Standalone ProviderSelector has an explicit geographic stub, but that is not evidence that the V2 call uses the stub.

The inspected route does not demonstrate per-user nearest-node discovery, continuous live gas quotations, budget/deadline deferral, chain fee-drop triggers or chain-aware settlement equivalence. PerformanceMonitor accepts geographic/cost updates; tracing their freshness and deployment-specific producers remains a release task.

Build routing eligibility first: supported operation, data presence/freshness, finality, authorization, residency, key access, then cost/latency ranking. Distinguish provider type from an individual endpoint/node. A fast empty or stale store cannot safely serve the same request.

### C17 — Failover improves resilience; 100% uptime is not established (P1 for the claim)

V2 includes failover diagnostics, classification and reconciliation-aware behavior. A native transaction with an unknown outcome must be reconciled before replay; failure detection alone cannot make replay safe. Preserve that safeguard.

Availability also depends on replica readiness, common credentials/DNS/gateways, hosted process uptime, network partitions, capacity and chain finality. An available stale replica may violate consistency requirements. Define operation-specific SLOs, recovery time/data-loss objectives and measured fault domains. Offer degraded modes only where the application explicitly permits them.

## Additional completeness gaps to specify

These are proposed product contracts rather than claims that existing provider code lacks every capability:

- Session/context lifetime, identity resolution, tracked/no-tracking reads, attach/detach and aggregate boundaries.
- Validation and schema evolution independent of provider physical-schema migrations; required/unique fields and compatibility rules.
- Query capabilities, stable pagination, indexed metadata, server versus client evaluation, projections and cancellation propagation.
- Bulk operations, payload/graph limits, streaming export, backpressure and explicit unsupported-operation results.
- Logical operation IDs, deduplication retention, conditional delete, same-record write ordering and ambiguous-commit recovery.
- Observability linking logical operation, provider attempts, committed version, replica lag, native transaction IDs and finality.
- Data lifecycle: retention, tombstones, archive compaction, restore authorization and off-chain/private payload placement.

## Completion recommendation

Fix the tracking/save-state invariants first. Standardize capability and concurrency guarantees next. Connect durable synchronization to the exact CRUD routes for which eventual delivery is promised, then certify a small provider set end to end. Gas-aware deferral and broader routing policies should build on that durable foundation.

The commercial opportunity is credible, particularly for gradual modernization, offline applications and portable application data. The defensible differentiator will be verified semantics across providers, operational transparency and developer simplicity—not the provider count alone.
