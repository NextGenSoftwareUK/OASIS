# CQL projection and aggregate implementation contract

23 October 2026. Implementation design for the unchecked second phase of the completion roadmap. Core scalar intent/admission/execution contracts are now implemented in d71eebd; SQLite Count/LongCount/Any now have native verification; MongoDB/Neo4j aggregate adapters, transport and the remaining aggregate operations remain pending. The remaining items below are acceptance requirements, not shipped native aggregate support. Count/LongCount/Any are the first implementation slice; Select, Min/Max, Sum/Average and GroupBy remain required in the same programme.

## Query and result ownership

An immutable scalar/projection intent must retain its source query, provider scope, trusted access, relationship predicates, root window and explicit consistency requirements. Terminal execution must not materialize every holon and evaluate LINQ in the manager. A native adapter executes the admitted shape and returns detached scalar/DTO results through OASISResult; scalar values must not accidentally become tracked holons.

Count counts eligible roots in the composed window, not expanded children. Skip/Take apply before the terminal aggregate. Without a window, a root-row publication limit must not silently truncate an aggregate's input. Native work, memory, deadline and cancellation budgets still apply. Graph expansion and consistency intent must be handled explicitly: removing output expansion must never remove a requested snapshot guarantee.

Access is applied before aggregate participation. Child/descendant scope validation remains ahead of root publication/windowing as specified by the relationship contract. An earlier positive Any or a negative business predicate must not hide a required traversal error. No partial scalar or grouped result is published after cancellation, invalid data, overflow or native failure.

## Capability and provider scope

Planning and actual-adapter readmission must validate the terminal operation, input path/type, result type and bounds before native I/O. A borrowed plan or a capability bit alone cannot grant an implementation. Preserve existing provider interface/constructor signatures; add explicit supported contracts instead of breaking existing adapters.

Named/current-default requests select their eligible native provider. All-provider execution must report its per-provider outcomes. Do not add provider counts together and call the sum a global logical count: replicas may contain the same holon. Global deduplication, ordering/windowing, cursors and partial-failure policy remain the explicit federation phase.

WEB4 result shapes require a documented version/discriminator and existing authoritative JWT/admission/accounting. Preserve v1 rows behavior. Every admitted physical provider attempt has its normal receipt; rejected shapes have zero native dispatch and no fabricated usage.

## Scalar semantics to lock with tests

- Count and LongCount have explicit checked Int32/Int64 result domains; empty input yields zero. Any returns Boolean and must retain required scope validation.
- Min/Max define missing/null/empty-set behavior for nullable and non-nullable inputs before advertising support.
- Sum/Average define input domain, empty input, exact accumulation, output precision/rounding and overflow. Never silently coerce numeric strings or round an Int64 through binary64.
- GroupBy defines key identity, missing versus null, numeric type equality, key ordering and bounded result cardinality. Apply access before grouping.
- Audit supplementary/BMP Unicode ordering boundaries and invalid strings before reusing a native string sort as a cross-provider aggregate contract. Do not infer general collation equivalence from ASCII examples.
- Select admits explicit scalar/member expressions and supported DTO construction. Validate detached data and access before executing constructors/factories; reject arbitrary executable methods and unsupported shapes.

Native controls must exercise the operation itself, including datasets larger than the root publication limit, windows, exact values beyond2^53, overflow, empty/null/mixed-type data, hidden actors/tenants, cancellation/deadlines and unsupported-adapter zero-dispatch. Cross-provider controls must distinguish physical replication from logical identity.

## Implementation sequence

1. Add immutable scalar intent and typed capability/readmission contracts plus manager execution ownership; preserve all source-query restrictions.
2. Implement native Count/LongCount/Any on MongoDB, SQLite and Neo4j, with exact outcomes and shared attempt ownership. Add native positive/negative controls before declaring capabilities.
3. Implement explicit detached Select and the remaining aggregates/grouping with the scalar semantics above.
4. Expose certified shapes through documented WEB4 transport and spaced/commented SDK/HTTP demos. Certify authorization, receipts and actual completed-query evidence.
5. Complete the separate join/federation and scalar/policy phases; do not mark the full roadmap complete after this phase alone.

[Roadmap](CQL_COMPLETION_ROADMAP.md), [architecture](CQL_ARCHITECTURE.md), [descendant contract](CQL_DESCENDANT_CONTRACT.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).


## Count execution design checkpoint — 23 October

The first scalar intent will be immutable and typed: Count returns Int32, LongCount Int64 and Any Boolean. It retains the original source query rather than recreating predicates or stripping its actor, scope, window or consistency request. A separate additive scalar-provider interface preserves the existing row-provider ABI. Scalar capability admission must run both during planning and against every actual adapter before dispatch; an adapter that supports row queries alone must receive zero scalar calls.

Rows and scalars must share the existing prepared-storage attempt owner. `PreparedStorageExecution.ExecuteQueryAsync<T>` already supports a generic result and binds its receipt to the durable physical attempt. Extracting common adapter binding/admission/lifetime checks is preferable to copying a second orchestration path. The scalar result exposes per-provider values with provenance, without an automatic summed total or tracked entity factories.

Native count implementations must reuse each provider's predicate/relationship compiler and read owner. SQLite will count a native ordered/windowed subquery while retaining connection-local traversal guards and snapshot ownership. MongoDB will replace row publication with a native count branch while retaining snapshot membership validation, protected traversal-error envelopes, native limits and simple collation. Neo4j will retain projection/traversal validation outside paging and return a scalar count from the eligible window in its single read transaction. The scalar path must not download snapshots and call LINQ Count in the manager.

Any must not short-circuit a required relationship/projection error. A native existence optimization is admissible only when all mandatory validation still completes; deriving Boolean from the native count is the initial safe implementation. Empty windows are admitted and return zero/false without native dispatch only where the existing empty-window contract permits it. Count overflow is checked explicitly and cannot wrap or become a rounded floating-point value.

Graph output expansion is not aggregate participation. Count does not count children or invoke output factories. A requested provider snapshot must nevertheless remain enforced; relationship predicates still traverse their independently bounded scopes. Aggregate input is not truncated by the row-publication maximum, and no implicit ten-thousand-root cap may masquerade as the complete count.

Existing string sorting uses Unicode scalar/codepoint order, not CLR UTF-16 ordinal ordering. The native U+E000/U+10000 boundary is already proved on Neo4j; additional candidate SQLite/Mongo controls now cover ascending/descending windows and metadata buckets. They are unverified until the next leased native batch. Invalid UTF-16 inputs and broader scalar/aggregate domains remain separate acceptance work; these candidates do not declare aggregate APIs shipped.


## Implemented Core foundation

Core `d71eebd7dda6d46d0192ae179d7461c6da842842` adds typed Count/LongCount/Any intent, immutable scalar target/plan capabilities, actual-adapter readmission, exact checked conversion from native Int64 counts, per-provider detached values and `HolonManager.ExecuteScalarQueryAsync`. The existing row executor and new scalar executor share `CosmicQueryAttemptOwner`; neither creates another storage-admission or receipt path. Unsupported row-only adapters reject before any earlier target dispatch.

Full Core **1,956** tests pass, including 22 new scalar cases: source/access/graph/window preservation, capability borrowing, exact values beyond 2^53, Int32 overflow, per-provider counts above the row-publication bound, all-target preflight, cancellation and no partial publication. Three hosted scalar controls verify durable attempt completion before native invocation and the normal completed-query receipt afterward; a denied inherited provider has zero native calls. At this Core-only checkpoint no provider advertised the scalar interface; the later SQLite gate below supersedes that availability statement. Native store implementations, WEB4 scalar transport, annotated demos and later aggregate/projection phases remain required.


Committed-foundation native regression now passes Core1,956/Mongo64/jointNeo114/SQLite205 with zero skips. All three consumer PE/PDB/source identities match Core716/Shared55 documents. The supplementary-character ordering candidates described above are now verified, including both Mongo metadata representations, Neo descending pages and SQLite canonical/metadata sorting. This is regression/collation evidence for the foundation, not native aggregate execution certification.


## SQLite native scalar gate — 23 October

SQLite now implements Count, LongCount and Any through the additive scalar interface and generic HolonManager executor. It re-admits the original source against its own row/scalar contracts, compiles the existing native predicates and counts the entire eligible source under the existing instruction/deadline/traversal read owner. Exact native integer arithmetic applies the composed Skip/Take cardinality after validation; ordering cannot change cardinality and is still capability-admitted. There is no entity hydration, factory invocation, manager scan or default row/graph-publication truncation.

Full SQLite **1,269** tests pass, zero skips, including 12 new scalar controls. The initial selected gate passed44 cases before adding actual-reader cancellation; full verification includes empty/nonempty windows, offsets beyond the input, Count/LongCount/Any and generic manager execution with a throwing entity constructor, invalid unselected typed metadata, actor/public denial, 137 inputs above default row/graph-publication limits, zero-window/pre-cancel no-open, descendant-budget error/no publication/fresh reuse, read-only storage, retained caller transaction/connection and cancellation after an actual native reader with later reuse. Core716/Shared55 consumer source identities match the frozen producers.

MongoDB/Neo4j native scalar adapters, scalar WEB4 transport and the remaining Select/Min/Max/Sum/Average/GroupBy/join/federation/policy phases remain open. SQLite's gate is not three-provider or full aggregate completion.
