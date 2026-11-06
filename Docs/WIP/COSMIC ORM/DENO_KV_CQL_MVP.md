# Deno KV CQL MVP

The candidate targets Deno Deploy’s current managed KV Connect URL at
`https://api.deno.com/v2/databases/<database-id>/connect`. The existing storage
provider continues to use its Classic connector. The hosted query source uses
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

Local protocol verification passed 11 hosted profile checks and two Classic
regressions without skips at Core `79405fdb497834f275ef10edeb7d4b3b2c47ee38`.
The tests cover the public Holon filter before paging, exact-ID reads, absent
records and rejection of invalid snapshots and unreviewed endpoints. The TRX
SHA-256 is `1ec643d65d667745f5962bf33c20de4bf21123ec673086d30828f65118453549`.

Before admitting the source to WEB4, prove a known probe and native 3/2/0
counts against a dedicated managed Deno KV database. Confirm
the actual authority of the connector token, the database UUID and the strong
data endpoint before transmitting credentials. Hosted HTTP and browser counts
remain open. LocalFile retains the first live Development WEB4 rollout slot.
