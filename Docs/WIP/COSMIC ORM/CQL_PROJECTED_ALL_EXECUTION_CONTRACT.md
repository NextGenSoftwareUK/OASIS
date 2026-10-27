# Projected All: native execution acceptance contract

Status: implementation in progress; Q23e stays red for SQLite, MongoDB and Neo4j. This is a working contract, not a supported-feature claim.

## Invariant

All reduces the complete selected sequence after every captured source and selected-result filter, ordering and window. A work limit is an error; it cannot redefine that sequence to a prefix. Native execution decides the Boolean. No DTO construction, compiled user callback or client Enumerable.All may substitute for the native terminal.

## Finding from the existing owners

The existing composition executor enforces a query-wide budget by validating complete detached input/stage proofs and consuming their measured input rows/bytes and stage rows/proof bytes between providers. Its native snapshot owners validate the complete bounded input before selected stages can hide malformed or inaccessible cells. The first projected-All draft returned only bool, which could not carry measured work between targets. The contract now returns CosmicProjectedAllPacket: a native counterexample count and complete source/stage proof. The generic executor validates that proof without constructing DTOs, charges final selected-row evidence against proof bytes, and debits query-wide work before the next provider. Native adapters remain unenrolled.

Implementation entry points:

- [Composition executor](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Queries/CosmicProjectionCompositionExecutor.cs)
- [Composition proof validator](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Queries/CosmicProjectionCompositionProof.cs)
- [Remaining work limits](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Queries/CosmicProjectionCompositionExecutionLimits.cs)
- [SQLite snapshot owner](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/Persistence/Repositories/HolonRepository.Cql.Composition.cs)
- [SQLite stage compiler](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/Persistence/Repositories/HolonRepository.Cql.CompositionSql.cs)
- [MongoDB stage compiler](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoCqlTranslator.Composition.cs)

## Required implementation

1. Share the existing source/stage compiler and snapshot/proof owner; add native reduction after the final selected stage, including the zero-selected-stage case. Do not append a selected filter or consume a selected-stage slot for All.
2. Return acknowledged native reduction with complete bounded source/stage evidence and measured work. Verify native counts, proof shape and source completeness before accepting true or false. Empty input is true only after successful complete validation.
3. Pre-admit every actual target before invoking any provider. Re-admit live identity/capabilities inside the owned invocation; retain cancellation and usage receipt ownership. Never return a successful partial provider list after an error.
4. Debit shared input/proof budgets after each acknowledged provider packet. The second provider cannot receive the original limits again. Zero remaining budget requires proof of empty participation, not an unconditional skipped call.
5. Expose generic execution through HolonManager with the existing V2 requirement. Physical per-provider Booleans remain separate, as with entity scalar results; do not invent cross-provider logical identity semantics.
6. Enroll each native adapter only after its terminal, work evidence and error behavior are proved. Keep existing composition constructor defaults false.

## Acceptance cases required on each database

| Case | Expected evidence |
|---|---|
| Plain scalar and record projection | Native true/false without DTO/callback invocation |
| Empty source and selected Take(0) | Acknowledged true; visited source errors still fail |
| Source window plus selected Where/order/Skip/Take | Final sequence determines truth; counterexample outside final window does not |
| Counterexample after default row page | False or explicit work-limit error, never prefix true |
| Exactly 32 selected stages | All does not consume a 33rd selected stage |
| Missing/null/invalid required selected cell | Error even when later selected predicate/window would hide it |
| Source traversal error outside source page | Existing protected error semantics retained |
| Input/stage/proof limit boundary | Exact limit succeeds; over-limit fails with no Boolean acknowledgement |
| Multiple physical targets | Remaining work debited; later error discards partial success |
| Cancellation, missing acknowledgement and live capability revocation | No successful Boolean or unowned physical attempt |

No Q23e database cell may turn green until the native tests cover these invariants. This work does not reopen green features or federation cursors.

## Core executor checkpoint

3,847 Core tests passed, zero failures/skips; net10.0 and netstandard2.1 builds passed. [Execution tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicProjectedAllExecutionTests.cs) cover empty and 150-row sequences, native false, preflight of all actual targets, shared-budget depletion, absent acknowledgement, impossible native counts, live capability revocation and cancellation. [Proof](Verification/projected-all-execution-core.trx.gz).

HolonManager.ExecuteProjectedAllQueryAsync requires HyperDrive V2. The executor uses CosmicQueryAttemptOwner for physical attempts and retains separate per-provider Boolean values. The existing composition proof validator now permits a zero-stage source only through its internal terminal-source entry; ordinary composition still requires selected stages. This is shared ownership needed by the red terminal, not additional hardening of a green feature.

Remaining: native SQLite/MongoDB/Neo4j reducers sharing each existing snapshot/stage owner, then native acceptance cases above. Unit adapters are not native database proof; Q23e stays red.`nThe terminal scan also consumes stage-work budget: total charged stage rows are selected-stage work plus the complete final selected row count. Final evidence payload is charged to proof bytes. These costs are debited before any later provider dispatch.

## SQLite projected All checkpoint (27 October 2026)

SQLite now implements the projected terminal in the existing serializable selected-query snapshot. Its one shared compiler emits the original source and every selected filter/order/window, followed by a native counterexample count. It validates the complete bounded source before selected stages, and checks terminal stage-work and evidence-byte limits before reducing. No source entity or selected DTO is constructed. Existing composition continues to use the same compiler/read owner.

[Native tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests/CqlProjectedAllNativeTests.cs) prove scalar and record predicates, both paging sequences, empty input/Take(0), 150-row late counterexamples, 32 selected stages, null comparisons, malformed values hidden by later filters, exact input/reduction work boundaries, cancellation before/inside SQL, reuse after cancellation, activation checks and actual shipping-adapter save/read. The 14 new cases and 33 composition regression cases pass (47 total); 9 existing compiler tests also pass. See [native TRX](Verification/projected-all-sqlite.trx.gz), [compiler TRX](Verification/projected-all-sqlite-compiler.trx.gz), and [matching binary cohort](Verification/projected-all-sqlite-cohort.json).

Q23e remains red on all three databases. MongoDB/Neo4j reducers are not implemented yet; complete protected relationship-scope and physical cross-provider work acceptance is still pending. These focused SQLite tests do not prove the entire parent row. Green features/cursors were not hardened; existing composition was rerun because its owner is shared by the new red terminal.

## MongoDB projected All checkpoint (27 October 2026)

MongoDB now compiles the terminal after the complete selected pipeline and performs a native grouped counterexample count. Source validation, selected stages and terminal reduction share one snapshot session and one cumulative 30-second deadline. The same source/stage compiler and snapshot owner serve ordinary selected composition; terminal output evidence includes its identity cost, and terminal scanning consumes stage work. Native counts must agree with the complete selected sequence before acknowledgement.

The [18 new native cases](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/CosmicCqlNativeTests.ProjectedAll.cs) prove a 150-row late counterexample, selected windows/order/filters, empty source/Take(0), 32 stages, missing/wrong-domain required values, exact work boundaries, all six remaining budgets with fresh reuse, null semantics, no DTO construction, cancellation and a real concurrent write between source capture and terminal reduction. In that concurrent test the in-flight result observes the original snapshot and a fresh query observes the update.

All 49 focused tests pass: 18 new terminal cases plus 31 regression cases required by the shared owner. See [native TRX](Verification/projected-all-mongo.trx.gz), [replica initialization](Verification/projected-all-mongo-init.trx.gz) and [matching binary cohort](Verification/projected-all-mongo-cohort.json). The retained replica environment was reused; the owned test database was removed after acknowledged writes/queries, failpoints disabled, owned service stopped, original WEB4 fixture preserved.

Q23e remains red for every database until full acceptance is satisfied. SQLite and MongoDB have native implementations; Neo4j is next. Remaining acceptance includes protected relationship scopes and cross-provider work behavior. This checkpoint does not claim full All/EF parity or harden green features/cursors.

## Neo4j projected All checkpoint (27 October 2026)

Neo4j now reduces the final selected sequence natively from the same captured scalar cells used by its selected-stage pipeline. The compiler emits a terminal count envelope after all source/stage proof rows; it does not re-MATCH mutable holons. A negative native count explicitly signals exhausted stage work. The decoder requires complete source/stage evidence and one valid terminal acknowledgement, verifies counterexample bounds, and charges terminal scanning plus final evidence against remaining work. Both selected composition and All use the same decoder and read owner.

This is captured-source consistency within one native statement; Neo4j does not gain a repeatable-snapshot capability. Existing source consistency admission still applies. Empty input is true only after successful source/error validation.

The [18 focused native cases](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/Neo4jNativeCqlProjectedAll.cs) pass: 17 terminal cases and one scoped shared-decoder composition regression. They cover a 150-row late counterexample, both window sequences, 32 stages, empty input/Take(0), missing/wrong-domain required cells, exact input/scan boundaries, six remaining budgets, null semantics, no DTO construction and cancellation/reuse. Every case scopes reads to its owned creator and fingerprints every pre-existing node/edge/label/property before and after exact fixture cleanup. The retained original WEB4 journal remains unchanged.

See [native TRX](Verification/projected-all-neo4j.trx.gz) and [matching binary cohort](Verification/projected-all-neo4j-cohort.json). Initial test setup failures (default Version=0 and a 150-ID membership predicate exceeding the existing 128-value contract) are preserved in [the initial report](Verification/projected-all-neo4j-initial-test-failures.trx.gz). Test data/scope were corrected without changing either shipping contract.

All three native adapters now implement projected All. Q23e still remains red: protected relationship-scope/cycle acceptance and physical cross-provider work acceptance must run across the three real stores before promotion. The new focused tests do not replace that remaining gate. Green features and federation cursors remain frozen.

## Source relationship grammar dependency (27 October 2026)

The new combined acceptance gate stopped at a builder rejection, not a native Boolean result: ordinary source Where/WhereAnyChild/WhereAnyDescendant previously used scalar-only Compile, while entity All used its separate relationship-enabled compiler. The initial failure is preserved in [TRX](Verification/projected-all-acceptance-initial.trx.gz) with the [pinned fixture journal](Verification/projected-all-acceptance-initial-fixture.json). Creates were acknowledged for the six-ID fixture on all three stores. Records and journal remain preserved; exact native counts must be verified before continuation, and no creation is to be replayed. The fixed pending journal SHA256 is FE53AE55DF84AC35E5A7D0A6787CDC51BB7AC4B5CDFC4207164D38C4B300EA1F. Native assertions after this rejection, including cross-provider budgets and cycle cases, are not yet proved.

This missing source feature is now explicit red row Q49a; Q23e remains red. Core now captures source relationship expressions through CompileSource and requires SourceRelationshipComposition for nested relationships or relationships below root Boolean logic. Existing direct WhereAnyChild/WhereAnyDescendant leaf shapes retain their previous admission. The scalar Compile and selected-result grammar remain independent. The constructor known-feature mask recognizes the new bit; actual providers do not yet advertise it.

Before native dispatch, newly composed source shapes require their independent feature and the shared bounded traversal-expansion owner, then ordinary exact field/traversal/read-access admission. Adding a feature to a planner profile cannot bypass re-admission against the actual native adapter. No native adapter was silently enrolled.

Validation: 3,851 Core tests passed with no failures/skips; net10.0 and netstandard2.1 builds pass. [Source contract tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicSourceRelationshipTests.cs), [TRX](Verification/source-relationships-core.trx.gz). These prove intent capture, independent permission, unchanged leaf admission, literal snapshots, arbitrary callback rejection and expansion bounds. Remaining work is actual provider source-owner enrollment/proof and reviewed continuation of the exact preserved fixture, followed by all 168 assertions. This gate is not complete, and neither Q49a nor Q23e can be promoted yet.
`nReviewed gate76503 verified six owned IDs/store and matching retained actor/version controls without creates, then exposed Mongo source translation failure for newly composed relationships. Capability enrollment was withdrawn pending native implementation. Original journal FE53 remains unchanged; no cycle patch or cleanup occurred. Failure proof: [TRX](Verification/projected-all-acceptance-reviewed-translation-gap.trx.gz). The user approved a dedicated long-lived COSMIC worktree; retained fixture state is being preserved outside disposable TestResults. Q49a/Q23e remain red.
