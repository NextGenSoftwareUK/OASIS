# COSMIC ORM implementation status

Updated 29 October 2026. [CURRENT_STATUS](CURRENT_STATUS.md) owns recovery and the exact next step.
The [parity checklist](CQL_PARITY_CHECKLIST.md) owns feature/provider acceptance. Programme incomplete.

## Current milestone

User priority: breadth first. Close missing priority-provider demo coverage (AWS first); deeper migration and parity are deferred. Mongo adoption3/3 and fence2/2 now pass, including unchanged native-source identity.

Shared **1599/1599**; native Mongo three-store **2/2** and Neo + SQLite **2/2** pass.
Native record barriers, exact administrative preview admission, persisted original evidence,
partial/lost-ack recovery and one actual coordinator process-crash boundary are verified in
those recorded scopes. Read-only delivery review retains current lease/state evidence and
other routes. Replacement review distinguishes empty/matching/foreign/local/conflicting
records without adoption or writes; exact consistency and held owners remain checked.

SQLite native mutation-start marker passes2/2, including old-schema preservation, restart,
immutable intent, cancellation/downgrade protection and start/cancel race. Core4462/DNA19/
GraphQL1 retained. Mongo2/2 and Neo2/2 now verify native mutation-start as well; Neo625
controls pass. Exact prepared-start recovery passes native Mongo/Mongo/SQLite1/1 and Neo/SQLite1/1,
plus preserved nonempty-head failure recovery1/1. Preparation, approval and read/write-once
evidence bind the original plan and explicit consistency; native start is not data cutover.

SQLite matching-head adoption now changes receiver lineage with an atomic dedicated receipt (single1/1, parent/child graph1/1 plus original-fixture continuation1/1; multi-route regressions2/2). Graph content-drift rejection, two-head rollback, reconnect/replay and nonempty-receipt downgrade protection pass; business content/version/local revision/history remain unchanged. No global cutover, automatic compensation or HTTP executor is enabled.
Next: native business-state CAS and idempotent migration receipts,
replacement-history admission, interrupted cutover and explicit old-work disposition.
Broader graph/multi-version/delete/provider-pair and deployed restart proof remain open.
[Proof and limits](V2_LINEAGE_MIGRATION.md).

## Remaining implementation and acceptance

1. Native record-scoped source/receiver fences covering V2 writes and delivery across old/new lineages, including in-flight/unknown outcomes and restart.
2. Replacement admission, native CAS, durable idempotent migration receipts, interrupted cutover and original-obligation disposition. Broader native graph/delete/race and ordered-pair acceptance remain open.
3. Remaining HyperDrive outage/cold-start/retry/actor-policy directions. Routing strategy reads, transaction-bound identities, durable native route catalogues and selected restart/exhaustion cases retain scoped proofs; X10 is not universally green.
4. Remaining ORM child/graph parameters, history/restore and authorized delta values/retention; placement, tenant/encryption/residency admission and remaining EF/NHibernate red rows.
5. AWS DynamoDB CQL demo admission/presets. Hosted Azure stays pending by user instruction. Preserve Solana's retained identities/ledger; no original missing historical chunk backup is known.

## Scope retained

- Original 12 investor rows green on SQLite/Mongo/Neo; full parity remains red. Entity-selected All/element/numeric depth and large POCO/migrations tooling stay deferred.
- Oracle/Cosmos emulator/protocol/Solana demo scopes and limitations are summarized in CURRENT_STATUS; no hosted-cloud or general protocol query claim beyond their proofs.
- Implement only V2; document legacy gaps. Work only on red checklist rows; green scopes/federation cursors frozen absent reproducible bugs.
- Work only in C:/Source/OASIS-cosmic on work/cosmic-cql. Named commits, bounded leased builds, raw outputs in TestResults; reusable resources tracked in the [ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

[Previous detailed milestone history](Archive/IMPLEMENTATION_STATUS_PRE_PREVIEW_V3_2026-10-29.md).
[Implementation roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) / [CQL roadmap](CQL_COMPLETION_ROADMAP.md).

PostgreSQL breadth milestone: control8/native1/restart1/paired1 and browser5/4 pass;
[scope and retained evidence](POSTGRESQL_CQL_MVP.md). Full parity remains open.
