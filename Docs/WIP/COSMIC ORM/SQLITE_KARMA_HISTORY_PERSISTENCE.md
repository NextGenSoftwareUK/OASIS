# SQLite karma history persistence

## Original defect and evidence

The four sync/async create/update regression cases all failed because the save acknowledgement returned an empty history. Original evidence: `TestResults/SQLiteCurrent/karma-history-original-red.trx` (4 failed, 0 passed, 0 skipped). The SQLite integration fixture disposes its temporary database; this TRX is the retained failure evidence.

The repository omitted the history navigation from reads, omitted reconciliation during updates, and omitted history from its DTO projection. Consequently updating totals could succeed without saving the associated records. New-record entity construction already inserted history, but acknowledgements and subsequent repository loads hid it.

## Current implementation work

The repository now includes history, replaces the owned collection in the same SaveChanges transaction as totals, and projects history into acknowledgements and loads. The entity constructor consumes the public history interface instead of requiring a concrete implementation. Foreign avatar ownership is rejected and failed tracked writes are detached through the existing aggregate cleanup mechanism.

Regression coverage exercises create/update through both sync and async methods, acknowledgements, reloads, duplicate entries, replacement, clearing, and rejection of foreign ownership without changing stored totals. Targeted verification passed 6/6 with zero skips (`karma-history-fix-current.trx`). The complete SQLite suite passed 1151/1151 with zero failures or skips (`karma-history-full-current.trx`, 2m27s). The provider build completed with zero errors. Both reports are retained in `TestResults/SQLiteCurrent/`.

## Verified representation follow-on

The follow-on adds an explicit SequenceIndex, DateKind and five enum-presence bits. The provider stores each supplied list position, retains duplicate entries, and orders by sequence then physical ID. Existing rows acquire sequence zero and retain physical-ID order. New dates retain exact ticks and Unspecified/UTC/Local kind; old dates remain Unspecified because their original kind cannot be recovered. Invalid persisted date-kind values fail explicitly.

Absent wrappers retain absence independently of their physical enum slot. Legacy rows default to present, preserving all previously observable zero/nonzero enum values. Both formal EF migration `20261020070000_AddKarmaHistoryFidelity` and activation's atomic additive schema upgrade cover the seven columns. Downgrade rejects rows with nonzero sequence, non-Unspecified kind or any absent wrapper rather than discarding representation.

The initial focused run passed 18 checks and failed six fixture-update cases after direct physical-ID rewriting invalidated the writer's tracked EF identities. The order/date/nullable assertions had passed; the test now reopens a provider before subsequent updates. Original report retained as `karma-history-fidelity-current.trx`. No production retry or concurrency suppression was added.

Corrected focused verification passed 27/27 with zero failures/skips (`karma-history-fidelity-verified.trx`, SHA256 `7B93E255F2CF04D89DCE70A493A483DC181365DDB919C501C87CE817AE9E0B2D`). This includes actual SQLite round trips for sync/async and all three date kinds, explicit order after physical-ID reversal, nullable enum wrappers, legacy additive upgrade, lossy downgrade rejection, invalid persisted date-kind rejection and existing snapshot checks. Producer and consumer SQLite assemblies match SHA256 `F1A63CB6C42AB17EFD1B6588158852C637F6B8DA6C4340ED8E7FA56DAEF0AB39`.

The complete SQLite suite passed 1164/1164 with zero failures/skips in 2m36s (`karma-history-fidelity-full.trx`, SHA256 `ADB41DC2A6313E7E68825C85928069DCADB57657C3626D6E3431F244F82F79DB`). The owned disposable integration database was absent after the run. These results close this SQLite history representation batch; they do not establish complete cross-provider history/replication acceptance.

The separately retained AWS/SQLite native failure fixture is owned by the Hyperdrive session. This regression does not replay or modify it. Legacy Hyperdrive routing remains documentation-only.
