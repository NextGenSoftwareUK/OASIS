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
  No native Mongo/Neo receiver binding, automatic enrollment binding, durable policy-version
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
Next:implement native transaction-bound admission for Mongo, then consume native
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
No downloads,installations,restores or replacement environments. Next native owner:Mongo.
