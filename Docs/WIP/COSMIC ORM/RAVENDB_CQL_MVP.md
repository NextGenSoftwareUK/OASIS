# RavenDB bounded CQL demo scope

Started 5 November 2026. Native and hosted acceptance remain open; no coverage
count increase. Existing provider uses RavenDB.Client 7.0.1 and OasisHolons/{id}
envelopes containing camel-case canonical Holon JSON. Preserve that layout.

The new query path uses the shared bounded identity compiler and exactly one
explicit RavenDB authority. It intersects IDs with the server-owned read
catalogue before SDK requests, loads the selected documents in a fresh
nontracking/noncached async session, validates native envelope and payload
identity/deletion/read authority, strips children, then applies paging in
admitted-key order. No index scan, provisioning, writes or alternate storage
path is used. Historical queries and unsupported predicates are rejected.

The leased provider/shared build passes with zero errors (four warnings). The
first build rejected tracking/cache flags on Advanced; SDK SessionOptions owns
those settings, and the corrected build passes. No native execution is claimed.
Next verify controlled
admission and malformed/foreign envelope handling, then qualify a real retained
or explicitly owned RavenDB service with native 3/2/absent/deleted reads and
source preservation. Follow with manager/LINQ, local HTTP/Mongo pairing,
private isolation and replicas before any Development registration. Do not
claim RavenDB completeness from compilation or the existing SDK dependency.

The SDK materializes its response before per-document (1 MiB) and aggregate
payload (4 MiB) validation; these are accepted-payload bounds, not transport
stream allocation guarantees. Native rows are moving reads, not a distributed
snapshot. Wider content predicates, conditional writes/history, graphs,
replication and full ORM parity require their own evidence.

Controlled query gate: 13/13 tests pass, zero skipped, including eight new CQL
cases and five existing metadata checks. The CQL cases capture mutable key
arrays, intersect the authority catalogue, reject wrong provider binding and
unsupported name/metadata/order/graph/unbounded shapes, and verify pre-cancel,
inactive-store and invalid-plan/budget errors without a network connection.
The shared compiler remains the single owner of bounded identity algebra.
Native envelope/payload behavior, real-service availability and manager/HTTP
execution still require separate tests. No server or teaching records created.
