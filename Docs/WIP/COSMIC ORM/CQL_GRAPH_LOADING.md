# CQL child graphs — implementation checkpoint

23 October 2026. Graph composition/admission is implemented in Core; native graph loading and typed graph publication remain open. The three native adapters still advertise leaf execution only and reject graph requests before adapter I/O. This is not a claim that Include/relationship queries are ready to demo.

## Implemented contract

`CosmicQueryGraphOptions.Create` returns an immutable validated value. Options select recursive versus direct children, maxChildDepth, a defined HolonType, total node and payload budgets. Roots are depth zero; depth one includes immediate children; zero maxChildDepth means recursion within the resource budgets. Nonrecursive traversal never expands grandchildren. Invalid negative/oversized bounds and undefined child types return CQL_GRAPH_OPTIONS_INVALID.

`WithChildren(options)` returns a new query. Filtering, scope changes, ordering and Skip/Take retain that exact immutable shape; the source query is unchanged. Loading children after selecting a root page is valid. `WithoutChildren()` explicitly returns leaf shape. Fluent error propagation preserves the original error rather than replacing it with a successful graph query.

Graph requests require GraphExpansion capability at planning and again against the actual execution adapter. A caller-supplied capability profile cannot bypass that check. No native adapter claims the capability in this checkpoint. v1 HTTP DTO/behavior is unchanged.

## Owners and remaining root work

1. Native expansion must run inside the provider's admitted execution and coherent native read owner. Recursively invoking the public leaf adapter would open independent read transactions and can splice incompatible graph revisions; avoid that design. SQLite/MongoDB/Neo4j must each provide their supported native snapshot semantics or explicitly reject an unsupported guarantee.
2. Query root paging precedes expansion. Node/payload budgets include roots and returned child occurrences across all selected providers. Core must validate graph shape and detach every node before publication, using bounded heap traversal and cancellation. Existing StoredRecordJson projection excludes navigations, so the present flat executor clone cannot preserve graph edges by itself.
3. HolonManager-Cql.V2.cs currently calls typed materialization with loadChildren:false. Graph publication must pass the selected depth/type policy to the common typed owner, decrypt before mapping and accept correct graph baselines.
4. Reuse persisted child-role manifests. HolonGraphProjection validates manifest owner, unique child identity and matching ParentHolonId; it does not itself fetch bounded native query results. Legacy records without a role manifest use parent links. Do not silently reinterpret missing/mismatched manifest members as a complete graph.
5. HolonChildRoleHydration.ApplyFullySelected is valid only for a fully selected immediate membership. Depth/type/visibility-limited selections must retain the stored manifest and incomplete observation. HolonChildRoleObservation.AssertCanReplace prevents a child-selecting save from overwriting an unobserved membership; keep that invariant.
6. Hosted access policy must constrain each child/relationship match inside native reads. SDK query options are not authorization. A root's arbitrary business predicates cannot simply be reused as the child visibility policy.

## Verification

Core focused graph options: 19/19 passed. Full Core: 1,698/1,698 passed, zero skipped; both Core target frameworks build. Cases cover depth/recursion, invalid budgets/types, immutable composition, root paging, first-error preservation and actual-adapter readmission before I/O. Existing native v1 cohort: 65/65 passed, zero skipped, against all three stores. Its 97-attempt/98-settlement HTTP matrix remains intact. This is leaf regression evidence, not native graph acceptance.

Core task commit: `014987d`. Reports: `C:/Source/OASIS/TestResults/CosmicCqlDemo/cql-graph-contract-red.log`, `cql-graph-contract-tests.log`, `cql-graph-core-tests.log`, `cql-graph-core-current.trx`, `cql-graph-native-tests.log`; native TRX remains `TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx`. Core producer/unit-test/native consumer SHA256 is `6B2D866F465C68D921EA0E526A3E09AB5164AD6E40F754166253AB9214BD2B3A`. Initial red compile established the missing contract; green builds corrected the portable target's Enum name binding at the owner. Existing production feature scope is not expanded by passing these contract tests. Native hosts stopped and successful fixtures/manifests removed.

[Completion gates](CQL_COMPLETION_ROADMAP.md), [architecture](CQL_ARCHITECTURE.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
