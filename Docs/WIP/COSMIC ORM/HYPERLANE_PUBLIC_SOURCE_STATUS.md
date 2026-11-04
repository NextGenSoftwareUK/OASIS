# Hyperlane public source qualification

4 November 2026. Open. No native CQL or hosted acceptance.

Hyperlane documents anonymous indexed message reads and a message ID equality
query. Its operations guide identifies the Hasura Explorer endpoint.
The scoped candidate is read-only lookup over a fixed catalogue of distinct
32-byte message IDs, using exact ID equality and bounded membership. Project
origin/destination domain IDs and indexed delivery state. Keep message body,
wallet addresses and payment data outside the demo. Delivery is explorer-reported.

- [Hyperlane GraphQL contract](https://docs.hyperlane.xyz/docs/reference/explorer/graphql-api)
- [Explorer operations guide at inspected source pin](https://github.com/hyperlane-xyz/hyperlane-monorepo/blob/46bd67392990755ef5220e7622446a56fdd918f6/docs/ai-agents/operational-debugging.md)

First qualification requested three delivered message IDs ordered by send time
from the documented Hasura endpoint. HTTP 200 carried a GraphQL
time-limit-exceeded error at the project's five-second cap. Zero records were
qualified. Preserve the original request and response under
TestResults/HyperlanePublicSourceCurrent. Response 150 bytes, SHA256
3296b5315701cad409882cd4dce9f75fbd6a6b248fc478e60981b634b87e51a6.

Next qualify known IDs using the documented exact-key operation with a separate
recorded intent. Preserve the original failed discovery. Require matching IDs
and exact three/two coverage before implementing the provider profile. No account,
write, package, runtime, tariff or hosted setting was created.

## Exact message IDs qualified

A separate lineage-bound discovery selected at most three IDs without sorting
or filtering the whole index. Three distinct 32-byte IDs returned. Exact-key
membership requests for all three and the first two returned exactly the
requested sets, both HTTP 200. This qualifies indexed source access only.
The first timeout remains recorded. Raw evidence and intent hashes are retained
under LocalState/COSMIC/HyperlanePublicQualificationCurrent.
Next implement the bounded read-only provider profile against this catalogue.
