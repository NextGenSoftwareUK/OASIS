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

The weak association is process-local, not a durable outbox, restart recovery mechanism, signed receipt or proof that a provider actually committed. Providers must attach evidence only after their authoritative success boundary. Nothing currently attaches or applies it automatically.

## Remaining implementation

1. Ethereum: publish the exact tombstone after successful paired native receipt validation; use the existing full-identity/ciphertext-CAS transaction to apply a supplied timestamp rather than selecting a new one.
2. SQLite: publish evidence only after commit; apply the supplied timestamp/attribution/inactive state to both matching records within one local transaction. Preserve existing data and rollback/tracker recovery behavior.
3. Preserve evidence through username/email/provider-key deletion wrappers, not just generic V2 conversion.
4. V2 replication: apply committed evidence through the explicit capability instead of replaying deletion. Missing evidence/capability must produce an explicit replication failure diagnostic after a primary commit, not a hidden command fallback or false complete success.
5. Prove exact canonical timestamp, attribution and inactive flags in both directions, sync/async. Retain independent raw/database reads, failed-secondary diagnostics, no replay after uncertain submission, and zero backup requests for terminal identity errors.
6. Resolve absent/stale/already-deleted replicas, concurrent updates, optional/missing detail, unsupported providers and durable recovery. Do not fabricate full account records to make a patch succeed.
7. Extend relevant provider implementations and hosted route tests before claiming all-provider support. Existing legacy behavior is not certified by this work.

## Verification boundaries

The existing Ethereum native suite still tests the earlier command-replay path and records timestamp divergence. The eight foundation tests are not end-to-end replication evidence. The current release-gates document records exact run results and remains authoritative for unresolved gates.
