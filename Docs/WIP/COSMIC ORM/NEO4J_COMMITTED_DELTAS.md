# Neo4j committed deltas

20 October 2026. Implementation and selected native acceptance pass. This checkpoint does not certify the full delta roadmap.

Neo4j retains authoritative before-images while it owns the existing per-record transaction locks. The canonical snapshots returned by the native write are the after-images. The transaction captures the shared Core delta and stores Shared receipt format4 beside the operation fence, graph/history changes and any explicitly enrolled primary outbox obligations. The transaction's history policy controls stored-value retention. Public result metadata contains only the redacted summary; no historical decryption or authorized history endpoint is introduced.

Acknowledged replay reads the original receipt and delta without loading current records or applying today's history policy. Old format3 receipts retain their original acknowledgement and report explicit missing-delta evidence. Replica receipts retain their existing protocol. The capability marker applies to new conditional writes only.

Original admission gate: `TestResults/Neo4jCurrent/neo-delta-admission-red.trx` fails all four sync/async/history cases before any native fixture creation. Closure extends the existing conditional graph/race/rollback/acknowledgement-loss scenarios with persisted delta/summary and replay-after-hard-delete assertions. The first native closure and corrected strengthened conditional closure each pass13/13, zero skips. Strengthened acceptance covers precise decimal retention, removed/null/array field descriptors, unchanged child snapshots, explicit old-format replay, and an acknowledgement loss after the real native receipt statement but before COMMIT; replica6/6, primary-outbox2/2 and paired Shared904/904 also pass, zero skips.

Still required: wider adapter/ordinary CRUD/replica enrollment, authorized historical-value reads, retention and key policy, operator workflows, full graph restore and fault/security acceptance. See [delta contract](COMMITTED_DELTAS.md) and [full roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md).

## Reviewed failed-test fixtures

`neo-delta-creation-path-red.trx` records four incorrect nested-field assertions: initial metadata creation is one added `/MetaData` value, while subsequent edits have nested field descriptors. The assertion now verifies the exact precise value inside that creation value. No production capture behavior was changed to accommodate the assertion.

Before rerun, `Neo4jDeltaFixtureReconciliation` independently verified the SHA256-bound original report, all four actor/operation/identity sets, eight native creation records, original receipt/delta contexts, version1 acknowledgement matches, one declared parent/child edge per graph, and absence of historical rows. It archived the exact native properties/receipts in `TestResults/Neo4jCurrent/neo-delta-reviewed-creation-fixtures.json`, then removed only property-matched owned records/receipts inside the same transaction. The explicit `-ReconcileDeltaCreationFixtures` runner gate passes1/1; it is not an automatic fixture reset. A compile-only SDK mismatch was corrected to use the installed driver's native numeric node identity before any service was started.

## Final artifact boundary

The terminal batch runs at parent `841ca4498` with unchanged Core `aa209f94418d5f11280345ceebd411c912717858`. `TestResults/Neo4jCurrent/neo-committed-delta-final-artifacts.json` records exact Core/Shared/Neo producer hashes, matching Shared unit-test consumers, stopped retained runtime and free disk65.8GB. Core SHA256 is `7B8680F86B797D84CAE0120D167F0BFA36E72B2BC4B7E9EFB22A379DC17620CC`; Shared is `715BDC197C0E79DC4BC92CB830DE4DC415FF945C290B518B6E89A0758C6C9792`. Final reports are `neo4j-cosmic-conditional-current.trx`, `neo4j-cosmic-replica-current.trx`, `neo4j-cosmic-primary-outbox-current.trx` under `TestResults/Neo4jCurrent`, and `TestResults/SharedCurrent/shared-neo-delta-paired.trx`. The local-path audit checks58 Markdown files/426 targets with zero broken paths; it does not validate anchors.

No Core source/pin change, new environment, dependency download, Development/master integration or deployment is included. These selected gates establish this conditional-delta adoption; broader Neo4j lifecycle/read/history/result acceptance and the full roadmap remain open.
