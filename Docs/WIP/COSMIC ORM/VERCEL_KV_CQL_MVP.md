# Migrated Vercel KV CQL MVP

Vercel retired its first-party KV product and migrated existing stores to
Upstash Redis. This candidate preserves the `VercelKVOASIS` authority and
its `oasis/holon/{id:N}` keys on a reviewed migrated store.
[Vercel Redis migration](https://vercel.com/docs/redis).

The bounded public CQL profile submits a single `GET` per exact Holon key
to the configured Upstash HTTPS origin. The response must be a string or null
in the documented JSON envelope. It rejects redirects, responses larger
than 1 MiB, non-Holon reads, listing and writes. An existing public record
proves activation. Identity and visibility are checked before paging.
[Upstash REST protocol and read-only tokens](https://upstash.com/docs/redis/features/restapi).

Two profile checks and two storage regressions passed without skips at
Core `79405fdb497834f275ef10edeb7d4b3b2c47ee38`. The TRX SHA-256 is
`70430699c508d5eea897be54cab0dd28165864094690031003e815311d380907`.
The first test run exposed an incorrect assertion that the inherited save
method makes no requests. It can read the old Holon before its write is
rejected. The corrected test verifies the backend rejects write, list and
non-Holon commands before sending them.

The native verifier compiled at the same Core pin. Build log SHA-256 is
`4c7bee7abde256331a2a3014274914cd01825f8d7c966ce65185fe0bbdee932c`.
It compares payload hashes from direct REST reads before and after CQL 3/2/0.
The build-only run sent no requests. Native proof requires an existing migrated
teaching store, its dedicated read-only token and a reviewed catalogue.
[Owner actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md#migrated-vercel-kv-teaching-store-access---2026-11-06).
This evidence is separate from `UpstashOASIS`, whose records use different
native keys. LocalFile remains first for live Development WEB4 rollout.

The protected settings draft binds `CQL_VERCEL_KV_QUERY_ENDPOINT` and
`CQL_VERCEL_KV_QUERY_PROBE_ID` to the reviewed catalogue. It requires the
protected read-only token in the baseline and rejects foreign origins.
The combined WEB4 run passed seven checks each for Workers KV, migrated
Vercel KV and Dapr, with zero skips, at Core
`4d6cfb6071ce76025f6ccff6a37ce0125f78680e`. The TRX SHA-256 is
`ee3bb7c276927b2a703f33181d5c27d387cfc7d26e9c4e9eeba5313c531d924c`.
Credentialed native and live Development WEB4 verification remain open.
