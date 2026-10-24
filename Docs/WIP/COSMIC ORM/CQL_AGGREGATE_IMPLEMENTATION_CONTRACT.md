# CQL projection and aggregate implementation contract

23 October 2026. All three priority stores implement native Count/LongCount/Any, detached Select, selected Min/Max and exact integer Sum/Average. Current acceptance:2,155 Core/HTTP tests,195 native regression tests,42 SDK/42 HTTP examples, zero skips (222 settled/217 physical attempts). GroupBy key capture subsequently passes Core2,169; grouped selection/planning/native execution remain required. See [grouping contract](CQL_GROUPING_CONTRACT.md). Historical checkpoints below retain earlier staged evidence and do not mark the full CQL/ORM roadmap complete.

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


## MongoDB native projection slice — 23 October 2026

MongoDB now implements the additive projection interface and separate canonical/metadata read domains. The native source compiler applies protected filters, relationships and ordering/windowing; a final native project returns scalar cells only. Source descendant errors retain the existing separate facet branch outside paging and columns. Projection intent never invokes stored-holon deserialization or an entity factory.

Metadata extraction supports the provider's object/array dictionary representations without collapsing duplicate matches through array-to-object conversion. Selected cells carry explicit state plus exact BSON scalar value. Wrong/missing required domains and duplicate paths are errors; nullable/string absent/null maps to null. GUID text/standard UUID/legacy UUID retain the actual read contract. UTC values reuse the storage-owned exact-ticks companion, with duplicate matching companions rejected instead of choosing the first. Native Int64 never travels through double.

Membership validation and projection share one native snapshot when required; graph consistency intent is retained independently of graph output caps. One cumulative30s native/driver deadline covers membership, aggregate and cursor consumption; simple collation and no disk spilling remain explicit. Count/byte limits reject rather than truncate. Every returned cell/envelope validates before Core constructs DTOs. The standard prepared attempt/receipt owner remains in Core, without a parallel accounting path.

Unit236/native81 passed for the initial implementation (7newunit/5newnative cases). The expanded unit237/native91 gate passes with zero skips, including snapshot reparenting, actual-aggregate cancellation/reuse,137rows and budgets, UUID layouts, canonical fields, duplicate paths/precision companions and unsupported admission. Neo4j and authenticated projection transport remain open.

## Neo4j native projection slice — 23 October 2026

The additive projection adapter reuses `CompileCqlSource` and `NeoCqlTranslator.Operand`, including mandatory visibility, membership and descendant validation. Invalid ownership and traversal branches remain outside paging. Explicit compiler-owned scalar cell aliases retain separate missing/null/valid/invalid states and exact native types. Typed decoding preserves Int64 without double conversion, checked narrow integers, defined enums, GUIDs and Boolean values. Unselected metadata and entity factories are not evaluated. DateTime metadata is not advertised.

Selection runs in one bounded read-committed transaction with the existing cumulative read-budget owner. Requested snapshot guarantees are rejected at actual-adapter readmission. Graph output caps do not change projected row counts; scope traversal caps remain enforced. A server-side character/structural lower bound suppresses impossible cell buffers before transfer; a one-row driver fetch size bounds buffering. Exact strict UTF-8 accounting then rejects multi-byte overflows. Count/byte limits never publish a truncated result.

Actual red70 showed65existing passes and5 missing-interface InvalidCast failures. Initial fullunit328/native81 pass unskipped, covering12canonical fields, nested/exact metadata, visibility, ordering/paging,137row projection independent of graph output caps, required-domain errors, no silent truncation, pre-cancel/actual-fetch cancellation and31second deadline/reuse, corruption outside an empty window, descendant overflow and borrowed snapshot rejection. Final fullunit328/joint native-WEB4 147 pass with zero skips on the updated production binary, including18new native projection cases. Two controls inspect actual cursor buffers to prove oversized ASCII values are suppressed before transfer and multi-byte values undergo exact UTF-8 accounting; fresh reuse succeeds. The driver4.2 blocked-I/O interruption limitation remains open; actual-fetch tests do not certify that stronger guarantee. Authenticated projection transport/demos and non-count aggregate/grouped shapes remain later gates.

## Public projection transport checkpoint

The new bounded request/compiler/endpoint uses generic Core runtime records and the existing authenticated admission, resource filter and prepared-attempt/receipt owner. FullCore2,059 passes unskipped, including30compiler and11endpoint controls. Signed native three-store endpoint acceptance1/1 passes24seconds, proving privacy before paging, exact Int64/null/GUID/nested values, correct provenance/receipts and no replay. Unsupported/schema rejection settles with no provider execution. This initial native run is endpoint-only; expanded10-exampleSDK/HTTPwalkthroughs, browser/Swagger examples and broad147 regression now pass fresh console-mode verification:30SDK/30HTTP and broad147native cases, unskipped (195settled/190physicalattempts). [Request/wire contract](CQL_PROJECTION_HTTP_CONTRACT.md).

## Min/Max semantics and implementation gate

The next scalar extension retains the original source, scope, actor restrictions, relationship validation, window and requested consistency. Min and Max select one typed scalar path and return that same CLR domain; they reuse the scalar executor and common prepared-attempt owner. Count/LongCount/Any remain compatible. No row enumeration or client aggregate evaluator is admitted.

| Input domain | Missing/null participation | Empty/all-null result | Comparison |
| --- | --- | --- | --- |
| Required value type | Any missing/null participating leaf is CQL_DATA_INVALID | CQL_EMPTY_SEQUENCE | Exact admitted scalar domain |
| Nullable value type | Ignore absent/null participating leaves | Null | Exact underlying domain |
| String | Ignore absent/null participating leaves; empty string participates | Null | Unicode scalar/codepoint order, matching established portable native ordering |

Wrong terminal storage domains always error; numeric text cannot become an integer. Invalid selected values outside the source window do not participate; mandatory ownership/relationship/traversal errors still cannot be hidden by a page or negative predicate. Graph output caps never truncate aggregate input. Requested snapshot consistency remains independent of output expansion.

Int64/narrow integer/Boolean/GUID/defined-enum domains retain exact values without binary64 conversion. UTC DateTime metadata requires the storage owner's exact tick contract; Neo4j cannot borrow DateTime support. Unsupported scalar/read/operator domains reject at planning and actual-adapter readmission before any target dispatch. Negative extrema are valid: the scalar executor's nonnegative validation must apply specifically to count domains. Result validation also rejects non-finite or invalid UTF-8 values and enforces a bounded scalar payload.

Capabilities separately authorize the operation, exact input/result domains and canonical/metadata read paths. Row/filter or count support cannot grant Min/Max. Extend the current scalar intent/planner/results/executor and native read owners; do not create another accounting or orchestration path.

Machine-readable reference inputs: OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/Fixtures/CqlMinMaxVectors.json (19vectors). They specify required/nullable empty inputs, missing/null/wrong domains, Int64 extremes beyond2^53, negative extrema, narrow integer bounds, Boolean order, Unicode scalar order, GUID order and individual UTC ticks. These are acceptance fixtures, not native certification. Tests must consume relevant vectors and add windows, visibility, large inputs, borrowed capabilities, cancellation/deadlines and no partial publication against each native owner.

Sum/Average and GroupBy remain in the full phase. Their accumulation/rounding/overflow, null-key and cardinality contracts require explicit implementation; Min/Max alone does not complete this phase or the broader goal.

Min/Max Core foundation is now implemented in cf4a5ab: typed selectors, operation-specific read capabilities, shared path/read admission, explicit empty results and exact selected-value validation. FullCore2,102/dual-target builds pass, including11new controls. The scalar executor now accepts negative selected values while retaining nonnegative count checks. All-target actual read-profile rejection occurs before first dispatch. Native execution gates are recorded in the provider slices below; Neo4j and selected-scalar HTTP/demos remain open.

## SQLite native Min/Max slice

SQLite now implements selected extrema in the existing scalar read owner. One native ordered/windowed CTE computes invalid-input count, participating-value count and MIN/MAX; a malformed non-winning value cannot hide behind a valid extremum. Source ownership/traversal validation remains outside the page, in the same serializable snapshot as selected values when multiple native reads are required. Requested graph consistency remains admitted while graph output caps do not truncate aggregate inputs.

Projection and scalar selection share the same presence/type operand and exact decoder. Canonical SQL storage is mapped into that same domain guard, preserving numeric bounds, defined enums, Boolean values and GUID shape. UTC dates use the existing fixed-width calendar representation, retaining individual ticks. Native scalar byte bounds suppress oversized output before transfer. A deterministic reserved UTF8 validity function checks raw SQLite blobs before string decoding can substitute replacement characters; the existing read-budget owner installs/removes it and retains instruction/deadline/connection-limit ownership.

Initial5capability-red controls now pass5green. Expanded19reference vectors plus9ownership/error controls pass32/33; the sole failure was a fixture using MetaData instead of the backing MetaDataJson column. Correcting that fixture then passes the raw-invalid-UTF8/function-teardown/fresh-reuse case on the unchanged production binary. The complete33case feature coverage includes exact negative Int64, null/empty/wrong types, Unicode/GUID/tick ordering, bad non-winning canonical values,137inputs beyond row/graph caps, actual-scope cancellation/reuse, acknowledged concurrent writer/snapshot, caller transaction retention and traversal errors outside the source window. Full SQLite regression now passes1,318tests with zero skips (2m9s), including all33new Min/Max cases. Neo4j Min/Max and selected-scalar HTTP/demos remain required; the MongoDB gate is recorded below.


## MongoDB native Min/Max slice

MongoDB now admits Min/Max through operation-specific projection read capabilities. The existing scalar cursor owner handles count and selected values, retaining the same linked cancellation/deadline, native MaxTime, no-spill policy, snapshot session and membership precheck. Projection and extrema share selected-cell compilation, exact decoding and ordered source windows. Nullable selections pass their underlying type to the shared decoder.

Native `$min`/`$max` reduce a normalized comparison key together with its original typed payload. A separate reduction validates every participating value, including values that would not win. Null/absent nullable or string values are ignored; required malformed values error. This avoids a second sort of selected inputs. Source windows apply before selected-domain validation, while mandatory traversal failures remain outside the window/reduction. Canonical/metadata values retain exact integers, Unicode scalar ordering, UUID normalization and individual UTC ticks; no entity is hydrated.

Verification: the initial five controls failed specifically at capability admission while all 91 existing native tests passed. The first selected run passed 113/115 and exposed the nullable decoder call; that call was corrected. Expanded coverage passed 123/124; the remaining test accidentally composed Skip after an existing Take, producing Take(0). Its fixture now applies Skip before Take. The final fresh run passes **124 native CQL tests and 237 provider unit tests, zero skips** (native 3m11s). All **33 new Min/Max cases** pass: five primary controls, 19 shared reference vectors and nine ownership/domain controls. These include 137 inputs under graph-output cap 1, traversal errors outside paging, acknowledged reparenting under one snapshot, cancellation during the actual aggregate followed by reuse, standard/legacy/mixed UUIDs, duplicate metadata and ambiguous precision companions.

Evidence: `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`, `TestResults/MongoCurrent/mongo-minmax-unit-current.trx`, and `TestResults/CosmicCqlDemo/mongo-minmax-artifacts.json`. Producer/consumer hashes agree across Core, shared storage, MongoDB, native and unit outputs. This verifies the MongoDB selected-extrema slice; Neo4j, selected-scalar HTTP/demos, Sum/Average/GroupBy and later roadmap gates remain open.


## Neo4j native Min/Max slice

Neo4j now admits selected Min/Max for its existing canonical/metadata read domains. TranslateProjectionPath shares projection presence/type/value guards; the scalar session/cursor owner shares exact decoding, transaction lifetime, fetch size 1, cancellation/deadline checks and payload accounting. Native min/max and invalid/present counters reduce every participating input without entity hydration. Required malformed values error even when another value would win. Ordering/windowing precede selected-domain checks; mandatory ownership and traversal errors remain outside that window/reduction. Empty required versus nullable/string behavior follows Core's common contract.

The five primary controls first failed specifically at capability admission while all 83 existing native CQL tests passed. First execution then passed 106 tests: 83 prior, five primary controls and 18 relevant reference vectors. The expanded fresh matrix passes **111 CQL tests and all 328 provider unit tests, zero skips** (native 2m40s, unit 1s). All **28 new cases** pass, including exact negative Int64, Boolean/byte/GUID/Unicode domains, empty/null/wrong non-winning values, source windows, 137 inputs under graph-output cap 1, traversal failure outside paging, actual-fetch cancellation/reuse, delayed actual-delivery timeout/reuse and rejection of a borrowed UTC-date read permission before any fetch.

The DateTime reference vector is not a native Neo4j success claim: UTC DateTime persistence/query enrollment is still an explicit read-profile gap. Neo4j remains read committed and rejects a borrowed snapshot guarantee. Prompt interruption of blocked driver I/O remains open; these tests verify observed delivery/deadline checks. Native string character counts only provide a wire lower bound; exact strict UTF8 accounting occurs in the cursor budget. Those limitations are not removed by selected extrema.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-native-current.trx, neo-minmax-unit-current.trx, archived neo-minmax-red-current.trx/neo-minmax-green-current.trx, and TestResults/CosmicCqlDemo/neo-minmax-artifacts.json. Producer/consumer Core, shared-storage and Neo4j hashes match. No new environment or runtime was installed. The existing fixture owner cleaned acknowledged data and the runner stopped the retained container. Selected-scalar HTTP/demos, Neo4j dates/strict I/O, Sum/Average/GroupBy and the later roadmap remain open.


## Selected HTTP and demo gate verified — 23 October 2026

The retained fixture was reconciled by the exact AC9E6B... journal and76BF29... native evidence:180settled operations/175matching receipts,411acknowledged scalar writes and all146owned holons in each of3stores verified before cleanup. Original journal and438snapshot hashes are retained. No queries were replayed. The stale Swagger assertion now checks Min and its bounded Int64 metadata selection.

Fresh combined verification **passes175native regression tests, zero skips (3m7s)**, plus **36SDK queries and36HTTP examples** (12perprovider) on MongoDB, SQLite and Neo4j. The demo reports **216settled operations and211physical provider attempts**. Signed selected queries verify exact negative Int64 values beyond2^53, visibility before reduction (hidden rows contain stronger extrema), source windows, nullable empty inputs and no replay. Full Core/HTTP unit evidence remains2,117passing/0skip, including11new endpoint controls. Browser JavaScript syntaxcheck passes.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx; TestResults/CoreCurrent/cql-selected-scalar-http-current.trx; TestResults/CosmicCqlDemo/v1-demo-result.json, v1-walkthrough.txt, selected-http-native-final.log and selected-http-native-artifacts.json. Five producer/consumer DLL pairs agree (Core109677, shared151E8DC5); the existing runner verified fixture ownership, stopped owned services and removed the exact SQLite/wal/shm/marker after process exit. All four paths and27029/17687listeners were explicitly absent. No runtime/download/new environment was introduced.

This supersedes the pending native/demo checkpoint above. The selected-extrema HTTP/demo gate is complete; the full CQL/ORM roadmap remains open, including Neo4j UTC-date enrollment and strict blocked-I/O interruption, Sum/Average/GroupBy and later federation/query/ORM acceptance.


## Integer Sum/Average Core checkpoint — 23 October 2026

Immutable typed intents, operation-specific exact read admission and constant-size native accumulator normalization now pass both Core targets and all **2,141 tests, zero skips**, including24new cases. Integer Sum checks its final mathematical domain; Average returns Decimal without a binary64 intermediate. Empty/null, precision/rounding and explicit work-limit semantics are specified. **No native provider advertises Sum/Average yet**; native execution/HTTP/demos and the rest of the roadmap remain open. [Contract, examples and evidence](CQL_NUMERIC_AGGREGATES.md).


## SQLite native Sum/Average verified — 23 October 2026

SQLite now advertises Sum/Average for Core's exact Int32/Int64 and nullable domains. It reuses the projection operand guard and scalar read owner. One native ordered/windowed CTE reduces signed high/low integer limbs, invalid domains and participating count; Core normalizes only that constant-size packet. Every participating value is validated, so a numeric string cannot coerce into a successful total. Sum checks the final mathematical range; Average remains exact through its integer numerator and Decimal division, including cases where an Int64 sum would overflow.

A separate native input-work sentinel allows at most 2^31 selected rows to enter the limb reduction (the permitted bound plus one detection row). Both limb sums still fit Int64 at that sentinel. Exceeding the bound returns CQL_QUERY_LIMIT, never a truncated success. Missing/null optional rows count toward native work even though they do not participate in the arithmetic. The root-row/graph publication caps do not limit numeric input. Existing protected traversal validation, snapshot/deadline/cancellation and caller connection/transaction ownership remain unchanged.

Evidence: genuine capability-red18/18 failures, then18/18 native green, then expanded1,344/1,345. The single failure was a test using137 membership IDs beyond CQL's existing membership bound; the corrected prefix predicate now reaches the intended aggregate. The final fresh **full SQLite suite passes 1,345 tests, zero skips (2m20s)**, including all **27 new numeric cases** (five primary, 13 shared reference vectors, nine boundary controls). Coverage includes intermediate cancellation, final Int32/Int64 overflow, averages beyond2^53/over an overflowing sum, empty/null/wrong domains, windows, visibility before overflow,137 inputs under graph-output cap1, canonical type corruption, actual-scope cancellation/reuse, acknowledged concurrent writer/snapshot, borrowed transaction retention and traversal errors outside paging.

Authoritative reports: TestResults/SQLiteCurrent/cql-numeric-red-current.trx, cql-numeric-green-current.trx, cql-numeric-expanded-current.trx and cql-numeric-full-current.trx. Compact logs and matching producer/consumer hashes are in TestResults/CosmicCqlDemo/sqlite-numeric-*.log and sqlite-numeric-artifacts.json. The fixed DB/wal/shm paths are absent after verified teardown. No runtime, environment or download was added.

MongoDB and Neo4j numeric execution, numeric HTTP/Decimal wire encoding and demonstrations remain required. GroupBy, broader scalar domains and the remaining CQL/ORM roadmap remain open. SQLite's native gate does not imply all-provider numeric support or overall completion.
