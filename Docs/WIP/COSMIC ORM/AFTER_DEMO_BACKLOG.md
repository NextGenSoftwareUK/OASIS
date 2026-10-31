# Return after the MVP demo

Updated 30 October 2026. These remain part of the full goal; they are deferred, not complete.

Current priority: fix reproducible demo blockers and deliver provider breadth/presets. Do not expand generic recovery infrastructure or parity depth ahead of the demo.

| Later work | Current evidence / remaining acceptance |
|---|---|
| Durable initialization application coordinator | Native baseline, durable one-obligation application, interrupted-call receipt recovery and Mongo-to-SQLite shell host now implemented/tested. Hosted Mongo-to-SQLite execution now verified for two barriers and35-step three-database demo; broader provider/operator/API composition remains deferred. |
| Fenced source reconciliation and queue completion | Mongo source completion is implemented/tested against exact original source and target evidence, with atomic comparison and retained failure history. Hosted two SQLite conflicts repaired and ordinary delivery verified; broader source-provider repair tooling remains deferred. Never skip/reset a held entry or replay acknowledged writes. |
| Atomic late enrollment / future-write cutover | Baseline primitives do not certify gap-free enrollment while new source writes race capture. |
| Large-store baseline/migration operations | Neo receipt audit currently bounded at 10000; Mongo migration batches1–128. Scaling, operator tooling and full operational acceptance remain. |
| Deep LINQ / EF / NHibernate parity | Resume red checklist rows, especially All/element/numeric terminals after entity-selected stages, reselection and graph composition. Keep green scopes/cursors frozen absent a reproduced bug. |
| General POCO mapping and migrations tooling | Unrestricted entities without IHolon, model mapping/conventions, schema diff/up/down/scaffolding remain deferred. |
| Full ORM acceptance audit | Reconcile tracking/save ownership, conditional concurrency, child loading/saving/depth parameters, rollback/history/deltas, placement and migration against original requirements/provider tests. Existing focused proofs do not establish 100% completion. |
| Broad provider production depth | MVP queries/presets do not certify complete provider semantics, auth, billing/settlement, scaling or cross-provider snapshot consistency. |

Hosted Azure remains pending at the user's direction (free-only; no existing free subscription). Broader provider order and demo gates remain in CURRENT_STATUS and the strict checklist. Legacy HyperDrive issues are documentation-only; implementation changes remain V2 only.

Recovery entry: [CURRENT_STATUS](CURRENT_STATUS.md). Feature acceptance: [CQL_PARITY_CHECKLIST](CQL_PARITY_CHECKLIST.md). Native recovery limits: [REPLICA_BASELINE_AND_RECOVERY](REPLICA_BASELINE_AND_RECOVERY.md).

Azure Blob MVP (30 October): local Azurite + Mongo identity query is now current-branch SDK/HTTP/browser verified (all5/subset4). Return later to hosted free-only Azure acceptance, application encryption, content/metadata queries, business conditional writes and wider HyperDrive acceptance; see AZURE_BLOB_CQL_MVP.md. Do not deepen its verified bounded identity controls without a reproducible bug.

LocalFile bounded identity MVP integrated30 October:7controls/native1/paired1 + independentbrowser5/4, original source unchanged. Return later to plaintext-record encryption, content/metadata queries, child semantics, conditional writes and wider HyperDrive/parity; current profile deliberately does not certify those. See LOCALFILE_CQL_MVP.md.
