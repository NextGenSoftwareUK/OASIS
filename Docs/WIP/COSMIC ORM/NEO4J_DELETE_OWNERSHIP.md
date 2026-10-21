# Neo4j delete transaction ownership

20 October 2026. Implementation and selected verification pass. This follows [identity save ownership](NEO4J_IDENTITY_SAVE.md).

The previous paired avatar and ordinary hard-holon delete paths marked transactions uncertain before COMMIT, relied on disposal for most rollbacks, and published success only after resource disposal. Avatar deletion also omitted result counts/changed flags. Repeated matching soft deletion issued another write transaction.

The private native transaction owner is now shared by identity saves and these delete routes. Its body validates native acknowledgements and explicitly requests commit or read-only rollback. The owner publishes candidates only after the requested boundary is acknowledged; lost COMMIT/rollback responses fence mutation replay. Successful writes remain successful warnings on cleanup failure, with original result/metadata/counts intact. Counts are zero for uncertain/failed operations. A paired avatar deletion counts one logical avatar, with its detail retained in the same atomic transaction.

A matching existing soft tombstone returns known deletion with zero changed/deleted count and no COMMIT, retaining original timestamp/actor evidence. Missing rows retain RecordNotFound. Selector admission still precedes native dispatch. Conditional graph receipt/delta protocols are unchanged.

Controlled evidence:38 cases for missing controls, repeated-soft no-op, normal/rollback/COMMIT/cleanup outcomes across sync/async paired avatar and hard holon deletes;34 real SDK cases additionally reject the paired detail statement after profile mutation/history work. Full native regression also covers identity saves after transaction-owner extraction. The hash-bound original Neo producer `CFE15726A9B8CFBD6C53B069C1B7CC6BFDC29EC20336C978FE70F048274F366A` fails32/passes6 missing controls in `TestResults/Neo4jCurrent/neo-delete-ownership-red.trx`, without native I/O. Full controlled276/276 now passes, zero skips (`neo-delete-controls-full.trx`). Focused native34/34 passes, zero skips (`neo-delete-ownership-current.trx`), including explicit rollback of paired detail rejection and a real COMMIT-loss probe proving repeated soft deletion does not send COMMIT. Final full native265/265 and paired Shared917/917 pass, zero skips; the focused34 cases are a subset of265; no Core/Shared source changes are required.

Wider provider parity, conditional/default CRUD enrollment, explicit restore, related-kind lifecycle coordination and the complete ORM roadmap remain open.

## Final verification boundary

The batch terminates0 at parent `f4f5cab41`, on unchanged Core `aa209f94418d5f11280345ceebd411c912717858`. `TestResults/Neo4jCurrent/neo-delete-final-artifacts.json` verifies matching Core/Shared across Neo controlled/native and Shared consumers, matching Neo producer copies, the stopped retained runtime and free disk65.4GB. Core SHA256 `7B8680F86B797D84CAE0120D167F0BFA36E72B2BC4B7E9EFB22A379DC17620CC`; Shared `AEA43856E08C798D8B4681ABAFE0717AA456CFD1A122193CE12F72E975D5768A`; Neo `6D32C8106FDF6E36C6F884F263DC7FA51A10FA7BD1B9A8FD068C8767538949D0`.

Reports: `neo-delete-controls-full.trx` (276), `neo-delete-ownership-current.trx` (34), `neo4j-native-current.trx` (265) under `TestResults/Neo4jCurrent`, and `TestResults/SharedCurrent/shared-neo-delete-paired.trx` (917). No native failure required fixture reconciliation. Successful synthetic fixtures are cleaned by their owners. No Core/Shared source or pin change, new environment, download or Development/master deployment is included.
