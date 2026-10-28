# Transaction-bound SQLite replica admission

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
Next:implement native transaction-bound admission for Mongo/Neo, then consume native
identities in automatic enrollment and persisted route discovery/operator reconciliation.
