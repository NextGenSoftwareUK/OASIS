# COSMIC ORM implementation status

Updated 29 October 2026. [CURRENT_STATUS](CURRENT_STATUS.md) owns recovery and the exact next step.
The [parity checklist](CQL_PARITY_CHECKLIST.md) owns feature/provider acceptance. Programme incomplete.

## Current milestone

Mongo record barrier: native2 race/reactivation/lost-namespace cases and1 preview
regression pass. Shared1506/SQLite2 retained. Snapshot write skew is prevented by
transactional per-record admission. Neo and paired migration admission remain open.

First SQLite native record barrier: Shared1506/1506 and native2 race/restart cases pass.
Affected V2 saves/deletes/replica replay/new claims are blocked durably; unrelated records
remain writable. Mongo/Neo and paired migration admission/quiescence remain open.

Preview v3 binds source receipt, exact frozen transport and historical receiver intent.
Shared1497/1497 and native SQLite2/Mongo1/Neo-source2 pass scoped combined reviews.
Physical bindings, graph membership/content/revisions, obligation state and explicit
read consistency are checked; reviewed evidence enters the operator grant digest.
No migration writes or execution endpoint enabled. [Proof and limits](V2_LINEAGE_MIGRATION.md).

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
