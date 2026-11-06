# Cloudflare Workers KV CQL MVP

The provider candidate uses the Workers KV REST API to read existing
`holon:{id:D}` records in one reviewed account and namespace. The shared
exact-ID CQL profile validates canonical identity and public visibility before
paging. The native transport bounds each response to 1 MiB and inherits the
profile's query deadline. The reader never follows redirects, and the query
source rejects list operations, writes and non-Holon keys.
[Workers KV value API](https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/methods/get/).

Use a dedicated namespace containing only public teaching records. Workers KV
reads can be stale after a write, including reads of missing keys. The fixture
must be stable before qualification. Data whose visibility can be revoked is
outside this teaching scope.
[Workers KV consistency](https://developers.cloudflare.com/kv/concepts/how-kv-works/).

Both protocol tests passed without skips at Core
`79405fdb497834f275ef10edeb7d4b3b2c47ee38`. They verify exact requests,
deleted and absent records, private records before paging, and denial of
writes, lists, non-Holon reads, forbidden responses and oversized bodies.
The TRX SHA-256 is `f303080c5fce9f4a69e35ce8b13c04bcebc3dfcbe0991cc490f7cce09fab592e`.

The native verifier compiled at the same Core pin. The build log SHA-256 is
`64eead0325a233f3c2b88f1ca76c4b8d7b527031d865648cb4eae8939b32d632`.
It compares native payload hashes before and after CQL and stops before
building if the reviewed descriptor or reader token is missing. The build-only
run made no Cloudflare requests.

Native qualification needs three stable public live records, one deleted
record and one reserved absent ID. Review the account
boundary of a token with Workers KV Storage Read. Confirm the real missing-key
response, prove the exact probe and 3/2/0 counts, then review WEB4 enrollment.
The access review is tracked in the [owner actions list](../Release%20and%20Status/ACTIONS-FOR-DAVID.md#cloudflare-workers-kv-teaching-namespace-access---2026-11-06).
LocalFile retains the first live Development rollout slot.
