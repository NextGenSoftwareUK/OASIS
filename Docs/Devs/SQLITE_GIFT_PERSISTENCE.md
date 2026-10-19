# SQLite gift persistence and schema verification

Updated 2026-10-19. Scoped provider work, not full SQLite/HyperDrive V2 certification.

## Contract and root cause

Ordinary avatar gifts incorrectly required a chakra FK with default zero. The chakra FK is now optional. Ordinary gifts are stored independently from chakra-unlocked gifts; root reads and replacement affect only ordinary gifts. Interface implementations, explicit list order, empty-owner binding to the owning detail, replacement and clearing are supported. A conflicting explicit owner or invalid enum wrapper fails without changing the stored detail. Callers' gift objects are not rewritten.

Activation upgrades the known legacy gift child table within the existing schema transaction, retaining actual columns, rows, indexes, triggers and the autoincrement high-water mark. Foreign keys stay enabled. Inbound references, occupied upgrade targets or unrecognized table definitions cause explicit refusal, not deletion or schema guessing. This activation path does not claim to reconcile every historical EF model difference.

## EF migration contract

Two historical inventory migrations lacked EF discovery attributes and were skipped. Their explicit identities are now registered. Frozen migration target models and the current snapshot are present; cached official EF tooling was reused without installation. The design-time factory uses an in-memory model database, never the user's default DB.

Reconciliation includes existing spelling renames, refresh-token StorageId/public-ID preservation, conflicting owner-shadow refusal, legacy inventory type validation/conversion, canonical GUID validation, and canonical empty initialization for newly introduced JSON maps. Unknown/null legacy item types and malformed non-null NFT identities are rejected atomically; no silent enum coercion. Optional absent GUIDs retain the model's empty-GUID semantics.

Reconciliation downgrade is explicitly supported only with empty affected record tables: the old schema cannot preserve all newly represented data. A populated downgrade is refused before schema changes. This is not a general populated reversible migration or a substitute for reviewed backups. The gift downgrade separately refuses standalone gifts rather than inventing chakra ownership. Native tests prove refusal preserves a saved gift, then explicitly remove the owned fixture through provider permanent-delete before proving empty-schema Down/Up.

## Current evidence

- Current producer build: zero errors, 37 existing warnings.
- Full native integration suite: **516/516 passed**, zero skipped, 34 seconds. Fixed report `TestResults/SQLiteCurrent/sqlite-native-current.trx`, SHA256 `4F43473E104DB53A8D88363265286BCF2A5FC63E674B9CCBB4652CFAA2840EF4`.
- Six native fault cases: synchronous/asynchronous gift insert trigger failure preserves original XP and gift; refused upgrades for inbound references and occupied target preserve original schema and roll back earlier SequenceIndex addition. SHA256 `F2FB4BDA9D1848EF6872E01A742012500A640242A2F4B02CB92704BBAB1ACD25`.
- Native inventory migration rejects invalid values without changing original fields/history, and accepts valid QuestItem/GUID with initialized JSON maps. Token migration retains two distinct original IDs and exact payloads. Snapshot/runtime relational-model comparison has no differences.
- Railway dependency manifest/gitlinks validator and whitespace check pass.

Fixed reusable database/lease and existing SDK/dependency cache reused. Owned test database is absent after disposal; compact reports retained. No downloads, clones or additional environment. Free C: after final run: 83,661,115,392 bytes. Frozen designer files are required migration source, not disposable build copies.

## Remaining programme scope

These tests do not establish every legacy/custom schema migration, every field/interface/option, multi-process schedules, hosted acceptance, fee policy or V2 failover/replication/load-balancing certification. See `HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md`. Firestore history work is a separate unverified batch and must not be swept into this integration.
