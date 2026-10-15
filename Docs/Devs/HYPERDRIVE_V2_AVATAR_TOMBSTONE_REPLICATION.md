# V2 committed avatar tombstone replication

Status: in progress. Exact-state replication remains a release gate. No dev merge or hosted rollout is authorized by the tests below.

Latest failure-path evidence: current real Mongo/SQLite matrix4/4 (22s) includes validator-induced Mongo detail failure with full replica-pair rollback, primary SQLite success plus replication warning, and native commitTransaction rejection preserving reconciliation-required markers with no committed evidence. Validator/failpoint cleanup runs in finally. These checks do not prove successful-commit/lost-reply recovery or durable reconciliation; those gates remain open.

Latest Mongo capability update: implemented snapshot/majority transactional lifecycle-only application with exact ticks, duplicate/conflict/missing-avatar rejection, optional detail, no-write repeat detection and original uncertain-commit marker. Current real V2 Mongo/SQLite matrix passes4/4 (22s) with exact committed equality in both directions and sync/async, while retaining replica-specific XP101/202 and Karma29. Explicit repeat/conflict/missing tests pass. Older statements that Mongo capability is absent are historical. Failure-injection and durable recovery remain required before broad certification.

Replica-failure follow-up: real sync/async V2 trigger-injection cases pass; primary Ethereum success is preserved, SQLite pair rolls back completely and a replication warning is surfaced. Native suite now **91/91, zero skips,29s**. Unsupported capability dispatch coverage and broader recovery/policy gates remain outstanding.

Latest local update (supersedes historical pending statements below): SQLite capability and V2 automatic committed-state dispatch are implemented in the working tree. SQLite passed 278/278 tests; real Ethereum/SQLite native tests passed 89/89 with exact timestamp/actor/inactive-state assertions in both primary directions and sync/async paths. Explicit soft-delete command replication is now rejected before quota/provider I/O because it lacks primary commit evidence; focused evidence tests passed 13/13 and both Core framework builds passed. No silent command replay is used for missing evidence or unsupported capabilities. Replica-failure and unsupported-capability dispatch tests, durable recovery and remaining policies are still required. These latest changes are uncommitted and not deployed.

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
2. SQLite implemented and locally verified: publishes evidence after commit; applies supplied timestamp/attribution/inactive state atomically while preserving profile data and unrelated tracked changes. Full suite 278/278.
3. Ethereum username/email/provider-key deletion wrappers now preserve the actual result/evidence and failure cause/code. SQLite and other providers still need equivalent preservation.
4. V2 replication implemented: applies committed evidence through the explicit capability instead of replaying deletion. Missing evidence/capability produces explicit replication failure diagnostics. Explicit unproven soft-delete command replication is rejected. Four sync/async dispatch guard tests reject forged payloads/unsupported capabilities before storage calls; focused suite 17/17.
5. Prove exact canonical timestamp, attribution and inactive flags in both directions, sync/async. Retain independent raw/database reads, failed-secondary diagnostics, no replay after uncertain submission, and zero backup requests for terminal identity errors.
6. Resolve absent/stale/already-deleted replicas, concurrent updates, optional/missing detail, unsupported providers and durable recovery. Do not fabricate full account records to make a patch succeed.
7. Extend relevant provider implementations and hosted route tests before claiming all-provider support. Existing legacy behavior is not certified by this work.

## Verification boundaries

Exact Mongo deletion representation now preserves full UTC ticks through an optional companion while leaving the BSON date field intact. ISupportInitialize validates/reconstructs after field loading; reordered documents and legacy date-only documents are covered, conflicts fail. Three serializer tests and four real routing cases pass with untruncated primary timestamps. This supersedes the earlier millisecond-only primary workaround. Mongo replica capability is still required, and native BSON date range comparisons remain millisecond-granularity unless explicitly enhanced; do not claim exact-tick query parity from representation tests.

Mongo primary pair lifecycle/evidence is now corrected and verified in all four real sync/async Mongo/SQLite routing matrix cases. The primary chooses millisecond-precision UTC matching persisted BSON, deactivates both rows and normalizes shared actor; uncertain-commit markers remain unchanged. Mongo replica capability is not implemented yet. A supplied timestamp may contain finer precision than BSON DateTime, so an explicit exact representation is required before claiming arbitrary cross-provider state equality. Rounding incoming evidence would violate the invariant.

The current real V2 Ethereum/SQLite tests assert exact committed state in both directions, with sync/async coverage and replica-failure rollback cases (native suite 91/91). Offline evidence/guard tests (17/17) are not native-provider evidence. MongoDB pair deletion still lacks inactive-state updates and committed-evidence/capability wiring; its existing uncertain-commit marker must be retained. The current release-gates document records exact run results and remains authoritative for unresolved gates.
