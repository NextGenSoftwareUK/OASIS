# Native source-outbox ownership

Updated28October2026. Implementation in progress;X10 stays red.

## Required invariant

Native source identity must be validated in the same native transaction/snapshot as queue access. A preflight capture alone cannot authorize a later claim, lease renewal or acknowledgement. Copying a queue and its receipt/lease token must not transfer its source ownership. Failure must change neither queue state nor attempt counts.

## Current implementation

SQLite's existing two-argument outbox constructor remains the explicit unbound contract. A new three-argument constructor accepts an immutable expected source identity. Bound claim, renewal, finish and recovery finish validate `SqliteStorageIdentity.Read` after BEGIN IMMEDIATE and before queue access. Bound inspect, peek and route inventory keep identity and queue reads in one read transaction. No schema/identity initialization happens in the outbox.

Mismatch returns `COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH`;unverifiable native identity returns `COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_INVALID`. Neither reports a successful claim or mutation. A foreign provider authority is rejected at construction.

| Acceptance | SQLite | MongoDB | Neo4j |
|---|---|---|---|
| Transaction-bound native outbox implementation | ✅ | ❌ | ❌ |
| Actual native queue copy/replacement refuses original identity | ✅ native backup copy | ❌ | ❌ |
| Automatic worker supplies original expected source | ❌ | ❌ | ❌ |
| Concurrent source replacement/claim race proof | ❌ | ❌ | ❌ |

## Native proof

`OutboxSourceBindingTests.cs` → `CopiedOutboxCannotReadClaimRenewOrAcknowledgeTheOriginalSourceLease` creates a real source write/lease, then SQLite BackupDatabase copies payload, UUID and exact lease token into an owned different file. Bound inspection/peek/inventory/claim/renew/finish/recovery finish all reject the copy. `PRAGMA data_version` proves those refusals commit no changes. Corrupting only the owned copy's identity produces an explicit invalid-binding error without delivery repair. The actual source still renews/completes its original lease and returns no unfinished heads.

Six native tests pass including existing lease fencing, competing clients, expiry ordering, read-only blocked-head inspection and atomic recovery-evidence failure. Evidence:`TestResults/OwnedRoutingCurrent/outbox-source-sqlite.trx`;SHA-256:`EA8B327B8756E6F95ED6735AD186160360EEDFF0FE6AB996DA3DFAC109284407`. Build/log outputs are adjacent. Fixture copy/sidecars are removed only after successful checks/connection disposal;unknown outcomes retain the existing pending journal.

## Exact next step

Implement the same transaction-bound outbox expectation in Mongo and Neo using their existing native source validators. Add the provider factory contract that captures the activated native owner with the expected identity, then require it in the shipping automatic worker. Prove real replacement and claim/renew/finish refusal before claiming automatic ownership completion. Durable cold-start offline identity snapshots and operator repair remain separate acceptance.
