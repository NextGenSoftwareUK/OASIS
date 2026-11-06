# Deno KV CQL MVP

The candidate targets Deno Deploy’s current managed KV Connect URL at
`https://api.deno.com/v2/databases/<database-id>/connect`. On 6 November 2026,
unauthenticated metadata requests for the same synthetic UUID received HTTP
401 at that route and HTTP 404 at the Classic route. The storage provider
and hosted query source share the current connector URL. The storage provider
resolves relative endpoints against the metadata exchange for protocol v2
and later. The current route needs credentialed qualification against an
owned database. The hosted query source uses
the existing exact-ID public Holon profile and reads canonical
`oasis/holon/{id:N}` keys through a single strongly consistent snapshot range.
[Deno Deploy KV](https://docs.deno.com/deploy/reference/deno_kv/)
[KV Connect specification](https://github.com/denoland/denokv/blob/main/proto/kv-connect.md)

The long-lived token is scoped by the client to the fixed metadata exchange.
The ephemeral token is sent only to the reviewed HTTPS data endpoint after
verifying the database UUID and strong consistency designation. The query
transport rejects redirects, bounds request time and response bytes, and
checks that any returned entry matches the requested key. The query backend
disallows listing and mutations.

Local protocol verification passed 11 hosted profile checks and two storage
regressions without skips at Core `79405fdb497834f275ef10edeb7d4b3b2c47ee38`.
The tests cover the public Holon filter before paging, exact-ID reads, absent
records and rejection of invalid snapshots and unreviewed endpoints. The TRX
SHA-256 is `6f61be7cc18d87a025cad5a89d4bb9c0c637c303621c4a2ed5871260ae64b192`.

The native verifier compiled with zero errors at Core
`79405fdb497834f275ef10edeb7d4b3b2c47ee38`. The build log SHA-256 is
`066d1eeb6124bd0173b5b4d9ec0e100a63da1b1e6831cf01bd6c0169bb1d7c02`.
The build-only run made no native requests. The verifier checks the exact
probe, an existing tombstone, an absent key and CQL 3/2/0 results. The
descriptor contains `DatabaseId`, `ReviewedEndpoint`, `Actor`, `LiveIds`,
`DeletedId` and `MissingId`. The credential is supplied privately as
`CQL_DENO_KV_QUERY_READER_TOKEN`.

Before admitting the source to WEB4, run the verifier against a dedicated
managed Deno KV database and confirm its connector authority, UUID and
strong data endpoint. [Owner actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md#deno-kv-managed-teaching-database-access---2026-11-06).
Hosted HTTP and browser counts remain open. LocalFile retains the first live
Development WEB4 rollout slot.
