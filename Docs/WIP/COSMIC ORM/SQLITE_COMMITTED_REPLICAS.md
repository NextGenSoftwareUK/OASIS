# SQLite committed replica receiver

Updated 19 October 2026. Locally implemented and verified under the existing SQLite conditional graph transaction owner. Direct CRUD atomic outbox enrollment and hosted target-adapter integration remain open.

## Native ownership

`SQLLiteDBOASIS` now implements `ICommittedHolonReplicaStorageProvider`. `HolonRepository.Conditional.cs` owns one native IMMEDIATE transaction for edits and explicit committed-replica intent. Replica application uses the shared admitted plan and strict receipt codec, preserves source logical revisions/audit, assigns local physical revisions/keys, and keeps the target's metadata namespace local. It neither restamps a user edit nor changes submitted source objects.

The primary graph, `CosmicReplicaHead` rows and exact operation receipt commit together. Receipt lookup precedes lineage/local-state comparison. The unique head retains source installation/authority, logical snapshot, target physical token, SHA256 integrity digest and hard-delete state. A source gap, changed source, unenrolled existing row, local raw write, unexpected deletion/recreation, malformed head or invalid acknowledgement rejects atomically. The physical anchor detects local changes even when the logical version ID was retained.

Hard deletion retains the head and exact receipt while removing selected current rows. Old creates cannot bypass the tombstone; old exact receipt replay returns its original acknowledgement without resurrecting current data. Initial enrollment still requires an absent row at source logical version one; authorized baseline/source-handover tooling is not implemented.

## Transaction result and snapshot ownership

Commit dispatch, acknowledged result, rollback and cleanup are explicit. Pre-commit failures await native rollback. No abort/replay occurs after commit dispatch. `SqliteReplicaReconciliationException` carries source/operation identity and original cause; SQLite exposes no native transaction ID here, so none is invented. A validated durable acknowledgement survives later cleanup failure with a warning. Original/rollback/disposal/connection-cleanup causes are retained. The EF wrapper commits the same IMMEDIATE native transaction and preserves its real transaction observer boundary.

Caller data, intent JSON and selected child membership are captured before I/O. The shared planner serializes each source once and records child identities during that traversal. SQLite maps the detached snapshots and builds acknowledgements from the captured topology. Conditional fingerprints use the same captured source intent; ordinary stable-input hash semantics and the existing edit receipt format are retained. Native tests mutate caller names/child collections during a read and verify the accepted graph and replay stay unchanged while pending caller edits remain intact.

SQLite's deletion-UTC validator runs on selected source nodes before canonical serialization can normalize away invalid input kind; the same validator still guards direct EF writes. The async surface uses the driver's synchronous native I/O and checks cancellation before I/O, between preparation stages and before commit. It does not promise active-query interruption or undo an acknowledged commit.

## Mapping corrections required by strict replica validation

- Nested metadata uses the existing canonical stored-record writer, preserving JSON token/CLR value representations instead of feeding Newtonsoft tokens into a different serializer that changed their content. `MetaDataJson` remains the relational owner and its reader still yields JSON values.
- Creation provider/OASIS audit enums preserve absent versus explicitly supplied values. Two presence bits combine with their relational enum values; edits preserve the original values and presence. Replica application preserves the committed origin exactly.
- Existing databases default these presence bits to true, retaining their prior observable enum values. Missing historical absence information is not guessed. Downgrade refuses to discard stored absence information.

These extend the [persistent-value mapping](SQLITE_PERSISTENT_VALUES.md); they do not introduce an extension-field override of relational data.

## Schema

`20261019220000_AddCosmicReplicaHeads` installs the native unique head table; `20261019230000_AddHolonCreationAuditPresence` installs the two audit-presence columns. The existing EnsureCreated upgrade path establishes the same required storage. EF target snapshots/runtime-model comparison pass. Downgrade guards reject retained heads or meaningful null-audit state inside native migration transactions. Empty/lossless down/up and lock release are tested. Deployment must follow a deliberate EF migration or existing EnsureCreated upgrade workflow; this is not automatic reconciliation of arbitrary mixed migration histories.

## Evidence

- **616 SQLite native tests pass**, zero failures/skips: nineteen new cases beyond the preceding 597 checkpoint (ten receiver/transaction/topology/head-migration cases and nine audit/metadata cases).
- **510 shared tests pass**, including new single source-serialization and single child-membership capture cases.
- **165 Neo4j native tests pass**, including six replica sequences: four SQLite-to-Neo4j and two rich Neo4j-to-SQLite cases.
- **75 MongoDB unit tests pass** after rebuilding its shared-plan consumer. MongoDB native crash/delivery behavior is not certified by those unit results.

SQLite receiver sequences use actual source commits in a separate owned SQLite database. They verify ordering/gap rejection, provider reconstruction, exact replay, changed intent/source rejection, local conflicts, duplicate workers, root-only soft/hard deletion and unchanged descendants, retained tombstones, stale creation and head corruption. Fault cases verify late cancellation, graph/head/receipt rollback on head/receipt rejection, known committed cleanup warnings, and a real commit followed by an injected application acknowledgement exception. Independent native inspection precedes exact replay; the real commit count stays one. This is not a physical network-drop/process-kill harness.

The reverse cross-provider cases use actual Neo4j conditional commits with nested JSON, exact large integer, additional parents/custom key, origin metadata and absent creation OASIS audit. Source logical/audit values survive at SQLite, and target-local metadata is excluded. Successful synthetic databases/sidecars are removed and the retained Neo4j container is stopped; no environment/tool download occurred. [Verification](VERIFICATION.md#sqlite-committed-receiver-checkpoint--19-october-2026) records hashes and the original-only inspection of a retained test source fixture after a test SQL-formatting error.

## Remaining roadmap

MongoDB receiver parity, the hosted generic target adapter, primary mutation/receipt/outbox enrollment, durable scheduling/reconciliation/retention and actual process-crash recovery remain open. SQLite still stores the current holon row rather than a coherent per-record/graph historical archive; preserving incoming logical versions is not full historical restore support. Broader ordinary relationship/cycle/unlink and all-provider certification remain open. Actor/tenant/source authorization, baseline migration, encryption/placement, deltas and CQL remain active requirements. Legacy Hyperdrive routing was not changed.
