# CQL ordering and paging

Current documentation: [manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md), [three-provider demos](CQL_DEMO_RUNBOOK.md), [WEB4 v1 contract](CQL_V1_HTTP_CONTRACT.md). Historical checkpoints retain their original scope/counts.

20 October 2026. Common query composition and native SQLite/MongoDB ordering/paging are implemented and verified against clean immutable Core `fe492b2291513002652aa6ac8c81cd5d71f187fb`, which includes query commit2c37117 and the coordinated shared-manager corrections. The parent gitlink and deployment manifest are paired. Publication targets only the existing task branch; no Development deployment is included.

Implemented in Core:

- Immutable `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `Skip` and `Take`, each returning an `OASISResult<CosmicQuery<T>>`.
- Safe scalar selector compilation through the existing canonical/custom-property/typed-metadata path compiler. No getters, delegates or arbitrary methods execute.
- Explicit global Ordering/Paging features plus independent exact canonical/metadata ordering permission. Comparison permission does not grant ordering permission.
- OrderBy replaces earlier ordering keys; ThenBy appends and requires a prior OrderBy. Scope changes and pre-page predicates preserve ordering.
- Sequential paging algebra: Skip adds offsets; Take limits an existing window; Skip after Take reduces the remaining window. Negative counts, offset overflow and excessive page sizes reject explicitly.
- Eight ordering keys, one-million maximum offset and ten-thousand maximum page size.

Example composition:

```csharp
var ordered = CosmicQuery<Holon>.Create().OrderBy(h => h.Name);
if (ordered.IsError) throw new InvalidOperationException(ordered.Message);
var page = ordered.ThenByDescending(h => h.Version).Skip(20).Take(10);
if (page.IsError) throw new InvalidOperationException(page.Message);
```

Fluent forwarding extensions preserve the first `OASISResult` error and use the existing immutable operators. A complete query can also be written as `CosmicQuery<Holon>.Create().Where(h => h.IsActive).OrderBy(h => h.Name).Skip(20).Take(10)`. The compiler still freezes values and rejects executable selectors; forwarding does not add a separate execution path. Both Core targets build and the complete fluent cohort passes1,603/1,603, zero skips (`TestResults/CoreCurrent/cql-window-fluent-core-current.trx`). The separate provider session has since added diagnostic tests; later complete counts must be measured afresh.

Filtering or changing ordering after paging requires a subquery; the current contract rejects that shape with `CQL_COMPOSITION_UNSUPPORTED` rather than moving those operations before paging and changing LINQ meaning. Full operator-pipeline/subquery support remains part of the roadmap. The common planner also rejects global multi-provider pages before any I/O; per-provider offsets cannot implement a global page.

Original absent-API baseline:6/6 failures against the unchanged committed Core (`TestResults/CoreCurrent/cql-window-red.trx`). After implementation, both Core targets build and full Core passes **1,569/1,569**, zero failures/skips (`cql-window-core-current.trx`). This verifies common composition, immutable copies, reset/append behavior, Take/Skip algebra, bounds, safe selectors and exact admission; it does not prove native ordering or paging.

After the global-page guard, both Core targets build and full Core passes **1,570/1,570**, zero skips, in the same report. That uncommitted Core net10 SHA256 is `A3BE0383FC70B975F8EDCC359169B359AB75F0A5F1391CE4D68C8B5A05CA5338`. A concurrent provider programme is now implementing unrelated AvatarManager karma affinity in separate Core files; later combined producers must be measured afresh rather than relabelling this cohort.

SQLite's focused native CQL gate passes **138/138**, zero skips (`TestResults/SQLiteCurrent/cql-window-native-current.trx`). New cases exercise ordinal canonical keys, secondary direction, logical-ID ties, nested hostile metadata keys, Int64 bounds, missing/null/wrong-type buckets, sequential Take/Skip, empty pages without opening the database and page-size versus result-budget behavior. Direct SQLite adapter execution now re-admits against its actual profile before translation; caller-supplied broader capabilities cannot bypass that boundary.

Ascending metadata order is missing, explicit null, valid typed scalar, invalid domain; descending reverses those buckets and the scalar direction. Invalid scalar values are never coerced into a comparison key. Canonical nulls precede valid canonical values in ascending order. The unique logical identity is an ascending final tie-breaker. Ordering applies within each provider's result group; global ordered federation remains open.

MongoDB now generates a single native aggregation pipeline for filtering, projection, computed typed ordering keys, offset and limit. Computed keys wrap the original row instead of overwriting persisted fields. It retains simple collation, the native deadline, streamed payload bounds and explicitly disables sort spilling to disk. The first attempted test run stopped at a local test-variable naming collision before executing native CQL; that compilation error was corrected, and no native fixture was created.

The paired native run then passes MongoDB3/3, controls197/197 and SQLite focused139/139, zero skips. MongoDB checks cover both dictionary layouts, canonical and metadata ordering, one-tick dates, identity ties, sequential paging and empty pages through HolonManager. SQLite's additional scalar gate covers equivalent uppercase/lowercase UUID identities, exact UTC ticks and boolean domain buckets. Its UUID sort key normalizes to lowercase hexadecimal identity before ordering. Reports: `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`, `TestResults/CosmicMongoCurrent/cql-window-controls-current.trx`, `TestResults/SQLiteCurrent/cql-window-native-current.trx`.

That paired cohort consumes Core `83C283D40E147DFA525D34CBC7D0B9FDE427DF6B25C43B9F4EC0F7C0E6F640B4`, which includes the separate uncommitted karma-affinity fix. Its complete Core report independently records1,588/1,588 (`TestResults/CoreCurrent/karma-provider-affinity-final-core.trx`). These are measured working-tree results, not an immutable source/pin publication claim. Mongo27029 is stopped, successful owned native fixtures are removed and SQLite's fixed database is absent after the run.

## Native resource fault acceptance

The expanded MongoDB resource case now exercises a real blocking sort over fourteen8MiB records, with disk spilling disabled. MongoDB's actual error292 maps to `CQL_QUERY_LIMIT`, returns no partial rows and is followed by a successful small query. A server-issued error50, scoped to the owned `CosmicCqlResources` app name and restored in finally, maps to `CQL_QUERY_TIMEOUT` with the real exception retained.

The first timeout attempt exposed a harness configuration gap: CQL mode had not enabled the native test commands already used by other fault-test modes. That failed report is preserved at `TestResults/CosmicMongoCurrent/cql-timeout-original-failure.trx`, SHA256 `A513E8EB09F79280FCE35E2477C3041A66DDEA67E6F1A5D4BE133266D5CC4A3B`. The original14-row fixture was independently verified against names, complete8MiB ASCII payloads, audit/revision/relationship state and compact full-body SHA inventories, then re-read before exact deletion. An initial inspection correctly refused its mistaken embedded-children array expectation: saveChildren=false stores BSON null; source mapping and the exact expectation were corrected without changing production data. Reconciliation passes1/1 and the corrected native suite passes3/3, zero skips.

The reviewed inventory is only1,725 bytes (`TestResults/CosmicMongoCurrent/cql-timeout-fixture-reviewed.json`); no112MiB payload archive was retained. The final owned Mongo process is stopped and successful fixtures are removed. Core producer `B2D48154411A19E1BFB10A3B40FF06DCCC070B619BD3514118001B69D81C0303` remains the measured uncommitted cohort, not an immutable pin claim.

The concurrent shared-settings correction has now passed complete Core1,605/1,605 (`TestResults/CoreCurrent/karma-settings-scope-core-cold-create.trx`) and scoped DynamoDB400/400 (`TestResults/DynamoHistoryCurrent/dynamo-settings-scope-all-current.trx`), zero skips. Settings metadata writes explicitly leave child membership unselected and preserve full persistent headers; the child-observation guard remains unchanged. Native provider acceptance and its scoped Core commit are the remaining dependency before the final combined CQL producer/pin cycle.

Required next work:

- SQLite's [native work, deadline, record-size and streamed payload owner](SQLITE_CQL_RESOURCE_BOUNDS.md) passes focused5 and the final complete SQLite1,145 gate, including original oversized-query and connection-state restoration acceptance.
- Extend the declared domains and native scenarios as the wider CQL roadmap adds operators/types; the selected ordering/window/resource domains and complete affected suites pass the final pinned cohort below.
- Preserve the now-verified native work, payload, cancellation, record-size, memory-exhaustion and timeout classifications through the final immutable producer/consumer cohort.
- Prevent a global multi-provider page from being implemented as independent per-provider offsets. Implement authorized federation/merge and conflict handling before advertising that scope.
- Extend native repeated-window, concurrent-write and composition regression acceptance beyond the first passing cases above.
- Retain the separately discovered SQLite Akashic history persistence gap as open provider work; this increment does not claim to close it.
- Retain the full Stage8 scope: stable pagination under a declared consistency model, continuation/snapshot behavior, relationships, projections/aggregates, authorization-aware provider discovery and wider adapters.

Offset pages alone do not provide a snapshot across concurrent writes. That distinction must remain explicit in the eventual paging contract and acceptance evidence.

## Final immutable-source acceptance

Build epoch: parent351fa086d0a5809dd497ba41587728afe4ffa444; clean Corefe492b2291513002652aa6ac8c81cd5d71f187fb. The exact report, source and assembly SHA bindings are retained in `TestResults/CosmicMongoCurrent/cql-window-final-artifacts.json`; all17 declared provider/consumer copies match. Both Core targets build.

| Gate | Passed / total | Report |
|---|---|---|
| Full Core | 1,606 / 1,606 | `TestResults/CoreCurrent/cql-window-pinned-core-current.trx` |
| Full Shared | 925 / 925 | `TestResults/SharedCurrent/cql-window-pinned-shared-current.trx` |
| MongoDB controls | 197 / 197 | `TestResults/CosmicMongoCurrent/cql-window-pinned-controls-current.trx` |
| Native MongoDB | 3 / 3 | `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx` |
| Full native SQLite | 1,145 / 1,145 | `TestResults/SQLiteCurrent/cql-window-pinned-full-current.trx` |

All gates have zero failures/skips. Native fixtures are removed, Mongo27029 is stopped and the disposable SQLite test database is absent after execution. No runtime or environment download/duplicate was created.

| Producer | SHA256 |
|---|---|
| Core net10.0 | `289FF16468E8418CB1656A4C1434E36D1DEE876423295DC31AF82182B326A884` |
| Shared | `4C2E7B67A136B27DD1EC4BF699F8A9235A6945DFB197D089E3E5FEED81B6F675` |
| MongoDB | `BBC49C9AB954FB08542FD49C006292BF4E8D05E5D6876A37BA6CFB5B7DFE8496` |
| SQLite | `EDA4B7F6330A5AB8D03C74094F368B24F4EE24A9F5C36C9107D7960B349F2D86` |

Complete Stage8 remains open: authenticated discovery/routing, relationships, other adapters, richer numeric/schema domains, projections/aggregates, global federation, subqueries and snapshot/continuation semantics. Offset-page behavior remains explicitly live rather than a cross-request snapshot guarantee.

## Neo4j ordering/paging candidate — 21 October 2026

The initial Neo profile correctly rejects ordering/paging in two reproduced controls (TestResults/CoreCurrent/neo-cql-window-red.trx). The current candidate adds exact per-path ordering permission, the existing missing/null/valid/invalid metadata buckets, canonical null ranking, direction per key and an ascending normalized logical-ID tie-breaker. Skip/Take uses the common immutable window algebra and fetches at most the smaller of the requested page and the result budget plus one. A zero-length window completes after admission/translation without opening a native session.

Projection validation is outside the page branch in a single native CALL/UNION ALL query. It must produce an error even when an invalid record lies outside the user predicate or beyond the requested offset. Windowing the previous invalid-first result directly would incorrectly hide corruption; the native candidate explicitly tests Skip(1000) after removing a projection.

String order follows binary Unicode scalar/codepoint order, consistent with the existing SQLite UTF-8 binary and MongoDB simple native ordering domains; it does not promise CLR culture or UTF-16 StringComparer.Ordinal sort order for supplementary characters. The native candidate distinguishes U+E000 from U+10000. Neo4j documents [Unicode codepoint comparisons](https://neo4j.com/docs/cypher-manual/5/functions/string/) and [post-union CALL subqueries](https://neo4j.com/docs/cypher-manual/4.4/clauses/call-subquery/). These source statements guide the implementation; native tests must still verify the retained server's actual behavior.

Producer/control dispatch was rejected by the occupied shared lease before execution. Tests now include composed order/page admission, empty-window no-I/O, canonical and metadata buckets/directions, tie identity, sequential windows, explicit page budget and offset-independent validity. Current implementation remains uncommitted and unverified pending the peer's live native run.

### Verified Neo4j window gate

The candidate now passes completeNeo300/300 controls, pairedShared943/943 and the expanded nativeNeo64/64, zero failures/skips. Reports are TestResults/CoreCurrent/neo-cql-window-final-controls.trx, TestResults/SharedCurrent/neo-cql-window-final-shared.trx and TestResults/Neo4jCurrent/neo4j-cql-regression-current.trx. Native execution takes31seconds and covers all new window assertions plus existing conditional/preparation/ordinary/delete gates. Unicode codepoint order, exact domain buckets/directions, identity ties, sequential window algebra, explicit page budgets and offset-independent invalid projection detection are proved against the retained server.

Twenty-three declared producer/consumer copies match (TestResults/Neo4jCurrent/cql-window-final-artifacts.json). Core remains97A343EF51A6A4BF8942250BB2995F5A4E37CD51E0129EEA07F5729B379CAD11 from unchanged committed c4a89b1; native Shared914CD096FC9A73F8A24C68B3B4B9CCB1EE37A7E37B56BFF4FFC0EF75FDFE0184. The fixture manifest is absent and the exact retained container is stopped. No migration/schema change is needed for these sort keys, and no new runtime/environment/download/deployment is included. Broader numeric/enum/date profiles, resource stress, global ordered federation and snapshot continuation remain open.


## Three-provider supplementary-character boundary — 23 October

The fresh Core d71eebd cohort passes Mongo64, joint Neo4j/WEB4 114 and SQLite native205, zero skips. Mongo's existing native fixture now proves metadata Unicode scalar order in both document and array-of-pairs dictionary representations; Neo adds descending and Skip/Take controls to its existing ascending boundary. SQLite's new native case proves canonical Name and metadata ordering in both directions plus a descending page.

The explicit values include null, ASCII A, U+E000 and U+10000. U+E000 precedes U+10000 in ascending Unicode scalar order, while CLR UTF-16 ordinal order reverses them. This is an explicit expected order, not an oracle based on CLR string sorting. Metadata null/missing ranks retain their existing contract. These controls establish this boundary for current row ordering; they do not advertise Min/Max, invalid UTF-16 acceptance or unrestricted aggregate/string collation support.
