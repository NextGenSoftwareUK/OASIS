# MongoDB fenced holon outbox delivery

Updated 19 October 2026. Local native store/worker implementation; default host scheduling, authenticated reconciliation and wider programme acceptance remain open.

## Native owner

`MongoHolonReplicationOutbox` implements the existing recovery-capable outbox interface over the same `CosmicHolonOutbox` enrolled by the [primary graph transaction](MONGO_PRIMARY_OUTBOX.md). Construct it from an initialized provider context; it reuses that client/database, and does not create a queue, repair indexes, copy a database or enqueue after Save(). Source activation remains the schema/index owner.

Claims select the oldest unfinished sequence for exactly one target. A lease or future retry blocks later revisions for that target; reconciliation blocks automatic replay. Other targets are independent. A fresh GUID token is generated on every successful claim, including reuse of the same worker name. Concurrent claim transactions can return an explicit native write conflict; there is no hidden retry loop or skipping of the older obligation.

Claim/update state changes use snapshot transactions and majority+journal commit concern. Lease and retry eligibility are evaluated by server-side `$$NOW` expressions converted to UTC ticks; callers cannot renew an expired lease using an earlier worker timestamp. Mongo's server clock has millisecond resolution, although stored duration/expiry values use ticks. Leases are evaluated by the native command's server-time predicate, not a claim that all deployment clocks are perfectly synchronized. Native topology/time assumptions and managed/sharded deployment certification remain separate gates.

Renew/finish match sequence, operation, target, worker, token, InFlight state and unexpired native lease. Renewal never shortens the stored expiry. Dynamic worker ids, error text and recovery JSON use literal update expressions; values beginning with `$` are not evaluated as field references. Bad identities, durations, claims, outcomes and cancellation are admitted before starting a native session.

## Evidence and uncertainty

Claims validate format/identity/state/timestamps, payload SHA256 and source/actor binding before changing attempts or publishing a claim. Corrupt data fails without submitting it. Fenced completion persists state and recovery JSON/digest in one transaction; an evidence write rejection cannot leave a blocked state without its native identities. Read-only majority inspection validates the same typed recovery record and excludes business payload/lease token.

The single [recovery codec](HOLON_OUTBOX_RECOVERY_EVIDENCE.md) now lives in `HolonRecoveryEvidenceJson`, shared with SQLite. Format1 bytes are unchanged; unknown versions/types/fields, duplicate JSON fields, invalid state or checksum/presence mismatches fail explicitly.

Before dispatching a delivery-state commit, the owner captures its original session id and transaction number. A failed/uncertain commit returns that exact reconciliation marker and does not abort or submit another update. A known mutation or read-only result followed by session cleanup failure remains known with a warning. The cleanup owner accumulates abort/disposal causes explicitly, so a disposal error cannot hide the original native commit marker. The dispatcher now retains claim/renew/finish cleanup warnings as well as target warnings, without repeating the target mutation.

## Actual transport path

The native SQLite/MongoDB matrix now uses atomic enrollment -> fenced source outbox -> existing dispatcher -> committed target receiver -> durable source completion in **both directions**, followed by exact receipt replay and fresh provider reads. MongoDB-source delivery is no longer only a direct hosted receiver demonstration. Source graph and every target obligation still share the original primary transaction.

Explicit usage after activation/enrollment:

```csharp
var outbox = new MongoHolonReplicationOutbox(sourceProvider.Database);
var target = new CommittedHolonReplicaTarget("sqlite-eu", sqliteProvider,
    ProviderType.SQLLiteDBOASIS, "application-primary", ProviderType.MongoDBOASIS);
var worker = new HolonOutboxDispatcher(outbox, new[] { target }, "onode-worker-1");
var result = await worker.DispatchOnceAsync("sqlite-eu", cancellationToken: stoppingToken);
```

This remains a bounded explicit worker call. It does not register a BackgroundService or silently convert global/default auto-replication to durable mode.

## Verification

Two new sync/async native sequences cover ordering, target isolation, renewal after original claim expiry, expired/reused-worker fencing, retry payload identity, large recovery identities, atomic evidence failure, corruption, competing sessions and literal strings. They also inject an actual post-commit acknowledgement fault and prove the original delivery-state transaction with a causal majority read after its operationTime, while asserting exactly one update and no abort/replay.

Six new unit cases prove invalid admission/cancellation starts no session using interface-only proxies. Ten shared codec cases lock format1 bytes and corrupt/missing/state behavior; one worker case locks retained store warnings and single target dispatch. Final suites pass **140 Mongo unit, 630 shared unit, 60 Mongo native and 657 SQLite native cases**, zero failures/skips. The SQLite suite passes after codec extraction. The final fresh-consumer cross-provider suite passes all four source/order combinations (4seconds), with no failures/skips. Final hashes are recorded in [verification](VERIFICATION.md).

The reusable local single-member Mongo replica set is the tested topology. This is not multi-member election, real process termination, public-cloud availability or settlement finality proof. Existing primary-termination exclusions are retained and not counted as passing cases.

## Remaining acceptance

Neo4j primary enrollment/delivery, automatic host registration and authorized target catalogue, actor/tenant/encryption policy, authenticated reconciliation/resume, pre-submission/process-kill journaling, queue/payload/retention bounds and operational metrics remain open. Native providers must own bounded I/O/cancellation; a bounded batch is not a promise to terminate arbitrary uncooperative target code. No100%uptime, exactly-once transport or whole-roadmap completion is claimed. Legacy Hyperdrive remains documentation-only.

Concurrent Oracle/shared CAS publication was coordinated and preserved. No COSMIC commit/push/deployment was performed in this batch.


Latest readiness checkpoint: [PeekNextAsync](COSMIC_REPLICATION_READINESS.md) reads the oldest unfinished head without a claim, validates row/payload/evidence and preserves restart-visible reconciliation. Both inspection surfaces pin primary/majority independently of configured general secondary preferences; the native64 suite verifies that behavior on the retained replica. Mongo unit141 cases pass. Earlier counts remain historical checkpoints.
