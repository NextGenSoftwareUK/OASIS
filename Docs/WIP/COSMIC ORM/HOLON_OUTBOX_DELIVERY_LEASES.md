# Holon outbox delivery leases

Updated 19 October 2026. Local SQLite implementation; full delivery and all-provider certification remain open.

## Ownership protocol

`IHolonReplicationOutbox` consumes the same immutable per-target obligations that [atomic enrollment](ATOMIC_HOLON_OUTBOX.md) writes. It does not create a second mutation log or reconstruct payloads from current holons. `SqliteHolonReplicationOutbox` opens an existing durable database in ReadWrite mode; it neither creates a missing database nor initializes/repairs schema.

Every claim receives a fresh GUID lease token. Claim identity includes sequence, operation, target, worker and token. Renew and finish compare all these fields, require InFlight state and require expiry strictly after the current UTC time. Matching a reused worker name is insufficient. The original claim object remains usable after renewal because the stored token and expiry are authoritative; renewal never shortens the stored expiry.

Claim, renewal and completion acquire the native IMMEDIATE writer reservation before reading the clock. This matters when a writer waits for a lock: a timestamp captured before that wait must not authorize an already expired lease. Lease durations must be positive and at most one hour. Production uses the existing HyperDrive UTC clock; tests inject a clock without wall-clock sleeps for expiration cases.

The oldest unfinished obligation per target is the only eligible claim. A future retry, active lease or reconciliation block prevents later revisions for that target from overtaking it. Other targets remain independent. This deliberately uses target-wide ordering; future finer partitions must preserve graph/source lineage and cannot silently bypass blocked revisions.

Before changing claim state, the store verifies SHA256 and decodes the versioned graph, then checks operation/source/authority/actor against the immutable enrollment fields. Corrupt payloads fail without consuming an attempt or acquiring a delivery lease. A successful native claim increments the attempt counter once; expired claims may be reclaimed with a new token and the same original operation/payload.

## Durable outcomes

- Completed retains the immutable obligation and records completion time; it becomes ineligible for automatic claim.
- Retry retains diagnostics and a required future UTC retry time. It blocks following revisions until due.
- ReconciliationRequired retains diagnostics and blocks automatic replay. There is no implicit operator override or automatic source mutation retry.

A stale or expired owner receives `COSMIC_OUTBOX_LEASE_LOST` for renewal/completion. Native and validation errors remain explicit OASISResult errors with their exceptions. Diagnostics are bounded to 128-character codes and 4,096-character messages; invalid outcome/diagnostic combinations reject instead of being silently rewritten. Caller cancellation before an operation remains cancellation.

## Schema and evidence

Migration `20261019234500_AddCosmicHolonDeliveryLeases` adds delivery state, lease token/worker/expiry, attempt/retry/error and completion fields. Existing version-one obligations become Pending with zero attempts, preserving the original frozen payload/digest. The EnsureCreated schema owner applies the same columns. Downgrade refuses to remove delivery state while any obligation remains.

Native cases cover competing clients, reused worker names, renewal beyond the original expiry, exact-boundary expiration, immutable retry identity, target isolation, reconciliation blocking, corruption before mutation, rollback of a failed claim, missing-database admission and old-schema upgrade/downgrade protection. A reservation-checking clock proves the claim/renew/finish clock reads occur while another writer is excluded. Final verification passes **646 SQLite native, 1,012 Core unit and 549 shared unit cases**, zero failures/skips. Core net10.0/netstandard2.1 and shared/SQLite producer builds pass with existing warnings. Final hashes are recorded in [verification](VERIFICATION.md).

Reproduce with the existing SQLite integration test project using `dotnet test --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false`; build Core/shared/SQLite producers first in dependency order. Reports remain in the fixed `TestResults/CosmicMongoCurrent` directory.

## Remaining acceptance

The explicit [bounded dispatcher](HOLON_OUTBOX_DISPATCHER.md) now connects fenced claims to the committed-graph target adapter and renews during long I/O. Native SQLite-to-MongoDB delivery passes. Automatic host scheduling and default registration remain open. Operator reconciliation/resume requires authenticated authority and original receipt evidence; retention, bounded queue/payload policy, clock assumptions, real restart/process/wire-loss tests and MongoDB [enrollment/delivery](MONGO_OUTBOX_DELIVERY.md) is now implemented; Neo4j remains open. No default auto-replication durability, exactly-once transport, universal availability or full-roadmap completion is claimed.

## Concurrent session coordination

David authorized direct coordination with the active “Hyperdrive & Providers” session. Scope messages were exchanged: that session owns Oracle/OCI/provider completion and generic backend hooks; this batch owns COSMIC contracts and SQLite transaction/delivery behavior. Both preserve the other's dirty files and scoped evidence; neither session's passing dirty-build tests establish clean-source deployment parity. No new branch, commit, push or deployment was performed by this COSMIC batch.
