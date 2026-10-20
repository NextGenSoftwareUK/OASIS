# COSMIC replication readiness

Updated 19 October 2026. Local prerequisite implementation verified; final native and host evidence is recorded in VERIFICATION.md. This is a prerequisite for enabled ONODE configuration/readiness integration, not a completed deployment gate.

## Native head truth

A persisted ReconciliationRequired target head must remain visible after worker or process reconstruction. The claim API returns no work for such a head; interpreting that response as an empty healthy queue hides a durable block. `IHolonReplicationReadinessOutbox` therefore extends recovery inspection with read-only `PeekNextAsync(targetId)`.

SQLite, MongoDB and Neo4j implement the same oldest-unfinished target selection, excluding Completed rows. The result contains operation/sequence/target, lifecycle state, attempt count, bounded diagnostic fields and typed recovery evidence. It excludes the frozen business payload and lease token. The implementation validates stored identity/state/evidence and payload checksum/source binding internally. Corruption is a real error. Inspection never claims, renews, completes or reschedules an obligation and never increments attempts.

SQLite uses one native read snapshot and the existing no-create connection; its claim and peek share frozen-payload validation. MongoDB pins inspection to the primary with majority concern independently of general secondary-read preferences, and uses existing row/payload validation. Neo4j uses its explicit read-only transaction owner and retains known read-close/disposal warnings. No queue, schema or damaged record is created or repaired by a readiness probe.

## Hosted status

Configured hosting requires native head inspection capability; unsupported custom outboxes fail before registration or queue I/O. Each independent route probes its native head before dispatch. A retained reconciliation head is degraded and cannot be claimed. Missing/corrupt/unavailable source state is degraded. Deferred delivery or returned errors are degraded. An empty available queue can become healthy. These are point-in-time operational observations, not a guarantee of future connectivity, settlement finality or replication lag limits.

`HolonReplicationHostState` publishes bounded structured target/state/code observations. `HolonReplicationHealthCheck`, registered with the ready tag, reports unhealthy until the worker and every configured route are healthy, and after stop/fault. Its response excludes payload, tokens, credentials and raw exception text. Health and its route data use one locked snapshot. Native error details remain owned by the operational result/log/reconciliation path.

Ready does not independently authenticate a source/actor/tenant or validate an arbitrary supplied endpoint descriptor. Grant authorization still happens before target dispatch; enabled application configuration must validate its own grant expiry and provider bindings. The [ONODE readiness endpoint](ONODE_COSMIC_REPLICATION_STARTUP.md) is now wired and passes five real HTTP cases, with the full ONODE build passing. The [strict configured factory](ONODE_COSMIC_REPLICATION_CONFIGURATION.md) now awaits shared initialization and validates enabled catalogs before starting workers; physical identity verification and default enrollment remain open.

## Verification and scope

Passed: 656 Shared unit, 658 SQLite native, 141 Mongo unit, 64 Mongo native, 129 Neo unit, six Neo focused delivery and two native host/restart cases, zero failures/skips. Native tests check Pending/InFlight/reconciliation heads, reconstruction, no attempt/lease mutation, unknown targets and corruption. The hosted native test restarts the service against the same expired-grant blocked source and remains unhealthy without another claim or target mutation. Shared tests check initial/stopped state, blocked/unavailable source refusal and recovery to healthy without an alternate queue path. Final source/consumer binary parity is checked before native startup.

Remaining: default CRUD enrollment, physical endpoint verification, authenticated tenant/record/encryption context, persisted catalog/grant migration, native process-loss journaling and operator recovery, retention/queue/metrics, broad provider/graph/history certification, migration, placement, save deltas and CQL. Legacy Hyperdrive remains documentation-only.
