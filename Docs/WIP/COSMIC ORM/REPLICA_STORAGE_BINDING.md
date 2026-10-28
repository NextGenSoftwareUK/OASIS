# Transaction-bound native replica admission

28 October 2026. Scoped prerequisite for X10; full convergence/placement remains red.

## Invariant and implementation

A receiver must compare the enrolled endpoint/store identity under the same native
transaction that owns graph, lineage and receipt. A successful receipt replay cannot
bypass that check. Capture is not sufficient: configuration can change after capture
or even after policy authorization.

Core adds the optional IBoundCommittedHolonReplicaStorageProvider capability. SQLite
implements both sync/async surfaces through its existing single transaction owner.
After BEGIN IMMEDIATE, it reads the actual main-file path and durable identity row
from that connection, compares the full binding fingerprint, then reads receipts and
applies graph/lineage. No preflight connection, new write pipeline or delivery retry.

Capture and transaction admission share one strict read implementation. Only explicit
CaptureStorageIdentityAsync may initialize an absent metadata table. Replica admission
never initializes/repairs it. Missing/empty/corrupt metadata returns
COSMIC_REPLICA_STORAGE_BINDING_INVALID; a different endpoint/store/authority returns
COSMIC_REPLICA_STORAGE_BINDING_MISMATCH. Neither writes business rows or acknowledges
a prior receipt. Rollback/unknown-commit handling remains in the existing transaction owner.

Shared HolonReplicationDestination accepts a native CosmicStorageIdentity plus the
bound receiver capability. Its route digest uses catalog-bound-route-v1, so an unbound
string descriptor cannot impersonate this contract even with identical text. Store or
endpoint changes produce a different route. CommittedHolonReplicaTarget passes the
captured identity only through the bound native method after policy admission. Existing
five/nine-argument constructors and unbound route digest bytes are retained.

## Proof and limits

- Seven new native cases: sync/async matching apply and exact replay; endpoint mismatch;
  store replacement rejects old receipt/new mutation; missing/empty/corrupt metadata;
  replacement between policy authorization and native apply; independent writer reservation.
- Lock proof runs another real SQLite connection during EF graph read, after identity
  admission. Identity replacement fails with native BUSY/LOCKED until the replica commits;
  the same replacement succeeds after commit and then rejects old receipt replay.
- 23 affected native cases pass (7 new plus identity and existing replica regressions).
- 1,280 full Shared tests pass, including 2 new capability/authority/no-downgrade/route-digest controls.
- 4,377 full Core tests pass. Both Core target frameworks, Shared, SQLite and consumers build.
- Fixture has coherent source protocol lineage but is not an authoritative-source proof.
  At this SQLite checkpoint,no native Mongo/Neo receiver binding, automatic enrollment binding, durable policy-version
  enforcement, old-route reconciliation, source binding or tenant/residency acceptance is claimed.

First new native run used the wrong table name Holons (actual Holon). That authoring
error retained its journal and blocked following fixtures. Read-only reconciliation
proved zero graph/receipt/head rows and only intended corrupt identity metadata; exact
owned fixture cleanup preceded correction. Failed evidence is retained. Initial compile
also exposed a missing repository-interface declaration; fixed at that boundary.

## Evidence and next step

Raw evidence: C:/Source/OASIS-cosmic/TestResults/BoundReplicaCurrent; no raw output in Docs.
- sqlite.trx: SHA256 2B84528B4705E002F7D6EB056965B9ACFC64C980073913310D1ACB2EE9F040F0.
- shared.trx: SHA256 348C651B07C36EA69DB58EB0326BA268FC10C87104F6267D33287C78514353A4.
- core.trx: SHA256 8E3DE180716DCA885089CE917319649AAF1EE28B8FDAFC863B5F52C4D4ED7076.

Successful fixtures disposed providers/contexts and removed their exact database/journal;
only the reusable environment.lock remains in SQLiteCurrent. No services/downloads/restores.
Remaining automatic routing still uses type-only IDs and must not be declared bound.
At that checkpoint,next was native Mongo admission, then consume native
identities in automatic enrollment and persisted route discovery/operator reconciliation.

## Neo4j receiver milestone

Neo4j now implements the same optional bound receiver capability through its existing
atomic graph/receipt owner. It holds the lifecycle gate across bound apply and queries
`CALL db.info() YIELD id,name RETURN id,name` on that write transaction, before even
MERGE/lookup of an operation receipt. Native metadata and the captured storage-driver
URI form the same fingerprint as explicit capture, using one shared parser. Mutable
Host and credentials cannot relabel that driver. No identity node/table is introduced.
An injected driver without captured endpoint refuses bound apply. The existing
unbound contract stays explicit; automatic type-only routes still use it and remain open.

Neo documents db.info as database identity metadata in READ mode:
[official procedure reference](https://neo4j.com/docs/operations-manual/current/procedures/built-in-procedures/#_db_info).
The retained native node also proves the procedure works inside the write transaction.

Three new native cases pass:sync/async mismatch before graph/receipt/head writes,
matching apply/replay, mismatch despite existing receipt, descriptor mutation and fresh
provider replay; plus refusal of an uncaptured injected driver. Three existing identity
cases also pass (6total). Existing 6native replica lineage/history/rollback/unknown-outcome
regressions pass separately. Producer/consumer hashes match Core/Shared/source driver;
Shared,SQLite,Mongo,Neo,WEB4 and native consumer rebuilt without package restore.

These cases supply mismatched expected IDs; they do not drop/recreate the retained
shared Neo database, alter its home database, exercise cluster topology, or certify
administrative database replacement while a transaction is active. The protocol
fixture models a committed source revision; authoritative source/save tests remain
separate. Automatic route discovery/source binding/policy durability remains open.

Evidence under C:/Source/OASIS-cosmic/TestResults/Neo4jCurrent:
- neo4j-storage-identity-current.trx:6passed/0failed/0skipped;SHA256 6E458E76BCB887EE65C6B6C8F74A84020AD39F81CE6A1BCFDC06D3E25566A7C9.
- neo4j-cosmic-replica-current.trx:6passed/0failed/0skipped;SHA256 6482F446718E2D35FE1FE0832EA0A1485651F3A8BD6F5FBE9F5443ECF735EBB3.

Earlier 3case identity report29C73FAC4399CC8B785903FB0701EE0E7F6CC35B350F8F3AF748ABA7C078B719
is preserved in TestResults/Neo4jCurrent/Archive. Other fixed-path historical raw hashes
retain earlier revision scope; do not assume the latest TRX has historical bytes.
Each new fixture journals before apply, deletes only its exact root/head/receipt after
success, verifies absence, deactivates providers and removes its completed journal.
Both owned WSL/container processes exited; retained container stopped, Bolt port free.
No downloads,installations,restores or replacement environments. Mongo was next at that checkpoint.

## MongoDB receiver and single-capture acknowledgement milestone

Mongo implements the same bound receiver capability. It compares native database/client
endpoint identity, then sends a native find with expected collectionUUID as the first
operation in the SAME snapshot transaction, before receipt replay, fences or graph writes.
The empty single-batch read retains native namespace ownership until transaction exit.
Missing/recreated Holon collections reject without implicit creation or unbound retry.
All graph/fence/head/receipt/outbox helpers now share the captured IMongoDatabase and
its client. A mutable MongoDbContext cannot redirect auxiliary writes during an await.
This fixes the transaction-owner root cause rather than adding a preflight connection.

Mongo prohibits listCollections inside transactions ([official restrictions](https://www.mongodb.com/docs/manual/core/transactions-operations/)).
The server defines collectionUUID as an expected UUID with unstable API status
([official source IDL](https://github.com/mongodb/mongo/blob/master/src/mongo/db/query/find_command.idl)).
It is verified here on retained Mongo8.3.11; unsupported/strict-API/server refusal fails
closed as COSMIC_REPLICA_STORAGE_BINDING_INVALID, never weaker admission. UUID/endpoint
mismatch returns COSMIC_REPLICA_STORAGE_BINDING_MISMATCH. No cross-version, Atlas,
sharded-cluster or universal vendor compatibility claim is made by this local proof.

Three new native cases prove sync/async matching apply/replay, expected store/endpoint/
authority mismatch, ACTUAL owned Holon drop/recreation while retaining old head/receipt,
and no missing-collection repair. The concurrency case pauses the real driver after
UUID admission, proves a second connection's drop hits its bounded server timeout,
then mutates the public context to another database before release. Graph, operation
fence, head and receipt commit only to the captured database; the other DB stays absent.
The UUID command carries startTransaction/autocommit/lsid/txnNumber as asserted evidence.

The expanded regression exposed a pre-existing post-commit acknowledgement bug:
ordinary save re-enumerated a ReadOnceChildHolon getter after committing its captured
graph, then reported membership mismatch. Core now accepts the original immutable
HolonChildCollectionState bound by owner reference; Shared plans retain and publish
that capture. Existing two-argument Core API is retained. Foreign captures are rejected,
partial/scoped selections still cannot become complete, and later scalar/membership
edits remain dirty. No save retry, second graph selection or weakened scope guard.

Latest verification:
- Mongo15native cases:3new bound,4identity,6primary routed/atomic,2existing replica.
- Core4379/Shared1283full suites;2additional scoped-guard variants and2Shared controls.
- SQLite23affected native and Neo6existing replica regressions pass after the shared fix.
- All have0failures/0skips. Both Core targets,Shared,SQLite,Mongo,Neo,WEB4/consumers build.
- Native source-protocol fixture proof remains distinct from automatic routing/policy
  binding. All3local native receiver capabilities exist; automatic type-only routes,
  source binding, queued route discovery/migration and durable policy meaning remain open.

Failure/reconciliation record:initial run11/15passed; DDL rejection was the correct
MongoExecutionTimeoutException rather than the test's expected base type, and native
replica regression exposed the getter bug. Second run13/15passed after that fix; the
existing fault test required enableTestCommands=1 in its OWNED test process. Its monitored
client now supplies both session and native database, matching the captured-owner contract.
No production server flags changed. Before any rerun, explicit opt-in recovery tests
validated the exact owned receipts/heads/rows and preserved raw evidence, then deleted
only recorded fixture DBs and verified absence. Failed TRX/logs remain hash-named in
TestResults/OwnedRoutingCurrent/Archive; reconciliation is never an automatic reset.

Raw latest evidence: C:/Source/OASIS-cosmic/TestResults/OwnedRoutingCurrent.
- mongo-bound-replica.trx: SHA256 475BB29B1E648FD75DE0EF044ACB6ED91986B5FBD382CEFAD736E5B31EDA8F90.
- captured-membership-core.trx: SHA256 A7EA9EFC528E3F80B8D68A837CCB76F2F70876514E0C36D6CEF2D720EBC22FE3.
- captured-membership-shared.trx: SHA256 438877641E9835285E7A85152EA0ACC5DF5CF04B5DD91653D5D28DFC6E4F7DDE.
- captured-membership-sqlite.trx: SHA256 9F63F69BF3EE201BD1838D62EE4F7894993995D3C4DFE9AA3EE7D6FCEDFCE845.
- mongo-bound-reconciliation.trx: SHA256 B3EFB4DF7240CA259E030E4F11A34B2BC74227662ECA37211D977CCB6904C806.
- mongo-prefault-reconciliation.trx: SHA256 49EF44AB972BFB058FC22AF519EDFD2E1375C5ED68F3DD4D8B44E920886CF753.
- bound-fixture-reconciliation.json: SHA256 AA75203A8293C29D2899D9BF64F89919A1029B39B0D14851E70882F607745185.
- pre-fault-fixture-reconciliation.json: SHA256 C6DDCAF3F9AE6FF76B5C851F525A527F9464715F85DBC35A33CA22BD46C4F508.
- TestResults/Neo4jCurrent/neo4j-cosmic-replica-current.trx: latest6case SHA256 31D277DC331F6FD735A8EE63CBBDA427733151232E0ADD7E6E495AC7E4D70965.

Prior fixed-path hashes above retain their checkpoint scope; this final Neo TRX replaced
the earlier settled file. No raw evidence was committed to Docs. Both native services
are stopped; journals/owned fixture databases are absent. Reusable runtimes retained.
Next:consume native bindings in automatic durable enrollment/delivery, including route
inventory and explicit operator reconciliation of old/unbound or changed policy routes.
