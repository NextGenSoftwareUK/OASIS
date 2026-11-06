# Fastly KV CQL MVP

The query transport candidate uses Fastly's exact item endpoint for
canonical `oasis/holon/<UUID>` keys. It keeps the credential on
`api.fastly.com`, uses guarded HTTPS and rejects listing, writes and
deletes. It bounds the native body to 1 MiB and distinguishes a missing
item from a denied read. The ordinary storage transport remains available
through its existing constructor.
[Fastly KV item API](https://www.fastly.com/documentation/reference/api/services/resources/kv-store-item/).

The isolated protocol tests cover the exact request, rejected operations,
absence versus denial and oversized payloads. Seven Fastly provider tests
and nine Netlify regressions passed without skips at Core
`77d943944552f188e0aaccf128ba2c8cc2f4ef9d`. The TRX SHA-256 is
`cd3a4c2af324eac9d68d0d7a8598f4e894485b459ad48bce7916de07c57e79f1`.
WEB4 admission and native qualification remain open.

Fastly and Netlify share public probe verification, canonical payload
validation and paging. Each provider keeps its own guarded HTTP transport.
The Fastly CQL regression verified rejection of a private selected item
that would fall beyond the first page.

The WEB4 settings draft uses `CQL_FASTLY_QUERY_STORE_ID`,
`CQL_FASTLY_QUERY_READER_TOKEN` and `CQL_FASTLY_QUERY_PROBE_ID`. The
protected-settings reviewer binds the exact store and first public UUID.
All 21 settings checks passed, including rejection of a substituted store.
The WEB4 registration and tests remain outside the running Netlify build.

The native verifier draft checks the exact live, deleted and absent keys,
runs CQL 3/2/0 and compares Fastly generation markers and object hashes
before and after. It stops before the build lease if the reviewed catalogue
or reader token is missing. The C# probe still needs to compile.
[Fastly generation markers](https://www.fastly.com/documentation/reference/api/services/resources/kv-store-item/).

Fastly documents a `global:read` scope and notes that service restrictions
do not constrain non-service endpoints. Before native verification,
confirm that the reviewed teaching account and token can read the KV
route with the required access boundary. No credential has been requested
for this candidate.
[Fastly user token scopes](https://www.fastly.com/documentation/reference/api/auth-tokens/user/).
