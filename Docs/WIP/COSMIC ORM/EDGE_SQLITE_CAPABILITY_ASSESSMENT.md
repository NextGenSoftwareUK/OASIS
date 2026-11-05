# EdgeSQLite associated-component assessment

5 November2026. Read-only source assessment, no native acceptance claim.

EdgeSQLiteOASIS is an associated support project, not a concrete ProviderType
identity in the current enum. Its EdgeSQLiteSyncStateStore implements
IHyperDriveSyncStateStore, IHyperDriveIdempotentReplicationTarget,
IHyperDriveLocalReplicationOutbox and IDisposable. It owns real SQLite entity,
outbox, inbound-change/conflict and checkpoint transaction boundaries.
It does not implement IOASISStorageProvider or ICosmicQueryStorageProvider.

LoadEntityAsync(entityType, entityId, token) performs parameterized native key
reads and returns version/payload/deletion state. ReadViewAsync deliberately
reads a consistent entity/command projection, including unsettled durable
commands. These are HyperDrive synchronization APIs, not full Holon CQL.
Do not substitute ReadViewAsync table enumeration for bounded CQL queries or
register another provider name/count merely because the project uses SQLite.
Existing specialized tests/source are preserved; no broad rerun or changes to
this other subsystem were made for this assessment.

Integration remains roadmap work: define canonical Holon versus domain-entity
payload contracts and ownership, then integrate an explicit query view at the
sync-store boundary with server-selected authority and real native predicates.
Preserve atomic entity/outbox/checkpoint writes and command-outcome visibility.
A separate provider identity/registration requires an explicit architecture
change; no fabricated alias or placeholder storage provider is added here.
Associated-component/platform/sync conformance and full ORM/HyperDrive roadmap
remain in scope. This assessment adds no accepted CQL profile or hosted coverage.

Sources: [store](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS/EdgeSQLiteSyncStateStore.cs),
[read view](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS/EdgeSQLiteSyncStateStore.ReadView.cs),
[entity contracts](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS/EdgeLocalMutation.cs).
