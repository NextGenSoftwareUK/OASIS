# WEB4 vision, implementation assessment and AI direction

26 October 2026. Strategic assessment; proposed future work does not replace the
active full COSMIC ORM/CQL roadmap or certify unverified providers.

## Assessment

The useful product is a policy-controlled data and execution fabric across web2
and web3: applications keep a common model while storage, chain and transport
choices can change. COSMIC ORM owns entity/state semantics, CQL owns typed query
intent and provider-capability admission, and HyperDrive V2 owns eligible routing,
placement and replication. Their value depends on sharing identity, authorization,
precision, concurrency, recovery and accounting contracts across these layers.

The common holonic interfaces make interoperability practical inside OASIS.
World-wide interoperability additionally requires versioned public wire/schema
specifications, lossless native mappings, conformance tests, compatibility rules
and independent adoption. A common envelope cannot by itself establish that two
sources mean the same thing or allow access to a closed platform.

## Realized and remaining

| Area | Evidence and boundary |
|---|---|
| COSMIC ORM/CQL on MongoDB, SQLite, Neo4j | Substantial implemented/tested vertical slice: typed mapping, query capabilities, exact value semantics, child/graph access, conditional writes and scoped recovery. Full roadmap not complete. |
| Cross-provider CQL | Native federation, global ordering/windows, copy provenance, per-target grants and metered attempts verified. Deterministic priority is not causal freshness. |
| HTTP continuation | Controlled Core3,739pass; latest actual shipping/native gate20cursor requests,18settlements,32attempts,2duplicate rejections; host restarts2, key rotation1/revocation1. Fixed scope/expiry, fresh policy and cumulative accounting exercised. |
| Joined native regression | 526settled operations/608physical attempts; source snapshots unchanged, seven producer/consumer pairs match; fixtures/services cleaned. Separate existing actual writer-process recovery covers all3stores. |
| Cursor operations | Actual abrupt Windows host termination/fresh-process continuation now passes with unchanged DPAPI-protected persisted keys and shared durable outbox. Multi-host/container/KMS deployment acceptance remains open. |
| HyperDrive V2/provider breadth | Significant scoped provider implementations and controlled/native gates exist. Current provider reports explicitly leave full interfaces, hosted credentials/fees and joined failover/load-balancing/replication acceptance open. Not all-provider production certification. |
| Remaining broader scope | Scalable authorization/tenant discovery, causal agreement, repeatable distributed snapshots, distributed joins, failure-window/retention reconciliation and remaining ORM history/replication/migration/placement gates. |

Do not derive a percentage from test counts or provider names. Certification needs
a fixed capability-by-provider-by-environment matrix, with evidence for every cell.
Use [CQL roadmap](CQL_COMPLETION_ROADMAP.md),
[implementation status](IMPLEMENTATION_STATUS.md) and
[provider V2 coverage](../HyperDrive-Providers/HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md).
Latest cursor proof: Verification/federation-cursor-native{.trx.gz,-source.json.gz,
-binaries.json,-result.json}; Core proof federation-cursor-http-core.trx.gz.

## Recommended sequence

1. Finish bounded production acceptance for the first three stores: production key deployment, failure windows, authorization deployment, operational
   reconciliation, and measured scale. Keep the full remaining roadmap explicit.
2. Define semantic contracts: stable entity identities, schema versions, units,
   currencies, timestamps, authority, provenance, finality and lossless mappings.
   Make capability/guarantee differences discoverable through the API.
3. Add an explain/observability surface: resolved scope, native versus coordinated
   execution, consistency/freshness, attempts, costs, replication lag and rejected
   capabilities. Separate source evidence from derived inferences.
4. Build a read-only AI analyst through existing authenticated WEB4/CQL boundaries.
   Natural-language intent becomes a validated typed plan. The existing policy,
   planner, budget and accounting owners remain authoritative. Return reproducible
   deterministic metrics plus source identities/versions and cited explanations.
5. Validate one customer workflow, then use real telemetry for ML scheduling.
   Compare learned latency/cost/placement recommendations against a rules baseline
   in shadow mode before bounded automation. Enforce residency, permissions,
   durability and finality constraints outside the model.

This release sequence does not declare the broader programme finished or remove
its outstanding acceptance criteria.

## AI architecture and limits

Proposed flow:

`User/agent -> authenticated AI tool gateway -> semantic catalogue -> validated CQL
plan -> existing provider execution -> deterministic results/provenance -> explanation`

Use a compatible authorized MCP surface for external AI clients, reusing WEB4
identity/delegation rather than granting raw provider credentials. MCP provides a
transport authorization model; OASIS must still enforce its own record/provider
permissions and budgets. [Official MCP authorization specification](https://modelcontextprotocol.io/specification/2025-11-25/basic/authorization).

Track derivations so an answer records which source entities, transformations and
versions produced it. W3C PROV provides an existing interoperable provenance model
worth mapping to rather than inventing an incompatible representation.
[W3C PROV overview](https://www.w3.org/TR/prov-overview/).

Retrieval/vector indexes must inherit source permissions and revocation. Retrieved
content is data, never permission to execute instructions. Keep generated queries
bounded and capability checked. Begin with read-only summaries, reconciliation,
search and anomalies; introduce writes only through existing conditional-write,
authority, spending and recovery contracts. Training a new model or installing a
large ML stack is unnecessary for the first pilot.

## Commercial direction

Recommended first pilot: explain and reconcile operational records across existing
web2 storage and on-chain evidence, with permission-scoped queries and traceable
answers. Demonstrate the same application against multiple providers, cross-store
querying and recovery evidence before claiming automatic universal migration.

Further use cases: cross-system support/operations investigation; portable user
profiles; supply-chain attestations; permissioned organizational knowledge search;
and operational dashboards combining application data with chain confirmations.
Label each as a proposed application, not an already certified product capability.

For hybrid purchases, distinguish accepted application intent, provider persistence,
chain submission and finalized settlement. Replicating a holon to a chain is not
itself an asset transfer or finalized payment. Fee-aware deferral needs deadlines,
funding/price constraints, idempotency and observable failure/compensation states.

Replace absolute100percent uptime claims with measured availability/recovery
objectives and explicit degraded-mode guarantees. Alternate providers cannot
create connectivity, access rights, fresh replicas or chain finality during every
failure. Position as unified, policy-controlled web2/web3 access with portable
applications and verifiable execution; make broader claims only after their gates.
