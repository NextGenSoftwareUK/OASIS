# ONODE COSMIC replication startup integration

Updated 19 October 2026. Endpoint wiring is implemented and the full ONODE build and five actual HTTP cases pass. Full enabled-configuration/catalog factory integration remains open.

## Readiness endpoint

ONODE Startup maps `/health/cosmic/ready` through the production `CosmicHolonReplicationEndpoints` module. It returns200 with `ready` only when the named `cosmic-holon-replication` health check exists and reports Healthy. Missing health services, an empty aggregate or unrelated health checks return503 with `not-configured`; a named unhealthy check returns503 with `not-ready`.

The endpoint is anonymous operational status, sends Cache-Control:no-store and returns a fixed plain-text body. Health descriptions, raw exception text, credentials, business payloads, lease tokens and route diagnostics are not serialized. It filters the exact COSMIC check, so the framework's Healthy result for an empty aggregate cannot produce a false ready response. This route is specific COSMIC readiness, separate from general API liveness.

Five tests start real loopback Kestrel hosts using the linked production endpoint module and exercise absent, empty, unrelated, healthy and unhealthy registration. The actual ONODE project builds with zero errors. It is not a deployment or full application-authentication test.

## Activated source capability

`IHolonReplicationOutboxSource.GetHolonReplicationOutbox()` allows generic startup code to capture an activated native readiness outbox without reflection or exposing connection strings/driver objects. Canonical MongoDB, SQLite and Neo4j implement it at their existing context/driver owner. Final verification passes142 Mongo unit/64 native,130 Neo unit/six native delivery and659 SQLite native cases. Native delivery/inspection uses the provider-captured store; inactive capture is tested before queue I/O. An inactive source returns COSMIC_OUTBOX_SOURCE_NOT_ACTIVATED before native queue I/O. Invalid durable bindings return a real OASISResult error.

The method captures the existing provider binding and does not activate a provider, create schema, enqueue, claim or transfer lifecycle ownership. The application must keep providers stable while workers run, stop/join workers before closing sources, and probe native readiness before relying on availability. Captured source stores remain subject to their native lease and lifetime contracts; this capability does not create a new global failover owner.

## Enabled startup configuration

The [strict configured catalog factory](ONODE_COSMIC_REPLICATION_CONFIGURATION.md) is implemented through COSMIC_HOLON_REPLICATION_JSON. Enabled startup awaits the existing cached OASIS initialization task before registry lookup, pins explicit provider authorities, checks capabilities and finite grants, captures native outboxes and probes readiness before starting workers. Startup errors propagate even with global background-service exception ignoring. Shared providers retain platform lifecycle ownership; worker shutdown joins before provider close. Full Shared685, native configured-host2 and the full ONODE build pass.

Default/global CRUD is still not automatically enrolled. Target/source/tenant/record/encryption authorization, physical endpoint verification, persisted catalog/grant migration, process-loss journaling/reconciliation, retention/queue/metrics, broader graph/history/provider conformance, migration, placement, deltas and CQL remain open. Legacy Hyperdrive remains documentation-only.
