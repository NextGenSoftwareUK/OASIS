# COSMIC ORM implementation status

## 23 October — Neo4j native Select acceptance

Native SDK selection now reuses the generic V2 projection executor and Neo4j's existing source, operand and read-budget owners. Initial fullunit328/native81 pass unskipped, after5new cases proved the missing interface. Final fullunit328/joint native-WEB4 147 pass unskipped on the updated production binary, including18new native projection controls and actual cursor-buffer inspections. Authenticated projection transport/demos, non-count aggregates/grouping and the remaining CQL/ORM gates are still open. [Contract/evidence](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#neo4j-native-projection-slice--23-october-2026).

## 23 October — native MongoDB Select

MongoDB now executes native scalar/anonymous/flat DTO projections through the generic Core/V2 manager. Native pipeline retains protected source filters/order/window, error branches outside paging, snapshot membership ownership and exact BSON decoding. Duplicate dictionary paths/UTC precision companions reject, UUID layouts and exact Int64/ticks retain their domains. Unit **237** / native **91** pass unskipped (8newunit/15newnative controls). Producer/unit/native-consumer DLL04CD09A3 matches. Core720/Shared55 source unchanged; owned Mongo graph fixtures clean and runner runtime stopped normally. Next Neo4j native Select, then authenticated projection transport/demos. [SDK example availability](CQL_MANUAL.md#mongodb-select-availability), [contract](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#mongodb-native-projection-slice--23-october-2026).

## 23 October — native SQLite Select

SQLite now executes scalar/anonymous/flat DTO projection through generic V2 HolonManager. Native columns reuse source predicates, visibility/order/window and bounded relationship semantics, with exact type/presence decoding and no entity hydration. Scope validation and projection share a snapshot; read-only/caller ownership, cancellation/reuse,137rows beyond graph output caps, ambiguous paths and corrupt canonical domains are verified. FullSQLite **1,282** passes unskipped; final **16**focused projection cases include3additional test-only canonical controls on the unchanged provider binary (no full1,285claim). PriorCore2,011 remains recorded; no Core/Shared source changed in this slice. Next MongoDB/Neo4j native Select and authenticated projection transport/demos. [Working SQLite SDK example](CQL_MANUAL.md#native-select--sqlite-sdk), [contract](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#sqlite-native-projection-slice--23-october-2026).

## 23 October — generic projection execution pipeline

Core `27f75b7` adds separate exact field/type projection permissions, actual-adapter readmission, detached scalar-cell results, global row/byte limits and generic V2 `HolonManager.ExecuteProjectionQueryAsync`. All adapters preflight before dispatch, and all raw buffers validate before any DTO construction. It shares the existing durable attempt/receipt owner; no entity hydration or client predicate evaluator. FullCore **2,011** passes (22newcontrols); both frameworks build. Dedicated native row/scalar regression passes27SDK/27HTTP,182settled/181attempts. No native provider advertises Select yet. Next: native SQLite projection/typed decoding inside its existing read owner, then MongoDB and Neo4j, followed by authenticated projection transport/demos. [Current contract](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#core-projection-execution-owner--23-october-2026).

## 23 October — immutable projection intent

Core `aa35a43` adds bounded scalar/anonymous/flat DTO Select intent through the existing path compiler, preserving the original source/access/scope/window/graph-consistency object. It invokes no selected getter, constructor or setter and rejects entities, arbitrary methods, computed values, collection shapes,33columns and discarded numeric conversions. FullCore **1,989** (7newprojectioncontrols) passes; netstandard2.1/net10.0 both build. The dedicated three-provider row/scalar demo still passes27SDK/27HTTP,182settled/181attempts after the compiler refactor. No provider advertises native projection execution yet: capability planning/readmission, native Select and detached-value construction are next, not claimed complete. [Projection boundary](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#projection-implementation-boundary--23-october-2026).

## 23 October — signed scalar HTTP transport

`POST /api/data/query-cql/scalar` implements Count/LongCount/Any through the same generic Core/HolonManager scalar owner on MongoDB, SQLite and Neo4j. A shared source compiler preserves nullable/unbounded scalar Take while keeping row default25. Both routes share authenticated admission and provider eligibility; startup tariff validation now includes the scalar route. Exact LongCount is always a tagged Int64 decimal string.

Core **1,982** and joint native **129** pass, zero skips. Native signed counts return137 on every store, with visibility/window/relationship checks and155settled/154physical attempts. The earlier prepared-route test-host gap was reconciled from its exact retained manifest with affirmative zero-execution evidence, then fixed at one shared route predicate. Core reservation guard committed/pushed **af6aed5**. The committed-producer refresh and expanded annotated demo pass:27SDK/27HTTP,182settled/181physical attempts. Each store rejects a repeated scalar operation with409 and no additional attempt/receipt. Final Core1982 and dedicated demo1 pass with zero skips; the earlier broader native129 gate remains recorded separately. Broader roadmap gates remain open. [HTTP scalar contract](CQL_SCALAR_HTTP_CONTRACT.md), [manual](CQL_MANUAL.md), [demos](CQL_DEMO_RUNBOOK.md).

## 23 October — Neo4j native Count/LongCount/Any

All three priority stores now execute native scalar count/existence through generic Core/HolonManager. Neo4j unit **322** / joint native-WEB4 **129** pass unskipped, including15new scalar controls for windows, privacy, large inputs, validation, cancellation/expiry and explicit snapshot rejection. Common native source extraction preserves existing row behavior. Final all3consumer source checks match Core716/Shared55; owned fixtures/processes are cleared. Strict blocked-driver I/O interruption and later roadmap phases remain open. [Scalar manual](CQL_MANUAL.md#scalar-terminals--mongodb-sqlite-and-neo4j-native-execution), [pending HTTP contract](CQL_SCALAR_HTTP_CONTRACT.md).

## 23 October — MongoDB native Count/LongCount/Any

MongoDB now shares the generic scalar Core/HolonManager API with SQLite. Native reduction retains access, canonical membership snapshots, traversal errors and exact windows without entity hydration or row/graph-publication truncation. Unit **229** / native **76** pass with no skips (9new unit/12new native controls), including a real snapshot/count race with acknowledged reparenting. Consumer Core716/Shared55 source identities match; owned runtime stopped after fixture teardown. [Manual](CQL_MANUAL.md#scalar-terminals--mongodb-sqlite-and-neo4j-native-execution), [native contract](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#mongodb-native-scalar-gate--23-october). Neo4j scalar source and later aggregate/transport phases remain pending.

## 23 October — SQLite native Count/LongCount/Any

SQLite now executes native scalar terminals through generic Core/HolonManager, with exact window cardinality and existing access/snapshot/traversal/deadline ownership. Count does not hydrate entities or inherit row/graph-publication limits. Full SQLite **1,269** passes unskipped, including12new scalar controls and actual-reader cancellation/reuse. Consumer identities match Core716/Shared55; fixed owned DB is removed. MongoDB/Neo4j scalar execution and WEB4 scalar transport remain open. [Manual example](CQL_MANUAL.md#scalar-terminals--mongodb-sqlite-and-neo4j-native-execution), [acceptance contract](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#sqlite-native-scalar-gate--23-october).

## 23 October — Scalar plans and common query attempt ownership

Core d71eebd adds immutable typed Count/LongCount/Any intents, independent scalar capability admission/readmission, checked native count conversion, detached per-provider results and the generic V2 HolonManager scalar entry point. Existing row execution and new scalar execution share one inherited admission/durable-attempt owner. Native aggregate adapters and WEB4 scalar transport remain pending; current row-only providers reject scalar calls before I/O.

The committed cohort passes Core **1,956**, MongoDB **64**, joint Neo4j/WEB4 **114**, and SQLite native **205**, all unskipped. Twenty-two new Core cases include exact overflow, source/access/graph/window retention, all-target preflight, no partial publication and durable scalar attempt/receipt sequencing. Native Unicode ordering boundaries now pass on all three stores. All three consumers match Core716/Shared55 source documents and producer hashes; owned fixtures are absent and test ports stopped. [Aggregate contract and remaining work](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md).

## 23 October — Concurrent reads and pre-admission HTTP errors

SQLite snapshot and Mongo snapshot controls retain the original path during an acknowledged concurrent reparent, then fresh reads see current membership. Neo4j read committed controls permit an already-fetched old row and require fresh-path correctness. Post-read cancellation/expiry never publishes partial results and permits a fresh read. Strict interruption of an outstanding Neo4j driver await remains open.

The Core usage middleware now classifies native Kestrel pre-admission body-limit exceptions as `REQUEST_TOO_LARGE` (413) and writes complete JSON with explicit UTF-8 length. Known oversized HTTP/1 controls negotiate `Expect: 100-continue`; no request retry was added. Exact reconciliation proved 133 settled operations and two absent trailing operation IDs before owned cleanup.

Fresh verification: Core **1,834**, joint Neo4j/WEB4 **114**, MongoDB **64**, SQLite native **204** plus CQL **18**, and the focused real-Kestrel **4** cases pass with zero skips. All three consumers match Core 712/Shared 55 source documents and producer hashes. Fixtures are removed and owned test ports stopped. Full CQL completion remains governed by the unchecked [roadmap](CQL_COMPLETION_ROADMAP.md).

## 23 October — Actual depth boundary and moved subtrees

A shared test-only33-edge chain now proves actual depth31/32 selection, exclusion of node33 and31-node overflow on all3 native stores. Existing acknowledged reparent fixtures also prove that a moved grandchild leaves its old path and requires new-owner membership. FullSQLite1249/focused46, Mongo62 and jointNeo110 pass0skip; all3source/PDB consumers match Core712/Shared55. No production Core/Shared/provider change was needed. Broader cancellation/concurrent-reparent and aggregate phases remain open. [Native contract](CQL_DESCENDANT_CONTRACT.md#exact-boundary-and-moved-subtree-certification).

## 23 October — Bounded descendant WEB4 transport

Optional anyDescendants compiles through generic immutable CQL with server actor binding, depth1–32/nodes1–10000 and four combined relationship groups/twelve total filters. FullCore1830/joint109 pass, including owner/outsider signed native privacy/denial-budget controls on all3stores. Core/Shared production unchanged. Sixth spaced/commented SDK/HTTP examples pass fresh actual18/18 counts (152 settled/151 attempts) in terminal78364. [HTTP contract](CQL_V1_HTTP_CONTRACT.md#bounded-descendant-existence-over-http).

## 23 October — Mongo native bounded descendants

Mongo now compiles depth-32 flat native breadth pipelines under the membership snapshot, with per-hop canonical membership/access pruning, visited-node/cycle limits and protected errors outside root paging. Unit 220/native 61/joint 109 passed unskipped, including the native memory146 classification fix and both-order negative-condition/overflow controls. Final40754 confirms compiler preallocation bounds, unit220/native61/joint109 unskipped and source/PDB712Core/54Shared. Owned services stopped and fixture files absent; full CQL remains unfinished. No Core/Shared source changes. [Contract](CQL_DESCENDANT_CONTRACT.md#mongodb-native-execution).

## 23 October — Neo4j native bounded descendants

Neo now executes WhereAnyDescendant with inline membership/access pruning in quantified paths, distinct visited-state budgets and typed cycle/limit errors outside paging. Unit316/scoped45/joint109 pass unskipped on5.26.12; Neo5.9+ required. Separate projection MATCH scopes fix self-loop validation; read committed remains explicit. Source/PDB712Core/54Shared verified and owned fixtures cleaned. Mongo descendants/remaining acceptance gates stay open. [Contract](CQL_DESCENDANT_CONTRACT.md).

## 23 October — SQLite native bounded descendants

SQLite now executes WhereAnyDescendant via a bounded recursive CTE (depth32/nodes10000), with native membership/access before each hop, scope budgets/cycles, typed errors and leaf output before root paging. Full1248/focused45 native checks pass unskipped. Empty-parent malformed-manifest red2 found a common direct/descendant omission; validation now precedes existence. Mongo/Neo descendants and further descendant controls remain open. [Contract](CQL_DESCENDANT_CONTRACT.md).

## 23 October — Bounded descendant contract

Core23da2dc adds immutable WhereAnyDescendant, depth/node bounds and certified traversal limits. Core1815/Shared968/joint98 pass unskipped; net10/netstandard2.1 build, source/PDB712Core/54Shared verified. All adapters still reject native descendant execution pending implementation. [Contract](CQL_DESCENDANT_CONTRACT.md).

## 23 October — Bounded HTTP child predicates and demo examples

HTTP anyChildren groups now compile to generic WhereAnyChild with server actor policy before paging; twelve total root/child filters, at most four independent nonempty groups. Full Core1799/joint96 pass unskipped. Fresh console demo verifies15SDK and15HTTP examples across all three providers,131settled operations; browser/Swagger presets updated. Dedicated runner reuses dependencies and requires a fresh report/counts. Descendants and other unchecked roadmap gates remain open. [HTTP contract](CQL_V1_HTTP_CONTRACT.md).

## 23 October — Native direct-child predicates on all three providers

Mongo now declares ChildExists: canonical final-write membership, whole-record CAS upgrade, same-snapshot validation/lookup, actor visibility and independent matches before paging. Unit216/native37/joint96 pass, zero skips; Core711/Shared54 source identity verified. SQLite/Neo native direct-child owners remain implemented. Descendants, extended relationship controls, HTTP relation descriptions, aggregates/joins/federation and full roadmap completion remain open. [Contract and evidence](CQL_RELATIONSHIPS.md).

## 23 October — Mongo canonical relationship write state

Central final-BSON writer now derives schema1 canonical membership state for ordinary/conditional/replica record replacements. Unit213/native29 pass unskipped, including stale-projection replacement and unchanged children during root-only conditional saves. Hash-bound preparation-fixture reconciliation1 passed; Core711/Shared54 source identity verified. Mongo ChildExists stays closed pending migration, snapshot validation and native lookup. [Contract](CQL_RELATIONSHIPS.md).

## 23 October — Neo4j native child existence verified

Neo4j now implements WhereAnyChild with native HAS_CHILD correlation, schema3 canonical role membership, current parent/revision and child visibility checks before root paging. Existing activation migration upgrades projections without changing canonical snapshots. Unit314/joint96 pass unskipped, completed-fixture reconciliation1 passed; source/PDB711Core/54Shared match. Owned fixtures removed and reusable services stopped. Mongo native direct-child support, descendant predicates and the full roadmap remain open. See [relationship contract](CQL_RELATIONSHIPS.md).

## 23 October — Native child existence, SQLite first

Core6c68968 adds immutable WhereAnyChild/ChildExists capability admission. SQLite implements correlated native membership/current-parent/child-visibility matching before root paging, with shared parameter/native work budgets. Native tests prove orphan exclusion, reparenting, nested exact metadata and independent child aliases. Published Core1,785/Shared968/SQLite29/joint91 pass unskipped;711Core/54Shared source/PDB checks match. Mongo/Neo direct-child existence remains capability-rejected pending implementation; descendant predicates and the full roadmap remain open. [Contract](CQL_RELATIONSHIPS.md).


## 23 October — Native graph-loading matrix verified

SQLite25/Neo joint91 pass with the final native cycle/cancellation controls, zero skips; unchanged Mongo27 includes its cycle/in-flight cancellation proofs. SQLite also reuses the same repository after cancellation. Alongside prior shape/policy/custom-role/conditional-save gates, the graph-loading verification matrix is checked for all three providers. Core711/Shared54 source identity matches and owned fixture resources are absent. Relationship predicates, aggregates, joins/federation and the full roadmap remain open. [Evidence](CQL_GRAPH_LOADING.md).


## 23 October — Native named typed child roles

One source-linked fixture now verifies registered Extras role materialization, typed metadata, accepted baseline/local change tracking, and hidden-role omission/full store observation across all three providers. Mongo27/Neo joint90/SQLite23 pass, zero skips; Core711/Shared54 source/PDB identity matches. No production Core/Shared change or new environment. The full graph matrix remains open for its remaining explicit native corruption/cancellation controls. [Evidence](CQL_GRAPH_LOADING.md).


## 23 October — Authorized partial saves verified on all three stores

Mongo25, SQLite21 and joint88 pass, zero skips. Both type-selected and access-selected views, with/without stored manifests, survive AcceptChanges without gaining replacement rights. A root-only conditional save advances the root once and leaves all child versions/provider revisions unchanged. The ownership roadmap gate is checked. A flaky HTTP fixture now explicitly declares its promised child ordering; the retained failed fixture was hash/identity/settlement-verified before cleanup. [Evidence](CQL_GRAPH_LOADING.md).


## 23 October — Public access-bound CQL graphs

Core0ca01b9 adds explicit GraphReadAccess capability admission and opens immutable graph/policy composition. WEB4 validates optional graph shape before paging and returns safe bounded DTOs. All three signed native HTTP providers pass child ownership/public visibility, private-branch privacy, metadata non-grant and settled receipt checks. Full Core1,781/Shared960/Mongo23/SQLite19/joint86 pass, zero skips;711Core/54Shared source/PDB checks match. C#/HTTP/browser/Swagger graph demos updated; dedicated executable C#/HTTP demo99646 passes1/1 across all three providers. Remaining relationships/aggregates/federation and full roadmap remain open. [Evidence](CQL_GRAPH_LOADING.md).


## 23 October — Safe graph DTO projection

An explicit iterative WEB4 row projector now serves leaf responses and defines bounded public graph output without stored manifests/provider state/unselected ancestor IDs. Eight new controls pass in full1,770 linked Core/WEB4 units. Native/hosted86/86 passes, zero skips;48957 completed terminal0; no Core/Shared source change. Graph request composition/capability admission remains gated. [Contract](CQL_GRAPH_LOADING.md).


## 23 October — Detached/typed graph access and mapper visibility

Core8f057e0 validates every detached node before typed construction, permits policy-selected membership, applies partial typed-role ownership for All-type views, and rechecks policy after callbacks. It also fixes the common base-state mapper dropping IsPublic in generic/non-generic projections. Full Core1,762, Shared960, Mongo23, Neo joint86 and SQLite19 pass, zero skips. SQLite policy cases include native-to-detached-to-typed publication; source/PDB checks match711Core/54Shared. SDK/HTTP graph-policy gates remain unchanged pending safe public transport. [Details](CQL_GRAPH_LOADING.md).


## 23 October — Native child read-access policy

Mongo, SQLite and Neo now carry the immutable owner/public rule into native child predicates before limits, using the existing translator and transaction/deadline owners. Twelve new adapter boundary cases cover selected/all-hidden children with and without stored membership and tight node budgets. Mongo23, SQLite19 and Neo joint86 pass, zero skips; source/PDB checks match711Core/54Shared. No Core/Shared source changes in this increment.

SDK/HTTP access-bound graph gates remain closed: detached graph validation must enforce policy throughout the graph, typed publication must retain partial ownership, and public transport must omit hidden store membership. Native tests use test-only private query construction to isolate this layer. [Evidence](CQL_GRAPH_LOADING.md).


## 23 October — Shared access-bound assembly and monotonic save restriction

The shared native expansion contract now propagates mandatory read policy to each selection, rejects unauthorized returned nodes and retains hidden store membership while budgeting selected nodes. Core2070977 supplies the monotonic incomplete-child marker, which cannot inherit or regain complete replacement permission through AcceptChanges. Full Shared958 and Core1,756 pass with zero skips. Native reader wiring, Core graph admission and safe public transport remain open; no existing graph gate is relaxed. See [graph evidence](CQL_GRAPH_LOADING.md).


## 23 October — Partial typed child-role ownership

Core a242d0e publishes one common full/selected hydration owner. Type-filtered manager loads materialize registered child types; selected IDs resolve in persisted order while omitted IDs are untouched. Full stored membership must remain on the owner and unchanged by callbacks. A partial view supersedes previous complete observation, so AcceptChanges cannot authorize child replacement; root-only saves remain available. Full Core 1,755/1,755 passes, zero skips. Published certification completed terminal0: full Core1,755/1,755 and joint native/hosted82/82, zero skips;711Core/54Shared source documents and producer/unit/native identity match. Core SHA25630184FD575D51431261ABAD651A50CE8A275F0224915EDC8A3437C8E094235E2; Shared CBF1CBF39985A02A251C948D642CE3B57B435A516EA958D1B3740A8F8C394BF3.

Access-bound graph queries remain gated pending native child access, partial membership validation and safe graph DTO transport. This checkpoint does not complete the full CQL roadmap.


## 23 October — Immutable mandatory read-access identity

Published Core638b70a gives the WEB4 owner/public rule an immutable actor/predicate binding. Native predicates precede business paging; scope/order/fluent composition preserves the rule without duplication. Actor replacement and admitted-actor mismatch reject, metadata labels cannot grant access, and foreign rows reject before typed constructors. WEB4 v1 uses that same policy owner from its JWT/admission identity. SDK policy creation itself is not authentication or a grant of credentials.

Full Core1742/1742 and joint native/hosted82/82 pass, zero skips;711Core/54Shared source checks and producer/unit/native hashes match. Existing unbound trusted SDK graph queries retain their documented behavior. Visibility-bound graphs explicitly reject CQL_GRAPH_ACCESS_UNSUPPORTED until native child filtering, partial-role hydration, membership handling and safe transport are implemented. This is the policy foundation, not completion of child authorization, shared-recipient or tenant authority. [Detailed boundary and evidence](CQL_GRAPH_LOADING.md).


## 23 October — Explicit graph consistency requests and admission

Core d548e55 adds immutable ProviderDefault/ProviderSnapshot requests, preserving the original factory binary signature. Snapshot capability is declared by SQLite/Mongo and rechecked against the actual adapter; Neo rejects stronger requests before I/O. Native race tests now request the guarantee explicitly. No weaker fallback or silent provider substitution occurs; per-provider snapshots do not create a distributed common snapshot.

Verified full Core1,730, Mongo19, SQLite15, Neo32, hosted82 cases; all pass, zero skips. Final consumers match the published Core hash and710 Core/54 Shared source documents. Informational version now matches the published commit after a bounded nonincremental refresh. [Contract/evidence](CQL_GRAPH_LOADING.md). Next: authoritative child access and relationship predicates, then remaining query shapes. The full goal remains active.


## 23 October — Neo4j native CQL graph reader and type projection

Added bounded native HAS_CHILD expansion after root paging, persisted membership ordering/batching, graph budgets, partial-save ownership and native snapshot/revision consistency checks. Neo4j uses one read-committed transaction; it does not offer the SQLite/Mongo cross-command snapshot guarantee. Cooperative driver cancellation is checked around awaits and before publication. Canonical HolonType projection/translation/capability is now implemented under schema2, using the existing activation migration owner.

Native81/81, unit31/31 and scoped18/18 pass, zero skips;17 graph cases include real interleaved child updates and schema1→2 preservation of canonical records. Joint hosted82/82 tests passed; the runner's old65 count is corrected to82, with its final gate now passing terminal0 and successful teardown. [Exact guarantees/evidence](CQL_GRAPH_LOADING.md). All three SDK readers now support graph shape; HTTP graph policy/transport, relationships, projections/aggregates, joins and federation remain open. Keep the full goal active.


## 23 October — MongoDB native CQL graphs certified

MongoDB now expands root pages and child membership inside one snapshot session with one cumulative30-second deadline. Native parent/type/member predicates use the existing translator,128-ID batches and pre-decode BSON/node bounds; Shared owns ordered graph assembly and Core owns detached/typed publication and partial-save ownership. No Core/Shared source change in this increment. Native104/104 and admission31/31 pass, zero skips, including16 graph cases covering a concurrent child update, in-flight cancellation, cumulative deadline across two commands, depth/type limits, root paging,150-member batching and malformed links. Producer and both consumer copies match. [Exact contract/evidence](CQL_GRAPH_LOADING.md).

SQLite/Mongo SDK graph expansion is implemented. Neo4j graph loading, relationship predicates, aggregates/projections, joins, federation and new hosted graph policy/transport remain open. HTTP v1 remains leaf-only; no broad CQL completion claim.


## 23 October — SQLite native CQL graph expansion

Implemented bounded native SQLite child queries after root paging, reusing one deferred Serializable snapshot and native work budget. Added reusable Shared expansion, persisted membership ordering/batching, read-only and caller-transaction support. Fixed private incomplete-selection ownership for manifest-free raw/typed query results, including forged-manifest/AcceptChanges attempts. Root-only saves preserve omitted links.

Verified: Core 1,722, Shared 954, SQLite integration 1,218 tests; all passed, zero skipped. Includes 15 native graph cases and 8 shared assembler cases. Core38d8696 contains the ownership fix. MongoDB/Neo4j graphs, relationship predicates, aggregates/projections, joins, federation and new hosted transport/policy remain open. Existing bounded HTTP v1 remains available. [Detailed graph contract](CQL_GRAPH_LOADING.md), [remaining roadmap](CQL_COMPLETION_ROADMAP.md).


## CQL detached graphs and early typed admission — 23 October 2026

Core now preserves graph edges with iterative detached publication, total node/UTF-8 budgets and structural/manifest validation before returning any group. The typed manager applies graph selection to role hydration/tracking; V2 selected-forest metadata admission now precedes typed constructors/factories. Controlled tests prove partial-role save protection and depth-boundary manifest retention. New execution/ownership22/22, full Core1,720/1,720 and native leaf regression65/65 pass, zero skips; both Core targets build. [Exact red/green evidence and native reader owners](CQL_GRAPH_LOADING.md). Core945386c is task-pushed with parent dependency-pin update. Native graph capability remains disabled: native expansion/authorization/relationship acceptance is still required before advertising it.

## CQL child graph composition/admission — 23 October 2026

Core now carries immutable child depth/type/node/payload options through scope/filter/order/page composition and rechecks graph capability against actual adapters before I/O. Focused19/19, full Core1,698/1,698 and existing three-provider native v1 cohort65/65 pass, zero skips; both Core targets build and consumer copies match. [Source contract, evidence and next owners](CQL_GRAPH_LOADING.md). Native graph expansion/publication/ownership remain unfinished and no adapter advertises GraphExpansion yet. v1 stays leaf-only. The full goal is active; next work is bounded coherent native graph reads and detached typed publication, then relationship predicates.

## Three-provider CQL v1 developer demos — 22 October 2026

Annotated C# SDK and HTTP walkthroughs now run against MongoDB, SQLite and Neo4j, with a spacious guided browser page and working prefilled Swagger. Nine SDK queries, nine HTTP examples, 11 grouped API checks, the normal native 65/65 cohort and the existing coffee console/six API checks pass. Owned services stopped and fixture cleanup verified. [Run it](CQL_DEMO_RUNBOOK.md), [developer manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md), [verification evidence](CQL_V1_DEMO_VERIFICATION.md). Examples use real shipping query/authentication/admission code with controlled fixture authority; production contract scope and broader roadmap are unchanged.

## CQL v1 native acceptance complete — 22 October 2026

The defined Mongo/SQLite/Neo4j WEB4 v1 implementation and automated acceptance are complete. Shipping JWT validation/HTTP/admission code executes against real native providers and a Mongo usage outbox, with controlled avatar-directory/authority fixtures. Full Core 1,679/1,679; native cohort 65/65 (one HTTP matrix: 97 durable native attempts, 98 settled operations); existing real-Mongo accounting 312/312. All zero skips. WEB4 builds; source/runtime hashes and verified teardown retained. Prefix/suffix transport expressions now use the generic CQL engine's required ordinal semantics. [Exact acceptance, limitations and remaining scope](CQL_V1_MILESTONE_3.md), [C# HTTP example/operator guide](CQL_V1_OPERATIONS.md). No production deployment/price approval or larger-roadmap completion is claimed.

## CQL v1 authenticated WEB4 boundary — 22 October 2026

Bounded POST `/api/data/query-cql` now reuses generic Core planning and native adapters, with server-owned selection, authenticated reservation binding, native owner/public predicates before paging and explicit durable query-tariff receipts. Full Core 1,676/1,676 and configured real-Mongo accounting 312/312 pass, zero skips; WEB4 builds. [Milestone 2 evidence and remaining acceptance](CQL_V1_MILESTONE_2.md), [request/configuration examples](CQL_V1_HTTP_CONTRACT.md). Native HTTP/JWT/outbox acceptance remains open; production-ready v1 is not yet claimed.

## CQL v1 provider admission — 22 October 2026

Shared hosted query execution now honors inherited server-created storage admission and durable provider-attempt accounting. All-target identity binding, post-persistence lifetime/cancellation checks and explicit rejection prevent bypassing the admitted provider boundary. Full Core 1,651/1,651 passes (16 new cases); both targets build. [Milestone evidence and resume point](CQL_V1_MILESTONE_1.md). HTTP authorization/visibility, native v1 acceptance and remaining roadmap are open. Work stops at this milestone at the user's request to conserve tokens.

## Runnable CQL demos — 21 October 2026

A real C# walkthrough and standalone loopback Swagger sample now exercise native typed metadata/field queries, ordering/paging, clean tracking, no-op saves, committed delta summaries and independent stale-client conflicts through HolonManager/Hyperdrive V2. Console assertions and all six HTTP/Swagger checks pass with host/fixture teardown verified. Core/Shared/SQLite demo runtime copies match their producers. [Run commands and demo script](CQL_DEMO_RUNBOOK.md); [dependency inventory](CQL_DEMO_DEPENDENCIES.md). No Core/provider product implementation changed in this increment. Production authenticated ONODE routing/admission and the full roadmap remain open.

## Neo4j GUID metadata — 21 October 2026

Guid/Guid? metadata now execute natively with exact canonical text validation, GUID-only case normalization and shared null/invalid-domain semantics across predicates, membership, fields and ordering. Full Neo307/307 and expanded native64/64 pass, zero skips;20 declared producer/consumer copies match. Stable feature report and exact source/artifact hashes are retained; successful fixtures removed and container stopped. [Contract and evidence](CQL_SCALAR_METADATA.md#neo4j-guid-metadata--21-october-2026). No Core/Shared/pin/schema change or new environment. Date metadata, resource stress and the remaining original roadmap are still open.

## Neo4j enum metadata — 21 October 2026

Exact HolonType/ProviderType metadata and nullable domains now execute through the common CQL pipeline with one native defined-value guard for comparisons, membership, fields and ordering. Complete Neo controls pass 305/305 and the expanded native regression passes 64/64, zero skips; 20 declared producer/consumer copies match. A prior interrupted five-record fixture was independently reviewed and reconciled with locked exact snapshot hashes before the fresh native run. [Acceptance and limitations](CQL_ENUM_METADATA.md#verified-neo4j-enrollment--21-october-2026); [resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md). No Core/Shared source or pin change; broader types, native stress, relationships and federation remain open.

## Narrow integer domains and replication shutdown — 21 October 2026

Neo4j now admits byte/sbyte/short/ushort/uint metadata and nullable forms with exact range guards shared across predicates, membership, field comparisons and ordering. Full Neo controls pass 301/301. A separately reproduced pre-execution BackgroundService cancellation bug is repaired at the shutdown owner; its three controls distinguish expected scheduling cancellation from expired deadlines and post-entry faults. Full Shared passes 946/946. Rebuilt native CQL plus configured replication hosting passes 66/66, zero skips. See [integer domains](CQL_INTEGER_DOMAINS.md) and [shutdown evidence](REPLICATION_HOST_SHUTDOWN.md). Broader CQL domains, resource stress, federation and remaining roadmap acceptance are still open.

## Neo4j native ordering/paging — 21 October 2026

Native window execution preserves shared missing/null/valid/invalid buckets, per-key direction, normalized logical-ID ties and sequential Skip/Take algebra. A separate guard branch within the same native query prevents offsets from hiding invalid projections. Empty windows perform no native I/O. FullNeo300/300, Shared943/943 and expanded nativeNeo64/64 pass, zero skips;23 declared assembly copies match. [Semantics and acceptance](CQL_ORDERING_PAGING.md#verified-neo4j-window-gate). Existing Core pin is unchanged; broader scalar domains, native resource stress, global federation and snapshot continuation remain open.

## Neo4j fields and shared integer/Guid literals — 21 October 2026

Native field comparisons now preserve independent type guards and nullable semantics. Shared Corec4a89b193aff419d1e5cf0f9b979ff56f2f80f4e recognizes Guid.Empty safely and admits whole-domain lossless integral field promotions while rejecting narrowing/nullable erasure and requiring both exact provider domains. FullCore1,635, Neo298, Shared943, SQLite1,203 and selected nativeNeo64 all pass, zero skips;26 declared copies match. Two original native compiler failures were preserved, independently reconciled and cleaned with fresh locked snapshot hashes. [Exact evidence and remaining scope](CQL_NEO4J_NATIVE.md). Native ordering/paging, broader domains, routing and the remaining roadmap remain open.

## Neo4j native CQL foundation — 21 October 2026

The selected Neo4j provider now implements the common CQL execution contract with native parameterized predicates, typed metadata presence/null/integer/string/boolean domains and a bounded execution path. A transactional self-relationship projection and restartable activation upgrade preserve final snapshots and revisions; the native lock-observed concurrent writer test passes. Selected native64/64, fullNeo297/297 and pairedShared943/943 pass, zero skips;23 declared assembly copies match. [Scope, usage constraints and remaining gates](CQL_NEO4J_NATIVE.md). Broader scalar/field/order/page execution, native resource stress and complete Stage8 remain open.

## SQLite audit timestamp fidelity — 20 October 2026

Committed619c38ff5 preserves audit timestamp ticks and DateTime.Kind across Holon, Avatar and AvatarDetail reads, saves and atomic tombstone updates. Additive activation and formal migration preserve old Unspecified values; downgrade rejects retained kind information. Focused43/43 and completeSQLite1,203/1,203 pass, zero skips. [Evidence and remaining date policy](SQLITE_AUDIT_DATE_KINDS.md). Coref4d1623c7afdc4999e4704c71d7ff33312ff16b4 separately fixes the reproduced Local-minimum audit absence conversion. ExpandedCore1,629/1,629, Shared943/943 and SQLite1,203/1,203 pass, zero skips; both Core targets build and12 declared assembly copies match. Other provider native suites and audit-date CQL policy remain open.

## CQL defined enum metadata on SQLite — 20 October 2026

MongoDB now enrolls the same defined HolonType/ProviderType domains, with native Int32/Int64 admission and rejection of undefined integers and integral-looking Double/Decimal128 values. Full205 controls and expanded3 native scenarios pass, zero skips, across both dictionary layouts;12 producer/consumer copies match. Explicit forged-enum-profile rejection remains covered. [MongoDB native evidence](CQL_ENUM_METADATA.md#mongodb-native-acceptance).

SQLite now explicitly admits HolonType/ProviderType and nullable metadata paths, using the enum's defined integer values as the native domain. Comparisons, membership, fields and ordering share that guard; undefined integers and wrong JSON types do not become typed matches. Original two absent-capability failures close in focused7/7 and full1,176/1,176, zero skips. Eight producer/consumer copies match. [Usage, limits and evidence](CQL_ENUM_METADATA.md). Broader enum/schema registration and other provider enrollment remain open.

## CQL exact narrow integer domains — 20 October 2026

MongoDB enrollment now adds the same exact narrow integer/nullable domains, with one native BSON guard reused by comparison, membership, field comparison and ordering. Original five admission failures close in full202 controls and expanded3 native CQL scenarios, zero skips; both dictionary layouts and Int32/Int64 storage representations pass, with12 declared producer/consumer copies matching. [Native evidence and hashes](CQL_INTEGER_DOMAINS.md#verified-mongodb-enrollment). Other adapters and decimal/double/enum/UInt64 domains remain open.

The common compiler now admits whole-domain lossless integer promotions, fixing ordinary C# byte/short comparisons while rejecting narrowing and nullable erasure. SQLite adds exact byte/sbyte/short/ushort/uint and nullable metadata domains for comparisons, membership, field comparisons and ordering. Full pinned Core1,611/1,611 and SQLite1,174/1,174 pass, zero skips; six declared producer/consumer assemblies match. [Usage and exact verification evidence](CQL_INTEGER_DOMAINS.md). Other native adapters and broader numeric/enum domains remain open.

## SQLite karma history fidelity — 20 October 2026

Published root fix2b0eb51e2 and representation follow-onbbb176134 restore history loading, updates and acknowledgements, ordered duplicate entries, nullable enum values and exact DateTime.Kind/ticks. Both activation upgrades and formal migrations preserve legacy values; guarded downgrade refuses representation loss. Full SQLite1,164/1,164 and focused27/27 pass, zero skips. [Contract and original failure evidence](SQLITE_KARMA_HISTORY_PERSISTENCE.md). Broader cross-provider durable history/restart acceptance remains open.

## SQLite nullable avatar enums and CQL resource gate — 20 October 2026

Scoped avatar/schema commit351fa086d preserves null-versus-defined AvatarType and creation-provider/OASIS wrappers across repository/entity mappings and updates. Both owned-schema and EF migrations preserve prior values, and downgrade refuses loss of absence evidence. Required historical migration target metadata and the current snapshot are aligned. Final complete SQLite1,145/1,145 and focused migration7/7 pass, zero skips. [Original failures, root fix and exact working-tree cohort](SQLITE_AVATAR_OPTIONAL_ENUMS.md).

That historical complete run also covered the [CQL native resource owner](SQLITE_CQL_RESOURCE_BOUNDS.md): native work/deadline/cancellation/record-size interruption, streamed payload bounds and exact connection-state restoration. Ordering/resource publication and immutable combined Core acceptance subsequently completed inb7975134c, as recorded below. This is not a deployment or full-roadmap completion claim.

## Native CQL ordering, paging and resource bounds — 20 October 2026

Fluent Where/OrderBy/ThenBy/Skip/Take composition uses immutable common Core plans and exact ordering capabilities. SQLite/MongoDB execute typed ordering and windows natively with unique-ID ties, explicit missing/null/invalid domains and preserved integer/date/UUID precision. Native work/deadline/record/payload bounds, real MongoDB sort exhaustion and scoped timeout classification pass. Final clean Corefe492b2 cohort passes Core1,606 / Shared925 / Mongo197 controls+3 native / SQLite1,145, zero skips;17 declared consumer copies match, and the Core gitlink/manifest are paired. [Usage, original failure reconciliation, scope and exact hashes](CQL_ORDERING_PAGING.md). Global federation, subqueries, projections/aggregates, snapshots/continuations, authenticated discovery and wider adapters remain open. SQLite Akashic persistence has since been repaired with the evidence above.

## Native CQL field comparisons — 20 October 2026

Canonical and typed metadata operands now compare through the common immutable Core query representation and exact two-path admission. SQLite and MongoDB execute comparisons natively, preserving null/missing/type domains, exact integer/date precision and supported UUID representations. Regression tests exposed and fixed MongoDB hex-case normalization and SQLite JSON `id` shadowing of the outer holon identity. Final pinned Core1,558 / Shared925 / Mongo197 controls / native CQL3 / SQLite1,130 pass, zero skips; all17 declared producer/consumer copies match. Corec030926 is remotely verified and paired with the parent manifest. [Usage, scope, reviewed original failures and full acceptance evidence](CQL_FIELD_COMPARISONS.md). Ordering, paging, projection/aggregation, relationships, authorization-aware discovery/federation and other native adapters remain open.

## Shared save lifecycle across SQLite, MongoDB and Neo4j — 20 October 2026

One shared rule now validates committed deletion timestamps/actors and inactive state against protected authoritative observations. SQLite conditional and ordinary graphs preflight every selected record before history/data/receipt changes. Mongo atomic graphs validate actual native audit fields before preparation, while single-record tombstone visibility remains RecordNotFound. Neo4j delegates to the same implementation. Original SQLite16 accepted-invalid failures/8 controls close in final SQLite1,121, Mongo88, Neo native265/controlled276 and Shared925, zero skips; exact provider/consumer hashes match. [Contract, reviewed fixture evidence, dependency commit and final reports](SHARED_SAVE_LIFECYCLE.md). Explicit restore, broader identity/provider/fault/security and full roadmap acceptance remain open.

## Neo4j delete transaction ownership — 20 October 2026

Identity saves, paired avatar deletes and ordinary hard-holon deletes now share one private native transaction owner. Validated candidates publish only after acknowledged COMMIT/read-only rollback; definite rejected writes retain real errors, unknown outcomes have zero success flags/counts, and known commits survive cleanup as warnings. Avatar deletion reports one logical avatar; matching repeated soft deletion preserves its original evidence with zero changes/count/COMMIT. Original32 failures/6 missing controls close in full276 controls, focused34/full265 native and Shared917, zero skips. [Contract and exact evidence](NEO4J_DELETE_OWNERSHIP.md). No Core/Shared/pin change; broader lifecycle/restore/provider/default-CRUD and full roadmap acceptance remain open.

## Neo4j avatar/detail save ownership — 20 October 2026

Both identity save routes now use one native owner for locked observations, lifecycle admission, optional history, exact observed-snapshot/creation-claim replacement, acknowledgement, COMMIT/rollback and disposal. History-off saves enforce the same tombstone preconditions. Definite rejected writes retain their real errors after acknowledged rollback; known commits survive cleanup as saved warnings. Original lifecycle32 failures/16 controls close in full238 controls, focused20/full231 native and Shared917, zero skips. [Contract, original broad-run certification corrections and exact inspected fixture/artifact evidence](NEO4J_IDENTITY_SAVE.md). Delete ownership, related-kind lifecycle coordination, explicit authorized restore and the full roadmap remain open.

## Neo4j save lifecycle and acknowledgement parity — 20 October 2026

Neo4j now validates committed tombstones at locked observations before any conditional history stamp or ordinary graph write. Save cannot clear/retime/reattribute deletion or make the tombstone active; compatible tombstone edits and live saves remain allowed. The explicit lifecycle error is preserved. Conditional/replica acknowledgement assembly delegates to the existing Shared helper, fixing missing changed-state flags without duplicating graph/count logic. Original baseline12 failures/4 controls and two receipt-flag failures close in full Neo190, native conditional13, ordinary-outcome16 and Shared917, all zero skips. [Contract, native evidence and remaining lifecycle scope](NEO4J_SAVE_LIFECYCLE.md). Avatar/detail/all-provider parity and explicit authorized restore remain open.

## Neo4j atomic committed deltas — selected acceptance verified

Neo4j adopts the shared Core delta model and Shared format4 receipt in its existing native graph transaction. Locked previous snapshots and native acknowledged snapshots own capture; original replay retains its delta after physical deletion and history-policy changes. Strengthened conditional13/13, replica6/6, primary-outbox2/2 and paired Shared904/904 pass with zero skips. The original structural assertion failure was independently reconciled against native state before exact owned fixture cleanup. [Scope and evidence](NEO4J_COMMITTED_DELTAS.md). Authorized historical values, retention/operator workflow and wider enrollment remain open.

## MongoDB committed delta adoption — 20 October 2026

MongoDB conditional writes now capture deltas from authoritative BSON observations and final persisted BSON bodies inside the graph transaction. Acknowledgements are constructed from that final representation, including preserved unselected frontiers. Shared receipt format4 persists and validates the original delta; legacy format3 replay reports explicit missing-delta evidence. SQLite and MongoDB advertise the new conditional-delta capability. Core validation also binds logical predecessor identity/version and membership ownership; Shared rejects wrong operation/intent before metadata construction. [Implementation, acceptance evidence and remaining scope](MONGO_COMMITTED_DELTAS.md).

This increment does not finish Stage7: Neo4j and other adapters, ordinary CRUD/replica enrollment, authorized historical-value reads/decryption, retention and operator reconciliation remain open.

Checkpoint: 20 October 2026. The goal remains active on `fix/hyperdrive-v2-providers`. Selected foundations are implemented and verified; the full roadmap is unfinished. Source commits, assembly hashes, original failures and fixture reconciliation are retained in [integration evidence](INTEGRATION_BOUNDARY.md) and [verification history](VERIFICATION.md).

## Latest working increment — atomic committed deltas on SQLite

Core now owns immutable, provider-reusable committed-delta capture/validation. SQLite conditional writes persist the delta with records/history/receipt in one native transaction and return a redacted summary through HolonManager; replay returns the original summary after newer commits, restart and history-policy change. Historical values remain storage representations under the existing history policy; public summaries omit values/membership identities. Final committed Core1,546/1,546, Shared895/895 and SQLite1,089/1,089 pass with matching consumer artifacts and zero skips. Additional native codec/conditional regressions pass MongoDB84/84 and Neo4j13/13; these do not implement their delta adoption. [API example, retention/encryption semantics, original failures and remaining Stage7 scope](COMMITTED_DELTAS.md). The remotely verified Coreb3c74d4 is paired with the parent dependency pin; broader provider/authorization/retention rollout remains open.

## SQLite receipt integrity prerequisite for committed deltas

SQLite primary receipts now bind checksummed graph data to original operation/actor/authority/intent and validate selected roots, edges and revisions. Save replay verifies exact submitted persistent intent; each new primary receipt is read/validated before COMMIT. Seven original accepted-corruption regressions now reject; checksum-valid wrong saved content and historical-envelope reconciliation are also covered. Final focused20/20 and full native1,078/1,078 pass, zero skips. [Contract, compatibility gate, original failures and artifacts](SQLITE_RECEIPT_INTEGRITY.md). That prerequisite changed no Core/Shared source or deployment pins; the newer capture checkpoint above extends the boundary.

## SQLite exact UTC metadata date queries

SQLite now translates DateTime/DateTime? metadata comparisons and membership over its canonical UTC JSON text, preserving all seven fractional digits through fixed-width binary comparison. Native guards validate the Gregorian calendar and exclude malformed, non-UTC and overprecision values. The focused date/scalar gate passes69/69; final full native1,068/1,068 passes with zero skips after replacing one stale unsupported-date assertion with unsupported enum coverage. [Domain, original red evidence and final artifacts](CQL_SCALAR_METADATA.md#utc-dates). Core source and dependency pins are unchanged.

## MongoDB exact UTC metadata date queries

MongoDB DateTime/DateTime? metadata predicates now compare exact Int64 ticks using the existing persisted precision companion; records without it retain their actual millisecond precision. All six comparisons and membership preserve sparse/null/NOT semantics without parsing strings or numeric epochs as dates. Final controlled193/193 and expanded native3/3 pass, zero skips, including one-tick separation, pre-1970 values and DateTime bounds in both dictionary layouts. [Contract, reports and remaining date-query scope](CQL_MONGO_NATIVE.md#utc-metadata-date-increment). No Core source, dependency pin or deployment change is included.

## MongoDB GUID metadata queries

MongoDB now translates Guid/Guid? metadata equality, inequality and membership over canonical D strings, standard UUID binary and CSharpLegacy UUID binary. The existing sparse/null/NOT contract is preserved; malformed text and unrelated binary remain outside the domain. Final controlled191/191 and expanded native3/3 pass with matching consumer assemblies and zero skips. [Native contract, original red evidence and exact verification scope](CQL_MONGO_NATIVE.md#guid-metadata-increment). No Core source or dependency pin change is included.

## SQLite GUID metadata queries

SQLite now admits `Guid`/`Guid?` metadata equality, inequality and membership over canonical D strings, with case-insensitive GUID identity and strict shape/byte-length validation. Missing/wrong-type values, explicit nullable nulls and NOT preserve the existing scalar contract. String markers remain ordinal. The original native gate failed9/passed1; final focused52/52 and full native1,051/1,051 pass, zero skips. Core and dependency pins are unchanged. [Contract and verification](CQL_SCALAR_METADATA.md#sqlite-guid-increment).

## MongoDB CQL and shared scalar-property queries

Focused follow-on: native comparison vectors exposed MongoDB inequality excluding canonical nulls and explicit nullable metadata nulls. Its native guard now matches the established SQLite contract; **188/188 controlled and 3/3 expanded native cases** pass, zero skips, after independent pre-write fixture reconciliation. Core/pins are unchanged. [Exact targeted epoch and distinction from the broad baseline below](CQL_MONGO_NATIVE.md#verification).

MongoDB now executes the shared CQL predicate contract natively, including typed metadata paths, presence/null tests, logical predicates, membership and ordinal strings. Annotated scalar properties such as `row.Rating >= 7` compile centrally and execute on both MongoDB and SQLite without getter/attribute-constructor execution. Native results support clean tracking, no-op save and conditional write-back retaining unselected children. MongoDB streams bounded projected payloads and enforces a native deadline. Final acceptance: **Core 1,536; Shared 895; MongoDB unit 188/selected native 84; SQLite native 1,042**, all passing with zero skips and matching Core/Shared artifacts. [Implementation, exact reports/hashes, reconciled failures and open CQL scope](CQL_MONGO_NATIVE.md). Core a6d0865 is paired with the parent dependency pin; no Development deployment is included.

## Earlier protected preparation ownership increments

MongoDB numeric prerequisite discovered during CQL review is fixed: JsonElement prefers exact decimal storage, lossless JRaw retains numeric identity, and unsupported rounded/underflowed/overflowed values reject rather than persist a successful substitute. Final paired **175/175 controlled and 12/12 native cases** pass, zero skips, including prior conditional/preparation regressions. [Precision evidence and limits](MONGO_AUDIT_PRECISION.md#json-numeric-storage-follow-up--20-october-2026). This is a storage correction; the MongoDB CQL adapter remains unfinished.

Neo4j now implements the shared opaque preparation contract using its existing unique receipt node and transaction lock. The final paired full controlled suite passes **174/174** and subsequent native conditional suite **13/13**, zero skips; the native cases include actual encrypted V2 graph replay across fresh managers and key rotation. The selected cleanup, synchronized preparation races and single/batch sync/async preparation-commit acknowledgement-loss recovery cases pass; broader acceptance remains open. See the [Neo4j checkpoint](NEO4J_WRITE_PREPARATION.md).

MongoDB now has the native opaque preparation store and shared operation fencing, with final controlled tests **153/153**, native conditional cases **10/10**, and independent retained-fixture reconciliation **1/1**, zero skips. The native cases include encrypted sync/async graph replay across fresh managers and key rotation. Runtime-only encryption policies are excluded before BSON serializer resolution. See the [MongoDB checkpoint](MONGO_WRITE_PREPARATION.md) for evidence, deployment constraints and remaining concurrency/fault acceptance. Neo4j implementation evidence is recorded in the newer checkpoint above.

Shared V2 encrypted conditional-save recovery is now implemented. The original replay regression passes4/4, with original prepared ciphertext and complete provider fingerprints retained. Core authenticates/compares the entire selected graph before constructing recovered entities; a new red test exposed and fixed early root construction on late-child failure. Final Core **1,524/1,524** and complete native SQLite **1,041/1,041** pass, zero skips or exclusions. The final type-contract increment also binds CLR FullName and assembly simple name; two new red cases now pass. Twenty-three selected native cases cover graphs/batches, restart/write-key rotation, changed private/clear values/conditions/actor/foreign keys, tampering/denied old keys, cross-client races and original receipt replay after a later revision. Six additional native lifecycle/CBC cases, the prior-receipt/no-preparation gate and strengthened per-node-context assertions also pass in the complete native suite. [Current behavior and remaining acceptance](WRITE_PREPARATION.md#current-v2-save-flow).

SQLite has the selected end-to-end encrypted-save proof. The coordinated provider session also verified eight DynamoDB/AWS native encrypted manager/V2 cases on the same Core D490 epoch, with a separately recorded Shared artifact; provider-specific evidence remains in HyperDrive-Providers. Other providers must declare and implement that capability before encrypted conditional saves can execute. Actual process/network interruption, claim transaction fault injection, retention/reconciliation, paid-native preparation and complete authorization/history/replication/migration contexts remain open. No full-roadmap completion or overall percentage is claimed.

Previous storage-only increment: Core's immutable `IHolonWritePreparationStorageProvider` contract and the native SQLite atomic first-winner claim owner, activation/EF schema and downgrade protection passed Core1,511 and native1,011. Its excluded replay cases and two encrypted failures are historical evidence, superseded by the subsequent shared V2 recovery checkpoint above. Exact reports remain preserved.

### Earlier working checkpoints

The original encrypted logical-operation replay defect was reproduced natively: unencrypted sync/async controls pass, encrypted sync/async retries fail with `COSMIC_OPERATION_ID_REUSED` across fresh managers/providers. The provider fingerprint correctly detects new randomized ciphertext. The required fix is durable authorized preparation ownership, not relaxed fingerprinting. See [defect and acceptance cases](CONDITIONAL_WRITES.md#encrypted-preparation-replay-defect--20-october-2026).

The first prerequisite is implemented: shared domain-separated preparation protection, authenticating record/context/key and operation/actor/authority bindings while preserving the existing metadata envelope format. Full Core **1,508/1,508**, zero skips, passes; both target frameworks build. [Crypto evidence and remaining journal integration](ENCRYPTION_CONTEXT.md#protected-preparation-crypto-owner--20-october-2026). At that earlier checkpoint the durable preparation store and manager/V2 claim path were absent and the two encrypted retry tests remained red. The current recovery checkpoint above supersedes that state. Native compatibility verification is separate from fixing those regressions.

A consolidated [CQL getting-started guide](CQL_GETTING_STARTED.md) now documents the implemented shared API, native SQLite examples, provider scope, metadata/encryption semantics, tracked write-back and unfinished features. The original full roadmap remains active.

## Previous accepted snapshot — 20 October, 12:20 local

**Active priority: P0 persistence correctness and encryption context.** This is still a partial implementation of the full roadmap, not a completed product release.

The current locally implemented batch implements `oasis2:` authenticated metadata envelopes (AES-256-GCM), immutable runtime policy, owned/disposed key material, resolver requests scoped to logical record/context/key identity, per-holon overrides and save-plan context ownership. Reads use the stored key reference, including an older retained key after the current write key changes. Native SQLite cases verify fresh-manager graph/CQL reads, edited writes using a new key ID, and rejection of unauthorized, copied or tampered ciphertext without changing storage. Seven new native cases now also pass within the final complete SQLite **1,003/1,003** run on the same final Core/Shared cohort.

The same batch adds the explicit central `StoredRecordJson.DeserializeMetadataRecord<T>` API for storage DTOs, preserves exact untyped metadata and typed root decimals, and accepts nullable/absent metadata with the original constructor/default semantics. Duplicate/case-ambiguous metadata remains rejected. Latest complete Core result: **1,494/1,494 passed**, zero skips (`TestResults/CosmicMongoCurrent/metadata-null-core-full.trx`), Core SHA-256 `F6A4E026662FAC8B4C181FA141796FD1C0B77742F589AF031F4FA1AD9D80BC90`. Both target frameworks build. Shared **887/887** and Solana **559/559** now pass against this final API with no skips or exclusions. The full SQLite **1,003/1,003** run passed against the same Core/Shared cohort, zero skipped, 126 seconds, terminal exit 0 (`TestResults/SQLiteCurrent/metadata-envelope-final-native-full.trx`).

**Last broader committed verification:** Core 1,450 and full SQLite 996 at their recorded epochs, plus the subsequent 31-case native failure-flag cohort. The current envelope/DTO Core source is committed locally as `25a15a8`; parent integration pairs its gitlink and manifest with the verified tests/docs. Nothing has been pushed or deployed by this session.

Remaining encryption acceptance includes durable encrypted conditional-operation retry preparation/replay, history and replication/migration context, mixed authorized tenant contexts, key-retention/retirement operations and full provider conformance. This does not close the encryption stage or guarantee that every encrypted conditional-write route is replay-safe. The application's resolver is a trusted runtime dependency; authenticated WEB4 actor/tenant admission is still a separate unfinished requirement.

Next priority after acceptance of this batch is the remaining concurrency/history/security integration and durable delivery gates. Controlled migration, gas-aware settlement/placement, atomically persisted per-save deltas and complete CQL remain unfinished, as listed below. No overall percentage is asserted without a weighted acceptance checklist.

## Verified scope

| Area | Evidence | Acceptance still open |
|---|---|---|
| Tracking and save ownership | Detached preparation, private baselines, no-op detection, failed-write isolation, submitted membership and acknowledgement checks; Core 1,494 tests pass, including authenticated key contexts, typed numeric hydration and generic metadata decoder regressions, predicate compilation, composition and pure capability planning | Complete encryption/key-context and arbitrary callback failure acceptance |
| Cross-client concurrency | Native revision checks and idempotent operation receipts under MongoDB, SQLite and Neo4j transaction owners | Wider provider capability admission and complete public route/fault coverage |
| Typed child roles | Captured manifests, root-only authoritative-null preservation, hydration before tracking, explicit partial selections | Remaining parameter, visibility, history and provider conformance |
| V2 graph loading | ID/key, collection, metadata and parent routes; source-bound member reads, heap traversal, depth limits, unlink protection and overlapping roots | Coherent historical snapshots and wider failure/security policies |
| Search selection | Child source and type reach V2 execution; navigation objects are excluded from root search values | Cache implementation and explicit relationship predicates/CQL |
| Selected native regression | MongoDB 77/77 and Neo4j 202/202 at earlier epochs; SQLite 1,003/1,003 at the current authenticated-metadata/DTO epoch; separate Oracle 96/96 query/role cohort | Counts belong to recorded assembly epochs and do not certify every provider |
| Explicit replication | Selected native primary/outbox transactions, fenced delivery, committed-replica receivers and four MongoDB/SQLite delivery combinations | Default enrollment, crash journals, authorization/encryption context, retention, limits and operator migration |

The search-navigation fix has Shared 828/828, SQLite 815/815 and Neo4j 202/202 evidence. Core 1,289/1,289 and MongoDB 77/77 have their own recorded search-selection cohorts. These are separate runs, not one all-provider release test. Subsequent Shared projection changes are tracked by the provider session under `Docs/WIP/HyperDrive-Providers` and need independent acceptance.

Recent local commits include Core `768d2e6` (manager graph ownership) and `ccb7b64` (search selection), paired parent commits `b5b984185` and `a19405b71`, root-search fix `5129b53d2`, and Neo4j certification `69852c20c`. The strict dependency manifest/gitlink validator passed at the paired commits. Local commits do not establish remote publication or deployment readiness.

## Full roadmap remaining

| Stage | Current boundary and remaining work |
|---|---|
| 1. Entity lifecycle | Foundation implemented; finish encryption, runtime binding and failure-publication cases |
| 2. Atomic concurrency | Selected native owners verified; finish provider admission and full route/fault matrix |
| 3. Children, queries and history | Selected graph/search fixes verified; finish parameter/visibility/cache matrices, provider conformance and historical graph policy |
| 4. Durable delivery | Explicit owners/receivers/workers implemented; finish automatic enrollment, security context, process-crash recovery, limits, retention and operator migration |
| 5. Controlled migration | Full checkpoint/catch-up/verification/cutover workflow is not implemented; import/export alone is insufficient |
| 6. Gas-aware placement | Authorized durable settlement, quote freshness, budgets, deadlines, finality and reconciliation remain open |
| 7. Per-save deltas | Local comparison/preview prerequisites exist; atomically persisted, authorized committed deltas and replay semantics are incomplete |
| 8. CQL | [Typed CQL compiler, composition, scope and manager pipeline](CQL_EXPRESSION_CONTRACT.md) implemented; SQLite leaf queries include canonical comparisons, metadata presence/null and selected scalar metadata, strings and membership; authenticated WEB4 policy/routing, broader native adapters, graph queries, paging/aggregates and federation remain open |

The [full roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) retains the original requirements and acceptance criteria. No overall percentage is asserted without a verified weighted checklist. Failover selection does not prove 100% uptime, completed migration or finalized settlement.

## Coordination and publication

Implementation targets Hyperdrive V2; [legacy gaps](LEGACY_HYPERDRIVE_GAPS.md) are documentation only. Reuse the task checkout and retained environments. Preserve other sessions' changes and original uncertain-write evidence.

Keep coherent verified batches on the task branch. Merge and push to `Development` only once or twice daily, as requested to limit Railway builds. Core task-branch publication and substantive STAR/WEB6 remote changes still require coordinated release review. Never advance foreign dependency pins merely to clear a branch gate.

## Evidence history

Detailed dated checkpoints remain in the linked verification, integration and topic documents. Earlier counts, branch names and descriptions of unfinished work describe their original cohorts; use this page and the latest integration entry to interpret current acceptance. This status page is a current synthesis, not a replacement for those original records.

CQL composition checkpoint: immutable `CosmicQuery<T>` with captured AND predicates and explicit current-default/named/all-eligible scope is implemented. Named-scope validation and query-wide composition limits pass five new tests; full Core 1,303/1,303 and both TFMs build. Execution planning, authorization and native execution remain open. See [CQL contract](CQL_EXPRESSION_CONTRACT.md).

SQLite CQL checkpoint: parameterized native predicates with CLR-compatible admitted null/boolean semantics, fixed column identifiers, cancellation and bounded result materialization pass focused8/8 and full823/823. NativeCore4B2E/SharedCA844 epoch is separate from earlier gates. See [native scope](CQL_SQLITE_NATIVE.md); full CQL and earlier roadmap requirements remain open.

Exact field capability checkpoint: Core1,310/1,310 and SQLite824/824 pass. `CqlCapabilities` and native translation share a single field/type/operator map; unsupported canonical fields reject during planning. Current copied Core38E010/SharedCA844/SQLite2FF450 epoch is retained separately from prior gates.

Metadata CQL checkpoint: native Exists/IsNull supports literal nested object keys and distinguishes missing from explicit JSON null. Core1,313/1,313 and SQLite832/832 pass. Native metadata scalar comparisons remain open; presence/null predicates do not imply scalar conversion or full metadata querying.

Shared CQL execution checkpoint: native provider interface and preflight/execution coordinator pass seven new Core cases; SQLite implements and exercises the contract natively. FullCore1,320/1,320 and SQLite833/833 pass. Results retain per-provider groups rather than claiming global deduplication, ordering or snapshot consistency. AuthenticatedWEB4 routing/typedmanager/federation remain open. See [execution pipeline](CQL_EXECUTION_PIPELINE.md).

V2 typed metadata ownership checkpoint: canonical identity, audit, visibility, provider observations and runtime state cannot be overwritten by matching metadata keys (case-insensitive). Eight Core cases and a real SQLite typed-load case pass in fullCore1,328/1,328 and SQLite834/834, zero skips. Custom metadata hydration remains functional. Strict conversion/schema error handling is still open; legacy behavior is documented only.

V2 metadata conversion checkpoint: typed values use the canonical serializer, StoreAsJsonString is explicit, ambiguous names/read-only or indexed setters reject, and conversions complete before setters run. Invalid values return COSMIC_METADATA_HYDRATION_FAILED with no typed point/collection/search result. V2 search now resolves instance-injected Default provider bindings before routing. FullCore1,334/1,334 and SQLite842/842 pass; eight native ID/key/all/search sync/async cases verify explicit errors and unchanged stored data. Broader schema/callback/resource admission and typed CQL integration remain open.

Typed CQL manager checkpoint: V2 ExecuteQueryAsync<T> hydrates leaf query rows through the existing strict metadata/tracking boundary, retains per-provider groups and returns no result on later materialization failures. FullCore1,337/1,337 and SQLite845/845 pass. Native proof includes clean custom values, no-op save preserving logical/provider revisions, an invalid later ordered row and cancellation without query I/O. Authenticated target derivation, relationship options, automatic runtime/entity-kind mapping and full CQL acceptance remain open.

CQL write-back acceptance: new native two-client tests verify sync/async conditional stale-write rejection using queried baselines, unchanged losing caller and winning stored state, and recovery after fresh query. A leaf-query root save with saveChildren:false preserves its unselected child link and independent logical/provider revisions. Focused3/3 and fullSQLite848/848 pass0skips on the same provider assembly epoch as the earlier845 cohort. This batch adds tests/evidence only; no production fix or Core pin change was needed.

CQL ordinal string checkpoint: immutable Contains/explicit-ordinal prefix/suffix predicates and exact field capabilities are implemented with native parameterized SQLite translation. FullCore1,341/1,341 and SQLite874/874 pass, including26 focused Unicode/NUL/empty/null/wildcard/capability cases. Core unit and native copies share the same Core3CC0F440 epoch. Culture/case alternatives, scalar metadata execution and remaining CQL/roadmap acceptance stay open.

CQL array membership checkpoint: bounded scalar arrays capture immutable values and execute as native IN with explicit empty/null/negation semantics. Compiler normalization covers current SDK array/span/default-null-comparer shapes without executing user collections, and normalized nodes consume depth/work budgets (red-before regression retained). FullCore1,347/1,347 and SQLite900/900 pass0skips. Mixed numeric precision is now gated by explicit capability; SQLite exact mixed-numeric translation remains a required next implementation rather than silently coercing decimals.


## Exact SQLite mixed numeric comparisons — 20 October 2026

SQLite now declares MixedNumericComparison for its admitted canonical Int32 fields (Version and Int32-backed HolonType storage). Decimal/double literals are compared in their original precision against Int32 boundaries, then translated to exact integer bounds using floor/ceiling. Fractional equality is false; fractional inequality matches non-null values. Out-of-range literals reduce to the correct constant/non-null predicate. Only exact integer parameters reach SQLite, preventing REAL affinity from rounding a high-precision decimal into a false match. Unsupported numeric path types still reject explicitly.

Six focused native cases pass, covering 420 comparisons against separately compiled CLR predicates plus the decimal equality regression. Cases include six operators, reversed operands, fractional/negative values, floating-point neighbors, Int32 boundaries and enum storage. Full SQLite regression passes 905/905 with zero skips; the owned fixture is absent after teardown. This does not certify arbitrary numeric metadata, undefined enum literals, other adapters or the unfinished security/graph/federation roadmap.


## Exact scalar metadata capability admission — 20 October 2026

`CosmicQueryMetadataCapability` declares an exact CLR metadata path type, its comparison and string operators, and separate membership/mixed-numeric semantics. The exact `CosmicQueryCapabilities(features, canonicalFields, metadataTypes)` profile freezes these declarations and rejects duplicate/null descriptors. Canonical and metadata capabilities cannot grant operations to one another. Nullable types remain distinct; global feature gates still apply. Existing exact field-only profiles declare no scalar metadata support. The broad type-based constructor remains a declaration mechanism and does not certify native adapters.

Five new cases verify operator/type separation, metadata-only profiles, nullable admission, membership/string/mixed-numeric gates and immutable descriptor ownership. Full Core 1,352/1,352 passes with zero skips, and both target frameworks build with zero errors. This is admission groundwork: SQLite scalar metadata translation is not enabled by this checkpoint. Native missing/null/type semantics and the full original roadmap remain required.

Native compatibility verification on the new Core contract: SQLite 905/905 passes in117s, zero skips, with fixture absent after teardown (`sqlite-cql-metadata-profile-full.trx`). No scalar metadata native support is claimed.


## Native SQLite scalar metadata checkpoint — 20 October 2026

The [typed scalar metadata contract](CQL_SCALAR_METADATA.md) adds native comparisons, membership and ordinal string matching for string, boolean, Int32 and Int64 metadata, including nullable value types. The production profile and translator share exact type/operator declarations. Missing and wrong-type paths remain distinct from explicit JSON null; integer bounds and SQLite runtime types are checked before comparison. Arrays and JSON-encoded strings are not traversed as objects. Shared native string/membership helpers preserve existing canonical field behavior. Unsupported metadata types and mixed numeric metadata comparisons reject before I/O, including with overbroad caller plans.

The compiler also handles method-free built-in nullable scalar literal lifting, fixing the nullable boolean comparison exposed by the original red fixture. Full Core 1,353/1,353 and focused native 43/43 pass with zero skips. The original 26-case failure report is retained. Native decimal/double/GUID/date/enum metadata, authenticated WEB4 policy/routing, broader adapters, graph queries, paging/aggregates/federation and all unfinished original roadmap stages remain required.

Final native regression: SQLite 948/948 passes125s, zero skips (`sqlite-cql-scalar-metadata-full-verified.trx`), with the fixture absent after normal teardown. Full Core1,353/1,353 and focused native43/43 belong to the recorded matching Core/provider epoch.


## Bounded query snapshots — 20 October 2026

Planning and execution now share one internal indexed snapshot owner. Authorized target and adapter lists validate a nonnegative count of at most64 before allocating/copying. Provider rows validate their count against the remaining result budget before allocating/copying. No external collection enumerator is used to build these snapshots. A count change detected after copying rejects the snapshot with an explicit plan/registry/provider-result error and no result publication. The executor checks cancellation during indexed copying and canonical row detachment, including after the last row.

The old executor used ToArray before checking the row budget, and the planner enumerated targets after checking only their declared count. Six executor cases and two planner cases fail against those old paths; all eight pass within full Core1,361/1,361, zero skips. They cover index-only collections, excessive/negative counts, changing counts, no provider I/O for invalid registries, and cancellation during copying. The original red reports are retained.

This bounds entry count and removes enumeration from snapshot admission. It does not establish atomic consistency for arbitrary same-count mutations, bound arbitrary indexer/getter duration, implement native scan deadlines, or limit serialized bytes per record. Provider-owned query scans/deadlines, payload-size admission and the complete security/federation/graph roadmap remain required.

Final compatibility gate: fullSQLite948/948 passes123s, zero skips (`sqlite-cql-snapshot-full-verified.trx`), with fixture absent after teardown. This uses the final shared planner/executor helper and matching Core unit/native copies; the earlier executor-only native report remains a separate interim epoch.


## Exact local numeric snapshots — 20 October 2026

Decimal review exposed a correctness gap in the delta prerequisite: default JObject JSON parsing converts fractional object values to double, so nearby high-precision decimals could appear unchanged. Local tracking now captures canonical tokens directly, and save staging, local clones, rollback and concrete projection use those tokens without a text-reader round trip. `StoredRecordJson.CaptureRuntimeToken` is the shared trusted-runtime capture owner. `CaptureInputToken<T>` performs one validated input capture with the declared root contract and existing delegate/cycle rejection, preserving exact scalar values for downstream graph staging.

Mutable byte-array token values need explicit copying: Newtonsoft token DeepClone can retain byte[] references. Capture detaches those values, and private-baseline restoration clones tokens and byte scalars before conversion. This preserves both high-precision decimals and full double range without choosing a lossy global numeric parser policy.

Nine Core cases cover JSON value kinds, tiny custom/nested decimal edits and exact previews, rollback, raw JsonElement numeric values, projection, byte-array ownership and validated input rules. Original five decimal failures and two byte ownership failures are preserved. The first broader run exposed remaining save-staging reparsing; that layer now consumes the same token representation. Final Core1,370/1,370 passes, zero skips, and both targets build with zero errors. Native precision acceptance is recorded separately; local tests alone do not prove external JSON/ciphertext readers or persisted delta correctness.

The provider session owns companion shared graph-planning/publication guard changes using these APIs. Stage7 remains open for atomic committed delta persistence, operation/revision/actor binding, authorization/redaction/encryption, history, deletion/restoration and idempotent original-delta replay. This fixes prerequisite ownership and precision, not the complete committed Save delta API.

JSON element follow-on: strings, booleans, null and containers now retain structural value kinds when captured as tokens; numeric raw lexemes remain exact. Metadata encryption serializes its payload through the same canonical serializer so raw numeric tokens are encoded as numbers. The first native950 run had948pass/2encrypted-publication failures; the original encryption assertions remain intact. Focused final6/6 passes across decimal sync/async plaintext/encrypted variants plus independent root/child key contexts. Final native regression is recorded separately.

Final precision/ownership acceptance: Core1,370/1,370 passes9s; Shared847/847 passes on matching Core/Shared copies; focused native6/6 passes2s; fullSQLite952/952 passes121s, zero skips. The owned fixture is absent after normal teardown. Native cases exercise actual sync/async saves, reloads, exact decimal edits and plaintext/encrypted metadata. Generic text/ciphertext/receipt numeric parsing and decimal-safe conditional fingerprints remain explicit follow-on gates; this does not close committed delta persistence/replay or the full roadmap.


## Exact stored receipt metadata — 20 October 2026

`StoredRecordJson.DeserializeStoredHolon(string)` reads canonical holon metadata through its raw JSON-value codec, excluding that field before restricted typed decoding of the remaining record. Metadata therefore has one decoder owner and never becomes a rounded temporary double. Metadata values use the same JsonElement domain as native SQLite reads, preserving numbers beyond decimal precision/range, wide floating-point lexemes, strings, nested containers and explicit null. Root type binding remains restricted; metadata type-name text remains data. Missing metadata keeps the normal empty dictionary. Ambiguous case-insensitive metadata fields and non-object metadata reject as corrupt data.

SQLite conditional receipt replay now uses this decoder for each stored root/child record. Receipt format1 and its original record JSON are retained; no schema rewrite or alternative receipt fallback is added. Two native restart/replay cases originally fail on precise decimal metadata, then pass unchanged: replay returns original root/child values and original logical/provider revisions while the current database keeps its later update. Wide-double/null/string controls remain intact. Four new codec cases pass in full Core1,374/1,374, zero skips; both Core targets build with zero errors.

This certifies the selected SQLite receipt boundary, not every provider/replica/text reader. Shared replica receipt adoption is coordinated with the provider session. Full committed delta generation, atomic persistence, authorization/redaction and replay remain unfinished, together with the complete original roadmap.

Final receipt gate: Core1,374/1,374 passes11s, focused native2/2 passes2s and fullSQLite954/954 passes117s, zero skips (`receipt-precision-core-full.trx`, `sqlite-receipt-precision-focused.trx`, `sqlite-receipt-precision-full.trx`). Fixture absent after normal teardown. These reports certify the captured SQLite receipt epoch; other decoders and the full roadmap remain open.


## SQLite exact intent fingerprints — 20 October 2026

Precision audit found that SQLite still reparsed captured intent through the default double reader and omitted every provider's keys/revisions. The conditional owner now fingerprints the already captured canonical Holon snapshot and uses the shared exact-number hash owner, without recapturing live inputs. Only this authority's assigned current key/revision and persistence-assigned audit/version fields are excluded. Foreign provider keys/revisions and previous-version keys remain persisted write intent. Equivalent numeric scale and JsonElement/CLR representations normalize without double conversion; strings remain strings.

The corrected hash has explicit fingerprint domain3 (plain intent) or4 (submitted relationship intent), distinct from historical rounded hashes. New SQLite primary receipts retain acknowledgement format1 and add a fingerprintFormat marker. An unmarked/obsolete receipt returns COSMIC_RECEIPT_FINGERPRINT_RECONCILIATION_REQUIRED before another mutation; there is no rounded-hash fallback or automatic second write. Operators must reconcile the original committed operation before retrying with a new intent. This compatibility gate remains an operational rollout requirement before deployment.

Nine native regressions cover changed tiny decimals, nested values, foreign keys/revisions/prior keys, equivalent numeric/local-observation controls and historical receipt refusal without mutation. The original gate fails6/passes3, establishing the defects and controls. Final native acceptance is recorded separately.

Final fingerprint acceptance: focused9/9 passes2s and fullSQLite963/963 passes122s, zero skips, with fixture absent after teardown. Shared exact-hash API dependency83d6523cc has its own856/856 report on matching recorded Core/Shared hashes. No Core source/pin change or deployment is included in this SQLite caller batch.


## Exact archived metadata and physical-observation comparison — 20 October 2026

SQLite archived-record decoding now uses the canonical stored-Holon reader, preserving raw metadata numeric precision rather than converting untyped fractions to double. Current/archive observations of one native revision are compared through the shared exact payload-equivalence owner on canonical tokens. No double-based text reparsing remains in this merge check. Numeric scale-equivalent representations remain equal; a checksum-valid tiny decimal change under the same physical revision remains corrupt data.

Four native red-before cases fail against the old reader/merge. Six new cases now cover sync/async versioned reads, point/collection conflicts and same-value/different-scale controls. Combined with the existing physical-conflict/rollover cases, focused12/12 passes3s, zero skips. Final full native acceptance is recorded separately. This fixes selected historical before-value precision prerequisites; coherent historical graphs, retention/restore policy, committed delta persistence and broader roadmap requirements remain open.

Final history precision gate: focused12/12 passes3s and fullSQLite969/969 passes128s, zero skips, with owned database/wal/shm absent after teardown (`sqlite-history-precision-focused.trx`, `sqlite-history-precision-full.trx`). Shared equality dependency9f345ff4a has its recorded866/866 gate on C82/F2E6; subsequent Oracle0071 evidence is a separate build epoch. No Core source/pin change or deployment is included.


## One exact numeric-value owner for pending state — 20 October 2026

Core.Serialization.StoredRecordValueEquality now owns logical JSON-value comparison, numeric classification and coefficient/exponent normalization. Tracking, pending value previews and save-publication edit checks use the same owner; the provider-session Shared wrapper delegates instead of keeping a second algorithm. Equivalent raw/CLR numeric values and decimal scales do not create artificial pending changes. Numeric text remains text; distinct precise numbers, missing/null distinctions and ordered arrays retain their existing meaning.

Normalization uses coefficient text and BigInteger exponents, without double conversion or allocating powers; valid JSON exponents beyond Int64 do not overflow this comparison. Finite canonical fingerprint bytes are unchanged, verified by the provider-session compatibility gate. This is logical numeric value equivalence, not a general CLR runtime-type identity guarantee or completed CQL scalar conversion policy. Serialized nonnumeric scalar representation equivalence remains follow-on review.

Four initial Core cases have2fail/2controls against the old comparer. Six final new cases pass within fullCore1,380/1,380, zero skips, including unbounded exponent/zero controls. Native sync/async cases verify an equivalent metadata edit yields an empty pending preview, a no-op Save and unchanged stored logical/provider revisions. Final native regression is recorded separately. Generic raw-metadata reader expansion and atomic authorized committed deltas remain open.

Final equality acceptance: Core1,380/1,380 passes10s, Shared883/883 passes on matching recorded dependencies, native no-op2/2 passes2s and fullSQLite971/971 passes122s, zero skips. The fixed fixture database/wal/shm are absent. Existing finite fingerprint bytes remain unchanged; generic metadata reader expansion and committed delta persistence remain separate open gates.


## Generic record metadata decoder — verification in progress, 20 October 2026

The generic `StoredRecordJson.Deserialize<T>`, runtime-Type overload and `DeserializeStored<T>` now dispatch declared `IHolonBase` roots through exact metadata token capture. Holon, Avatar and AvatarDetail roots preserve decimal and arbitrary JSON numeric lexemes without an intermediate double. Generic metadata retains restricted runtime-type binding and typed collection contracts. Recursive metadata capture checks embedded `$type` names even within untyped arrays and objects. The explicitly raw `DeserializeStoredHolon` receipt reader retains its existing JSON-valued metadata contract; its metadata type-name fields remain data, while root runtime types remain restricted.

Import graph attempt staging now uses detached token snapshots rather than a wire text round trip, preserving owned CLR values and replica isolation. No existing failure assertions were weakened.

Evidence: `TestResults/CosmicMongoCurrent/generic-metadata-binder-full-verified.trx`: **1,399 passed, zero failed/skipped**, terminal exit 0. Core assembly SHA-256: `BB68B18BA77A686AD0458E514734854C5BC51F15EF25B08BDEDC4BF44FBDBBE7`. Producer builds succeeded for both target frameworks. Original reports are retained: the first generic change caused three local staging failures; the next full run exposed raw receipt contract differences and an array-nested forbidden type gap (1,395 passed / four failed). These were fixed centrally and the full suite rerun.

Provider acceptance is still pending for this final assembly: Shared 883 passed against the intermediate `1079A928...` assembly, which does not certify the final `BB68B18B...` build. SQLite native acceptance and final cloud consumer compatibility must be recorded before a dependency pin is advanced. Generic collection/envelope roots, nested owned record metadata and lossless typed numeric narrowing remain open. Persisted per-save deltas, wider CQL and the remaining full roadmap remain unfinished.

Final Shared compatibility: **883/883 passed, zero skips**, terminal exit 0, against the final Core `BB68B18B...` and Shared `ECDDACB033BC794BF4B79908AE1D674ADBF7DE971B114BB0F875DC8B5E5D9608`. Report `TestResults/SharedCurrent/shared-generic-decoder-final.trx`, SHA-256 `BA3926D1E2919E5175542119401C66E894E148F5193A955F93938D55657E4D7C`. Original three provider assertions remain intact. SQLite native full-suite process is running; no dependency pin has been advanced.

Provider-session final controlled compatibility on Core `BB68B18B...` / Shared `ECDDACB0...`: Dynamo 299, Google 169, Azure 95 and Cosmos 249 passed with zero skips. Dynamo/Google include precise current and archived record reads. These controlled results do not certify hosted/native deployments. SQLite native full suite remains running at this checkpoint; the provider session is arranging its retained Dynamo native regression.

Final SQLite acceptance: **971/971 passed, zero failed/skipped**, 124 seconds, terminal exit 0 (`TestResults/SQLiteCurrent/generic-metadata-native-full.trx`). Copied Core `BB68B18B...` and Shared `ECDDACB0...` hashes match the final producer cohort. Core source committed locally as `bab32888c0dc5a70560b624feae2d95d443a802c`; paired root commit advances the Core gitlink and manifest together. No push or deployment. Generic root decoder acceptance is verified for this cohort; the remaining roadmap scope is unchanged.

## Typed numeric hydration conservation — 20 October 2026

V2 custom metadata hydration now compares an input JSON number with the serialized converted value using the existing exact numeric equality owner before running any custom setter. Conversion that rounds a precise decimal or integer, or underflows to zero, returns `COSMIC_METADATA_HYDRATION_FAILED` and no query result. Representable decimals and scale-equivalent values remain accepted. This rule applies to numeric JSON inputs, including explicitly decoded `StoreAsJsonString` payloads. It does not establish a complete policy for implicit conversions from ordinary numeric strings, nested numeric members, or schema migrations; those remain open.

New public typed CQL regression cases first reproduced **four failures / five controls passing** (`TestResults/CosmicMongoCurrent/typed-numeric-red.trx`). After the central fix the complete Core suite passed **1,405/1,405**, zero skipped, terminal exit 0 (`typed-numeric-full.trx`). SQLite native CQL cohort passed **18/18**, zero skipped, terminal exit 0 (`TestResults/SQLiteCurrent/typed-numeric-cql-native.trx`). This is a selected native CQL cohort, not a second full 971-test run. Core assembly SHA-256 `C34807F52D7E62DA1FDC82D13DCBDA2989BAE6AB1350AE3264B0084654A8D43F`; earlier full SQLite and cloud codec proofs belong to the preceding `BB68B18B...` cohort.

## Numeric-string hydration precision — 20 October 2026

The V2 typed metadata owner now extends exact numeric conservation to ordinary strings converted into declared numeric scalar properties (including nullable numeric types). A numeric string must contain a finite JSON number and the converted value must serialize to the same numeric value. This retains exact representable values while rejecting rounding, underflow, NaN/Infinity and non-JSON numeric forms such as `+1` and `01`. Literal string properties retain their contents; named enum strings retain their existing conversion contract. Explicit `StoreAsJsonString` decoding still precedes this admission. All conversions finish before custom setters run.

The additional regressions first failed four rounding/underflow cases with eleven controls passing (`TestResults/CosmicMongoCurrent/typed-numeric-string-red.trx`). Final complete Core acceptance: **1,416/1,416 passed**, zero skipped, terminal exit 0, `typed-numeric-string-final-full.trx`. Final Core assembly SHA-256 `19EF62AAB10CE2C6AB999FBB7311A418CA25E89B34BA02228BD7C11DB28B6DDE`; both producer target frameworks built successfully. Named-enum and literal-text controls pass. Earlier SQLite/native/cloud results retain their original assembly epochs; no new native run is claimed for this scalar-string admission batch. Nested numeric conversion and a complete schema migration policy remain open, along with the full roadmap.

## Owned numeric metadata hydration — 20 October 2026

V2 metadata conversion now uses an internal exact token reader for custom property values instead of sending owned values through default floating-point text parsing. Only numerically equivalent CLR token representations are selected for typed conversion; an unrepresentable token remains raw and cannot silently narrow. This shares the existing restricted binder and exact numeric equality owner. Generic stored-record decoding retains its existing default contract.

A nested numeric validator compares numeric JSON leaves with their converted object/list/dictionary paths before publishing a typed row. It understands canonical collection `$values` wrappers and case-insensitive owned-property binding. Missing or changed numeric leaves reject hydration with `COSMIC_METADATA_HYDRATION_FAILED`. This checks numeric JSON leaves; nested string-to-numeric policy, arbitrary converter/getter/setter side effects, duplicate-field schema admission and broader schema migration remain open.

Seven new regressions initially failed: six nested rounding/underflow cases and an exactly representable decimal control that had already rounded to 1 (`TestResults/CosmicMongoCurrent/typed-numeric-owned-red.trx`: seven failed / twenty passed). An intermediate exact raw-token reader exposed three valid-decimal reader failures (`typed-numeric-owned-focused.trx`), retained as evidence and fixed without weakening assertions. Final complete Core suite: **1,423/1,423 passed**, zero skipped, terminal exit 0 (`typed-numeric-owned-final-full.trx`). Both target frameworks built. Core SHA-256 `177F2A175A5898B812AC9A33EA993657E6DFA8945AD7965AFEED2C852403F465`.

SQLite native typed CQL cohort: **8/8 passed**, zero skipped, terminal exit 0 (`TestResults/SQLiteCurrent/typed-numeric-owned-native.trx`), including two new durable-store precision/rejection cases. Both prove stored content remains unchanged; representable typed results start clean. This is a selected eight-case cohort, not full provider recertification. The full roadmap, including persisted save deltas and wider CQL, remains active.

## Owned metadata schema admission — 20 October 2026

The internal typed metadata reader now resolves declared member and collection types through the existing serializer contracts. Before typed construction, it rejects duplicate JSON fields and case aliases that would assign one owned CLR property twice; dictionary keys retain ordinal identity, so `a` and `A` remain distinct. Restricted runtime subtype binding is retained and incompatible declared/runtime owners are rejected. This is metadata admission, not a complete persisted schema registry or migration engine.

Numeric-string conservation now follows declared nested property, collection-item and dictionary-value types. Exact nullable decimals are accepted, while nested rounding/underflow and invalid numeric literals fail explicitly. Named enum strings and literal strings preserve their contracts. The validator reuses the same contract lookup for numeric paths, removing the separate root-only numeric conversion path.

The initial focused regression report (`TestResults/CosmicMongoCurrent/typed-metadata-schema-red.trx`) retained **four failures / twenty-seven controls passing**: three nested numeric-string shapes and duplicate owned-property aliases. Final full Core proof: **1,429/1,429 passed**, zero skipped, terminal exit 0 (`typed-metadata-schema-final-verified.trx`). A deliberately throwing owned constructor confirms ambiguous fields are rejected before invoking it. Both Core target frameworks built. Assembly SHA-256 `DE42F9E4AA865898C8CCB803B130EFF77FADE690DD30441D426CD9A33D70EDE1`.

Native SQLite typed CQL acceptance: **10/10 passed**, zero skipped, terminal exit 0 (`TestResults/SQLiteCurrent/typed-metadata-schema-native.trx`). The durable owned-number tests now cover JSON numbers and numeric strings, both representable and rejected cases; persisted records remain unchanged. Copied Shared SHA-256 `7F820EB6DF2F572D50ADFB97ACA5B13A754A5A6259B515527054CFD767BC73E8`. This selected cohort does not replace earlier full-suite or cloud evidence. Arbitrary valid-object constructor/setter/getter side effects, resource policy, complete schema/version migration and the rest of the roadmap remain open.

## Encrypted metadata admission and publication — 20 October 2026

The shared holon metadata helper used by V2 now requires a recognized ciphertext prefix under `__oasis_enc__`; ordinary JSON cannot masquerade as encrypted content through the generic crypto helper's plain-value pass-through. Decrypted data must be one JSON object with unique keys, no reserved encryption marker, and no collision with already visible metadata (using the current dictionary comparer). Validation completes before changing metadata. Successful publication assigns a newly owned merged dictionary, retaining clear values and exact JSON numeric data without mutating a previously held dictionary reference.

The four additional failure cases initially all failed while the four existing wrong-key/corruption/payload controls passed (`TestResults/CosmicMongoCurrent/encrypted-metadata-admission-red.trx`). Full Core acceptance after the fix: **1,435/1,435 passed**, zero skipped, terminal exit 0 (`encrypted-metadata-admission-full.trx`). Positive CBC and layered GCM controls verify exact metadata and dictionary ownership. Both Core target frameworks built. Core SHA-256 `EAB09C9E1591127FE015ADA7B6DBAB1B43B577A93B20758F4C44BA14776DB8DF`.

SQLite native acceptance: **12/12 passed**, zero skipped, terminal exit 0 (`TestResults/SQLiteCurrent/encrypted-metadata-admission-native.trx`): eight new sync/async typed-load malformed-envelope cases plus four existing encrypted/plain decimal lifecycle cases. Errors return `HOLON_METADATA_DECRYPTION_FAILED` without a loaded result; direct stored reads prove ciphertext, clear fields and revisions remain unchanged. This is selected lifecycle evidence, not full provider recertification.

Persisted non-secret key identity, authorized key resolution/rotation, authenticated versioned envelopes (including the older CBC format), encryption context across durable replication/migration, resource bounds and arbitrary callback behavior remain open. No global wallet/password crypto semantics or legacy routing implementation was changed. The shared helper was hardened because V2 calls it; this does not certify legacy Hyperdrive.

## Decrypt typed collection roots before hydration — 20 October 2026

The V2 shared typed-collection materializer now decrypts each detached root before mapping custom metadata, child-role hydration and tracking acceptance. Typed CQL uses this same owner. Previously encrypted values were absent during custom-property mapping, causing default values to be accepted as clean; malformed/wrong-key ciphertext also bypassed admission. Decryption failure propagates its original error code and produces no CQL execution result. No legacy mapping path was changed.

Five new public CQL tests initially failed (`TestResults/CosmicMongoCurrent/cql-encryption-red.trx`): a valid encrypted custom value, plain JSON under the marker, wrong key, duplicate decrypted fields and conflict with clear metadata. They retain assertions that provider observations are unchanged. Final complete Core: **1,440/1,440 passed**, zero skipped, terminal exit 0 (`cql-encryption-full.trx`); both target frameworks built. Core SHA-256 `C643340449BAF3DA817BD2A0FE4B394DB36682E2A6C735DDB9573AC9BCF6261E`.

SQLite native selected acceptance: **23/23 passed**, zero skipped, terminal exit 0 (`TestResults/SQLiteCurrent/cql-encryption-native.trx`). Four additional malformed-envelope CQL cases and a durable encrypted custom-decimal CQL case pass alongside point-load admission and existing typed-query regression cases. Successful query values start clean, while direct stored reads retain ciphertext and identical committed content.

This certifies selected typed collection/CQL roots. Untyped collection decryption, encrypted descendants/typed roles, key identity/rotation, authenticated/versioned envelopes, authorization/cost admission and complete query/provider conformance remain open. Private encrypted fields do not become provider-queryable merely because returned roots are decrypted. The full roadmap remains active.

## V2 loaded-graph decryption and baseline admission — 20 October 2026

The manager's common loaded-result owner now passes a V2 decryption admission callback into `HolonChangeTracking.AcceptBatch`. Each selected node is admitted immediately before its existing single child-membership capture and baseline preparation. All baseline publication still occurs only after every selected node is prepared. A bad descendant therefore rejects the result with its original decryption error and no returned graph or partially accepted baselines. The same capture drives admission traversal and tracking; there is no additional child-collection walk. Overlapping roots reuse the existing reference-identity visit set. Legacy calls do not receive the V2 callback.

The tracking owner is now an instance method so it uses the correct manager's encryption configuration. The private metadata-point selection helper follows that instance owner. Existing reflection-based batch-failure tests were updated to invoke the instance while retaining their baseline-preservation assertions; they now also assert no V2 result is returned on failure.

Core evidence: **1,450/1,450 passed**, zero skipped, terminal exit 0 (`TestResults/CosmicMongoCurrent/graph-decryption-final-full.trx`), both target frameworks built. New public cases cover point/LoadAll, sync/async and valid/invalid encrypted children; provider observations remain untouched. Two direct admission cases prove single membership capture and no baseline publication after late rejection. Initial reports are retained: the first eight-case red report includes four genuine point-route failures and four collection fixture failures caused by an omitted empty leaf manifest; that fixture was corrected. The first full run also exposed two reflection harnesses still expecting a static method. No baseline-preservation assertions were weakened.

SQLite selected native proof: **31/31 passed**, zero skipped, terminal exit 0 (`TestResults/SQLiteCurrent/graph-decryption-native.trx`), including eight new durable point/LoadAll graph cases and prior encrypted point/CQL checks. Both root and child ciphertext-at-rest and committed revisions remain unchanged. Core SHA-256 `03766A44A7A20F6452CDFC22D14B1749FD0B85DC32B4914F1A90468A911A521A`; copied Shared `A8AAF9C149B61BBE64E0C16AAB7CDACA2B5B80E12C2C1745B032963E6A893095`. The complete SQLite suite is running at this checkpoint; the selected cohort does not substitute for its result.

Typed semantic child property mapping, mixed per-node durable key identities, key rotation/resolution, authenticated versioned envelopes, complete route/provider conformance and the full roadmap remain open. Decrypting selected nodes does not authorize new provider reads or querying encrypted private fields.

Full SQLite graph-admission acceptance completed: **996/996 passed**, zero failures/skips, 125 seconds, terminal exit 0 (`TestResults/SQLiteCurrent/graph-decryption-native-full.trx`) on the same `03766A44...` Core / `A8AAF9C1...` Shared consumer epoch. The earlier running-suite note is superseded by this result. Disposable fixture DB is removed by the owned fixture after completion. No push/deployment or all-provider certification is implied.

## Decryption failure-state flags — 20 October 2026

The common decryption result owner now clears `IsLoaded` when rejecting a read. Existing graph and typed-CQL assertions exposed eight failures with five success controls (`TestResults/CosmicMongoCurrent/decryption-flags-red.trx`): an error with a null result had incorrectly retained the loaded flag. The corrected invariant is an explicit error, `IsLoaded=false`, and no result. Existing native point/graph/CQL cases now assert all three without weakening prior content, revision or baseline checks.

Final full Core **1,450/1,450** and selected SQLite **31/31** pass with zero skips and terminal exit 0 (`decryption-flags-full.trx` and `TestResults/SQLiteCurrent/decryption-flags-native.trx`). Both Core targets built. Core SHA-256 `2A27DB17AE34A417F6BB7B0B1181CDE0DA7AF081AB81F7C209ADAB22C9247C62`. The earlier full SQLite **996/996** proof belongs to the preceding `03766A44...` assembly; this follow-on changes the failure flag only and does not claim another full native run. Durable key identity and the complete roadmap remain open.


Final authenticated-metadata / exact-DTO batch acceptance: **Core 1,494/1,494; full native SQLite 1,003/1,003; Shared 887/887; Solana 559/559**, zero failures/skips/exclusions, terminal exit 0. SQLite final report `TestResults/SQLiteCurrent/metadata-envelope-final-native-full.trx` (126 seconds); all four consumers use Core `F6A4E026662FAC8B4C181FA141796FD1C0B77742F589AF031F4FA1AD9D80BC90` / Shared `3C84DBF709EE5EE6F58068F2E8DE491AA10A7A61192F486C75F7F8AC86EB380F`. Additional controlled cloud compatibility: Dynamo 306, Google 169, Azure Storage 95, Cosmos 249, all passing at the same epoch; these are controlled tests, not new hosted/native certifications. Documentation audit: 49 Markdown files, 348 local link targets, zero broken (`cosmic-doc-link-audit.json`). Fixture DB removed by the owned native fixture; no services or new downloads were needed. Core `25a15a8` is paired with the parent manifest/gitlink and scoped tests/docs. No push/deployment. This supersedes the earlier pending-run notes, not the declared remaining roadmap gates.

23 October runtime-record prerequisite: Core945c65e adds immutable typed runtime column bindings and detached records through the existing projection compiler/planner/executor. FullCore2,018 passes unskipped and both framework targets build. Seven controls cover bounded immutable shapes, exact domains, whole-buffer rejection, source preservation and no arbitrary expression invocation. Projection HTTP/compiler/transport and annotated native demos are still pending; no full CQL/ORM completion claim.

## 23 October — public selected-column endpoint

`POST /api/data/query-cql/projection` now reuses the generic Core record shape and existing authenticated admission/prepared-attempt/receipt owners. FullCore2,059 controls and signed native three-store acceptance1/1 pass unskipped. Native visibility before paging, exact scalar cells, nested metadata, no replay and no-execution rejections are verified. Annotated10-exampleC#/HTTPwalkthroughs and browser/Swagger examples are implemented, with final30/30demo and broad147native regression passing unskipped (195settled/190physicalattempts). Non-count aggregates/grouping and remaining CQL/ORM gates remain open. [Contract](CQL_PROJECTION_HTTP_CONTRACT.md).

## Min/Max Core foundation

Corecf4a5ab preserves helper8d45 and adds typed Min/Max intent plus exact selected-value contracts in the existing scalar pipeline. Both target builds and full2,102unit tests pass (11newcontrols), including valid negative Int64 execution and all-target read rejection before dispatch. Native adapters remain count/existence-only until their Min/Max gates pass. Source19vector fixture is a contract, not native certification. [Implementation requirements](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#minmax-semantics-and-implementation-gate).

## SQLite native Min/Max implementation

Existing scalar ownership now executes native extrema with whole-input domain validation, exact UTC/numeric/string handling and caller/snapshot retention. Initial5 pass; expanded32/33 plus the corrected UTF8 fixture1 pass on unchanged production. Full SQLite regression passes1,318tests with zero skips (2m9s), including all33new Min/Max cases. MongoDB/Neo4j Min/Max, selected-scalar HTTP, Sum/Average/GroupBy and later CQL/ORM gates remain open. [Evidence](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#sqlite-native-minmax-slice).


## MongoDB native Min/Max — 2026-10-23

Implemented in the existing scalar owner, sharing projection cells/windows and exact decoding. Native reduction validates all participating values without entity hydration or a second input sort. Final fresh native matrix: **124 passed, zero skipped**; full provider unit suite: **237 passed, zero skipped**. All 33 new cases pass, including the shared 19-vector matrix, snapshot reparenting, actual aggregate cancellation/reuse, traversal errors outside paging and standard/legacy/mixed UUID ordering. [Implementation and exact evidence](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#mongodb-native-minmax-slice).

SQLite and MongoDB selected extrema are verified. Next: Neo4j selected extrema, then selected-scalar HTTP/demos. Sum/Average/GroupBy and subsequent CQL/ORM roadmap gates remain open; this is not 100% completion.


## Neo4j selected Min/Max — 23 October 2026

Native Min/Max now executes through Neo4j's existing scalar owner, sharing projection guards and exact decoding. Final acceptance: **111 native CQL tests plus 328 provider unit tests, zero skips**, including all 28 new cases. The 18 supported reference vectors pass; UTC DateTime reads remain unsupported and borrowed date permissions are rejected before fetch. Read-committed semantics, blocked-I/O interruption and native character-count wire-bound limitations remain explicit. [Implementation and exact evidence](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#neo4j-native-minmax-slice).

Next: selected-scalar HTTP and demonstrations, followed by the remaining aggregate/read-domain and CQL/ORM gates. This does not complete the full roadmap.


## Selected Min/Max HTTP checkpoint — 23 October 2026

Implemented bounded Selection for Min/Max on the existing scalar route. Count/LongCount/Any reject Selection; Min/Max require it. Projection and scalar HTTP share one typed bounded selector compiler through a generic consumer, without reflection/dynamic invocation or executable delegates. Actual adapter selection/read permissions remain authoritative. Selected values reuse the existing exact projection wire encoder (Int64 decimal text, Guid D, UTC O, nullable null), five-second response checks, one-attempt receipt owner and atomic1MiB output limit.

Full Core/HTTP tests pass **2,117, zero skips**, including11new selected endpoint controls. Browser JS syntax passes. SDK/HTTP walkthrough source now has12 spaced/commented examples/provider, with Min/Max over signed exact Int64 data; the runner requires fresh36SDK/36HTTP. Browser presets and Swagger include selected extrema.

Native verification is **not complete**: combined175case run passed172/failed3. Signed all-store test failed at a late stale Swagger assertion (expected LongCount; sample now Min), after the new signed selected-query checks. Two later tests encountered retained fixture data. Assertion now checks Min plus typed metadata selection. Walkthroughs were not reached, so36/36 is NOT certified. Retained marker SHA256 AC9E6BCCD1AF2A90133F85E32DB7DFD1A7CEA31E28C79EAA90E2908F3ADB2565, stage before-http-request,9graphIDs+137scalarIDs,396journalentries. Do not reset/replay. Existing six-entry no-execution reconciliation does not fit: add an exact reviewed reconciliation that verifies terminal usage outcomes, all146 owned records and scalar-write receipts before cleanup, then rerun broad175 and fresh36/36.


## Selected HTTP and demo gate verified — 23 October 2026

The retained fixture was reconciled by the exact AC9E6B... journal and76BF29... native evidence:180settled operations/175matching receipts,411acknowledged scalar writes and all146owned holons in each of3stores verified before cleanup. Original journal and438snapshot hashes are retained. No queries were replayed. The stale Swagger assertion now checks Min and its bounded Int64 metadata selection.

Fresh combined verification **passes175native regression tests, zero skips (3m7s)**, plus **36SDK queries and36HTTP examples** (12perprovider) on MongoDB, SQLite and Neo4j. The demo reports **216settled operations and211physical provider attempts**. Signed selected queries verify exact negative Int64 values beyond2^53, visibility before reduction (hidden rows contain stronger extrema), source windows, nullable empty inputs and no replay. Full Core/HTTP unit evidence remains2,117passing/0skip, including11new endpoint controls. Browser JavaScript syntaxcheck passes.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx; TestResults/CoreCurrent/cql-selected-scalar-http-current.trx; TestResults/CosmicCqlDemo/v1-demo-result.json, v1-walkthrough.txt, selected-http-native-final.log and selected-http-native-artifacts.json. Five producer/consumer DLL pairs agree (Core109677, shared151E8DC5); the existing runner verified fixture ownership, stopped owned services and removed the exact SQLite/wal/shm/marker after process exit. All four paths and27029/17687listeners were explicitly absent. No runtime/download/new environment was introduced.

This supersedes the pending native/demo checkpoint above. The selected-extrema HTTP/demo gate is complete; the full CQL/ORM roadmap remains open, including Neo4j UTC-date enrollment and strict blocked-I/O interruption, Sum/Average/GroupBy and later federation/query/ORM acceptance.


## Integer Sum/Average Core checkpoint — 23 October 2026

Immutable typed intents, operation-specific exact read admission and constant-size native accumulator normalization now pass both Core targets and all **2,141 tests, zero skips**, including24new cases. Integer Sum checks its final mathematical domain; Average returns Decimal without a binary64 intermediate. Empty/null, precision/rounding and explicit work-limit semantics are specified. **No native provider advertises Sum/Average yet**; native execution/HTTP/demos and the rest of the roadmap remain open. [Contract, examples and evidence](CQL_NUMERIC_AGGREGATES.md).


## SQLite numeric gate — 23 October 2026

Native Sum/Average now share the existing scalar read owner and exact projection operand guard. Native two-limb reduction preserves exact mathematical sums and Decimal averages without a binary64 accumulation or input-row buffer. Final **1,345 SQLite tests pass, zero skips**, including 27 new numeric cases; producer/consumer identities agree and fixed fixture files are absent after teardown. MongoDB/Neo4j numeric execution and HTTP/demos remain open. [Semantics and exact evidence](CQL_NUMERIC_AGGREGATES.md#sqlite-native-sumaverage-verified--23-october-2026).


## MongoDB numeric checkpoint — 23 October 2026

Native Sum/Average now use exact signed limb aggregation through the existing scalar read/snapshot/cancellation owner. The final Mongo suite passes **144 native tests and 245 unit tests, zero skips**;20 native and 8 decoder controls are new. Core and SQLite numerical support remain verified from their earlier checkpoints. See [numeric contract and evidence](CQL_NUMERIC_AGGREGATES.md). Neo4j numeric support, numeric HTTP/Decimal wire and demos, GroupBy, composition/federation, strict blocked-I/O interruption and the remaining roadmap gates are still open. This checkpoint is not full CQL/ORM completion.
