# CQL v1 production milestone plan

Scope: production CQL v1 over the existing MongoDB, SQLite and Neo4j adapters. Reuse generic HolonManager/Core query planning and native execution. No new query engine, client predicate evaluation, advanced federation, global pages, aggregates or relationship-query language in this milestone series.

1. **Provider execution admission (current milestone):** route CQL native invocations through the existing server-created PreparedStorageExecution policy when present. Bind actual storage/query identities, persist provider attempts before I/O, fail closed on denied/budget/expired admissions, preserve cancellation/errors and trusted local execution. Controlled tests use all three provider identities. This is a prerequisite, not endpoint authorization or production certification.
2. **Authenticated bounded WEB4 boundary:** server-owned provider resolution, actor/public visibility predicates before native reads, request validation, request/result/deadline budgets, accounting/measurement and cancellation. Do not expose raw expressions or trust caller capabilities/adapters. Equivalent negative authorization tests for all three adapters. Tenant-specific and authoritative shared-recipient predicates are deferred explicitly; no tenant-isolation claim.
3. **Native acceptance and operational v1 contract:** real Mongo/SQLite/Neo runs against the same published subset, HTTP integration checks, failure/timeout controls, safe fixture teardown, version/metadata limitations and developer/operator docs. Reuse existing pinned environments and bounded test runners.

Report after milestone 1 before taking on milestone 2. Keep changes focused, use local regressions first, do not install/restore new dependencies unless existing resources require it. Report actual test counts; do not equate milestone completion with complete CQL v1 acceptance. Track resources in ENVIRONMENT_AND_CLEANUP_LEDGER.md.


Milestone 1 completed: [results and resume handoff](CQL_V1_MILESTONE_1.md). Full Core 1,651 passed, including 16 new cases. User subsequently resumed the work.

Milestone 2 completed: [WEB4 boundary and evidence](CQL_V1_MILESTONE_2.md). Full Core 1,676 passed; existing real-Mongo accounting 312 passed plus initialization. [Published HTTP subset/configuration](CQL_V1_HTTP_CONTRACT.md). Milestone 3 native HTTP acceptance remains open; v1 is not yet certified production-ready.
