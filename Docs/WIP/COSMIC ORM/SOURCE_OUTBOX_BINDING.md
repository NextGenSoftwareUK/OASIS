# Native source-outbox ownership

Updated28October2026. Implementation in progress;X10 stays red.

## Required invariant

Native source identity must be validated in the same native transaction/snapshot as queue access. A preflight capture alone cannot authorize a later claim, lease renewal or acknowledgement. Copying a queue and its receipt/lease token must not transfer its source ownership. Failure must change neither queue state nor attempt counts.

## Current implementation

SQLite's existing two-argument outbox constructor remains the explicit unbound contract. A new three-argument constructor accepts an immutable expected source identity. Bound claim, renewal, finish and recovery finish validate `SqliteStorageIdentity.Read` after BEGIN IMMEDIATE and before queue access. Bound inspect, peek and route inventory keep identity and queue reads in one read transaction. No schema/identity initialization happens in the outbox.

Mismatch returns `COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH`;unverifiable native identity returns `COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_INVALID`. Neither reports a successful claim or mutation. A foreign provider authority is rejected at construction.

| Acceptance | SQLite | MongoDB | Neo4j |
|---|---|---|---|
| Transaction-bound native outbox implementation | ✅ | ✅ | ❌ |
| Actual native queue copy/replacement refuses original identity | ✅ native backup copy | ✅ native collection missing/replacement | ❌ |
| Automatic worker supplies original expected source | ❌ | ❌ | ❌ |
| Concurrent source replacement/claim race proof | ❌ | ✅ native UUID/DDL transaction | ❌ |

## Native proof

`OutboxSourceBindingTests.cs` → `CopiedOutboxCannotReadClaimRenewOrAcknowledgeTheOriginalSourceLease` creates a real source write/lease, then SQLite BackupDatabase copies payload, UUID and exact lease token into an owned different file. Bound inspection/peek/inventory/claim/renew/finish/recovery finish all reject the copy. `PRAGMA data_version` proves those refusals commit no changes. Corrupting only the owned copy's identity produces an explicit invalid-binding error without delivery repair. The actual source still renews/completes its original lease and returns no unfinished heads.

Six native tests pass including existing lease fencing, competing clients, expiry ordering, read-only blocked-head inspection and atomic recovery-evidence failure. Evidence:`TestResults/OwnedRoutingCurrent/outbox-source-sqlite.trx`;SHA-256:`EA8B327B8756E6F95ED6735AD186160360EEDFF0FE6AB996DA3DFAC109284407`. Build/log outputs are adjacent. Fixture copy/sidecars are removed only after successful checks/connection disposal;unknown outcomes retain the existing pending journal.

## Mongo native gate

`MongoHolonReplicationOutbox(context, expectedSource)` captures the actual native database/client once. Every bound mutation runs the existing native UUID validator as the first statement in its existing snapshot transaction. Bound inspect/peek/inventory use the same transaction owner, including known-outcome cleanup handling. Explicit unbound reads retain their majority observation contract. The shared validator is reused, with outbox-specific mismatch/invalid error codes;no duplicate UUID/endpoint algorithm.

`MongoBoundOutboxTests.cs` proves missing/recreated native Holon collections cannot read, claim, renew, finish or attach recovery evidence to the retained original lease. Raw native BSON equality proves no outbox mutation;the original write receipt remains. Repointing the public context cannot redirect the captured owner or create the forbidden Ghost database.

The native race case pauses the real driver after UUID admission and before the lease query. Another client attempts to drop Holon with maxTimeMS500 and receives native timeout50 while the same claim transaction owns the namespace. The test releases the original transaction, verifies its operation identity, then renews and completes its lease. This is live namespace/claim proof, not a mocked preflight. Collection UUID admission remains server-version-specific (local Mongo8.3.11);unsupported servers refuse binding rather than using an unbound path.

Mongo gate:8passed (3new outbox cases plus4existing atomic-source cases and1physical original-receipt recovery case). Evidence:`TestResults/OwnedRoutingCurrent/source-binding-mongo.trx`;SHA-256:`6B59EE59A1891E4C2A738A9FB91B14BF1EE332FB63C2AC24025BF7882CCA4C43`. Existing fixture journals absent and owned Mongo process stopped after success. No new runtime/download/database environment.

## Exact next step

Implement the same transaction-bound outbox expectation in Neo using its actual driver endpoint and native database identity. Add the provider factory contract that captures the activated native owner with the expected identity, then require it in the shipping automatic worker. SQLite race proof and automatic routing integration remain open;do not claim full source-outbox ownership completion. Durable cold-start offline identity snapshots and operator repair remain separate acceptance.
