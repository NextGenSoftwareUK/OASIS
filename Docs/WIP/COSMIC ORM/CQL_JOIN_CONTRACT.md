# CQL native join implementation contract

25 October 2026. Required behavior for the open join milestone. This is an
implementation contract, not an announcement that joins are available. Current
shipping operators remain listed in the developer manual and completion roadmap.

## Ownership and scope

Inner and left joins must use the generic Core/HolonManager planner and existing
V2 physical-attempt/receipt owner. Providers translate admitted join intent into
native execution; no manager-side scans, fallback evaluators or entity hydration.

Each selected provider joins its own two admitted sources. Selecting several
providers returns separate provider results, consistent with current CQL behavior.
It does not create a cross-provider join, deduplicate identities or merge groups.
Distributed joins require the separate federation plan and accounting contract.

Both sources retain their filters, relationship predicates, trusted access actor,
consistency requests and source windows. Source windows apply before joining.
Actual-adapter admission must validate both sources, join keys, output columns
and requested composition before dispatch. A row/projection capability cannot
grant joins. Incompatible provider scopes or security actors reject explicitly.

## Identity and missing rows

Keys contain one to eight admitted scalar components. Equality uses the existing
exact scalar domains: ordinal Unicode strings, logical GUID identity, UTC ticks
and declared numeric domains. Native collations and lossy numeric casts cannot
change identity. Numeric widening needs explicit capability admission.

A null component prevents a key match. Composite matching requires every
component to match. Inner joins omit unmatched left rows. Left joins publish
each unmatched left row once, with explicit missing-right state. A missing right
row must remain distinguishable from a present row containing nullable values.
Projected right-side value types must represent absence; never invent zero,
false, an empty GUID or a fabricated entity for an unmatched row.

## Work, consistency and publication

Each source has independent input and byte limits. Pair cardinality, detached
output count and output bytes are independently bounded. Duplicate keys create
real fan-out; exceeding a pair limit is an error, never implicit truncation.
Count products must be checked before output expansion, including left-join
unmatched contributions. Post-join filtering cannot hide excess work or malformed
participating source data. Take(0) cannot bypass protected validation.

Both sources execute under one native read owner. SQLite/Mongo snapshot requests
must share their existing snapshot owner. Neo4j retains its declared read-committed
semantics and rejects unsupported snapshot requests before I/O. Cancellation,
deadlines and teardown cover validation, matching and publication together.

Post-join ordering and paging preserve operation order. Deterministic identity
ties include both source identities and missing-right state. Validate the entire
detached packet buffer before invoking result constructors. Failures publish no
partial result and never alter tracking/save ownership of either source.

## Required acceptance on MongoDB, SQLite and Neo4j

- Inner/left matches, duplicates, unmatched rows, nullable and composite keys.
- Exact scalar boundaries and alternate admitted GUID/date representations.
- Hidden rows on either side, malformed hidden data and conflicting actors.
- Source windows versus joined-result windows, deterministic ties and Take(0).
- Excess input/pairs/bytes, arithmetic overflow and borrowed capability rejection.
- Mutation between source observations under the requested consistency contract.
- Cancellation/deadlines during native I/O, joined teardown and later reuse.
- Generic manager and signed HTTP execution, durable receipts and no replay.
- Annotated SDK/HTTP examples and coherent producer/consumer regression evidence.

These gates supplement the existing roadmap. They do not remove remaining
projection composition, aggregates, federation, authorization or ORM requirements.

## Immutable join intent checkpoint — 25 October 2026

Core now captures same-provider `Join` and `LeftJoin` intent through the existing
projection/path compiler. Scalar and named composite keys have1–8 components;
DTO member initialization is aligned by alias, without invoking constructors,
getters, methods or a client evaluator. Exact scalar domains share one registry
with grouping; implicit numeric conversion does not acquire a join capability.

Both source query objects remain immutable and intact, including filters,
relationships, source ordering/windows, graph requirements and trusted access.
Provider scope sets must agree; mixed actors or an unbound side paired with an
actor-bound side reject. Either source can require the stronger native snapshot.
Independent left/right input rows/bytes, pair-work and output rows/bytes have
validated bounds. Null key components are defined not to match.

Both Core frameworks build; fullCore2,627 passes (23new intent controls), zero
skips. No provider I/O or new environment is involved. This is an intent/compiler
checkpoint only: join result selection, capabilities, physical planning/attempts,
native implementations, signed HTTP and demos remain unimplemented. No native
provider advertises join support, and row/projection execution cannot execute a
join intent. The unchecked join milestone remains open.

## Detached join selection capture — 25 October 2026

Join intent now supports `Select((left,right) => ...)` for scalar, anonymous and
flat DTO descriptions, reusing the shared detached-shape compiler. Columns retain
their source side, raw path/domain and output CLR type. Explicit `right != null`
and `right == null` selections capture row presence independently of nullable data.
User-overloaded comparisons do not become native presence tests.

Left-join right-side value types must represent absence. An identical scalar may
be lifted to Nullable<T>; numeric widening cannot be hidden inside that lift.
Reference/nullable fields remain distinct from missing-row presence. Computation,
entities, nested/collection output, duplicate aliases and over32columns reject.
No selected constructor, setter, getter or application method runs during capture.

FullCore2,645 passes (18new selection controls), zero skips; both frameworks build.
This is result-description capture only. Future join packets must carry validated
right-row presence/identity independently of selected nullable values, and all raw
buffers must validate before shared DTO construction. Join capability/planning,
physical attempt ownership, native matching on three stores, output validation,
HTTP and executable demos remain open. No native join support is advertised.
