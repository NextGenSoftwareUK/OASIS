# SQLite holon persistent-value mapping

Updated 19 October 2026. This fixes a prerequisite discovered while adding committed-replica receiver parity. The earlier mapping checkpoint preceded the SQLite receiver implementation; direct CRUD atomic outbox delivery remains open.

## Root cause and ownership

SQLite's `HolonRepository` mapped a subset of holon fields to relational columns. It discarded provider metadata except the local token, `CustomKey`, additional parent IDs/lists, previous-provider-key observations and other persistent common holon fields. A replica receiver correctly validating the complete committed logical snapshot would reject these acknowledgements. Weakening that validation would hide lost data.

`Holon.PersistentValueJson` now stores only persistent common holon values not owned by relational columns. `HolonRepository.PersistentValues.cs` uses the existing canonical stored-record serializer and navigation exclusions. `Id`, primary parent, name/type, logical versions, audit, visibility, dimension values, ordinary metadata and current provider keys retain their existing column owners. The extension cannot override them.

Provider metadata is retained, including other provider namespaces and local settings. This provider's reserved physical revision observation is excluded from the extension: `Holon.ProviderRevisionId` is its sole persisted owner. Reads reconstruct the observation from that column. Current provider keys retain their relational codec. Ordinary metadata stays in its relational column and now uses the canonical stored-record writer to preserve JSON token values. `InstanceSavedOnProviderType` is derived from the current provider, and loaded/acknowledged records are not marked new.

Extension reads reject malformed/nonobject JSON, relational or serializer-owned field overlap, a duplicate local physical token, and unknown/nonpersistent properties. Corruption returns an explicit error rather than a partial/default record. Runtime navigation is neither stored nor reconstructed from this value; graph persistence still owns actual child relationships. Stored ID lists/cache strings are values, not evidence that corresponding edges exist.

## Schema paths and downgrade protection

- EF migration: `20261019210000_AddHolonPersistentValues` adds a non-null TEXT column with `{}` default. Its target model and current model snapshot match the runtime model, verified by model-differ tests.
- Existing EnsureCreated databases: `EnsureRecordSchemaAsync` adds the same column in the existing native schema-upgrade transaction.
- Existing records receive an empty extension, preserving all existing relational values, logical revisions and physical tokens. Missing historical values are not invented; previously discarded values cannot be recovered by adding a column.
- EF downgrade refuses to discard any nonempty extension. The guard executes inside the migration transaction, preserving both data and migration history after rejection. Empty-database downgrade/re-upgrade is verified. Rejected downgrade also leaves no native EF migration-lock row.

No tooling/runtime download was needed. The migration/target snapshot follow the repository's existing EF model and are checked against the actual runtime schema; no user database was selected for scaffolding or verification.

## Receipt lifecycle correction

SQLite's original conditional receipt reader reconstructed committed holons through a constructor that marks them new. Replay now clears `IsNewHolon` for every acknowledged node. This changes runtime lifecycle state, not the stored persistent receipt format/content or its original logical acknowledgement. Two new sync/async cases verify extension content and original receipt replay after a later write, including changed-intent rejection.

## Native verification

The full SQLite suite passes **597 cases**, zero failures/skips, including **17 new cases**:

- Four sync/async single/batch round trips preserve additional parents/lists, custom keys, previous provider keys, local/foreign metadata, exact large integer and nested JSON. They verify navigation exclusion, sole column ownership and later updates/token rotation.
- Two conditional-write/replay cases preserve the same values and the original acknowledgement after an intervening write.
- Eight corruption cases reject invalid JSON, array/null, column overlap, unknown property, navigation, duplicate local token and root type metadata.
- One existing-database upgrade preserves logical lineage and the physical token without inventing additional values.
- Two real EF migration cases verify up, lossless empty down/up, and refusal of lossy downgrade with retained data/history.

Four updated native SQLite-to-Neo4j replica sequences also pass with richer actual source commits: custom key, additional parent ID and origin metadata survive, and caller-supplied target-local metadata is excluded by the receiver's explicit local-namespace contract. These are the existing sync/async × history-off/on receiver sequences; they are not four extra SQLite regression cases. See [verification](VERIFICATION.md#sqlite-persistent-value-mapping-checkpoint--19-october-2026).

## Remaining work

SQLite committed-replica graph/receipt/head ownership is now implemented; next are MongoDB receiver parity, the hosted adapter and atomic primary CRUD outbox enrollment. SQLite's original conditional receipt format/integrity and transaction cleanup semantics still need the broader parity audit. Full historical graph reconstruction, explicit baseline/source handover, all-provider codec certification, security/placement, deltas and CQL remain active roadmap gates. Legacy Hyperdrive remains documentation-only.

## Receiver follow-up

[SQLite committed-replica support](SQLITE_COMMITTED_REPLICAS.md) also preserves creation-audit null/default distinctions with explicit presence columns and fixes JSON-token metadata serialization. These additions pass the full 616-case native suite. The original 597-case mapping checkpoint above remains historical evidence.
