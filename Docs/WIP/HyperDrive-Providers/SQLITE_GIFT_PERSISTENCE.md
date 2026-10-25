# SQLite storage verification, gifts and schema

## Request snapshots and truthful acknowledgement counts — 25 October

Two further native-read boundary findings are now fixed at the existing owners:

| Invariant | Reproduction | Correction |
|---|---|---|
| Ordinary saves report acknowledged selected records |16native cases all reported0 despite independently observed1/2/3/5committed records across sync/async, single/batch, child selection and on-provider flag choices. | Set `SavedCount` from the committed graph models only AFTER the transaction commit returns. Failure/uncertain paths are unchanged. This is a logical record count, not SQL requests, physical history writes or billable usage. |
| Search uses its original request across native reads |24actual native-read-delegation cases changed caller owner, text/parent/metadata, field mask, metadata, parent or scope flag; every case returned wrong IDs or counts. | Validate, snapshot with existing Core `StoredRecordJson.Project<SearchParams>`, then revalidate before activation/I/O, matching the existing Mongo/Oracle/cloud approach. No new snapshot format or backend fallback. |

Focused 40/40 native PASS (5s), 39/39 units PASS (4s), and full **1,570/1,570 native PASS (196s)**, zero skips. Full report `TestResults/SQLiteCurrent/query-snapshot-count-full-native.trx`, SHA256 `D3F44DB82624E2517613FD7E7D3CD738F4E5E95F0DB2588DD0BC808E610B0E63`. Two explicitly historical reconciliation cases are excluded from the normal suite, not counted as passing or skipped. The callback delegates the actual SQLite repository result before changing the caller request; it does not fake rows or claim SQLite provides asynchronous network I/O. Every case verifies original native IDs and unchanged database/WAL bytes, and uses the fixed owned fixture/intent/baseline fences. Earlier 1,530 evidence remains its separate source cohort. Final fixture database/WAL/SHM, intent and baseline are absent. Ordinary child-role persistence/publication still needs independent follow-up; these fixes do not certify that contract or full V2.

The first snapshot attempt stopped on the count defect BEFORE query evaluation:1metadata failure+23fixture fences, NOT24query races. Its one committed synthetic row was hash-fenced/read-only inspected and the exact owned database retired after a compact proof. Actual24query reds were obtained only after the count fix. Original reports and resource lifecycle are preserved in [the ledger](ENVIRONMENT_INVENTORY.md). No hosted request, dependency restore, download or duplicate environment was used.

## Scoped graph privacy and ordinary-save admission — 25 October

Real SQLite tests reproduced two independent defects:

1. Scoped search and all three avatar export selectors filtered owned roots but exposed hydrated foreign descendants. Eight failing cases and four valid flat-query controls established the baseline.
2. Once visibility was corrected, the ordinary graph writer accepted a save of the filtered graph. Its graph preparation used structural `HolonGraphWritePlan.Create`, which deliberately does not establish caller mutation authority.

Scoped queries now reuse Core's `IOASISStorageProvider.ProjectVisibleHolons`, preserving independent descendant versions and stored membership. The existing common export completion method applies it for hydrated current/positive-version avatar exports; unscoped and flat all-version exports retain their contracts. Empty avatar IDs reject before activation. Ordinary writes alone select existing `CreateForSave`; structural receipt/replica validation remains separate. No second visibility policy, serializer, native writer, retry or workaround was added.

Focused **14/14 native cases passed**, followed by **39/39 unit tests** and the corrected broader **1,530/1,530 native regression (188 seconds)**, all zero skips. The native gate excludes only the explicit historical reconciliation category; it does not count that one-off as a normal passing test. Coverage includes sync/async, current/root-version3, search/export/flat controls, avatar ID/username/email, owned child-version7, foreign private/public descendants and an owned leaf under a hidden branch. Reads and rejected saves leave database/WAL bytes unchanged; `AcceptChanges` cannot grant replacement authority to a filtered graph. Fixed database/journal/intent/baseline paths are absent after teardown.

Compatibility boundary: supplying serialized `ChildRoleManifestJson` alone is not ordinary replacement authority. Three existing CQL fixtures relied on that shortcut to seed stored membership; the initial broad run was1527PASS/3setupFAIL. Those fixtures now write their deliberately controlled stored manifests through bound native SQL after ordinary graph creation; all reparenting/visibility/paging assertions remain unchanged. This is test setup, not a production workaround or permission to bypass the ordinary-save guard. Conditional/replay authority must keep its own expected-revision/receipt contract.

The second defect's original database and durable intent were preserved rather than overwritten. Explicit hash-fenced read-only reconciliation verified the same five holon identities/versions/parent edges and avatar, recorded compact evidence and retired only that owned synthetic database. The pre-partial-save byte hash was not persisted, so reconciliation does **not** claim those earlier bytes were unchanged or certify arbitrary corruption recovery. Exact evidence/resources: [inventory](ENVIRONMENT_INVENTORY.md).

This does not certify every conditional caller-admission/replay combination, post-acknowledgement publication race, public WEB4 endpoint, other provider or complete HyperDrive V2 matrix. Keep those gates open; scoped local tests are not provider-wide100%.

Artifact identity: the frozen native/unit provider copies match. Core744, Shared56, SQLite166, native117 and unit4 source documents were audited against the PE-bound PDBs. Concurrent parent commit97b15db6c regenerated Shared/SQLite informational-version attributes while the frozen binaries retained44ac44524. A read-only audit reconstructed **only** that attribute from each frozen assembly and required the resulting whole source-file SHA256 to equal its original PDB checksum; all other source bytes match. This is a separately documented frozen cohort, not a claim that it matches the newest producer binary or that the default strict verifier passed unchanged. No DLL copying, metadata/file editing or test replay was used to hide the distinction. Exact hashes are in the resource ledger.

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
