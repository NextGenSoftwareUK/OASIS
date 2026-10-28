# CQL / COSMIC ORM EF and NHibernate parity checklist

Baseline: 26 October 2026. Scope: SQLite, MongoDB and Neo4j through the shared
COSMIC ORM / CQL APIs. This is the work queue and completion denominator; the
older chronological roadmap is supporting history, not the source of green status.

## Rules

- ✅ means the precise row has a linked proving test/demo for that database.
  It does not imply adjacent features or arbitrary LINQ support.
- ❌ means missing, partial, or not yet proven at the stated provider scope.
  Existing code without adequate evidence stays ❌. Native rejection is not support.
- Work only on ❌ rows. Freeze ✅ rows unless a reproducible bug is found.
  Shared-code changes necessary for a ❌ implementation must preserve its ✅ tests;
  do not start unrelated hardening or expand the finished row's scope.
- Promote a cell only after implementation, an appropriate proving test/demo,
  recorded outcome and docs agree. Evidence must identify the provider; shared
  unit tests alone do not certify three native adapters.
- Parity means equivalent observable semantics. Literal EF/NHibernate API/tool
  compatibility, relational-only features and COSMIC extensions are explicit rows;
  they are not silently treated as supported or removed from scope.
- This is a feature-family inventory against the public EF Core, EF6 and NHibernate
  references below, not a claim to enumerate every provider-specific extension or
  every CLR overload. Split a row before implementation if its acceptance differs;
  preserve its parent/history and never make a broad row green from a narrow case.
- Existing native evidence: [three-store result][G], [source hashes][GS],
  [seven producer/consumer pairs][GB], [test result][GT]. It covers 526 settled
  operations / 608 attempts, 405 SDK cases, 138 HTTP cases and 30 save-demo checks; this total is not a parity percentage. The latest lineage
  correction has separate [unit evidence][LC] and is included in this fresh native gate.

## Mapping and schema

| ID | EF / NHibernate feature and required semantics | SQLite | MongoDB | Neo4j | Proving evidence or exact remaining gap |
|---|---|---|---|---|---|
| M01 | Persist unrestricted POCO classes without IHolon/base-class dependency | ❌ | ❌ | ❌ | **DEFER UNTIL DEMO GREEN** — No proven unrestricted model adapter. |
| M02 | Attribute-driven entity/property mapping equivalent to ORM annotations | ❌ | ❌ | ❌ | Partial OASIS attributes are not a complete mapping contract. |
| M03 | Fluent model configuration / mapping-by-code | ❌ | ❌ | ❌ | No proven public model builder. |
| M04 | NHibernate XML mappings | ❌ | ❌ | ❌ | No compatible importer/mapper proof. |
| M05 | Model conventions and configurable naming strategies | ❌ | ❌ | ❌ | No complete convention/override suite. |
| M06 | Assigned single GUID primary keys | [✅][R] | [✅][R] | [✅][R] | Three-provider REST create/read/update fixtures retain assigned IDs. |
| M07 | Generated GUID keys with caller publication only after successful commit | [✅][SV] | [✅][SV] | [✅][SV] | **DEMO** — [Save steps 1 and 7][SV], [real native commit observer][SB]: generated caller ID still empty after native commit; validated success publishes it; rejected changed intent keeps empty ID. [All-three gate][G]. |
| M08 | Integer identity / sequence / HiLo generators | ❌ | ❌ | ❌ | Declare portable semantics and native restrictions; no certification. |
| M09 | Composite primary keys | ❌ | ❌ | ❌ | Composite join keys are not composite entity identity. |
| M10 | Alternate/natural keys and uniqueness enforcement | ❌ | ❌ | ❌ | Metadata lookup is not atomic uniqueness. |
| M11 | Required/optional fields, length and precision model validation | ❌ | ❌ | ❌ | Query scalar checks do not prove persistence model constraints. |
| M12 | Custom value converters / NH IUserType equivalents | ❌ | ❌ | ❌ | No complete pluggable round-trip contract. |
| M13 | Value comparers, equality and snapshots for converted mutable values | ❌ | ❌ | ❌ | Needs converter/comparer integration proof. |
| M14 | Owned/complex/component types with nested updates | ❌ | ❌ | ❌ | Metadata objects alone do not prove component semantics. |
| M15 | Primitive and value-object collections | ❌ | ❌ | ❌ | Needs mapping, equality and update semantics. |
| M16 | Shadow and indexer properties | ❌ | ❌ | ❌ | No proven model/session state mapping. |
| M17 | Backing fields, private setters and constructor materialization | ❌ | ❌ | ❌ | Reflection support requires explicit complete acceptance. Positional record/DTO constructor selection without expression member bindings remains unsupported (native indexed demo reproduced rejection); public parameterless DTO/member-init and anonymous selections are separate proven shapes. |
| M18 | Immutable entities / read-only mappings | ❌ | ❌ | ❌ | Detached results are not immutable entity persistence. |
| M19 | Inheritance: discriminator / table-per-hierarchy equivalent | ❌ | ❌ | ❌ | HolonType alone is not proven polymorphic mapping. |
| M20 | Inheritance: table-per-type equivalent | ❌ | ❌ | ❌ | Define faithful non-relational equivalent and test. |
| M21 | Inheritance: table-per-concrete-type equivalent | ❌ | ❌ | ❌ | No complete polymorphic CRUD/query proof. |
| M22 | Entity/table splitting and multiple mappings of one class | ❌ | ❌ | ❌ | No complete mapping contract. |
| M23 | Keyless entities / read-only views / subselect mappings | ❌ | ❌ | ❌ | Scalar DTO projections are separate. |
| M24 | Database defaults, computed and generated properties | ❌ | ❌ | ❌ | Needs model configuration and returned-value ownership. |
| M25 | Indexes, unique indexes and constraint management | ❌ | ❌ | ❌ | Internal provider indexes do not prove user model management. |
| M26 | Schema migrations: diff, ordered up/down and data preservation | ❌ | ❌ | ❌ | **DEFER UNTIL DEMO GREEN** — Provider upgrades are not a public model migration framework. |
| M27 | Migration scripts/bundles and deployment history | ❌ | ❌ | ❌ | **DEFER UNTIL DEMO GREEN** — No parity toolchain proof. |
| M28 | Reverse engineering / database-first scaffolding | ❌ | ❌ | ❌ | **DEFER UNTIL DEMO GREEN** — No cross-store scaffolder proof. |
| M29 | Model-managed seed data and versioned initialization | ❌ | ❌ | ❌ | Test fixture seeding does not implement this. |
| M30 | EF6 visual designer / EDMX import-export compatibility | ❌ | ❌ | ❌ | **DEFER UNTIL DEMO GREEN** — Separate literal tooling requirement. |

## Session, tracking and persistence

| ID | EF / NHibernate feature and required semantics | SQLite | MongoDB | Neo4j | Proving evidence or exact remaining gap |
|---|---|---|---|---|---|
| S01 | Identity map: one instance per key within a session | ❌ | ❌ | ❌ | Detached query copies intentionally do not prove identity resolution. |
| S02 | Context/session unit of work spanning multiple pending entities | ❌ | ❌ | ❌ | Per-holon Save and explicit batches are not SaveChanges/Flush parity. |
| S03 | Added/Unchanged/Modified/Deleted/Detached state transitions | ❌ | ❌ | ❌ | Map all transitions and provider evidence before promoting. |
| S04 | Snapshot dirty detection and no-op saves | [✅][SV] | [✅][SV] | [✅][SV] | **DEMO** — [Save steps 2–6][SV]: acknowledged and freshly loaded no-ops preserve revision and create no delta, mutation or preparation receipt; local typed/metadata edits become dirty, acknowledged update becomes clean. [Native observer][SB], [all-three gate][G]. |
| S05 | Property-level original/current values and explicit modified flags | ❌ | ❌ | ❌ | Needs complete public entry API/evidence. |
| S06 | Notification/proxy change tracking | ❌ | ❌ | ❌ | No full notification/proxy proof. |
| S07 | Attach/update/merge disconnected graphs | ❌ | ❌ | ❌ | Conditional save is not general session merge semantics. |
| S08 | Detach/clear session and read-only entities | ❌ | ❌ | ❌ | No complete session API proof. |
| S09 | Refresh/reload and database values without losing unsaved state | ❌ | ❌ | ❌ | Explicit conflict/reload ownership matrix needed. |
| S10 | Persist a new assigned-key entity | [✅][R] | [✅][R] | [✅][R] | VerifyShippingRestAsync loops all stores; signed create assertions. |
| S11 | Update an existing entity with optimistic revision condition | [✅][R] | [✅][R] | [✅][R] | Same native REST matrix verifies winner/stale conditions. |
| S12 | Hard delete with optimistic revision condition | [✅][T] | [✅][T] | [✅][T] | Signed REST/GraphQL/gRPC deletion matrix, retained full native gate. |
| S13 | Async persistence and cancellation across entity graph operations | ❌ | ❌ | ❌ | Async healthy calls exist; complete cancellation/ownership row unverified. |
| S14 | Sync/async semantic parity for persistence | ❌ | ❌ | ❌ | Needs explicit per-store case mapping, not an async-only green. |
| S15 | Statement batching / ordered write-behind | ❌ | ❌ | ❌ | Explicit graph batches do not certify write-behind. |
| S16 | Set-based update without loading tracked entities | ❌ | ❌ | ❌ | No proven ExecuteUpdate / bulk HQL equivalent. |
| S17 | Set-based delete without loading tracked entities | ❌ | ❌ | ❌ | No proven ExecuteDelete / bulk HQL equivalent. |
| S18 | Insert-select / bulk insert from a query | ❌ | ❌ | ❌ | No proven equivalent. |
| S19 | Stateless session bulk-processing API | ❌ | ❌ | ❌ | Direct provider methods are not a certified stateless session. |
| S20 | Flush modes and explicit flush boundaries | ❌ | ❌ | ❌ | No unit-of-work flush mode contract. |
| S21 | Save interceptors, lifecycle events and validation callbacks | ❌ | ❌ | ❌ | Partial callbacks need ordering/error/reentrancy coverage. |

## Relationships and fetching

| ID | EF / NHibernate feature and required semantics | SQLite | MongoDB | Neo4j | Proving evidence or exact remaining gap |
|---|---|---|---|---|---|
| A01 | Arbitrary one-to-many entity relationships | ❌ | ❌ | ❌ | Parent/child holons are narrower than arbitrary mapped associations. |
| A02 | One-to-one entity relationships | ❌ | ❌ | ❌ | No full mapping/constraint/fix-up proof. |
| A03 | Many-to-many relationships with join entities | ❌ | ❌ | ❌ | No complete association CRUD/query proof. |
| A04 | Many-to-many skip navigations | ❌ | ❌ | ❌ | No equivalent proof. |
| A05 | Bidirectional navigation fix-up / inverse ownership | ❌ | ❌ | ❌ | Children linking alone does not certify automatic fix-up. |
| A06 | Cascade insert/update/delete for mapped associations | ❌ | ❌ | ❌ | Existing selected-child graph work needs full cascade semantics proof. |
| A07 | Orphan detection and delete-orphan semantics | ❌ | ❌ | ❌ | No complete automatic orphan lifecycle proof. |
| A08 | Eager loading selected holon child graphs | [✅][D4] | [✅][D4] | [✅][D4] | V1 example4 + three-store native walkthrough; bounded selected graphs. |
| A09 | Filtered Include with per-navigation ordering/paging | ❌ | ❌ | ❌ | Child-type/depth selection is not full filtered Include. |
| A10 | Explicit loading of previously unloaded navigation/collection | ❌ | ❌ | ❌ | Requires association-loaded state and public load contract. |
| A11 | Lazy-loading entity proxies | ❌ | ❌ | ❌ | No equivalent proof. |
| A12 | Lazy/extra-lazy collections: count, membership and element access | ❌ | ❌ | ❌ | Exists predicates are not deferred collection loading. |
| A13 | Split-query and join-fetch strategies | ❌ | ❌ | ❌ | No selectable fetch-strategy parity proof. |
| A14 | Batch/subselect fetch and future/multi-query execution | ❌ | ❌ | ❌ | Composition within one query is a different feature. |
| A15 | Bags, sets, ordered lists and indexed/map collections | ❌ | ❌ | ❌ | Explicit duplicate/order/key semantics need certification. |
| A16 | Polymorphic/any associations | ❌ | ❌ | ❌ | No complete mapping/query proof. |
| A17 | Relationship cycles and shared-reference identity preservation | ❌ | ❌ | ❌ | Bounded traversal/rejection is not full reference identity parity. |

## Query language, translation and materialization

| ID | EF / NHibernate feature and required semantics | SQLite | MongoDB | Neo4j | Proving evidence or exact remaining gap |
|---|---|---|---|---|---|
| Q01 | Standard IQueryable / IQueryProvider execution | ❌ | ❌ | ❌ | Initial entity-row bridge exists in shared [CosmicLinq](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Queries/CosmicLinq.cs): standard Where/order/Skip/Take, deferred synchronous enumeration and async OASISResult reads through V2 HolonManager.22 selected-terminal/numeric/extrema/reselection focused/2,536 CQL unit tests and1,501 native checks pass (SQLite500/Mongo500/Neo501), plus207 Mongo/141 Neo CQL/compiler/packet tests, including per-execution closure binding, negative counts and explicit errors. [Native test](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqAcceptanceTests.cs). Count/LongCount/Any/All now execute natively; All preserves its post-window predicate. Flat Select/selected Where/order/windows/All now execute through native projection owners. Selected Count/LongCount/Any now use independent native cardinality admission, including predicates after selected windows and zero DTO construction. Entity First/Single/Last/ElementAt/default families use native cardinality and typed hydration ownership. Selected families now use the V2 composition owner with independent native reversal, post-window predicates and cardinality before DTO constructors. Selector Min/Max/Sum and decimal/Single/Double Average, plus unstaged scalar-selected aggregates, are verified. Int32/nullable Int32 Average double output is independently enrolled with packet/domain/rounding tests. All3 checked Int64/nullable Int64 Average is verified with ordered-prefix overflow and paging proof. Entity Count/LongCount/Any predicates after paging are verified with protected relationship grammar. all3 signed-integer selected Sum/Average is verified (Q01a); general post-window entity projection/numeric composition remains partial (see Q01f); named single-provider scope only. Keep all cells red. [Evidence counts/hashes/paths](CURRENT_STATUS.md). |
| Q01a | Selected Sum/Average after Where/order/Skip/Take: Int32/Int64 and nullable, exact sum and standard double means | ✅ [Native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqStagedNumericAcceptanceTests.cs) | ✅ [Native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqStagedNumericAcceptanceTests.cs) | ✅ [Native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqStagedNumericAcceptanceTests.cs) | All3:27 native cases/store: scalar/record selectors, nullable/empty/negative values, sum overflow, both Int64 prefix-overflow signs, selected ordering/offset and direct decimal-result CQL mean. Shared Core contract/capability/packet tests: [focused proof](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicProjectedNumericTests.cs). Mongo194 CQL/compiler/packet tests pass, including [selected numeric schema/empty/admission tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests/MongoCqlSelectedIntegerTests.cs). Neo129 CQL/compiler/packet tests pass, including [selected numeric schema/empty/admission tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests/NeoCqlSelectedIntegerTests.cs). Freeze this row unless a bug is reproduced. |
| Q01b | Selected numeric reductions after result stages: remaining decimal/Single/Double/UInt64 and nullable domains | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqStagedWideAcceptanceTests.cs) | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqStagedWideAcceptanceTests.cs) | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqStagedWideAcceptanceTests.cs) | All3:37 native cases cover precision, cancellation/subnormals, overflow versus representable mean, null/empty, mixed-width records without DTO construction and post-selection windows. UInt64 uses direct CQL (Queryable has no UInt64 Sum overload). Mongo202 CQL/compiler/packet tests pass, including8 domain-width/empty/malformed cases. Neo137 CQL/compiler/packet tests pass, including8 exact domain-word/empty/malformed cases. Freeze this row unless a bug is reproduced. |
| Q01c | Selected Min/Max after result stages, scalar and flat-record selectors | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqSelectedExtremaAcceptanceTests.cs) | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqSelectedExtremaAcceptanceTests.cs) | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqSelectedExtremaAcceptanceTests.cs) | Shared [extrema owner](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Queries/CosmicProjectedExtremum.cs) lowers final null filter/order/Take and reads only validated winning cells;4 new [ownership tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicProjectedExtremumTests.cs),2,532 CQL regression tests pass. All3:218 native cases prove33 domains, empty/all-null/all-missing, exact multi-row numeric extrema, post-window semantics and no-DTO records. Reproduced nullable UInt64 JSON-null normalization bug fixed at its shared key owner. Mongo202 CQL/compiler/packet tests also pass. Neo137 CQL/compiler/packet tests pass. Freeze Q01c unless a bug is reproduced. |
| Q01d | Repeated Select over a selected sequence, scalar and flat-record column rebinding | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqRepeatedSelectAcceptanceTests.cs) | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqRepeatedSelectAcceptanceTests.cs) | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqRepeatedSelectAcceptanceTests.cs) | Preserve earlier windows/stages, exact aliases/types and deferred captures; reject unsupported computations explicitly. Shared [binding owner](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Queries/CosmicProjectionReselect.cs) now preserves physical columns/stages and maps only final outputs;4 new [tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicProjectionReselectTests.cs),2,536 CQL regressions pass. All3:26 native cases/store prove renamed/reordered/duplicated/dropped fields, scalar/record identities, earlier hidden-column predicates/windows, deferred captures, rows/elements/cardinality/All/numeric/extrema terminals and zero intermediate constructors. Unsupported indexed/computed forms fail explicitly. Freeze unless a bug is reproduced. |
| Q01e | Entity First/Single/Last predicate overloads after source paging | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqPostWindowElementAcceptanceTests.cs) | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqPostWindowElementAcceptanceTests.cs) | ✅ [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqPostWindowElementAcceptanceTests.cs) | Predicate must remain after the source window; preserve ownership/traversal errors and empty/multiple semantics. Shared [element owner](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Queries/CosmicElementQuery.cs) now retains terminal position, independent admission and physical attempt limits;4 new [owner tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicPostWindowElementTests.cs),24 focused/2,540 CQL regression tests pass. SQLite/Mongo45 and Neo46 native cases prove element families, page position/default/cardinality, exact metadata/membership, protected source/terminal scope errors before output limits, expanded graphs/depth/node budgets under independently admitted consistency and owner/public/authoritative visibility.35 SQLite compiler tests pass. Mongo207 CQL/compiler tests pass, including5 new window/error/envelope/enrollment cases. Neo141 CQL/compiler tests pass, including4 new state-carry/work/enrollment tests. Neo provider-default graphs pass; snapshot requests explicitly reject with CQL_CONSISTENCY_UNSUPPORTED. Freeze Q01e unless a bug is reproduced. |
| Q01f | General entity Where/order composition after earlier source windows | ❌ | ❌ | ❌ | Native immutable filter/order/window/reverse stages and owned source/work/payload checks implemented on all3. Standard rows, Count/LongCount/Any/All, First/Single/default/predicate, forward/from-end indexes, Reverse and ordered Last/default/predicate verified. Cardinality precedes DTO construction; protected errors, visibility, graph depth and explicit consistency preserved. Core2,578 regressions; SQLite45/Mongo216/Neo148 focused tests;1,781 native checks. Proof: [forward elements](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlEntityElementAcceptanceTests.cs), [cardinality](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlEntityCountAcceptanceTests.cs), [reversal/tails](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlEntityReverseAcceptanceTests.cs), [tail binding tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicLinqEntityTailTests.cs). Explicit First/Single/Last default-value overloads verified by3 [shared tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicLinqEntityDefaultTests.cs) and12 additional live cases/store:caller identity/tracking preserved, errors/ambiguity propagate. Shared entity-projection contract/plan/owner implemented;4 [new tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicEntityProjectionTests.cs) prove frozen stages, independent admission, cell validation before DTOs and output-window bounds. SQLite entity projection passes47 provider tests and10 native cases (combined1,791):source/stage windows, reversal, exact domains, visibility, protected scopes, budgets and validation before DTO construction. Mongo entity projection now passes218focused CQL tests/10native cases using the same stage-proof snapshot owner and typed decoder ([native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlMongoEntityProjectionNativeTests.cs)). Neo entity projection now passes149focused CQL tests/10native cases, preserving source proof and stage order in one native query ([native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlNeoEntityProjectionNativeTests.cs)). Standard Select and numeric integration remain pending; deferred behind [broader demo](CQL_DEMO_EXPANSION_ROADMAP.md). Keep cells red; exact hashes/current scope in [CURRENT_STATUS](CURRENT_STATUS.md). |
| Q02 | C# query-comprehension syntax with standard LINQ operators | ❌ | ❌ | ❌ | No complete standard query-provider proof. |
| Q03 | Typed expression Where with declared metadata equality | [✅][F] | [✅][F] | [✅][F] | Native SDK federation tests and typed federation demo; declared subset. |
| Q04 | Conjunction/disjunction and nested metadata predicates | [✅][D2] | [✅][D2] | [✅][D2] | V1 example2 on all three stores. |
| Q05 | Null and missing metadata tests | [✅][O] | [✅][O] | [✅][O] | **DEMO** — Present-null Exists/IsNull, selectable missing path, missing-not-null and non-null guards; examples 59–63. [All-three native gate][G]. |
| Q06 | Field-to-field comparisons | [✅][O] | [✅][O] | [✅][O] | **DEMO** — Native true/false Int64 path comparisons, canonical-to-metadata Guid and canonical field equality; examples 64–67. Declared path domains only. [All-three native gate][G]. |
| Q07 | Contains membership over bounded constant sets | [✅][O] | [✅][O] | [✅][O] | **DEMO** — Bounded name/Int64 sets, empty sets, duplicate constants and exact >2^53 values; examples 68–71. [All-three native gate][G]. |
| Q08 | String Contains/StartsWith/EndsWith translation | [✅][O] | [✅][O] | [✅][O] | **DEMO** — Ordinal Contains/StartsWith/EndsWith: literal metacharacters, empty patterns, case/normalization sensitivity and Unicode fixture; examples 72–88. No culture/ignore-case overload claim. [All-three native gate][G]. |
| Q09 | String length/case/substring/trim/replace functions | ❌ | ❌ | ❌ | General method translation unverified. |
| Q10 | Native selected scalar columns / flat records | [✅][D10] | [✅][D10] | [✅][D10] | V1 example10; this is not arbitrary DTO construction. |
| Q11 | Arbitrary constructor/record/member-init DTO materialization | ❌ | ❌ | ❌ | Common shape support needs precise provider proof. |
| Q12 | OrderBy/OrderByDescending with declared scalar domains | [✅][F] | [✅][F] | [✅][F] | Native federation order/tie cases and SDK demo. |
| Q12b | ThenBy/ThenByDescending for multi-key entity ordering | [✅][O] | [✅][O] | [✅][O] | **DEMO** — Both secondary directions determine order when every primary stock key ties; examples 89–90. [All-three native gate][G]. |
| Q13 | Skip/Take source paging | [✅][D1] | [✅][D1] | [✅][D1] | V1 example1; visibility enforced before page. |
| Q14 | First terminal, including empty-input behaviour | [✅][E] | [✅][E] | [✅][E] | **DEMO** — Examples 47–58 on all three stores ([native gate][G]); [cardinality/window/typed/error tests][EU] pass in [3,767-test gate][EG]. C# entity-query terminals; HTTP/selected-value variants are separate. |
| Q15 | FirstOrDefault terminal | [✅][E] | [✅][E] | [✅][E] | **DEMO** — Examples 47–58 on all three stores ([native gate][G]); [cardinality/window/typed/error tests][EU] pass in [3,767-test gate][EG]. C# entity-query terminals; HTTP/selected-value variants are separate. |
| Q16 | Single terminal: reject empty and multiple matches | [✅][E] | [✅][E] | [✅][E] | **DEMO** — Examples 47–58 on all three stores ([native gate][G]); [cardinality/window/typed/error tests][EU] pass in [3,767-test gate][EG]. C# entity-query terminals; HTTP/selected-value variants are separate. |
| Q17 | SingleOrDefault terminal | [✅][E] | [✅][E] | [✅][E] | **DEMO** — Examples 47–58 on all three stores ([native gate][G]); [cardinality/window/typed/error tests][EU] pass in [3,767-test gate][EG]. C# entity-query terminals; HTTP/selected-value variants are separate. |
| Q18 | Last/LastOrDefault terminals with explicit order | [✅][TL] | [✅][TL] | [✅][TL] | Explicit-order Last/LastOrDefault preserve source Skip/Take inside one native read, including graph expansion and exact scalar/tie ordering. [Examples 106–135][TL], [Core tests][TT], [native gate][G]. |
| Q19 | ElementAt/ElementAtOrDefault terminal family (full parent scope) | [✅][TL] | [✅][TL] | [✅][TL] | Int32 and System.Index (from-start/from-end) forms now covered. Zero/end/out-of-range/default semantics and window-relative ^N proven; bounded limits remain explicit. [Examples][TL], [Core gate][TG], [native gate][G]. |
| Q19a | ElementAt/ElementAtOrDefault with zero-based Int32 index | [✅][IX] | [✅][IX] | [✅][IX] | [Examples 91–105][IX] pass on all3stores; [15 boundary/window/admission tests][IT] in [3,782-test gate][IG]. Known-empty defaults avoid I/O; native failures remain errors. No from-end/System.Index claim; parent Q19 remains red. |
| Q20 | Count terminal over complete admitted input | [✅][D8] | [✅][D8] | [✅][D8] | V1 example 8; explicit source window precedes terminal. |
| Q21 | LongCount terminal and exact Int64 result / Count overflow semantics | [✅][D7] | [✅][D7] | [✅][D7] | Native example 7 and [137-row no implicit page test][NC]; [shared domain/overflow tests][SC] passed in [unit gate][LC], including Int64.MaxValue. |
| Q22 | Any terminal | [✅][D9] | [✅][D9] | [✅][D9] | V1 example9. |
| Q23 | All terminal (full parent scope) | ✅ | ✅ | ✅ | Parent audit: Q23a–Q23e and Q49a independently prove the declared entity/metadata/relationship and selected-pipeline contracts. [Shipping C# demo](../../../Examples/CosmicCqlDemo/SourceAndProjectedAllDemo.cs) passes24 manager-owned examples (8/store), plus216 source checks and168 selected-All acceptance checks, with exact cleanup and unchanged original fixture. Earlier entity-All cohorts remain linked below. [Counts/hashes/paths](CURRENT_STATUS.md). No arbitrary LINQ overload or complete681-case legacy walkthrough claim; broader parity rows remain independent. |
| Q23a | All with scalar/metadata predicates after the composed source window | [✅][AU] | [✅][AU] | [✅][AU] | [Native examples 136–157][AD]: 22 cases/store, empty/null/missing, windows, access and graph-filtered sources; [retained-fixture native proof][AG] passes all three. Parent Q23 is now verified at its declared scope. This is not a completed full-walkthrough claim. |
| Q23b | Direct-child Any predicates inside All, after source paging, with visibility and Boolean combinations | [✅][AC] | [✅][AC] | [✅][AC] | Examples 158–173 in [All demo][AD]; [retained native proof][ACG] passes 38 All cases/store (114 total), with 3,815 Core tests. Independent post-window capability; nested/descendant traversal remains in full Q23. Full walkthrough rerun pending. |
| Q23c | Nested-child Any predicates inside All, including lexical scopes and per-level visibility | [✅][AN] | [✅][AN] | [✅][AN] | [All examples 174–181][AD], [138 retained native checks][ANG] (46/store), 3,817 Core tests. Two/three child levels, scalar/Boolean child predicates, literal capture and hidden intermediate parent. Full Q23 descendant requirement remains red; full walkthrough pending. |
| Q23d | Bounded descendant traversal inside entity All, with complete protected error/composition semantics | [✅][DP] | [✅][DP] | [✅][DP] | Both relationship orientations and nested descendant scopes pass [390 native checks][DPG]:276All examples,24self-cycle,45protected-scope and45mixed-cycle checks. 3,831 Core tests/bothTFMs/console pass; expansion bounded before allocation and re-admitted natively. [Examples214–227][AD]. Projected All is now verified in Q23e; full681SDKwalkthroughpending. |
| Q23e | All over selected/projected sequences, preserving selected-result filters/order/windows and empty/error semantics | ✅ | ✅ | ✅ | [Native gate](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlProjectedAllAcceptanceTests.cs):168 checks passed (50/store plus18 rotated physical-budget cases), zero skips; complete protected source scopes, empty windows, visibility,12 exact scalar domains, cycles, global rows/bytes/stage/proof budgets and zero-budget empty participation. Exact six-ID cleanup/store verified; original fixture unchanged. [Current counts/hashes/paths](CURRENT_STATUS.md), plus recorded focused SQLite47/Mongo49/Neo18 boundary/window/cancellation cohorts. Q49a broader source-shape proof and parent Q23 integration/demo audit remain separate. No DTO/client reduction substitute. |
| Q24 | Min terminal over declared scalar selections | [✅][D11] | [✅][D11] | [✅][D11] | V1 example11; domain-specific expansion tracked below. |
| Q25 | Max terminal over declared scalar selections | [✅][D12] | [✅][D12] | [✅][D12] | V1 example12. |
| Q26 | Exact integer Sum | [✅][D13] | [✅][D13] | [✅][D13] | V1 example13. |
| Q27 | Integer Average with declared Decimal result | [✅][D14] | [✅][D14] | [✅][D14] | V1 example14. |
| Q28 | Exact Int64 values beyond JavaScript Number precision | [✅][D3] | [✅][D3] | [✅][D3] | V1 example3, supported scalar/wire domain. |
| Q29 | UTC DateTime predicates, projection and group keys | [✅][D20] | [✅][D20] | [✅][D20] | V1 examples20/21. |
| Q30 | DateOnly/TimeOnly/DateTimeOffset/TimeSpan translation | ❌ | ❌ | ❌ | UTC DateTime is not these distinct CLR domains. |
| Q31 | Date arithmetic and date-part functions | ❌ | ❌ | ❌ | No complete provider translation proof. |
| Q32 | Exact UInt64 predicates/selections/aggregates | [✅][D22] | [✅][D22] | [✅][D22] | V1 examples22/23/35-37. |
| Q33 | Exact Decimal predicates/selections/aggregates | [✅][D24] | [✅][D24] | [✅][D24] | V1 examples24/25/38-40. |
| Q34 | Single predicates/selections/Sum/Average | [✅][D28] | [✅][D28] | [✅][D28] | V1 examples28/29/44-46; explicit finite arithmetic domains. |
| Q35 | Double predicates/selections/Sum/Average | [✅][D26] | [✅][D26] | [✅][D26] | V1 examples26/27/41-43. |
| Q36 | Arbitrary arithmetic/computed projection expressions | ❌ | ❌ | ❌ | Supported aggregate operators do not imply general expressions. |
| Q37 | GroupBy with selected keys and aggregate columns | [✅][D15] | [✅][D15] | [✅][D15] | V1 example15. |
| Q38 | HAVING / filters over selected aggregate aliases | [✅][D18] | [✅][D18] | [✅][D18] | V1 examples18/19. |
| Q39 | Ordering and paging grouped results | [✅][D16] | [✅][D16] | [✅][D16] | V1 examples16/17. |
| Q40 | Materialize complete IGrouping collections | ❌ | ❌ | ❌ | Aggregate group records are not IGrouping parity. |
| Q41 | Same-provider inner equijoin with exact keys | [✅][D30] | [✅][D30] | [✅][D30] | V1 example31; source/output bounds explicit. |
| Q42 | Same-provider left join with independent right-presence result | [✅][D30] | [✅][D30] | [✅][D30] | V1 example30. |
| Q43 | Composite/nullable join-key parity | ❌ | ❌ | ❌ | Native contract support requires exact row-specific test links. |
| Q44 | GroupJoin / grouped right collections | ❌ | ❌ | ❌ | No complete operator proof. |
| Q45 | SelectMany / cross join / navigation flattening | ❌ | ❌ | ❌ | No complete operator proof. |
| Q46 | Non-equijoin / arbitrary join predicates | ❌ | ❌ | ❌ | Exact equijoin support is narrower. |
| Q47 | Correlated subqueries beyond child/descendant Exists | ❌ | ❌ | ❌ | No general correlated scalar/query support proof. |
| Q48 | Direct child Any/Exists predicates | [✅][D5] | [✅][D5] | [✅][D5] | V1 example5; visibility before root paging. |
| Q49 | Bounded recursive descendant Exists predicates | [✅][D6] | [✅][D6] | [✅][D6] | V1 example6; independent depth/node limits. |
| Q49a | Source relationship predicates nested in child/descendant bodies or composed with root Boolean logic | ✅ | ✅ | ✅ | [Native source matrix](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlSourceCompositionAcceptanceTests.cs):216 checks passed,72/store, across real rows/count/projection consumers. All four child/descendant orientations have positive/negative cases; AND/OR/NOT, hidden intermediate parents, protected errors and empty windows are proved. Core3,851 tests prove independent capability and pre-allocation expansion admission. The separate168-check gate proves source composition with protected cycles and selected-All execution. Exact six-ID cleanup/store and unchanged original fixture verified. [Current counts/hashes/paths](CURRENT_STATUS.md). Freeze this row unless a reproducible bug is found. |
| Q50 | Union with independent source/result windows | ❌ | ❌ | ❌ | Composition Where/order/page examples do not prove set union. |
| Q51 | Concat preserving duplicate rows | ❌ | ❌ | ❌ | No complete native operator proof. |
| Q52 | Distinct selected scalar rows | ❌ | ❌ | ❌ | Selected-value composition is not Distinct. |
| Q53 | Intersect | ❌ | ❌ | ❌ | No complete native operator proof. |
| Q54 | Except | ❌ | ❌ | ❌ | No complete native operator proof. |
| Q55 | Conditional/case/coalesce projection expressions | ❌ | ❌ | ❌ | No general translation proof. |
| Q56 | User-defined functions and extensible method translation | ❌ | ❌ | ❌ | No complete public extension contract. |
| Q57 | Collations, explicit case/culture comparison and LIKE escaping | ❌ | ❌ | ❌ | Existing ordinal subset is not general collation parity. |
| Q58 | Raw SQL/native query with parameter binding and mapped results | ❌ | ❌ | ❌ | Provider methods do not prove safe shared API parity. |
| Q59 | Stored procedures/functions including returned/out values | ❌ | ❌ | ❌ | No portable/shared contract and provider evidence. |
| Q60 | NHibernate HQL language compatibility | ❌ | ❌ | ❌ | CQL DTO/fluent syntax is not HQL. |
| Q61 | NHibernate Criteria/QueryOver compatibility | ❌ | ❌ | ❌ | No equivalent adapter proof. |
| Q62 | Dynamic filters / global tenant filters and explicit bypass | ❌ | ❌ | ❌ | Actor-bound access exists; general named filter semantics unproven. |
| Q63 | Tracking/no-tracking/identity-resolution query modes | ❌ | ❌ | ❌ | Detached copies alone do not implement all three modes. |
| Q64 | Async streaming / IAsyncEnumerable with bounded cancellation | ❌ | ❌ | ❌ | Buffered async execution is separate. |
| Q65 | Compiled queries and parameterized plan caching | ❌ | ❌ | ❌ | No public cache identity/invalidation proof. |
| Q66 | Query tags / native query explain / command inspection | ❌ | ❌ | ❌ | Internal compiler inspection is not a complete public feature. |
| Q67 | Async query execution with cancellation propagation | ❌ | ❌ | ❌ | Healthy async calls pass; complete cancellation matrix needs explicit proof links. |
| Q68 | Selected-value composition: filter, order and page projected rows | [✅][D32] | [✅][D32] | [✅][D32] | V1 examples 32–34; separate source and selected-result windows. |

## Transactions, caching, operations and tooling

| ID | EF / NHibernate feature and required semantics | SQLite | MongoDB | Neo4j | Proving evidence or exact remaining gap |
|---|---|---|---|---|---|
| O01 | Optimistic update/delete conflict detection across clients | [✅][R] | [✅][R] | [✅][R] | Signed native stale-write/delete checks + retained result. |
| O02 | Pessimistic locks / explicit lock modes | ❌ | ❌ | ❌ | No public cross-store lock contract. |
| O03 | Explicit transaction begin/commit/rollback spanning calls | ❌ | ❌ | ❌ | Atomic one-operation graph writes are narrower. |
| O04 | Savepoints and nested transaction behaviour | ❌ | ❌ | ❌ | No complete public contract/proof. |
| O05 | Configurable transaction isolation / repeatable-read snapshots | ❌ | ❌ | ❌ | Moving provider reads are not repeatable isolation. |
| O06 | Ambient System.Transactions / distributed enlistment | ❌ | ❌ | ❌ | No shared cross-store atomicity proof. |
| O07 | Automatic execution strategies with transaction-safe retry policy | ❌ | ❌ | ❌ | Receipt recovery forbids replay; not generic retry-strategy parity. |
| O08 | First-level session cache | ❌ | ❌ | ❌ | No identity-map/session cache certification. |
| O09 | Pluggable second-level entity/collection cache | ❌ | ❌ | ❌ | No invalidation/concurrency/grant-revocation matrix. |
| O10 | Query cache and invalidation | ❌ | ❌ | ❌ | No complete query cache certification. |
| O11 | Context/session pooling and factory lifecycle | ❌ | ❌ | ❌ | Request-owned managers do not prove pooling parity. |
| O12 | Model/compiled-model caching | ❌ | ❌ | ❌ | No public compiled model lifecycle proof. |
| O13 | Connection/command interceptors and diagnostics/statistics | ❌ | ❌ | ❌ | Metering exists; full extensible interception contract unverified. |
| O14 | Multi-tenancy with tenant-bound model/connection/query/cache scope | ❌ | ❌ | ❌ | Actor/catalogue controls are narrower than complete tenant isolation. |
| O15 | Temporal/audit history with as-of query semantics | ❌ | ❌ | ❌ | Per-save deltas/version history are not complete as-of LINQ queries. |
| O16 | Full text/spatial/hierarchy provider extensions | ❌ | ❌ | ❌ | Specify and split concrete supported extensions before implementation. |
| O17 | Validation tooling, design-time services and model diagnostics | ❌ | ❌ | ❌ | No complete design-time toolchain proof. |
| O18 | EF6 databinding/local-view integration | ❌ | ❌ | ❌ | No compatible local observable view proof. |

## COSMIC extensions (separate from the EF/NHibernate parity denominator)

These remain explicit roadmap items; they do not inflate EF/NHibernate parity.

| ID | Feature | SQLite | MongoDB | Neo4j | Evidence / gap |
|---|---|---|---|---|---|
| X01 | Explicit complete-source cross-provider federation/global page | [✅][F] | [✅][F] | [✅][F] | Shared federation native tests + demo; moving reads only. |
| X02 | Protected bounded continuation with fixed scope and budgets | [✅][C] | [✅][C] | [✅][C] | Native 20 requests / 18 settlements / 32 attempts and duplicate controls. |
| X03 | Cursor survives actual process restart with persisted protected key | [✅][P] | [✅][P] | [✅][P] | Same-user Windows DPAPI gate; multi-host topology is separate. |
| X04 | Distributed inner/left join execution over independently scoped sources | ❌ | ❌ | ❌ | [Deferred intent](Evidence/Deferred/CosmicDistributedJoinQuery.cs.txt) has no planner/executor/provider acceptance. |
| X05 | Cross-provider causal agreement / authoritative conflict resolution | ❌ | ❌ | ❌ | Provider priority is not causal proof. |
| X06 | Repeatable distributed snapshots | ❌ | ❌ | ❌ | Moving reads are not a consistent cut. |
| X07 | Multi-host/container/KMS key-ring deployment acceptance | ❌ | ❌ | ❌ | Local same-user DPAPI proof is narrower. |
| X08 | Per-save committed delta/history API (full parent scope) | ❌ | ❌ | ❌ | Parent retained red. X08a is the separately scoped demo summary child; [historical stored-value authorization/decryption, old-key access, retention and tooling remain missing](Archive/COMMITTED_DELTAS.md#values-encryption-and-membership). Full parent completion is not inferred from its child. |
| X08a | Operation-bound committed change summaries: added/removed/modified paths, create/update/hard-delete classification, original-summary recovery | [✅][SV] | [✅][SV] | [✅][SV] | **DEMO** — [Save steps 3–10][SV]: local preview, operation-bound committed path/classification summaries, original summary after update/delete, and foreign-actor rejection. [All-three gate][G]. Full parent X08 remains red. |
| X09 | Web2/web3 migration with resumable verified equivalence | ❌ | ❌ | ❌ | Full migration acceptance remains open. |
| X10 | HyperDrive V2 replica convergence/placement under failures | ❌ | ❌ | ❌ | Broader provider programme; not certified by CQL tests. |

## Investor demo priority and work ordering

**DEMO** tags define the first acceptance set for the same investor walkthrough
on **SQLite, MongoDB and Neo4j**. They were assigned only to ❌ rows; retain the
tag after promotion so the finished demo set stays auditable. Every tagged row
must be green on all three stores before work resumes on untagged backlog.
No broad row is narrowed merely to make the demo green.

| Priority | Tagged ❌ rows | Demo purpose |
|---|---|---|
| 1 — verified | Q14, Q15, Q16, Q17 | First/FirstOrDefault/Single/SingleOrDefault; native cardinality, empty/error semantics and source windows |
| 2 — verified | Q05, Q06, Q07, Q08, Q12b | Null/missing metadata, field comparisons, membership, strings and multi-key ordering |
| 3 — verified: ORM save story | M07, S04, X08a | Generated identity ownership, dirty/no-op saves and committed per-save deltas |

The existing green query/CRUD/concurrency/child-graph features supply the rest of
the demo. Use their existing evidence and examples; do not harden them further.
Federation cursors remain frozen unless a reproducible bug is found. Running
existing green regression checks to validate a necessary ❌ change is allowed;
adding unrelated cursor or green-feature work is not.

Unrestricted POCO mapping (M01), migrations (M26/M27), scaffolding/tooling
(M28/M30), unit-of-work/identity-map programmes and every other untagged row are
**deferred until the demo set is green**. They remain full parity requirements,
not deleted scope. Distributed joins (X04) stay deferred with their preserved
[untested source](Evidence/Deferred/CosmicDistributedJoinQuery.cs.txt).

For each tagged ❌ row: reconcile any existing proving test first, implement only
its actual missing semantics, add the necessary tests/demo, record the native
outcome, and then promote only the proved database cells. A total test count is
never a parity or demo completion claim.

## Evidence links

All ✅ cells above point to actual source demos/tests. The retained all-three
native result [G] is their execution evidence, with source/binary snapshots [GS]/[GB]
and raw-test outcome [GT]. A source file existing on its own is insufficient.

[D1]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L27
[D2]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L50
[D3]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L84
[D4]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L104
[D5]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L129
[D6]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L156
[D7]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L190
[D8]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L207
[D9]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L217
[D10]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L227
[D11]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L246
[D12]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L256
[D13]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L265
[D14]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L275
[D15]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L286
[D16]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L306
[D18]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L355
[D20]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L424
[D22]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L467
[D24]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L510
[D26]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L553
[D28]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L597
[D30]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L640
[D32]: ../../../Examples/CosmicCqlDemo/V1Walkthrough.cs#L683
[R]: ../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlV1Web4NativeTests.RestHttp.cs
[T]: ../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlV1Web4NativeTests.Transports.cs
[F]: ../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlV1Web4NativeTests.Federation.cs
[C]: ../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlV1Web4NativeTests.HostedCursors.cs
[P]: ../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlV1Web4NativeTests.CursorProcess.cs
[G]: Evidence/Verification/tail-elements-native-result.json
[GS]: Evidence/Verification/tail-elements-native-source.json.gz
[GB]: Evidence/Verification/tail-elements-native-binaries.json
[GT]: Evidence/Verification/tail-elements-native.trx.gz
[NC]: ../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlV1Web4NativeTests.cs#L866
[SC]: ../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicScalarQueryTests.cs#L119
[LC]: Evidence/Verification/cursor-lineage-core.trx.gz

## External feature baselines

Reviewed 26 October 2026; this inventory covers the union of the documented feature
families, not a claim that either ORM supplies every feature on every backend.

- [EF Core overview](https://learn.microsoft.com/en-us/ef/core/), [model configuration](https://learn.microsoft.com/en-us/ef/core/modeling/), [querying](https://learn.microsoft.com/en-us/ef/core/querying/), [saving](https://learn.microsoft.com/en-us/ef/core/saving/), [tracking](https://learn.microsoft.com/en-us/ef/core/change-tracking/), [migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/).
- [EF6 overview and feature families](https://learn.microsoft.com/en-us/ef/ef6/).
- [NHibernate5.7 reference](https://nhibernate.info/doc/nhibernate-reference/), [mapping](https://nhibernate.info/doc/nhibernate-reference/mapping.html), [LINQ](https://nhibernate.info/doc/nhibernate-reference/querylinq.html), [transactions](https://nhibernate.info/doc/nhibernate-reference/transactions.html), [fetching/caching/performance](https://nhibernate.info/doc/nhibernate-reference/performance.html).

## Evidence reconciliation log

- 26 October 2026: Q21 promoted on all three stores from existing native LongCount
  demonstrations and the retained exact-domain tests. The 137-row native case
  proves no implicit 25-row cap; three retained unit cases prove explicit Count
  overflow and exact LongCount through Int64.MaxValue. No code or green-feature
  hardening was needed. Q20 now links its Count example rather than LongCount.

- 26 October 2026: DEMO Q14–Q17 promoted on SQLite/MongoDB/Neo4j after native
  examples 47–58 pass (36 new terminal cases; total 405 SDK cases). Shared Core
  tests: 3,767 passed, zero skips; Core net10.0/netstandard2.1 and shipping WEB4
  build pass. Native source snapshots unchanged; seven binary pairs match.
  Eight DEMO rows / 24 database cells remain red. No cursor code was changed.

[E]: ../../../Examples/CosmicCqlDemo/V1ElementQueries.cs
[EU]: ../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicElementQueryTests.cs
[EG]: Evidence/Verification/element-terminals-core.trx.gz

- Demo operator acceptance in progress: new `V1OperatorQueries` examples 59–90
  exercise presence/null/missing, true/false native field comparisons, bounded
  membership (including empty/duplicate/exact Int64 values), ordinal string
  patterns and tied secondary ordering. Q05/Q06/Q07/Q08/Q12b remain red pending
  the three-database result. No green feature or cursor implementation changed.

- 26 October 2026: DEMO Q05/Q06/Q07/Q08/Q12b promoted after examples 59–90
  pass unchanged on all three stores. Native aggregate 270 SDK / 138 HTTP cases,
  526 settlements / 608 physical attempts; seven matching binary pairs. No
  production query-engine, provider implementation or cursor change was needed.
  Three DEMO rows / nine database cells remain red: M07, S04, X08.

[O]: ../../../Examples/CosmicCqlDemo/V1OperatorQueries.cs

- Demo inventory refinement before save acceptance promotion: X08 combined the
  implemented per-save summary feature and the much larger historical-value API.
  Its full parent remains red with every gap intact. New child X08a isolates the
  investor-demo summary contract and starts red. No full-history scope was removed
  or promoted; untagged historical-value work follows the demo acceptance set.

- 26 October 2026: M07/S04/X08a demo acceptance passes on all three stores:
  30 save checks, three generated-ID observations after real native commit,
  no no-op mutation/preparation intents, original summaries retained after
  update/delete. All 12 scoped demo rows are green. X08 full-history parent
  remains red; the full parity/backlog requirements are not complete.
  No production Core/provider/cursor changes were needed for this proof batch.

[SV]: ../../../Examples/CosmicCqlDemo/V1SaveQueries.cs
[SB]: ../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlV1Web4NativeTests.SaveDemo.cs

- Indexed-terminal scope before acceptance promotion: Q19 parent remains red;
  Q19a tracks the zero-based Int32 API being implemented. System.Index/from-end
  overloads remain in the full parent, along with their native window needs.
  Q18 Last/LastOrDefault and Q23 All remain red; two-read count+offset shortcuts
  would not preserve a native source window under concurrent writes.

- 26 October 2026: Q19a integer indexing passes native examples 91–105 on each
  store (405 SDK cases total), 138 HTTP cases and 30 save checks. Core3,782PASS,
  zero skips; net10/netstandard, shipping WEB4 and console builds pass. Source
  unchanged/seven native binary pairs and UnitCore copy match. Q19 parent,
  Q18 Last and Q23 All remain red; demo greens/cursor code were not hardened.

[IX]: ../../../Examples/CosmicCqlDemo/V1IndexedQueries.cs
[IT]: ../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicIndexedElementTests.cs
[IG]: Evidence/Verification/indexed-elements-core.trx.gz

- Native tail selection in progress for Q18/Q19: immutable plan TailIndex and
  TailPaging capability, SQLite nested source SELECT, Mongo same-aggregate
  post-window selection and Neo4j post-window WITH stage. Parent/Last cells remain
  red pending real provider acceptance. Source graph/scalar-order cases are in
  new examples 106–135. No changes to green cursor code; All remains red.

- 26 October 2026: Q18/Q19 promoted only after native examples 106–135 pass on
  SQLite/MongoDB/Neo4j. Core3,797PASS0skip; full native405SDK/138HTTP/30save,
  526settlements608attempts; sources unchanged/sevenpairs/UnitCorecopy match.
  Last/from-end selection remains inside one native operation after its source
  window, including graph and exact scalar-order tests. Q19a remains the prior
  integer child evidence. Q23 All and other red rows remain open; no cursor edits.

[TL]: ../../../Examples/CosmicCqlDemo/V1TailQueries.cs
[TT]: ../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicTailElementTests.cs
[TG]: Evidence/Verification/tail-elements-core.trx.gz

[AU]: ../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicAllQueryTests.cs
[AD]: ../../../Examples/CosmicCqlDemo/V1AllQueries.cs
[AG]: Evidence/Verification/all-predicate-retained-native.trx.gz

- 27 October 2026: Q23a scalar All promoted after 3,807 Core tests and 66 read-only native cases on the exact retained fixture. New Neo4j empty-input aggregate bug reproduced and fixed. Full walkthrough did not complete; its failed fixture/receipts remain preserved. Full Q23 traversal-inside-predicate remains red. Demo greens and federation cursor implementations unchanged.

- 27 October 2026: Q23 internal child-navigation compiler foundation passes 3,811 Core tests and both target frameworks ([tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicAllTraversalTests.cs), [proof](Evidence/Verification/all-traversal-compiler-core.trx.gz)). Public All traversal remains rejected pending post-window native ownership/error semantics and three-provider proof. Q23 stays red; no new green cell.

[AC]: ../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicAllTraversalTests.cs
[ACG]: Evidence/Verification/all-child-native.trx.gz

- 27 October 2026: Q23b direct-child All promoted on three native stores after 114 retained-fixture checks and 3,815 Core tests. Full Q23 remains red for nested/descendant predicates. No cursor/green-feature hardening; source-child lookup logic factored only to share the required terminal membership/visibility owner.

[AN]: ../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicAllTraversalTests.cs
[ANG]: Evidence/Verification/all-nested-native.trx.gz

- 27 October 2026: Q23c nested-child All proved on all three native stores (138 total All checks, 3,817 Core tests). Native lexical depth prevents inner/outer alias capture without intervening bind parameters. Descendant All remains red; full 543-SDK walkthrough pending. No unrelated green/cursor hardening.

[AX]: ../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicAllDescendantTests.cs
[AXG]: Evidence/Verification/all-descendant-native.trx.gz

- 27 October 2026: bounded descendant All implemented with separate scalar admission and protected terminal error envelopes; 198 native All checks and 3,824 Core tests pass. Q23/Q23d stay red for native cycle proof and mixed nested traversal. No green/cursor hardening; no full603SDKwalkthrough success claim.

[CY]: Evidence/Verification/all-cycle-result.json

- 27 October 2026: native All cycle acceptance passes on MongoDB/SQLite/Neo4j (24 checks; [test](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlAllCycleFixtureTests.cs), [TRX](Evidence/Verification/all-cycle-native.trx.gz)). Companion198AllchecksPASS. Exact synthetic records removed, original892A8fixture unchanged. No production/Core code changed; Q23/Q23d remain red for mixed nested traversal.

[CDG]: Evidence/Verification/all-child-descendant-native.trx.gz

- 27 October 2026: child-to-descendant All implemented with independent scalar admission; 3,826 Core tests/bothTFMs/console and285nativechecksPASS. Dedicated27checks (9/store) prove an earlier match and outer Boolean cannot hide later child error. SQLite bug reproduced, corrected at protected scope boundary, then proved read-only against pinned failed fixture before exactcleanup ([reconciliation](Evidence/Verification/all-child-descendant-reconciled.json)). Q23/Q23d remain red for reverse/deeper descendant composition and mixed-scope cycle proof. Full639SDKwalkthroughpending; no cursor/green-feature hardening.

[MCY]: Evidence/Verification/all-mixed-cycle-result.json

- 27 October 2026: mixed child-to-descendant cycle acceptance passes on three native stores:30newchecks; full4testgate315checksPASS. Sharedfixture preserves24self-cycle checks and adds consistent2-nodecycle, actorhidden-child/empty-window controls, exactcleanup. No production/Core change. Q23/Q23d remain red only for relationships inside descendant predicates; full639SDKwalkthroughpending.

[DP]: ../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicAllDescendantTests.cs
[DPG]: Evidence/Verification/all-descendant-predicate-native.trx.gz

- 27 October 2026: Q23d entity nested-relationship acceptance promoted on three stores after390nativechecks/3,831Coretests, including independent admission, bounded preallocation, all visited-scope errors and both nesting orientations. Full Q23 stays red: audit of current projection API found selected/projected-source All missing (Q23e). Parent scope retained; no total All/EF parity claim, no green/cursor hardening, full681SDKwalkthroughpending.
