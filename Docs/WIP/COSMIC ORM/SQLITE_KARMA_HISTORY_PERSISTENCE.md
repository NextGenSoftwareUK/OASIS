# SQLite karma history persistence

## Original defect and evidence

The four sync/async create/update regression cases all failed because the save acknowledgement returned an empty history. Original evidence: `TestResults/SQLiteCurrent/karma-history-original-red.trx` (4 failed, 0 passed, 0 skipped). The SQLite integration fixture disposes its temporary database; this TRX is the retained failure evidence.

The repository omitted the history navigation from reads, omitted reconciliation during updates, and omitted history from its DTO projection. Consequently updating totals could succeed without saving the associated records. New-record entity construction already inserted history, but acknowledgements and subsequent repository loads hid it.

## Current implementation work

The repository now includes history, replaces the owned collection in the same SaveChanges transaction as totals, and projects history into acknowledgements and loads. The entity constructor consumes the public history interface instead of requiring a concrete implementation. Foreign avatar ownership is rejected and failed tracked writes are detached through the existing aggregate cleanup mechanism.

Regression coverage exercises create/update through both sync and async methods, acknowledgements, reloads, duplicate entries, replacement, clearing, and rejection of foreign ownership without changing stored totals. Targeted verification passed 6/6 with zero skips (`karma-history-fix-current.trx`). The complete SQLite suite passed 1151/1151 with zero failures or skips (`karma-history-full-current.trx`, 2m27s). The provider build completed with zero errors. Both reports are retained in `TestResults/SQLiteCurrent/`.

## Remaining representation audit

The existing schema has no explicit sequence index, DateTime.Kind column, or nullable enum-presence representation for history records. Physical IDs currently provide read ordering. Explicit order persistence, nullable wrappers, exact date-kind round trips, migration compatibility, and guarded downgrade remain open and must be tested before claiming complete history fidelity.

The separately retained AWS/SQLite native failure fixture is owned by the Hyperdrive session. This regression does not replay or modify it. Legacy Hyperdrive routing remains documentation-only.
