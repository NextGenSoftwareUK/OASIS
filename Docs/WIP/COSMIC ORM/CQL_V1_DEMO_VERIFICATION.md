# CQL v1 developer demo verification

## 23 October refresh

Fresh dedicated runner23487 passed Core1799/joint96 and console demo1, zero skips. The result written2026-10-23T12:58:00Z reports15SDK/15HTTP examples,131settled operations and130provider attempts. Example5 exercises visible-child existence on every provider; browser/Swagger presets include it. Owned fixtures removed and reusable services stopped. Report freshness and both example counts are enforced; prior22October evidence below is historical.


22 October 2026. This increment adds runnable examples and presentation over the existing query implementation; it does not widen the production v1 contract.

## Demonstrations

- `V1NativeQueries.cs`: nine checked LINQ-style SDK queries across MongoDB, SQLite and Neo4j.
- `V1Walkthrough.cs`: nine checked HTTP queries over the shipping WEB4 controller/compiler, JWT validator, prepared admission and durable accounting pipeline.
- `/demo`: spaced editable requests, expected-result comments, provider/identity selectors and nine presets. Includes protected OR, paging, ordinal strings, null, typed metadata and exact Int64.
- `/swagger`: prefilled shipping DTO and required operation header. A real browser execution returned HTTP 200, B-owned/C-public/D-owned and settled usage. Dispatch-time synthetic authentication avoids a spec-initialization race; the pinned Swagger UI receives a single-line function declaration compatible with its initializer.

The host uses controlled avatar-directory/reservation authority and synthetic identities over real databases. It is not a full production application boot, production account or charge. The existing SQLite coffee example retains Save/tracking/delta/conflict demonstrations.

## Results

| Check | Result |
|---|---|
| Normal native cohort, without demo mode | 65/65 passed; zero skipped |
| Three-provider demo fixture | 1/1 MSTest; nine SDK + nine HTTP demonstrations within it |
| Demo page/API contract checks | 11 grouped checks passed, all three providers |
| Browser | Guided native query and Swagger returned 200; settled usage |
| Existing SQLite coffee console | Query/save/tracking/delta/conflict assertions passed |
| Existing SQLite coffee API | Six checks passed |
| Presentation JavaScript / documentation links | Node syntax check and manual/architecture/runbook local links passed |
| Deployment dependency manifest | Seven Dockerfiles and parent gitlinks valid |

Final interactive fixture: 119 durable provider attempts and 120 settled operations, including its pre-existing acceptance matrix, walkthrough and interactive/API verification. These are operation counters, not extra test-case counts. SDK demonstrations are trusted local queries outside hosted metering. The normal cohort separately verifies the original 97-attempt/98-settlement matrix.

Compact evidence is retained at `C:/Source/OASIS/TestResults/CosmicCqlDemo/v1-verification.json`, `v1-walkthrough.txt`, `v1-http-checks.json`, `v1-swagger-proof.txt`, `v1-browser-proof.png` and `coffee-regression.log`. Native TRX files remain under `TestResults/Neo4jCurrent/`; the compact JSON records their digests. Core producer SHA256 `51DB795347AF0E09AED9F4D22874EBDB0EE7D79DF2440E84CF0173B73ED2503E`; Shared producer `513DCE2B9DBDE31BD89D3DADC4B7DC3F151B6503FE00AF8D18131A58FA824121`. Runners verified consumer copies against producers.

All owned hosts stopped; ports 5087/5088/27029/17687 clear. Owned Mongo databases, Neo4j roots, SQLite files and readiness/ownership manifests were removed only after settlement and process teardown. Shared runtimes, cache, fixed outputs and stopped services remain reusable. [Resource lifecycle and sizes](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

[Run the demo](CQL_DEMO_RUNBOOK.md) · [Manual](CQL_MANUAL.md) · [Architecture](CQL_ARCHITECTURE.md)


## Selected HTTP and demo gate verified — 23 October 2026

The retained fixture was reconciled by the exact AC9E6B... journal and76BF29... native evidence:180settled operations/175matching receipts,411acknowledged scalar writes and all146owned holons in each of3stores verified before cleanup. Original journal and438snapshot hashes are retained. No queries were replayed. The stale Swagger assertion now checks Min and its bounded Int64 metadata selection.

Fresh combined verification **passes175native regression tests, zero skips (3m7s)**, plus **36SDK queries and36HTTP examples** (12perprovider) on MongoDB, SQLite and Neo4j. The demo reports **216settled operations and211physical provider attempts**. Signed selected queries verify exact negative Int64 values beyond2^53, visibility before reduction (hidden rows contain stronger extrema), source windows, nullable empty inputs and no replay. Full Core/HTTP unit evidence remains2,117passing/0skip, including11new endpoint controls. Browser JavaScript syntaxcheck passes.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx; TestResults/CoreCurrent/cql-selected-scalar-http-current.trx; TestResults/CosmicCqlDemo/v1-demo-result.json, v1-walkthrough.txt, selected-http-native-final.log and selected-http-native-artifacts.json. Five producer/consumer DLL pairs agree (Core109677, shared151E8DC5); the existing runner verified fixture ownership, stopped owned services and removed the exact SQLite/wal/shm/marker after process exit. All four paths and27029/17687listeners were explicitly absent. No runtime/download/new environment was introduced.

This supersedes the pending native/demo checkpoint above. The selected-extrema HTTP/demo gate is complete; the full CQL/ORM roadmap remains open, including Neo4j UTC-date enrollment and strict blocked-I/O interruption, Sum/Average/GroupBy and later federation/query/ORM acceptance.
