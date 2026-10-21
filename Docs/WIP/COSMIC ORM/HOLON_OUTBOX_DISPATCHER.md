# Holon outbox dispatcher

Updated 19 October 2026. Local bounded worker; default host scheduling and full delivery certification remain open.

## One delivery owner

`HolonOutboxDispatcher` requires `IHolonReplicationRecoveryOutbox`, consuming the existing fenced outbox claims and calls only an explicitly registered `IHyperDriveIdempotentReplicationTarget`. It does not create another queue, call user Save() again, or derive a historical payload from the current record. The selected target id comes from explicit enrollment. An unknown target fails before claiming work; malformed claims fail before target I/O. The worker processes one target at a time, 1–100 obligations per call.

`CommittedHolonReplicaTarget.ReadItem` constructs outer routing fields from the frozen graph while retaining the original payload string unchanged. Claim operation identity must match that payload. The target adapter binds source/actor/routing and validates the committed acknowledgement as before.

Ownership is renewed before target dispatch and every third of the lease duration during asynchronous target work. A failed or thrown renewal stops delivery, cancels and observes the target task, and leaves the original claim for recovery. It does not mark the operation completed or reschedule it with uncertain ownership. Host cancellation likewise observes target termination and leaves the original obligation retained. Native providers remain responsible for bounded I/O and their cancellation/uncertain-commit contract; a worker cannot prove rollback by cancelling a request.

## Outcome policy

Completion is counted only after both the target's durable acknowledgement and the store's fenced completion succeed. A failed completion is an explicit error; later recovery uses the original operation/payload and target receipt instead of another logical edit.

Ordinary failures use explicit bounded retry: one-second exponential backoff capped at 300 seconds, with ten claimed attempts by default (constructor-configurable). The worker persists the next UTC attempt and stops that target batch, preserving order. Exhaustion blocks for reconciliation rather than retrying forever. Declared native uncertainty, committed acknowledgement corruption, invalid payload, source enrollment mismatch and lineage conflicts block immediately for reconciliation. No automatic bypass or operator resume is implemented here.

The result reports claimed/completed/deferred/reconciliation counts. Retained failures and known claim/renew/finish/target cleanup warnings remain visible with exception chains; an acknowledged cleanup warning alone does not cause another mutation. Persisted error codes/messages are bounded to 128/4,096 characters with a visible ellipsis on truncation. Original exception objects are returned to the caller; [typed recovery evidence](HOLON_OUTBOX_RECOVERY_EVIDENCE.md) now persists marker-declared native identities separately with the fenced outcome.

## Explicit invocation

```csharp
var target = new CommittedHolonReplicaTarget("mongo-eu", mongoProvider,
    ProviderType.MongoDBOASIS, "application-primary", ProviderType.SQLLiteDBOASIS);
var worker = new HolonOutboxDispatcher(new SqliteHolonReplicationOutbox(sqliteConnection),
    new[] { target }, "onode-worker-1");
var result = await worker.DispatchOnceAsync("mongo-eu", maximumCount: 25,
    cancellationToken: stoppingToken);
```

The source must have enrolled the same target id atomically. This is an explicit bounded worker call, not automatic global/default replication registration or a running background service. Explicit [Generic Host registration and scheduled routes](COSMIC_HOLON_REPLICATION_HOST.md) are now available with versioned bindings and mandatory authorization. ONODE configuration/readiness, authenticated tenant/source resolution and default/global enrollment remain integration gates.

## Evidence and remaining gates

Fifteen dispatcher cases cover exact payload preservation, success/false/null results, conflicts, uncertainty, thrown target errors, exhausted attempts, unknown/misbound targets, lost or thrown initial renewal, periodic renewal/loss, cancellation observation, failed completion and retained cleanup warnings. Full shared suite: **564 passed**, zero failed/skipped.

Four actual SQLite/MongoDB combinations pass. Both source directions now commit graph+receipt+obligations and deliver through their fenced source store/worker, with durable completion and exact target receipt replay; see [MongoDB delivery](MONGO_OUTBOX_DELIVERY.md). Creation, ordered updates and soft/hard deletion run through that path; fresh independent contexts verify stored content. Earlier direct MongoDB receiver-only evidence is superseded by that outbox path. Final broader V2 regression passes **16 cases**, zero failures/skips (2 minutes 34 seconds), including all four cross-provider combinations against the final producer build. Final counts/hashes are in [verification](VERIFICATION.md).

Current host/catalog checkpoint: the [Generic Host](COSMIC_HOLON_REPLICATION_HOST.md) runs independent authorized routes over the existing dispatcher; all three selected native enrollment/delivery stores and all six selected worker directions are implemented. Cancellation callbacks cannot skip observation, and known target acknowledgements are no longer followed by target cancellation.

Remaining: ONODE configuration/readiness and default/global enrollment, authenticated source/target/tenant/encryption context, durable catalog manifests/migration, pre-submission/process-crash native recovery journals, authenticated reconciliation/resume, queue/payload limits and retention, real process/wire-loss/restart gates and operational metrics. No full distributed transaction, exactly-once transport, default auto-replication durability or whole-roadmap completion is claimed. Legacy remains documentation-only.
