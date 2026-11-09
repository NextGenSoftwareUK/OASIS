# Atomic primary-source identity binding

28October2026. Scoped X10 prerequisite;automatic routing/placement remains incomplete.

HolonReplicationEnrollment now has optional immutable SourceStorageIdentity and a
three-argument constructor. The existing two-argument constructor remains unbound.
IBoundAtomicHolonReplicationStorageProvider advertises native enforcement;V2 routing
refuses bound writes/recovery when the selected provider lacks that capability.
There is no downgrade to an ordinary atomic/unconditional write.

SQLite,Mongo and Neo implement the capability. Native source identity is checked
BEFORE receipt replay,graph,receipt or obligation mutation inside the same native
transaction. Bound enrollment uses fingerprint format2 including the physical binding;
unbound format1 bytes remain unchanged. Explicit/embedded enrollment cannot disagree
about source identity. An old operation cannot change/drop binding and replay a
successful receipt as if the expectation had never existed.

SQLite reuses strict native identity metadata reading under BEGIN IMMEDIATE. Mongo
reuses its expected collectionUUID assertion as the first transaction command. Neo
reads db.info on its write transaction while holding the lifecycle gate. Each returns
COSMIC_SOURCE_STORAGE_BINDING_MISMATCH for a different source (invalid/unavailable
identity returns the corresponding explicit failure). No native metadata is repaired
merely to satisfy an enrolled write.

Read-only original-outcome recovery also verifies source identity in its read transaction
before accepting the original receipt. SQLite uses its existing file's read-only
snapshot,Mongo a primary majority transaction with native UUID assertion,and Neo a
read transaction pinned by its lifecycle gate. Failures return unverified,not a false
Committed outcome. No claim,preparation,fence or business mutation is performed.

Mongo bound recovery and queue capture now use the activated repository owner even
if the public Database descriptor is null/replaced. An outbox's session client comes
from its captured native database,so the two handles cannot be independently retargeted.
This keeps write,capture,recovery and queue observations attached to one native owner.

Verification (0failures/0skips):
-4386fullCore tests,including sync/async refusal of a plain atomic provider for bound intent.
-1285fullShared tests,including physical-source fingerprint distinction,unbound intent
  compatibility,and rejection of conflicting embedded/explicit source expectations.
-SQLite5native:old unbound/bound sync/async enrollment,replay,original-outcome recovery,
  wrong-source refusal and downgrade rejection;actual backup copy retains its logical
  UUID/receipt but a changed endpoint cannot verify/replay it. PRAGMA data_version
  confirms the rejected copy/recovery operations do not mutate it.
-Mongo5native:same old/bound sync/async primary matrix,plus actual owned collection
  drop/recreation while keeping original receipt/outbox. It cannot implicitly recreate
  the missing Holon store or verify its old receipt under the replaced UUID. Public
  Database=null still yields the correct activated queue and original bound outcome.
-Neo4native:old/bound sync/async rollback,commit/replay,recovery and wrong-source cases.
  No shared Neo database was dropped/recreated;administrative replacement/cluster/home-
  database transitions remain broader acceptance,not inferred from expected-ID mismatch.

Failures were retained and reconciled,not reset blindly:Mongo's UUID exception branch
initially emitted the replica diagnostic for source mismatch (rejection itself worked);
fixed the hardcoded branch. Its recorded physical-source receipt/fingerprint/frozen
obligation and intentionally removed graph were validated before exact fixture cleanup.
Neo's expanded test replay initially omitted its newly bound expectation;corrected
that test intent. Both original committed receipts and live versions were verified
against recorded failure timestamps/report hash before exact owned-node cleanup;
shared sequence counters and unrelated rows were retained. A disposable recovery-runner
filter/count mismatch was detected before claiming reconciliation;its4passing primary
cases are preserved separately,followed by the correct1case recovery proof. Recovery
build's INode alias error was fixed before it ran. Raw failed evidence remains in Archive.

Evidence (raw files are only in the worktree TestResults):
- TestResults/OwnedRoutingCurrent/source-binding-core.trx: SHA256 5D7F9EF6DC7EC1E21B59C16BEC10103562D7E96A576D70004D8183E1112FD032.
- TestResults/OwnedRoutingCurrent/source-binding-shared.trx: SHA256 C16C03CDDA0E71D8B311F8201B04E5926D2A2680F08C5EFC50B1A6F758AD17F5.
- TestResults/OwnedRoutingCurrent/source-binding-sqlite-final.trx: SHA256 78CF46155EF4094120CD168FB2E6DA33AE7B91845D68BD9E2FC850C00D51C6FC.
- TestResults/OwnedRoutingCurrent/source-binding-mongo.trx: SHA256 94922D5857A1E03E56308E7FD93A198B96A5856FA3E8C598B27AEE1FE18FAAEE.
- TestResults/Neo4jCurrent/source-binding-neo-final.trx: SHA256 AB7EEE1F072D52F3A189B9C66642220622A3DA5B254BDD9C8F5EEA618A1C6898.
- TestResults/Neo4jCurrent/neo4j-source-binding-reconciliation-current.trx: SHA256 8FB12D6E01A0BADD6479684DFD063B7FF33E685DEFCE83894B09DBF31B08F574.

All owned native services stopped;successful fixtures/physical-copy files/journals cleared.
No installation/download/restore/new environment. Existing pinned services reused.
The default automatic Plan/worker still uses canonical type-only routes and does NOT
supply this source expectation. Next:publish deterministic native source/target/policy
route snapshots,use bound enrollment/receivers in the worker,and consume inventory to
reconcile old/unbound/changed routes explicitly. Grants,residency,encryption preparation,
placement,other crash directions and the remaining full ORM roadmap remain open.
