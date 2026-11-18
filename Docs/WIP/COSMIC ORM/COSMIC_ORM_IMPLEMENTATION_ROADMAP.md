# COSMIC ORM implementation roadmap

7 November checkpoint: coverage is119scoped local/92liveHTTP/28functional browser.
[Dapr's bounded live read/federation flow](DAPR_CQL_MVP.md) passes source durability,
snapshot reuse/privacy and exact SQLite/Neo4j replicas. Its browser gate remains
open. The dated counts below are retained historical checkpoints; CURRENT_STATUS
owns current release state. Full provider implementation and EF/NHibernate parity remain incomplete.
Virtual Holon mapping is a top priority alongside the live WEB4 demo, SQL-like CQL and Ask Your Data.

## Virtual Holon mapping and hybrid import

The [Virtual Holons import proposal](OASIS_Virtual_Holons_Import_Proposal.docx)
and the 12 November integrated WEB4 roadmap define three modes.

- **Materialized import** stores owned Holon payloads through the existing
  provider Import and ImportAll paths for HyperDrive replication.
- **Virtual mapping** stores source references and mapping metadata. CQL
  resolves matching records against the provider's native data.
- **Hybrid import** resolves records on demand and can materialize selected
  records for caching or explicit owned snapshot saves.

Start with an isolated external MongoDB collection through the shipping MongoDBOASIS
provider. Register one collection mapping with queryable metadata, an approved typed
field map, stable key scheme, version, scope and a server-side credential reference.
CQL must authorize the caller, push supported predicates, projection and bounds into
the native query, and return ephemeral Holons with stable IDs and provenance. Verify
source changes, tenant isolation, unsupported query rejection, response latency and
storage growth. Check that registration persists only the mapping and querying creates
zero source-row Holons or replication obligations. Follow with virtual plus materialized
federation, explicit snapshots and opt-in hybrid caching. Grow provider coverage using
each provider's verified operation capabilities.

### One world of data

CQL gives an application one query language for the data it is authorized to
use across connected sources. Each source keeps its own records, permissions,
and update lifecycle. OASIS can store a collection-level schema and field map
once, then resolve matching rows through the provider when queried. When a
workflow needs a stored copy, it can save that record as a full Holon and
configure HyperDrive replication for it.

- **Supply chain.** Keep a product catalog at a supplier and store purchase
  orders as owned Holons. A CQL view can bring supplier details and owned
  orders together by an approved product key where the query and provider
  support that relationship. Save a supplier record when an audit needs an
  exact historical snapshot.
- **Large files and media.** Map document titles, tags, file sizes and source
  IDs from an object store. Search those fields through CQL and fetch the
  original file from the source when opened. Materialize selected documents
  when the workflow needs to retain and replicate their contents.
- **Frequently read records.** Keep a large supplier catalog virtual, cache
  popular records within a freshness and size policy, and persist selected
  records as full Holons. An app or avatar owner can choose those policies in
  the planned OPORTAL controls.

The storage saving depends on what is copied. A field map stores definitions
such as the source field, CQL field and type once per collection. A source ID
or locally indexed metadata can add storage per row. Copying most values into
local metadata or indexes can approach the size of a full import for small
records. Leaving large payloads, attachments and seldom-used values at the
source avoids storing and replicating those bytes in OASIS.

HyperDrive can replicate full Holons and use eligible replicas for failover
or load balancing according to policy. A virtual read still depends on an
authorized and reachable source, or on a cache or equivalent source replica
that satisfies the query's freshness and completeness requirements. A partial
replica result cannot stand in for a complete collection. Source permissions
and provider capabilities determine which fields and operations are available.

The WEB4 demo uses the same CQL query to return matching stored and virtual
rows, including provenance. Saving a virtual row creates an owned snapshot
that can appear in a later query without duplicating its logical record.
General cross-source joins, automatic hybrid placement, and full OPORTAL policy
execution remain roadmap scope; the source checkpoint below distinguishes the
implemented controls from their remaining admission and live acceptance gates. See [current verification](CURRENT_STATUS.md) for the
specific sandbox flows that have been verified.

### WEB4 sandbox acceptance

Expose Full import and Virtual mapping in the sandbox import flow. Full import
persists the selected records under the existing ownership and replication rules.
Virtual mapping registers the collection and approved metadata fields. Its read
path resolves those fields from the source and does not persist one Holon per row.

Use one CQL request to return both imported and virtual Holons using the same typed
filter and projection. Verify exact results, tenant isolation, stable logical
identity, deduplication when physical copies overlap, and provenance on every row.
Edit a fixture at the external source and repeat the query to demonstrate live
resolution. Measure stored bytes and query latency for both modes and verify
that virtual reads produce zero row saves or HyperDrive replication obligations.

### Hybrid policy in OPORTAL

18 November source checkpoint: the OPORTAL editor exposes avatar/app scope,
Full import, Virtual mapping, Hybrid and explicit snapshot-save preference.
Its source is b5e56176ec20 on the existing portal feature branch. WEB4 source
c6e90269c261 persists versioned scoped policies and admits new placement operations;
frozen685dd81a0c1e is undergoing qualification44221309989. Eight editor groups and
the existing 23-group portal regression passed; no hosted settings save or live
publication is claimed. The Memory option and limits are present but disabled
when WEB4 refuses that capability. Existing full and virtual records remain
visible after a policy change. Clients select app scope with applicationId on
native import/mapping requests; the mapping retains it for later snapshots.
Complete read-through cache execution, collection/record exceptions, intelligent
placement and hosted verification remain required below.


Allow the owner to configure source, placement, caching and replication policy
per avatar or application, with collection and record selectors for exceptions.
A single collection may return ephemeral Holons, serve selected cached records,
and hold explicitly materialized Holons under the same policy. Show which
representation served a result, its authority and freshness, and the policy
that selected it. Define how queries deduplicate virtual and saved copies using
logical identity and provenance.

Start with deterministic rules for selection, TTL, maximum cached bytes, data
sensitivity, allowed replica providers and separate write intent. Use measured
read frequency and latency to recommend cache placement. AI recommendations
must obey policy and require explicit authorization for persistent copies,
external writes, or broader replication. Frequently updated data needs an
explicit authority and conflict policy before enabling automatic placement.
HyperDrive can replicate approved full Holons and separately replicate mapping
definitions. A mapping does not establish a payload replica.

Updated 2 November 2026. Full programme remains incomplete. [CURRENT_STATUS](CURRENT_STATUS.md) owns release state and the exact next action.

1. Complete breadth-first live provider demos: **100 locally integrated query types,
   60 scoped hosted HTTP proofs, 28 browser proofs/32 open**. ClickHouse's original
   hosted audit and card/static assets pass. ThreeFold code is published; its exact
   code job succeeded and original settings were applied once. Activation a737028a
   succeeded and original hosted acceptance/audit/card/static files pass. Next recover
   published launcher runtime/configuration gaps. Preserve accepted cohorts; no green hardening.
   CURRENT_STATUS owns exact release state and commands.
2. Continue explicit filtered source queries and Mongo federation for remaining red providers. Azure Cosmos, Oracle and PostgreSQL hosting are deferred. Public source reads do not establish native writes or full ORM support.
3. Finish scoped HyperDrive V2 operational acceptance: authorized late-replica baseline/reconciliation, retained obligations and uncertain-commit recovery. Preserve existing six-direction native proofs; broader hosted/operator/transport/scale acceptance remains open.
4. Reconcile original ORM requirements against provider-specific evidence: change tracking, save-state ownership, atomic cross-client conditional writes, child load/save/query parameters and depth, rollback, version/history, per-save deltas, migration and placement.
5. Define and implement primary-authority cutover with authority epochs and in-flight ownership; replica baseline delivery alone is not authority migration.
6. Resume deferred EF/NHibernate parity, unrestricted POCO mapping and migration tooling only after demo priorities. Work red checklist rows; freeze green rows/cursors absent a reproduced bug.
7. Finish the full implementation of every provider after the breadth-first WEB4
   demo and CQL/COSMIC ORM/HyperDrive MVP. Use each provider's real SDK, client,
   or documented API. Replace shipping-path placeholders and TODOs with working
   implementations and record native verification for every provider. Track
   CQL parity with Entity Framework and NHibernate across the provider inventory,
   with explicit capability evidence and remaining gaps in the parity checklist.

Only HyperDrive V2 receives implementation changes; document legacy gaps without fixing legacy code. Preserve root-cause-only policy and explicit capability errors. No overall completion percentage follows from focused demos.

[Acceptance checklist](CQL_PARITY_CHECKLIST.md) · [Detailed backlog](AFTER_DEMO_BACKLOG.md) · [Original requirements/history](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_history_2026-10-27.md) · [Prior milestone history](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_before_hosted_reconciliation_2026-11-02.md).
