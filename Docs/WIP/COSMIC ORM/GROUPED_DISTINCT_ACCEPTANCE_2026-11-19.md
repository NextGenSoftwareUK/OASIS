2026-11-19 DEV CI GATE ROOT CAUSE / MONGO-ONLY QUALIFICATION: Dev38d4320e automatic protocol run44600997848 failed before build because validate_subscription_rollout_gate.py still required old bare AddHostedService and single reconciliation argument. Shipping Startup already enforces subscriptionUsageEnabled AND HostedMongoWorkerControl.ReconciliationMayStart, plus maintenance controls for all3 workers. Correct validator to require these actual stricter contracts; no worker/runtime/DNA change. Six NEW RAM-only stdlib regression controls pass, including rejecting either missing gate, OR, each unguarded maintenance registration, unconditional block and telemetry escape; exact Dev Startup accepted. Mongo-only retry44602869946/job135838671694 on fc61c8babbd68ba25e523304acd9324ee8665cf3 now in_progress; distinct owned external Mongo DB, exactly1 expected case, no Neo container/prior accepted2 rerun. Railway57e3b7cd-61b5-4f0e-a934-5ac7b38ede33 remains QUEUED; no duplicate deploy. Atlas0/PIPER0/local downloads/builds/databases/archives0; temporary indexes self-remove.

# Grouped distinct counts — scoped acceptance

Updated19 November2026. This is an engineering acceptance record, not full EF/NHibernate parity or a live deployment claim.

## Verified cohorts

| Cohort | Exact source | Evidence |
|---|---|---|
| Core intent and mixed reducer:12 cases | Parent c82cf9ead5977d69bfa68bec2f178bcb9404f5e7 / Core8cffa72d17439e465e96183186512dfebcc89b97 | Run44570245768; artifact14555130764,3065bytes. |
| WEB4 compiler and captured intent:11 cases | Parent dfc26104fcc0fe81524cfff4f009037b0f9f4cff / same Core | Run44576502179; artifact14558555047,2610bytes. |
| Native admission6, SQLite SDK4, real-provider execution3 | Parent aa72afcac352fcf3d45e865a1eeb6cd347554e40 / same Core | Run44581607449/job135763435090; artifact14560496208,6512bytes. Actual WEB4 build passed. |
| SQL parser:12 cases | Website49e7a8506aabd4bb57d9cabdf53942d3e99f581f | Run44580490179 at parent3c9f84ec5436523d22ce0b40653b7009883c5b6c; artifact14558667321,1027bytes. |
| Ask review:2 new cases with mocked responses | Website485cb86edafb82531449a50f02e8e26f3a18ef8f; SQL parser unchanged from49e7a850 | Run44583096959 at parentd6f9c885489584283241ca06e246f1e3a7a67d93; artifact14560631016,773bytes. |

All artifacts were checked in memory against exact counts, method/class names, matching test IDs, outcomes and expected report files. No archive was retained on the laptop. The failed initial native run44579280022 remains part of the record: WEB4 built; new admission tests could not compile in Core.UnitTests because that assembly has no SQLite/Neo references. Moving those tests to the existing three-provider MSTest assembly resolved the test-project ownership problem. No production dependency workaround was added.

## What native verification proves

The shipping MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS classes executed CQL against their real SDK-backed isolated stores. Each fixture performed12 owned writes and four assertion groups. String and nullable Int64 selected counts proved non-null participation versus unique cardinality, ordinal case/Unicode/embedded-NUL equality, adjacent Int64 values above2^53, all-null groups, off-page invalid-data rejection and complete group budgets. Every report carries eight true verification/cleanup flags. Owned records/files were removed; workflow Mongo and Neo containers stopped successfully.

Whole fixture times were Mongo881ms, SQLite1744ms and Neo4548ms, including writes, queries and cleanup. These are not individual query latency, a benchmark or production performance acceptance.

The six admission controls prove explicit domain enrollment/refusal only. They do not substitute for SDK tests of Boolean, Int32, Single or Double. Guid, UTC timestamp, Decimal and UInt64 distinct selections remain explicitly unenrolled pending exact native normalization. Computed grouped selectors remain refused.

## What client verification proves

SQL produces a distinct selected-count flag, preserves provider/mapping and complete-input intent, and resolves ordinary COUNT(value) separately from COUNT(DISTINCT value) in HAVING and ORDER BY. Count outputs remain nonnullable even when their inputs are nullable. Unproved domains and computed/count-star distinct forms are rejected.

The actual Ask client initializer, parser and click handlers were tested with mocked WEB6 replies. A complex accepted draft waits for preview and an explicit Run click; an unsupported GUID count draft cannot enter the editor or execute. No real model response, live API, browser session or free-plan access was used or accepted.

## Remaining gates

- Actual WEB4 HTTP native and mixed stored/virtual grouped-distinct execution, including access and source/operation binding.
- SDK proof for the other declared primitive domains; exact normalized Guid/date/Decimal/UInt64 support.
- Computed grouped keys/selectors, larger sources and broader joins.
- Configured real free-model draft through reviewed SQL to hosted query results.
- Verified Development rollout when the shared-cluster/deployment stand-down is lifted; staging/production remain approval-gated.
- The complete EF/NHibernate and provider roadmap. Coverage remains61/168 EF rows plus4/13 separately counted extensions per primary store.

No PIPER data, shared Atlas, deployed DNA, replication queue535/543 or deployment was changed.
