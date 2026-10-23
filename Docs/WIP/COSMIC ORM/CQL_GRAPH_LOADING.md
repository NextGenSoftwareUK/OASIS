# CQL child graphs — implementation checkpoint

23 October 2026. Core graph composition/publication and native SQLite SDK expansion are implemented. MongoDB and Neo4j graph readers remain open and reject graph requests before adapter I/O. Relationship predicates and authenticated HTTP graph transport remain open.

## Implemented contract

`CosmicQueryGraphOptions.Create` returns an immutable validated value. Options select recursive versus direct children, maxChildDepth, a defined HolonType, total node and payload budgets. Roots are depth zero; depth one includes immediate children; zero maxChildDepth means recursion within the resource budgets. Nonrecursive traversal never expands grandchildren. Invalid negative/oversized bounds and undefined child types return CQL_GRAPH_OPTIONS_INVALID.

`WithChildren(options)` returns a new query. Filtering, scope changes, ordering and Skip/Take retain that exact immutable shape; the source query is unchanged. Loading children after selecting a root page is valid. `WithoutChildren()` explicitly returns leaf shape. Fluent error propagation preserves the original error rather than replacing it with a successful graph query.

Graph requests require GraphExpansion capability at planning and again against the actual execution adapter. A caller-supplied capability profile cannot bypass that check. SQLite now advertises it; MongoDB and Neo4j do not. v1 HTTP DTO/behavior remains leaf-only.

## Detached and typed publication

`CosmicQueryGraphSnapshot` uses heap traversal to preserve edges without recursively serializing navigations. A single node/UTF-8 payload budget covers all roots and selected provider groups; counts are checked before child-array allocation. Returned records, metadata and child lists are detached from adapter objects. Root result count remains distinct from total graph nodes.

Empty/repeated/cyclic identities, null children, wrong parents, unrequested types, excess depth, unstable child-list counts and foreign/contradictory role manifests reject the entire execution. Expanded all-type membership must match its persisted manifest; type-filtered or depth-boundary views retain that manifest without claiming a complete observation. Budget failures return CQL_QUERY_LIMIT, invalid provider graphs CQL_PROVIDER_RESULT_INVALID. No partial grouped result is published.

The typed manager now passes graph depth/type selection into the existing materializer. V2 collection materialization admits/decrypts the selected forest before invoking typed root constructors or registered child-role factories, then uses the common tracking owner. Existing tracking already decrypted children before baseline capture; the newly proven defect was earlier callback timing, not absent child decryption. A rejected encrypted child previously could run a typed root constructor. The new red test demonstrates that ordering and now passes with zero constructions.

Registered child factories receive admitted metadata and retain identity/revision/manifest checks. Type-limited views cannot issue a child-selecting save over unobserved membership; root-only saves preserve omitted links. A depth-one save updates selected root/child values without replacing an unloaded grandchild manifest.

## Owners and remaining root work

1. Native expansion must run inside the provider's admitted execution and coherent native read owner. Recursively invoking the public leaf adapter would open independent read transactions and can splice incompatible graph revisions; avoid that design. SQLite/MongoDB/Neo4j must each provide their supported native snapshot semantics or explicitly reject an unsupported guarantee.
2. Query root paging precedes expansion. Core publication now enforces total graph budgets and detached edges, but native readers must bound database work, row decoding and payload accumulation before returning their graph.
3. Typed graph publication is implemented and controlled-test verified. Native acceptance must prove custom roles, independent key contexts, stale-client writes and partial selections using actual provider responses.
4. Reuse persisted child-role manifests. HolonGraphProjection validates manifest owner, unique child identity and matching ParentHolonId; it does not itself fetch bounded native query results. Legacy records without a role manifest use parent links. Do not silently reinterpret missing/mismatched manifest members as a complete graph.
5. HolonChildRoleHydration.ApplyFullySelected is valid only for a fully selected immediate membership. Depth/type/visibility-limited selections must retain the stored manifest and incomplete observation. HolonChildRoleObservation.AssertCanReplace prevents a child-selecting save from overwriting an unobserved membership; keep that invariant.
6. Hosted access policy must constrain each child/relationship match inside native reads. SDK query options are not authorization. A root's arbitrary business predicates cannot simply be reused as the child visibility policy.

Native source notes: SQLite CQL uses a dedicated bounded native reader, not its ordinary whole-table AttachLoadedChildren path. Mongo ExecuteNativeCqlAsync currently aggregates without a session and maps children:false. Neo CQL already owns a read transaction but returns one SnapshotJson per selected root. Reuse their native mapping/transaction owners while adding bounded expansion, rather than recursively invoking public load/query methods.

## Native SQLite checkpoint

Root paging and descendant reads share one deferred Serializable SQLite transaction and one native instruction/deadline/cancellation budget. Read-only connections work. An existing Serializable caller transaction is reused without committing it; weaker caller transactions reject with CQL_CONSISTENCY_UNSUPPORTED. A WAL concurrency test changes a child after the root page and proves that the graph retains the original snapshot.

The shared HolonGraphQueryExpansion assembler charges nodes and UTF-8 payload when discovered, including queued records. SQLite predicates select parent, type and persisted membership natively, batch membership lists at 128 IDs, cap each read with the remaining node budget plus one overflow probe, and check streamed bytes before materialization. All-type missing members, contradictory links and cycles fail the whole query. Persisted role order is retained; records without role manifests use parent links and deterministic GUID ordering. These older record formats do not introduce changes to legacy Hyperdrive.

Private selection evidence prevents partial or leaf CQL views without manifests from becoming relationship replacements through local AcceptChanges, including after forged subset-manifest JSON. Root-only saves remain permitted. This fixes a proven ownership gap for both raw and typed query results; user-editable metadata cannot grant complete observation.

Acceptance: full Core 1,722/1,722, Shared 954/954 and SQLite integration 1,218/1,218, zero skips. These include 15 native SQLite graph cases and 9 shared expansion cases. Logs: TestResults/CosmicCqlDemo/{core-graph-full,shared-graph-full,sqlite-graph-full,shared-cql-graph-tests,sqlite-cql-graph-tests}.log. Core ownership regressions first failed in cql-legacy-selection-red.log. This certifies SQLite SDK graph expansion, not completion of the three-provider roadmap or HTTP graphs.

## Earlier composition checkpoint verification

Core focused graph options: 19/19 passed. Full Core: 1,698/1,698 passed, zero skipped; both Core target frameworks build. Cases cover depth/recursion, invalid budgets/types, immutable composition, root paging, first-error preservation and actual-adapter readmission before I/O. Existing native v1 cohort: 65/65 passed, zero skipped, against all three stores. Its 97-attempt/98-settlement HTTP matrix remains intact. This is leaf regression evidence, not native graph acceptance.

Core task commit: `014987d`. Reports: `C:/Source/OASIS/TestResults/CosmicCqlDemo/cql-graph-contract-red.log`, `cql-graph-contract-tests.log`, `cql-graph-core-tests.log`, `cql-graph-core-current.trx`, `cql-graph-native-tests.log`; native TRX remains `TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx`. Core producer/unit-test/native consumer SHA256 is `6B2D866F465C68D921EA0E526A3E09AB5164AD6E40F754166253AB9214BD2B3A`. Initial red compile established the missing contract; green builds corrected the portable target's Enum name binding at the owner. Existing production feature scope is not expanded by passing these contract tests. Native hosts stopped and successful fixtures/manifests removed.

[Completion gates](CQL_COMPLETION_ROADMAP.md), [architecture](CQL_ARCHITECTURE.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Earlier publication checkpoint evidence

Core task commit `945386c`: focused graph execution/ownership22/22; full Core1,720/1,720; native leaf cohort65/65, all zero skips. Both Core targets build. The initial12 graph-publication tests all failed before the edge/budget repair; the expanded20-case cohort then exposed two missing-manifest failures while confirming existing encrypted-child tracking. A separate typed-constructor timing case failed before the early-admission change. Final22 cases also cover a registered encrypted typed-child factory, 1,200-edge depth, cross-provider node budgets, UTF-8 payloads and partial-save ownership.

Reports under `C:/Source/OASIS/TestResults/CosmicCqlDemo/`: `graph-publication-red.log`, `graph-ownership-red.log`, `graph-authentication-red.log`, `graph-factory-tests.log`, `graph-publication-core.log`, `graph-publication-core-current.trx`, `graph-publication-native.log`. Native TRX stays in `TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx`. Certified Core producer SHA256 `C7CC3EB752DC27E0C5603D48CBE75488F91D9E4BFEE91D528BAE4F7428721C98`; native runner verifies consumer copies. These are Core publication and existing v1 regressions, not native graph-loading certification.
