# CQL projection and aggregate implementation contract

23 October 2026. Implementation design for the unchecked second phase of the completion roadmap. Core scalar intent/admission/execution contracts are now implemented in d71eebd; All three stores now have native Count/LongCount/Any verification; scalar transport and the remaining aggregate operations remain pending. The remaining items below are acceptance requirements, not shipped native aggregate support. Count/LongCount/Any are the first implementation slice; Select, Min/Max, Sum/Average and GroupBy remain required in the same programme.

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


## MongoDB native scalar gate — 23 October

MongoDB now implements Count/LongCount/Any using the same generic Core interface and result domains as SQLite. Row and scalar compilation share root predicates, child/descendant translation and pipeline size/depth/stage guards. Native scalar reduction counts valid roots and collects cycle/limit outcomes before applying exact window arithmetic; it does not retain entity arrays, sort whole stored snapshots, or truncate input to row/graph-publication limits. Controlled result fields cannot be forged by stored user field names.

The reader preserves a single snapshot across canonical membership validation and counting where required, with one linked cancellation/deadline budget, remaining native maxTime, simple collation and disabled spilling. Native output decoding accepts one nonnegative Int32/Int64 value and typed error flags; numeric strings, binary64, Decimal128, malformed/extra fields and invalid outcomes are rejected without coercion. Core performs checked requested-domain conversion.

Mongo unit **229** and native **76** pass unskipped, including9new unit and12new native cases. Native cases prove windows/empty input, Count/LongCount/Any and manager execution with a throwing typed constructor, invalid unselected typed metadata, actor/public access, 137 inputs above row/graph-publication bounds, descendant overflow/no publication/fresh reuse, pre-cancel/zero-window behavior and actual count snapshot consistency across an acknowledged concurrent reparent. Consumer Core716/Shared55 PE/PDB/source identities match the frozen producers. Initial native compile failed a missing test import before fixture mutations; the corrected native run passed.

Neo4j scalar source is being prepared separately and remains uncompiled/uncertified. No scalar WEB4 transport, Select/Min/Max/Sum/Average/GroupBy, join/federation or broader policy completion is claimed by these two native count gates.


## Neo4j native scalar gate — 23 October

Neo4j now implements Count/LongCount/Any through the generic Core/HolonManager scalar contract. Row and scalar shapes share one compiled native ownership/access/membership source. Projection validation and traversal error branches stay outside cardinality reduction/windowing; success requires consuming and validating the complete controlled scalar outcome. Exact Int64 cardinality/window arithmetic avoids entity hydration, factories, row caps and graph-output truncation. One read-committed transaction owns the operation; requested snapshots reject before dispatch against the actual adapter profile.

Neo unit **322** and joint native/WEB4 **129** pass with zero skips, including15new scalar native controls. Coverage includes composed windows/empty input, all3terminals and generic manager execution with a throwing typed constructor, invalid unselected metadata, owner/public restrictions,137inputs above output limits, descendant overflow/no partial scalar/fresh reuse, pre-cancel/zero-window behavior, cancellation after actual Fetch,31-second cursor-delivery expiry/reuse, projection corruption despite an empty source/page and borrowed snapshot rejection. These post-fetch controls do not prove strict interruption of blocked Neo driver I/O; that remains explicitly open.

Final all3consumer PE/PDB/source identities match Core716/Shared55 producers after normal no-restore consumer refresh. Prior Mongo76/SQLite1269 native evidence used the same source documents with earlier commit-stamped Shared binaries; those native suites were not claimed rerun during this Neo-only verification. Scalar HTTP transport and Select/Min/Max/Sum/Average/GroupBy/join/federation/policy phases remain open. [Next transport acceptance](CQL_SCALAR_HTTP_CONTRACT.md).


## Scalar HTTP gate — 23 October 2026

The [scalar HTTP contract](CQL_SCALAR_HTTP_CONTRACT.md) is implemented. FullCore1982 and jointnative129 pass, with exact unwindowed LongCount137 on MongoDB, SQLite and Neo4j. The shared source compiler preserves row-default25 independently of nullable scalar Take. The shared endpoint admission/eligible resolution and Core physical-attempt owner preserve actor affinity, no-replay, durable receipts and fixed exact result domains. Startup scalar tariffs require reservation coverage. Earlier pending-transport notes above describe preceding gates; Select/non-count aggregates/joins/federation/policy and strict blocked Neo I/O remain open. Annotated scalar demo certification now passes separately:27SDK/27HTTP,182settled/181physical attempts, all3scalar no-replay controls, generated Swagger coverage and final Core1982/demo1 unskipped.


## Projection implementation boundary — 23 October 2026

The next slice admits `Select(h => h.Name)`, anonymous scalar-member construction, and flat DTO member initialization as immutable native projection intent. It preserves the exact source object (trusted read access, scope, relationship predicates, window and graph consistency). At most32 explicit scalar columns are admitted. Every leaf uses the existing CQL path compiler; constants, arithmetic, entity results, collection initializers and arbitrary methods are rejected rather than executed in the manager. Constructor/member-setter execution belongs only after complete native result validation, not expression compilation.

A projection has separate actual-provider capabilities. Existing row/scalar support must not advertise projection execution. Native readers select admitted columns after source eligibility/windowing and keep mandatory projection/traversal validation ahead of publication. Source graph output does not hydrate entities for a projection, but requested consistency remains authoritative. Null/missing nullable/string leaves map to null; missing non-nullable values and wrong persisted domains are errors. No numeric string coercion or Int64-through-double conversion. Explicit missing-versus-null discrimination and computed selectors require a separately admitted extension.

The Core intent and shared path compiler now pass1,989tests, including7projection controls. Both net10.0 and netstandard2.1 build without restore. This paragraph does not certify a native projection provider or shipped Select endpoint. All3 native implementations, protected accounting, exact detached result conversion, positive/negative controls and annotated transport examples remain required.


Core intent `aa35a43` freezes scalar paths and flat construction metadata without retaining the selector delegate/closure. Columns are readonly and bounded; implicit numeric conversions are rejected explicitly rather than discarded. Anonymous/scalar/flat DTO selection, source/access/window/consistency retention, no user-code invocation, invalid/null source propagation,33-column rejection and result-domain preservation have focused controls inside the full Core1,989gate. Native row/scalar regression after this common-path refactor passes the dedicated demo1:27SDK/27HTTP,182settled/181attempts, with the owned fixture absent after teardown. This is not native Select certification.


## Core projection execution owner — 23 October 2026

Core `27f75b7` implements separate native projection capabilities/targets/plans and actual-adapter readmission. Canonical field name plus exact CLR type and explicit metadata domains grant result participation independently of predicate/order operators. Input arrays are captured before validation; fields/types/width are bounded. A row-only adapter rejects before any earlier native target dispatch.

`ICosmicQueryProjectionStorageProvider` returns immutable ordered raw scalar-cell rows, not holons or application DTOs. `HolonManager.ExecuteProjectionQueryAsync` requires V2 and delegates to the common prepared query-attempt owner. One durable physical attempt precedes every native callback; completed work uses the existing explicit service-query receipt. Inherited provider denial has zero calls. No activation, failover, entity factory or downloaded-row LINQ evaluator exists in this path.

Core validates every provider buffer before invoking any application constructor/setter: exact native domains, nullable/missing values, width/window, UTF-8 validity, finite numbers, total result count and total detached payload budget. Strings/immutable scalar values are not coerced or serialized through arbitrary getters. Nullable/string missing or null becomes null; required values and wrong types error. Int64 beyond2^53 stays exact. Only after every buffer validates are scalar/anonymous/flat DTO outputs constructed. Factory exceptions, cancellation or a later invalid store publish no partial execution. Per-provider provenance remains separate; replicas are not merged or summed.

The additive Core pipeline passes2,011tests including22new planning/result/executor/hosted/manager controls and both target-framework builds. The committed-cohort row/scalar native regression also passes dedicateddemo1:27SDK/27HTTP,182settled/181attempts. At this Core-only checkpoint no provider advertised native Select; the following SQLite gate supersedes SQLite availability. MongoDB/Neo4j adapters and explicit authenticated projection transport remain required.


## SQLite native projection slice — 23 October 2026

SQLite now implements the additive projection adapter. Its native column compiler reuses `CqlSqlTranslator` for source predicates, protected relationship scopes and ordering. It selects only admitted scalar columns, with compiler-owned ordinal aliases; metadata paths are bound parameters. Each selected metadata leaf carries presence/type state and its native value. Duplicate path matches and wrong terminal domains error rather than becoming null; nullable/string absent/null maps to null. Typed decoding accepts exact native domains only, preserving Int64 and UTC ticks; no numeric-string coercion or entity construction.

The existing EF concurrency scope, connection-local instruction/deadline owner and traversal guard own execution. Relationship validation completes before paging in the same deferred serializable snapshot as the column read. Caller-owned/read-only transactions remain owned by their caller. Graph output expansion/node caps do not limit projected rows, while requested consistency and source traversal limits remain enforced. Result/payload limits reject rather than truncate, and errors/cancellation release owned state before fresh reuse.

Initial5native controls passed after an actual5-case missing-interface red gate. Expanded13native controls pass, including concurrent acknowledged writer/snapshot visibility, actual-scope-reader cancellation/reuse, read-only/caller ownership, private-root paging, exact typed metadata, required-domain errors,137rows despite graph output cap, native output rejection and ambiguous nested path rejection. The complete SQLite regression passes1,282tests with zero skips. Three additional canonical-domain controls then pass in a final16-case focused gate on the unchanged production binary; full1,285was not redundantly rerun. The canonical gate covers all advertised canonical columns and rejects out-of-range Version/invalid Boolean storage. Other-store projection certification is not implied. MongoDB/Neo4j and projection HTTP remain pending.
