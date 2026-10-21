# Explicit transfer and replication ownership

Updated 19 October 2026. Implementation and verification are local. The full MongoDB/SQLite native routing matrix now passes. The transfer/migration roadmap remains in progress.

## Implemented

`SendHolon<T>` and the new `SendHolonAsync<T>` perform an explicit destination write even if source content is clean. Both use the common detached save lifecycle with an explicit operation intent, rather than setting an artificial change flag. The source graph is copied and decrypted into detached materialized values; the source DTO and its accepted revision remain unchanged. A malformed/missing source graph fails before destination mutation.

The selected source and destination are required endpoints. Predictive substitution, automatic load balancing and failover cannot silently turn another provider into the destination or replace the chosen source. Existing subscription/provider admission rules still apply. Passing `Default` resolves the current provider at entry; identical source/destination and invalid identities are rejected.

The `autoReplicate` parameter now overrides the runtime default for this operation. It does not change any `ProviderManager` setting. `StorageOperationRequest.AutoReplicate = null` retains the normal runtime policy; explicit true/false selects the request policy. Conditional command replication with an explicit true override is rejected before provider I/O until independent target preconditions and durable delivery are supported.

The existing overload obtains the actor from the logged-in context. New overloads accept an explicit actor ID, following the existing actor-bearing save API convention. A missing actor is rejected before source I/O; the creator of the record is not impersonated as the current actor. Supplying an actor ID is not itself authentication or tenant authorization; host authorization remains a separate roadmap gate.

```csharp
var transferred = await manager.SendHolonAsync<Holon>(
    holonId, actorId, ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS,
    autoReplicate: false);
if (transferred.IsError) { /* inspect the native error and reconciliation requirement */ }
```

Primary diagnostics, native causes, acknowledged data and replication warnings flow through the result instead of being replaced by a synthetic transfer-success message. Primary failure remains failure. A replica failure does not replace an acknowledged primary result.

## Replication data ownership

For ordinary holon saves routed through Hyperdrive, replication captures the actual primary acknowledgement, including its committed revision/audit values. It no longer fans out the submitted pre-commit payload. The captured graph is detached and each target receives a fresh materialization, preventing one target from changing the next target's input or the primary acknowledgement. Invalid committed evidence produces an explicit reconciliation-required error while preserving the primary payload. Existing avatar tombstone evidence handling remains in place.

This change applies to the canonical routing owner. Legacy unqualified save/delete fan-out gaps are documentation-only by user direction. V2 durable outbox delivery still requires implementation and native verification. An acknowledged primary plus synchronous best-effort fan-out does not supply durable eventual delivery.

## Verification and corrected native findings

- Core: **948 passed, zero failed/skipped**, including 23 transfer cases. Coverage includes legacy/V2 configuration, sync/async, clean sources, current-default resolution, global and operation replication settings, endpoint failure, actor validation, primary acknowledgement capture and replica isolation.
- Native MongoDB/SQLite: **12 passed, zero failed/skipped**: four replication cases and eight transfers covering both directions, sync/async and legacy/V2. Existing logical-version identity assertions remain intact. Parent collection assertions now account for immediate children starting at depth one.
- SQLite: **576 passed**, including provider-local token ownership, legacy schema upgrade, stale graph rejection and durable receipt replay. Logical versions and local tokens are independently verified.
- MongoDB unit: **70 passed**. Startup now registers the concrete Holon discriminator before reading stored interface children; embedded child serialization retains nested selected children while excluding hydrated parent navigation. Fresh-reader tests cover this behavior.
- Core netstandard2.1 build succeeds. The native consumer restore reused the existing offline NuGet cache.

The first failed transfer had committed two owned records before an assertion-side numeric JSON cast failed. Independent native reads verified both exact records in SQLite and MongoDB before removing the owned fixture, without replaying its save. Reconciliation passed in `cosmic-transfer-original-reconciliation.trx`; the original failing reports are retained once under `TestResults/SubscriptionUsageCurrent/`. The final runner cleaned up its owned database and stopped MongoDB; port 27029 has no listener.

An initial migration downgrade guard threw in managed migration construction and left the owned test database locked. The confirmed owned testhost was stopped; compact evidence was retained in `provider-revision-migration-hang.json`. All rows and dependent records were verified as belonging to the single synthetic fixture before deleting only its disposable database. The guard now executes transactionally in SQL; targeted migration checks and the full SQLite suite pass.

## Remaining work

1. Certify V2 deletion propagation, primary/target acknowledgements, partial failures, uncertainty and tombstones. Record legacy fan-out gaps without changing legacy code.
2. Integrate durable obligations with authoritative transactional outboxes. Synchronous replication does not guarantee eventual delivery after a crash.
3. Certify conditional writes in additional providers, actor/tenant enforcement, encryption retry intent and destination conflict policies.
4. Implement controlled historical migration. Send currently prepares a destination save with source lineage; it does not copy an entire version history.


## V2 hard deletion and uncertain outcomes

Provider reconciliation evidence overrides contradictory success flags before V2 failover, replication, telemetry and import publication. The original payload/cause remain available for reconciliation; an uncertain import request cannot be replayed by reusing the request object.

Hard-delete fan-out binds each replica request to the logical ID in the primary acknowledgement. A primary provider key is not portable to another provider. Missing, empty or mismatched logical identity produces `HYPERDRIVE_COMMITTED_EVIDENCE_INVALID`, retains the primary payload, clears the confirmed-deleted flag and prevents target I/O. Tests visit two configured targets and preserve primary diagnostics. These V2 unit checks do not replace native tombstone/convergence/outbox certification.

## V2 manager tombstone preparation

V2 soft deletion now loads and saves only the root, retaining a single captured authority across both operations. Detached preparation preserves the provider-returned DTO and avoids unintended descendant writes. This is an unconditional operation unless the caller selects the separate conditional deletion API. Hard deletion no longer performs an async prerequisite load; provider-key deletion requires the selected authority. Guid hard deletion keeps existing routing behavior.

Sixteen manager unit cases and four native SQLite cases pass. SQLite root tombstones advance the root revision/token and preserve the complete child row/token. MongoDB/Neo4j root-only embedded-child behavior, cross-provider tombstone history/replication and durable delivery still require native certification.
