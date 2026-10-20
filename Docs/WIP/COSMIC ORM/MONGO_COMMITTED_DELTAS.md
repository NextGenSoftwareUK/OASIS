# MongoDB committed deltas

20 October 2026. MongoDB adopts the shared Core capture model and extends the existing Shared receipt codec; no parallel graph decoder or mutation replay is introduced. Final committed-source regressions pass; the Core gitlink and deployed dependency manifest advance together on the task branch.

## Authoritative images and atomic storage

The native snapshot transaction reads the original BSON observations and checks every expected provider revision. After assigning native identities/revisions and serializing the complete selected graph, it preserves the opaque unselected frontier and constructs acknowledgements from the final BSON bodies. Receipt/delta capture therefore describes the actual stored representation, rather than the pre-serialization CLR metadata objects. Provisional DTOs are used only to stage selected embedded child evidence. The final acknowledgement has one owner.

Flat delta/acknowledgement projections omit embedded Children; selected edges are attached explicitly from the captured graph. An unselected child is not inferred as deleted. This does not certify all child-read/query-depth or historical graph scenarios.

The delta is encoded in the same native transaction as selected record mutations, operation fencing, original receipt and any enrolled source outbox obligations. A later native child rejection aborts the already-captured delta with the graph. Failed, conflicted or uncertain writes do not publish a successful delta summary. Positive receipt replay remains read-only and returns the original summary even after the root is physically deleted and the provider's history flag changes.

## Shared protocol and capability

`IConditionalHolonDeltaStorageProvider` advertises capture for new conditional writes. It does not claim ordinary CRUD enrollment or authorized historical-value reads. SQLite and MongoDB implement this capability; other providers must adopt the transaction contract before advertising it.

`ConditionalHolonReceipt` format4 adds a version1 stored delta to its existing graph format and covers it with the receipt checksum. Format3 serialization/digests remain unchanged; a valid old graph-only receipt returns its original acknowledgement with `COSMIC.CommittedDeltaUnavailable=ReceiptPredatesDelta`. Replica receipts keep their existing protocol. Operation/intent mismatch is now checked before stored metadata construction; a dedicated constructor-probe regression demonstrates the original ordering defect.

The Core validator additionally binds the predecessor logical version/ID to the acknowledgement, rejects another record's membership manifest, and rejects field-existence claims inconsistent with entity creation/deletion. Its original six-case gate fails5/passes1; the previously guarded physical-predecessor mismatch is the control. The new Shared tests cover retained/redacted values, old receipts and checksum-valid actor/operation/physical/logical revision rebinding.

## Values and API

Use the [same HolonManager Save-result metadata key and example](COMMITTED_DELTAS.md#reading-a-save-result), selecting `ProviderType.MongoDBOASIS`. Summaries omit previous/current values and historical membership identities/roles. Native `IsVersionControlEnabled` controls delta-value retention at admission; this does not certify the provider's broader historical-load/archive implementation. Already encrypted values stay storage ciphertext; this feature performs no historical decryption and leaves current clear/reserved metadata query keys unchanged.

Authorized historical-value access, encryption/key-retention policy, configurable retention, operator reconciliation, ordinary CRUD/replica enrollment and wider provider rollout remain open.

## Verification

The initial two native cases reject the old producer at capability admission before creating a fixture. After implementation, selected native conditional14/14 passes: the original12 cases plus history-on/off scenarios proving actual BSON deltas, precise numeric changes, missing/null/array changes, retained child bytes, late child rejection rollback, soft/hard deletion and original replay after physical deletion/policy change. Successful fixtures are removed and the owned Mongo process stops normally. No blind fixture cleanup is used.

Committed Core `aa209f94418d5f11280345ceebd411c912717858` builds both targets and is remotely verified. Its full1,552/1,552, Shared904/904 and SQLite delta compatibility11/11 pass, zero skips. Final broad Mongo86/86, Neo4j13/13, paired Shared904/904 and full SQLite1,089/1,089 pass, zero skips. Original reports: `TestResults/CosmicMongoCurrent/core-committed-delta-lineage-red.trx`, `TestResults/SharedCurrent/shared-delta-receipt-construction-red.trx`, `TestResults/CosmicMongoCurrent/mongo-committed-delta-admission-red.trx`. Closure reports are `core-mongo-delta-committed-full.trx`, `TestResults/SharedCurrent/shared-mongo-delta-committed-full.trx` and `TestResults/SQLiteCurrent/sqlite-mongo-delta-compatibility.trx`. Final evidence: `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx`, `TestResults/Neo4jCurrent/neo4j-cosmic-conditional-current.trx`, `TestResults/SharedCurrent/shared-mongo-delta-paired-full.trx`, `TestResults/SQLiteCurrent/sqlite-mongo-delta-full.trx`. `TestResults/CosmicMongoCurrent/mongo-committed-delta-final-artifacts.json` records Core SHA256 `7B8680F86B797D84CAE0120D167F0BFA36E72B2BC4B7E9EFB22A379DC17620CC` across five consumers and Shared SHA256 `C95D965D46347ACC9AAC89EDF46978AA09AF25F64C3F33BA580910985D1F4EC6` across four. The owning batch terminated normally, the fixed SQLite fixture is absent, and free disk remains65.8GB. The local-path documentation audit checks57 Markdown files/418 paths with zero broken targets; anchors are outside that check. No Development/master deployment or whole-roadmap completion is included.
