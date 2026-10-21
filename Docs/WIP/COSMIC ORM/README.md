# COSMIC ORM work

Start with [implementation status](IMPLEMENTATION_STATUS.md), the [full roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) and the [source integration boundary](INTEGRATION_BOUNDARY.md). They identify verified scope, unfinished acceptance criteria and publication constraints.

These are working documents. Dated reports retain historical evidence; earlier counts and findings do not override current status. Legacy Hyperdrive is documented only. Implementation targets V2.

## Documents

- [Neo4j native CQL: source gaps, transactional projection and acceptance](CQL_NEO4J_NATIVE.md).

- [SQLite audit timestamp kind fidelity and regression evidence](SQLITE_AUDIT_DATE_KINDS.md).
- [CQL enum metadata: defined-value domains and native acceptance](CQL_ENUM_METADATA.md).
- [CQL exact integer domains and compiler promotion rules](CQL_INTEGER_DOMAINS.md).
- [SQLite karma history persistence and remaining representation gaps](SQLITE_KARMA_HISTORY_PERSISTENCE.md).
- [SQLite avatar optional enum projection and additive presence upgrade](SQLITE_AVATAR_OPTIONAL_ENUMS.md).
- [SQLite CQL native work, deadline and streamed payload bounds](SQLITE_CQL_RESOURCE_BOUNDS.md).

- [CQL ordering and paging: common contract and remaining native work](CQL_ORDERING_PAGING.md).

- [CQL field comparisons: current implementation and native acceptance work](CQL_FIELD_COMPARISONS.md).

- [Shared tombstone save lifecycle: SQLite/MongoDB/Neo4j validation](SHARED_SAVE_LIFECYCLE.md).

- [Neo4j delete ownership, acknowledgement counts and repeated soft deletes](NEO4J_DELETE_OWNERSHIP.md).

- [Neo4j avatar/detail save lifecycle and transaction ownership](NEO4J_IDENTITY_SAVE.md).

- [Neo4j save lifecycle: tombstone admission and verification](NEO4J_SAVE_LIFECYCLE.md).

- [Neo4j atomic committed deltas: implementation and acceptance](NEO4J_COMMITTED_DELTAS.md).

- [CQL getting started: feature status and usage examples](CQL_GETTING_STARTED.md).
- [Durable write preparation: shared claim contract and SQLite owner](WRITE_PREPARATION.md).
- [MongoDB durable preparation: shared operation fence and verification checkpoint](MONGO_WRITE_PREPARATION.md).
- [Neo4j durable preparation: shared receipt ownership and verification checkpoint](NEO4J_WRITE_PREPARATION.md).
- [MongoDB native CQL: implementation and verification checkpoint](CQL_MONGO_NATIVE.md).
- [CQL expression contract: first implementation milestone](CQL_EXPRESSION_CONTRACT.md).
- [Atomic primary holon outbox](ATOMIC_HOLON_OUTBOX.md).
- [Child-holon load, save and query certification](CHILD_HOLON_CERTIFICATION.md).
- [Committed graph wire contract](COMMITTED_GRAPH_WIRE_CONTRACT.md).
- [Committed holon replicas — receiver contract and verification](COMMITTED_REPLICAS.md).
- [Conditional writes and cross-client concurrency](CONDITIONAL_WRITES.md).
- [Core graph lifecycle and save-plan ownership](CORE_GRAPH_LIFECYCLE.md).
- [Manager graph preparation](CORE_GRAPH_PREPARATION.md).
- [COSMIC holon replication host and catalog](COSMIC_HOLON_REPLICATION_HOST.md).
- [COSMIC ORM implementation audit](COSMIC_ORM_AUDIT_2026-10-19.md).
- [COSMIC ORM source evidence](COSMIC_ORM_SOURCE_EVIDENCE.md).
- [COSMIC ORM: use cases and commercial positioning](COSMIC_ORM_USE_CASES_AND_POSITIONING.md).
- [COSMIC replication readiness](COSMIC_REPLICATION_READINESS.md).
- [Durable child roles: implementation boundary](DURABLE_CHILD_ROLE_PLAN.md).
- [Durable COSMIC CRUD delivery — implementation contract and gates](DURABLE_CRUD_DELIVERY.md).
- [Metadata encryption context](ENCRYPTION_CONTEXT.md).
- [Holon outbox delivery leases](HOLON_OUTBOX_DELIVERY_LEASES.md).
- [Holon outbox dispatcher](HOLON_OUTBOX_DISPATCHER.md).
- [Durable holon delivery recovery evidence](HOLON_OUTBOX_RECOVERY_EVIDENCE.md).
- [Hosted committed graph delivery](HOSTED_COMMITTED_GRAPH_DELIVERY.md).
- [Legacy Hyperdrive gaps — documentation only](LEGACY_HYPERDRIVE_GAPS.md).
- [MongoDB audit precision and receipt cleanup](MONGO_AUDIT_PRECISION.md).
- [MongoDB committed-replica receiver](MONGO_COMMITTED_REPLICAS.md).
- [MongoDB metadata codec and cross-provider value ownership](MONGO_METADATA_CODEC.md).
- [MongoDB fenced holon outbox delivery](MONGO_OUTBOX_DELIVERY.md).
- [MongoDB atomic primary holon outbox](MONGO_PRIMARY_OUTBOX.md).
- [Neo4j child hydration and depth frontiers](NEO4J_CHILD_HYDRATION.md).
- [Neo4j outbox delivery store](NEO4J_OUTBOX_DELIVERY.md).
- [Neo4j atomic primary outbox](NEO4J_PRIMARY_OUTBOX.md).
- [Enabled ONODE COSMIC replication configuration](ONODE_COSMIC_REPLICATION_CONFIGURATION.md).
- [ONODE COSMIC replication startup integration](ONODE_COSMIC_REPLICATION_STARTUP.md).
- [Durable worker provider directions](OUTBOX_PROVIDER_DIRECTIONS.md).
- [Relationship delta comparison](RELATIONSHIP_DELTA_PREREQUISITE.md).
- [SQLite receipt integrity prerequisite for committed deltas](SQLITE_RECEIPT_INTEGRITY.md).
- [Committed delta capture, Save summaries and remaining rollout](COMMITTED_DELTAS.md).
- [MongoDB committed deltas and shared receipt validation](MONGO_COMMITTED_DELTAS.md).
- [Shared graph traversal ownership](SHARED_GRAPH_TRAVERSAL.md).
- [Soft-delete read contract](SOFT_DELETE_READ_CONTRACT.md).
- [SQLite child hydration and depth boundaries](SQLITE_CHILD_HYDRATION.md).
- [SQLite committed replica receiver](SQLITE_COMMITTED_REPLICAS.md).
- [SQLite holon history: implementation and remaining gaps](SQLITE_HISTORY_GAP.md).
- [SQLite holon persistent-value mapping](SQLITE_PERSISTENT_VALUES.md).
- [Explicit transfer and replication ownership](TRANSFER_AND_REPLICATION.md).
- [Typed child collection ownership](TYPED_CHILD_OWNERSHIP.md).
- [COSMIC ORM verification â€” 19 October 2026](VERIFICATION.md).

## Related work and local evidence

The provider session maintains [its own working evidence](../HyperDrive-Providers/). Other WIP topics are indexed in [the parent folder](../README.md).

Local reports and fixture archives are under `TestResults/CosmicMongoCurrent/`; exact paths and hashes are in the integration boundary. They are separate from this documentation commit. Provider certification, source publication and deployment remain separate gates.

- [CQL SQLite native execution](CQL_SQLITE_NATIVE.md) — parameterized leaf-row adapter scope and remaining WEB4 integration.

- [CQL native execution pipeline](CQL_EXECUTION_PIPELINE.md) — shared adapter contract, full-target preflight, budgets and provider provenance.

- [CQL typed scalar metadata](CQL_SCALAR_METADATA.md) — SQLite type, null, comparison, membership and string semantics.
