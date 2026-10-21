# COSMIC holon replication host and catalog

Updated 19 October 2026. Local explicitly configured Generic Host integration. Default ONODE/global replication conversion, deployment and the full roadmap remain open.

## One delivery owner

`AddCosmicHolonReplication` registers `HolonReplicationHostedService`, its [native-head readiness state and health check](COSMIC_REPLICATION_READINESS.md) once with an immutable complete set of initialized catalogs. The service invokes the existing `HolonOutboxDispatcher`; native provider queues remain authoritative. There is no second queue, post-commit enqueue or replay through user Save(). Native fenced claims, renewal, target receipts, retry and reconciliation ownership stay in their existing layers.

Each route has an independent joined asynchronous loop. A slow target does not stall another target. Configuration is bounded to 16 catalogs, 64 total routes, batches 1–100, attempts 1–1000 and leases of at most one hour. Default polling is one second and error polling five seconds; these are scheduled claim observations, not hidden native mutation retries. Source/native-store identity conflicts, duplicate routes, missing native head-inspection capability or duplicate host registration fail before registration changes or queue I/O. Readiness requires a live worker and available/unblocked native heads; a persisted reconciliation head remains degraded after service reconstruction. The health check is registered with the ready tag and exposes only target/state/code, not payloads, tokens or raw exceptions.

Cancellation propagates to the dispatcher and active target work. The service awaits all route loops. An unexpected route fault cancels and observes siblings before propagating. Native providers still require bounded I/O; host shutdown timeout cannot prove a native rollback. Source and receiver resources are caller-owned, initialized before registration, held stable throughout worker execution and closed after successful StopAsync completion. An expired stop deadline is an explicit failure if routes remain active; owners must continue observing that task rather than close providers underneath it. Cancellation callback errors cannot skip target observation or erase returned native warning/reconciliation evidence. A completed target is not canceled again after its acknowledgement.

## Versioned binding

A `HolonReplicationCatalog` binds catalog ID/version, source installation ID/authority, a stable native store identity and named destinations. Each destination supplies a server-owned endpoint identity, native receiver capability, target authority and mandatory authorization policy. Target IDs contain the catalog version plus a SHA256 digest of length-prefixed binding fields, including the captured policy identity. Changing declared source, store, destination, endpoint, policy or catalog version creates different enrolled target IDs. Input collections and resulting target IDs are frozen.

Use `catalog.Enrollment` for source writes so the exact versioned target IDs are committed atomically with the source receipt and payload. The host never remaps an old target ID onto new configuration. Outstanding old routes require their original authorized catalogs to remain registered or an explicit migration/reconciliation workflow. Automatic backfill, grant renewal and target-set migration remain separate gates.

Endpoint/store identities are trusted deployment descriptors. The library cannot independently prove that an arbitrary supplied receiver object matches its descriptor, nor stop an external owner mutating/reactivating that provider during delivery. The application factory and provider lifetime owner must enforce those bindings. The [ONODE configured factory](ONODE_COSMIC_REPLICATION_CONFIGURATION.md) now pins explicit registry authorities after shared initialization. Persisted environment/catalog manifests, physical endpoint verification and default CRUD enrollment remain integration work.

## Authorization boundary

Every destination requires `IHolonReplicationAuthorizationPolicy`. Null, failed, false, expired or changed policy authorization cannot invoke the receiver. Policy binding identity is checked before and after authorization. The authorized route privately snapshots the complete transport envelope before invoking or awaiting policy. Both policy parsing and receiver dispatch use that snapshot, so changing the caller envelope during authorization cannot replace the authorized payload or repair originally invalid routing assertions. The receiver adapter reparses the frozen bytes, so a policy cannot rewrite the dispatched business graph. Known policy warnings remain attached to the target result.

Authorization denial/failure is retained as ReconciliationRequired, not an ordinary retry. It blocks that ordered target head. Recovery preserves the original source operation and payload. `HolonReplicationActorGrant` is an explicit finite actor allowlist with UTC expiration and a digest binding the frozen actors/expiry. It does not authenticate callers or implement tenant/record/encryption policy. Deployments needing those rules must supply an appropriate server-side policy and authenticated source admission; those programme gates are still open. No implicit allow-all policy is supplied.

## Registration example

```csharp
// Existing providers are activated by the application resource owner before registration.
var grant = new HolonReplicationActorGrant(allowedActors, grantExpiresAtUtc);
var catalog = new HolonReplicationCatalog(
    "application-replicas", catalogVersion, sourceInstallationId, sourceDatabaseIdentity,
    ProviderType.SQLLiteDBOASIS, new SqliteHolonReplicationOutbox(sqliteConnection),
    new[] { new HolonReplicationDestination("neo4j-eu", targetEndpointIdentity,
        ProviderType.Neo4jOASIS, activatedNeo4jProvider, grant) });
services.AddCosmicHolonReplication(new HolonReplicationHostOptions(workerId), catalog);

// Source writes use the catalog's actual versioned enrollment.
var options = new CosmicWriteOptions(operationId, expectedRevisions: null,
    replicationEnrollment: catalog.Enrollment);
```

A Generic Host starts and stops this service automatically. ONODE Startup now maps the [specific readiness endpoint](ONODE_COSMIC_REPLICATION_STARTUP.md), verified by five actual HTTP cases and a successful application build. The opt-in configured factory now awaits shared initialization, validates capabilities/grants, captures native source stores and starts workers while preserving platform provider ownership. Existing legacy and hosted-domain fan-out workers are unchanged. Default/provider-wide CRUD is not automatically enrolled by this library registration.

## Verification and remaining gates

The complete shared suite now passes 656 cases, including 24 host/catalog cases and two additional dispatcher cancellation-owner cases. They cover route identity changes, allowlist freezing and expiry, fail-closed policy decisions/binding drift, immutable payloads, reconciliation outcomes, invalid registration, independently progressing routes and joined cancellation/fault handling. Both native Generic Host SQLite-to-Neo4j cases pass through real atomic obligations for sync/async source writes, creation/update/soft/hard deletion and receipt replay without manual dispatch. Expiring the configured actor grant retains a ReconciliationRequired source head with one attempt and zero native target records/receipts. Authorization is checked before target dispatch, not a distributed revocation of an already accepted native write. The final runner rebuilds every direct provider/consumer non-incrementally and verifies Shared DLL identity before startup; an earlier stale-copy run was rejected as evidence of the latest fix. Final results/hashes are recorded in VERIFICATION.md.

Remaining: default enrollment, physical provider identity verification, authenticated tenant/actor/record/encryption context, durable catalog manifests and migration, native pre-submission/process-kill recovery, authenticated operator resume, queue/payload/retention controls, operational metrics and deployment certification. Broad provider/history/migration/placement, committed deltas and CQL acceptance remain open. Legacy Hyperdrive remains documentation-only.


Authorization envelope checkpoint: full Shared692 passes, including seven new regression cases. A deterministic awaited-policy test first failed against the previous implementation because a replacement actor/payload reached the receiver. The fixed route delivers the original authorized operation; six routing-field cases prove policy cannot repair originally invalid assertions. This is local library verification, not authentication or tenant/record/encryption certification. See VERIFICATION.md.
