# CQL v1 native acceptance — 22 October 2026

Implementation and automated acceptance are complete for the [documented WEB4 v1 subset](CQL_V1_HTTP_CONTRACT.md) over MongoDB, SQLite and Neo4j. This is not completion of the larger CQL/ORM roadmap or a production deployment. Approved commercial rates, deployment configuration and application-specific access policies remain operator responsibilities.

## Verified behavior

One combined HTTP matrix runs the shipping controller, request compiler, endpoint, JWT validator and durable usage/admission middleware against all three actual native providers and a real Mongo execution outbox. Avatar-directory resolution and the reservation/settlement authority are controlled test components; no paid account or live billing transaction is involved. The entire production application's startup/avatar-directory integration is not booted by this fixture.

The matrix verifies native owner/public restrictions before paging, three independent subjects, spoofed sharing metadata, an OR group that cannot escape access restrictions, nested metadata, every advertised operator class, String/Int64/Boolean/Guid metadata, invalid typed values, exact integers above 2^53, all three ordering choices, default-provider resolution, empty results, literal injection resistance, metadata output and request limits. Authentication rejects anonymous callers, bad signatures/issuers, expired tokens and mismatched signed sub/id claims before reservation. Duplicate operation keys cannot start execution again. Rejected model binding settles with explicit no-provider-execution evidence.

The final matrix performs **97 native database query attempts** and verifies **98 settled operations**, including one model-binding rejection. Those are operations within one test method, not 98 separately discovered test cases. Attempt metrics come from durable provider attempts; a synthetic no-execution settlement receipt is not counted as native I/O.

## Results and provenance

| Check | Result |
|---|---:|
| Full Core, including three new string-operator regressions | 1,679 passed, zero failed/skipped |
| Native cohort: 64 existing controls plus the combined HTTP matrix | 65 passed, zero failed/skipped |
| Reviewed historical fixture reconciliation | 1 passed, zero failed/skipped |
| Existing real-Mongo accounting protocol, independently configured | 312 passed, zero failed/skipped; separate initialization 1 passed |
| WEB4 application build | Zero errors; 253 existing warnings |

Core producer SHA-256 `51DB795347AF0E09AED9F4D22874EBDB0EE7D79DF2440E84CF0173B73ED2503E` matches eight consumers. Shared and all three provider producer DLLs match the native test's loaded copies. Both Core target frameworks build; hosted admission remains net10-specific under the existing framework contract.

Exact report/source hashes, runtime copy checks, fixture evidence and resource measurements: `C:/Source/OASIS/TestResults/CosmicCqlV1Web4/milestone-3-evidence.json`. Final reports: `cql-v1-core-certified.trx`, `TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx`, and `TestResults/SubscriptionUsageCurrent/usage-real-replica-current.trx`. The final WEB4 build log is `api-certified-build.log`.

Expanded native checks found a genuine transport defect: single-argument StartsWith/EndsWith expressions selected culture-dependent .NET semantics and were rejected by Core's ordinal contract. The HTTP compiler now emits explicit ordinal calls, including Contains, at the expression owner. Three focused compiler cases and the actual native matrix lock this behavior in. No query-engine fallback or client evaluation was added.

The original failing 63/65 report remains preserved by digest. Its second failure was the next test correctly refusing the retained five-root fixture. A separately locked reconciliation verified the exact manifest, all root identities/content across the three providers, journal-derived operation identities and settled outbox states before removing only owned resources. It did not replay work or infer unknown completion. Earlier successful reports, the reconciliation evidence and the correction of an evidence-only operation counter remain retained.

## Scope and operations

The v1 HTTP contract is deliberately narrower than the generic local CQL engine: one active supported provider, canonical leaf fields, bounded scalar predicates and sorting/pages. Shared-recipient/tenant-specific authorization, joins/relationships, aggregates, advanced federation/global paging and additional scalar families remain outside this release. Existing demos and local generic CQL remain available. Cancellation is cooperative, not a hard five-second interruption SLA. Uncertain attempts retain the existing reconciliation/no-replay contract.

Owned Kestrel hosts exited; Mongo and Neo services stopped, with ports 27029/17687 clear. Successful fixture databases/Neo IDs were removed; exact SQLite files were removed after test-process exit, with cleanup evidence preserved. Existing shared runtimes, outputs, cache and stopped Neo data/log volumes remain reusable. No new environment, container/image, SDK or native runtime was installed. Native verification required no new cached package/version directory.

[Developer/operator guide and C# HTTP example](CQL_V1_OPERATIONS.md), [native dependency inventory](CQL_V1_NATIVE_DEPENDENCIES.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
