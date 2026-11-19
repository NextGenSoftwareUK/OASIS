2026-11-19 REQUIRED DISTINCT ROOT FIX PREPARED:44605337804/job135847418652 terminalFAILURE, WEB4 built; all3 cases failed first nonnullable Boolean duplicates. Strict2494byte14569862952 RAM TRX3failed/0passed/0skipped; no reports/acceptance; owned Mongo stop passed. Shared ValidateParticipation wrongly equated distinct cardinality with ordinary required-input participation. Core12c7d01f preserves existing3-argument API and adds explicit distinct mode:0<=cardinality<=inputs, required nonnullable cardinality>=1; ordinary required count still equalsinputs. Shared materializer and Mongo/Neo decoders use column.Distinct; SQLite receives shared validation. No filter/provider fallback or weakened null/type checks. NEW6 shared controls cover required/nullable cardinalities, bounds, retained ordinary invariant and actual materialization. Samefailed3 native required/canonical cases next, not accepted. Dev38d4320e unchanged/live; runtime fix not deployed. Website5ace4aae new complex grouped/mixed example source pushed; client44606863369 failed mixed example because the new example omitted required source metadata filter. Preserve mapped bounded-query guard; correct example with an explicit filter, no parser bypass. No PIPER/Atlas writes, worker/DNA/settings changes or local runtime/DB/image/archive/results.

2026-11-19 LIVE DEV AND MIXED HTTP ACCEPTANCE: Railway57e3b7cd-61b5-4f0e-a934-5ac7b38ede33 SUCCESS for Dev38d4320ef7cffa3ce8d6c77cb312dee3f42acb9d. One read-only Swagger GET200 exposes CqlGroupedV1ColumnWebAPI.distinct (2,954,790bytes/7,315ms single download sample; not data-query latency). No data queries/PIPER writes. Saved DNA SHA0755d6b0... unchanged, AutoLoadBalance=false/teaching=true; running DNA not independently exposed/verified. Mongo HTTP44602869946/job135838671694 SUCCESS on fc61c8ba; strict1768byte artifact14568348699 RAM verifies exactly1 passed case/report,3 checks/4 holon writes/2 raw rows/2 owner HTTP+1 foreign refusal/all11 flags and cleanup; Mongo container stop passed. Together with retained SQLite+Neo14567781285 from44598328056, all3 mixed HTTP cases accepted without rerunning accepted2. Scoped native fixture acceptance is not hosted browser/model acceptance or full parity. NEW required/canonical3 run44605337804 on dd2d9fcc6fa85125a9397a32a16bad8b3b8cdf1a dispatched, not accepted. Worker validator fix8b7f2f06 has6 new controls; Dev auto protocol44600997848 failed obsolete checker before build, runtime workers unchanged. Feature c2a846ba retains all source. Free WEB6 saved-DNA/env presence checks allfalse for GeminiFree/OpenRouter and Ollama URL/model; existing owner action remains. C free37.25GiB; no local runtime/DB/container/image/archive/results installed, pinned WEB6 source objects only retained. Normal next Dev push no sooner than hourly from19:41UTC unless critical; staging/prod approval-gated.

# Grouped distinct counts — scoped acceptance

Updated19 November2026. This is an engineering acceptance record, not full EF/NHibernate parity or a live deployment claim.

## Verified cohorts

| Cohort | Exact source | Evidence |
|---|---|---|
| Core intent and mixed reducer:12 cases | Parent c82cf9ead5977d69bfa68bec2f178bcb9404f5e7 / Core8cffa72d17439e465e96183186512dfebcc89b97 | Run44570245768; artifact14555130764,3065bytes. |
| WEB4 compiler and captured intent:11 cases | Parent dfc26104fcc0fe81524cfff4f009037b0f9f4cff / same Core | Run44576502179; artifact14558555047,2610bytes. |
| Native admission6, SQLite SDK4, real-provider execution3 | Parent aa72afcac352fcf3d45e865a1eeb6cd347554e40 / same Core | Run44581607449/job135763435090; artifact14560496208,6512bytes. Actual WEB4 build passed. |
| Native nullable Boolean/Int32/Single/Double:3 cases | Parent2e9968288db769f075568c285e315248e656de97 / Core8cffa72d | Run44586155910; artifact14561588731,2918bytes, strict RAM verified;22 owned writes/store,4 groups/all10 flags. |
| Mixed stored/virtual HTTP: SQLite and Neo4j | Parenteee085aea78985460dbaec7cf22a0dd89bf73af3 / same Core | Run44598328056; artifact14567781285,2887bytes, overall2passed/1failed; ONLY these2 accepted. |
| Mixed stored/virtual HTTP: Mongo-only retry | Parentfc61c8babbd68ba25e523304acd9324ee8665cf3 / same Core | Run44602869946; artifact14568348699,1768bytes; exactly1passed/0failed/0skipped, owned cleanup. |
| SQL parser:12 cases | Website49e7a8506aabd4bb57d9cabdf53942d3e99f581f | Run44580490179 at parent3c9f84ec5436523d22ce0b40653b7009883c5b6c; artifact14558667321,1027bytes. |
| Ask review:2 new cases with mocked responses | Website485cb86edafb82531449a50f02e8e26f3a18ef8f; SQL parser unchanged from49e7a850 | Run44583096959 at parentd6f9c885489584283241ca06e246f1e3a7a67d93; artifact14560631016,773bytes. |

All artifacts were checked in memory against exact counts, method/class names, matching test IDs, outcomes and expected report files. No archive was retained on the laptop. The failed initial native run44579280022 remains part of the record: WEB4 built; new admission tests could not compile in Core.UnitTests because that assembly has no SQLite/Neo references. Moving those tests to the existing three-provider MSTest assembly resolved the test-project ownership problem. No production dependency workaround was added.

## What native verification proves

The shipping MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS classes executed CQL against their real SDK-backed isolated stores. Each fixture performed12 owned writes and four assertion groups. String and nullable Int64 selected counts proved non-null participation versus unique cardinality, ordinal case/Unicode/embedded-NUL equality, adjacent Int64 values above2^53, all-null groups, off-page invalid-data rejection and complete group budgets. Every report carries eight true verification/cleanup flags. Owned records/files were removed; workflow Mongo and Neo containers stopped successfully.

Whole fixture times were Mongo881ms, SQLite1744ms and Neo4548ms, including writes, queries and cleanup. These are not individual query latency, a benchmark or production performance acceptance.

The six admission controls prove explicit domain enrollment/refusal only. The separate accepted primitive cohort now supplies SDK proof for nullable Boolean, Int32, Single and Double; nonnullable/canonical cases remain pending in44605337804. Guid, UTC timestamp, Decimal and UInt64 distinct selections remain explicitly unenrolled pending exact native normalization. Computed grouped selectors remain refused.

## What client verification proves

SQL produces a distinct selected-count flag, preserves provider/mapping and complete-input intent, and resolves ordinary COUNT(value) separately from COUNT(DISTINCT value) in HAVING and ORDER BY. Count outputs remain nonnullable even when their inputs are nullable. Unproved domains and computed/count-star distinct forms are rejected.

The actual Ask client initializer, parser and click handlers were tested with mocked WEB6 replies. A complex accepted draft waits for preview and an explicit Run click; an unsupported GUID count draft cannot enter the editor or execute. No real model response, live API, browser session or free-plan access was used or accepted.

## Remaining gates

- Hosted browser acceptance of the native/mixed HTTP path; isolated actual WEB4 HTTP proof is now accepted across all3 stores, including access, source/operation binding and usage.
- New nonnullable and canonical-field SDK proof (44605337804 pending); exact normalized Guid/date/Decimal/UInt64 support.
- Computed grouped keys/selectors, larger sources and broader joins.
- Configured real free-model draft through reviewed SQL to hosted query results.
- Hosted query/browser acceptance on deployed Development38d4320e; API schema exposure is verified. Staging/production remain approval-gated.
- The complete EF/NHibernate and provider roadmap. Coverage remains61/168 EF rows plus4/13 separately counted extensions per primary store.

No PIPER data, shared Atlas data, deployed DNA or replication queue535/543 was changed. Development38d4320e deployed successfully; the single live Swagger GET made no data queries.
