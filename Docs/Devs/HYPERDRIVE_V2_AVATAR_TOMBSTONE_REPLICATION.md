# V2 committed avatar tombstone replication

Status: in progress. Exact-state replication remains a release gate. No dev merge or hosted rollout is authorized by the tests below.

## Observed defect

V2 replays `DeleteAvatar` on each replica. Each provider independently chooses its deletion timestamp. Ethereum and SQLite now both deactivate the pair with one local UTC timestamp, but replicas still disagree on that timestamp. Passing logical deletion tests does not prove identical committed state.

## Required invariant

A replica applies the lifecycle state actually committed by the primary, preserving its own profile, credential, inventory and statistics data. It must not choose a second clock value, manufacture missing account data, silently replay the original command, or claim atomic pair persistence after only one record was updated.

## Implemented foundation

- `AvatarTombstone` is immutable: nonempty avatar GUID, nonempty UTC deletion timestamp, deleting-avatar GUID. It contains no profile or credential payload.
- `AvatarTombstoneEvidence` associates committed state with a successful deletion result using `ConditionalWeakTable`, outside serialized result metadata. Evidence is immutable once attached; generic V2 result conversion transfers it. Error results cannot acquire evidence through transfer.
- `IAvatarTombstoneStorageProvider` specifies synchronous/asynchronous application of this lifecycle patch, atomically preserving existing account data.
- Eight focused tests cover invalid descriptors, failed-result rejection, concurrent attachment, V2 generic conversion and non-disclosure through Newtonsoft/System.Text.Json serialization.

The weak association is process-local, not a durable outbox, restart recovery mechanism, signed receipt or proof that a provider actually committed. Ethereum now attaches it after receipt validation and implements explicit atomic application. SQLite and V2 replication dispatch are not yet wired to this capability.

## Remaining implementation

1. Ethereum implementation is present: receipt-bound publication, supplied-timestamp paired CAS, replica-local profile/XP/karma preservation, no-write recognition of an exact match and rejection of conflicting prior tombstones/missing accounts. Two real contract instances verify these paths; missing-account repair and concurrency stress remain open.
2. SQLite: publish evidence only after commit; apply the supplied timestamp/attribution/inactive state to both matching records within one local transaction. Preserve existing data and rollback/tracker recovery behavior.
3. Ethereum username/email/provider-key deletion wrappers now preserve the actual result/evidence and failure cause/code. SQLite and other providers still need equivalent preservation.
4. V2 replication: apply committed evidence through the explicit capability instead of replaying deletion. Missing evidence/capability must produce an explicit replication failure diagnostic after a primary commit, not a hidden command fallback or false complete success.
5. Prove exact canonical timestamp, attribution and inactive flags in both directions, sync/async. Retain independent raw/database reads, failed-secondary diagnostics, no replay after uncertain submission, and zero backup requests for terminal identity errors.
6. Resolve absent/stale/already-deleted replicas, concurrent updates, optional/missing detail, unsupported providers and durable recovery. Do not fabricate full account records to make a patch succeed.
7. Extend relevant provider implementations and hosted route tests before claiming all-provider support. Existing legacy behavior is not certified by this work.

## Verification boundaries

The existing V2 cross-provider tests still exercise command replay and record timestamp divergence. Explicit Ethereum-to-Ethereum tombstone application is tested independently, not through V2 dispatch. The eight foundation tests are not end-to-end replication evidence. The current release-gates document records exact run results and remains authoritative for unresolved gates.
