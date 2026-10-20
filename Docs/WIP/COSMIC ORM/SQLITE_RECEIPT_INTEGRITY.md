# SQLite primary receipt integrity

20 October 2026. Prerequisite for committed per-save deltas; delta capture/persistence is still open.

## Root cause and implementation

SQLite's primary format1 receipt decoded stored roots/nodes without a payload checksum or binding that decoded acknowledgement to its original operation. Matching the table's RequestHash did not prove that ResultJson belonged to that request. Seven native regressions demonstrate successful publication of changed record values/versions, changed roots, omitted children, unreachable extra records, changed actor context and a whole acknowledgement copied from another operation. The invalid-parent control already rejected. The original red report is `TestResults/SQLiteCurrent/sqlite-receipt-integrity-red.trx`; an initial fixture compile error was corrected before this native red run.

New format2 primary envelopes bind operation ID, actor, authority, operation, fingerprint format and exact request fingerprint to a checksummed graph. The graph delegates to the existing Shared conditional receipt codec, avoiding another graph decoder. Read verifies the envelope before constructing records, then checks exact ordered roots, selected identity set, ordered child edges, parent ownership, logical/provider revisions and tombstone state. Save acknowledgements additionally recompute the existing exact intent fingerprint over decoded persistent values, excluding only the fields already owned by that fingerprint contract. Delete consumes authoritative persisted values rather than caller edits; it binds conditions/context and tombstone semantics. A newly encoded primary receipt is read and validated before COMMIT, so an invalid acknowledgement rolls the native transaction back.

The checksum detects corruption and binds context; it is not a keyed signature against an authorized database writer. One additional test supplies valid graph/envelope checksums with the wrong saved business value, verifying the separate save-intent check rather than merely a broken checksum. Replay never loads a newer record to reconstruct an old acknowledgement. Invalid receipts return `COSMIC_RECEIPT_INVALID` with no successful result and no new mutation. Existing intent fingerprint domains3/4 and replica receipt format3 remain unchanged.

## Historical envelope admission

Primary format1 receipts require `COSMIC_RECEIPT_ENVELOPE_RECONCILIATION_REQUIRED`. A matching request hash alone cannot establish the old graph's integrity. Old or missing exact fingerprint markers retain `COSMIC_RECEIPT_FINGERPRINT_RECONCILIATION_REQUIRED`. Both gates return before a second mutation; neither upgrades, deletes nor overwrites the original receipt.

Before rollout, identify retained format1 receipts and reconcile their original operations against authoritative revisions, retained history and original caller intent. Preserve originals and reconciliation evidence. Do not manufacture a checksum in place or replay the mutation to upgrade a receipt. An explicit approved successor operation is a separate decision after reconciliation. An automated authorized migration/operator workflow remains open.

## Verification status

Initial focused gate passes17/17: eight corruption vectors plus restart/original replay and exact numeric/foreign-observation intent controls. Final expanded focused gate passes20/20, including checksum-valid wrong content, explicit historical-envelope refusal, sync/async soft/hard deletion replay, original root/child precise-number replay after a later revision, and encrypted graph/batch replay across restart/key rotation. Full native SQLite regression passes1,078/1,078 in140 seconds, zero skips. The fixed fixture is absent after normal teardown.

Reports: `TestResults/SQLiteCurrent/sqlite-receipt-integrity-red.trx`, `sqlite-receipt-integrity-focused.trx`, `sqlite-receipt-integrity-final-focused.trx`, `sqlite-receipt-integrity-full.trx` and `receipt-integrity-artifacts.json`. The artifact report retains exact consumer Core/Shared/SQLite hashes and free disk space. No Core/Shared source, dependency pin or deployment change is part of this increment.

This does not complete Stage7. Immutable committed deltas, atomic before/after capture, authorized values/redaction/encryption, retention and original-delta replay still require implementation over the verified receipt boundary. Existing local value/membership previews remain distinct from committed evidence.
