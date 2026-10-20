# Hosted committed graph delivery

Updated 19 October 2026. Local implementation; no release or deployment.

## Implemented boundary

`CommittedHolonReplicaPayload` captures an already committed selected graph into a version 1 envelope. Each record uses the existing strict canonical input serializer; selected navigation is stored separately as child identity edges. Capture uses one graph plan, without rereading caller child collections. The payload retains operation/source/actor identities, logical lineage, source revision observations, root order, graph selection flags and depth. Reading creates detached records and reconstructs navigation, then validates committed lineage and the exact selected graph. Unsupported versions, absent policy fields, unknown envelope fields, duplicate JSON properties, duplicate identities, unknown edges, cycles, disconnected nodes, unselected edges and trailing content fail before provider I/O. Existing receiver receipt fingerprints are unchanged by valid round trips. The [wire contract](COMMITTED_GRAPH_WIRE_CONTRACT.md) now validates exact field casing and JSON scalar kinds before typed conversion or record deserialization: booleans, integers, enum numbers and D-format GUID strings cannot be supplied through Newtonsoft coercion. Existing empty-actor semantics remain at the host authorization boundary.

`CommittedHolonReplicaTarget` implements the existing hosted idempotent-target interface. Its explicitly configured source identity/authority must match the payload. The outer operation, actor, first root identity/version, operation kind and entity discriminator must agree with the frozen payload. A batch retains all roots; outer entity/version fields index the first root. The discriminator is `cosmic.committed-holon-graph.v1`.

The adapter calls only `ICommittedHolonReplicaStorageProvider`, whose existing native transaction owns mutation, lineage heads and exact receipts. It never invokes a normal user edit or invents a target revision. A successful response must contain the expected graph, selected edges, committed content and target-owned physical revisions. Provider errors, metadata, warnings and exception chains are retained. Host cancellation remains cancellation. The fan-out handler owns per-target outcome reporting.

## Evidence

- Eleven payload tests cover frozen graph/fingerprint round trips, depth frontiers and malformed envelope rejection.
- Fourteen adapter tests cover fan-out dispatch, source/routing mismatches with zero dispatch, corrupted acknowledgements, retained provider rejection diagnostics and cancellation.
- Full shared suite: **548 passed, zero failed/skipped**.
- Native SQLite/MongoDB cross-provider suite: **four passed, zero failed/skipped**, through the hosted adapter before direct receipt replay. Covers both source directions and sync/async direct receipt checks, creation, ordered updates, soft deletion and hard deletion. Independent readers verify stored values; successful owned fixtures are cleaned after shutdown.

Reproduce with the existing shared test project and `Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1 -CosmicReplicaTransfers`. Reusable reports: `TestResults/CosmicMongoCurrent/shared-unit-current.trx` and `TestResults/SubscriptionUsageCurrent/cosmic-cross-provider-replicas-current.trx`.

## Remaining integration and acceptance

This adapter is explicit and is not automatically registered in ordinary CRUD replication. **Primary mutation plus immutable per-target obligations must commit in one native transaction.** SQLite now implements explicit [atomic enrollment](ATOMIC_HOLON_OUTBOX.md) through manager/V2 save options. Other providers, default auto-replication enrollment and the durable dispatcher remain open. Enqueueing this payload after a primary save would leave a crash window and is not an acceptable integration.

Configured source matching is enrollment, not authentication. Host actor/tenant authorization, source credentials, encryption context, retention, bounded payload sizes, process/wire-loss recovery, lease fencing, per-target scheduling/order and operator reconciliation remain acceptance gates. The existing canonical restricted binder is reused; broader DTO/type admission security certification remains open. Neo4j native hosted-adapter coverage and MongoDB/Neo4j transfer directions remain open. The payload is a committed intent, not an authorized baseline/cutover or settlement command.


Strict wire checkpoint:18 new scalar/receiver cases first failed against the prior decoder, proving coerced payloads could reach provider mutation. The final Shared suite passes713 with21 added cases including two casing checks and existing empty-actor compatibility. Both actual configured-host Neo4j-to-SQLite cases pass with the initial strict decoder; final compatibility-only admission and binary hashes are separately identified in VERIFICATION.md. No producer encoding or receipt fingerprint changed. Historical counts/integration checkpoints above are retained; current explicit native delivery/configuration is documented in [the host configuration](ONODE_COSMIC_REPLICATION_CONFIGURATION.md) and [all six selected worker directions](OUTBOX_PROVIDER_DIRECTIONS.md). Broad admission/security/crash/resource-limit gates remain open.
