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

## Native CQL profile and WEB4 build

Five focused tests passed with zero skips. Native provider activation and CQL
execution over the preserved Explorer catalogue passed one test with exact
3/2 results and zero skips. Native TRX SHA256 16315AE133D09811FDDD764713F2B23CC7F7854DCC16A2624F30856FF71394C5.
WEB4 builds with explicit query-source registration. Next verify the HTTP
presets and Mongo federation/privacy, then prepare guarded hosted admission.
No deployment or hosted coverage increment.

## WEB4 preset and rollout controls

All four presets plan against shipping native and Mongo capabilities in one
passing test. TRX SHA256 FF040A9DA82CB8C46E2D64A627DD6BB89A01B540D11ABE27D3B93A0F1107C2C1. The published client derives
the exact two-message preparation request. Two offline rollout checks pass.
The source-bound native receipt is retained. Local HTTP acceptance requires
an owned loopback Mongo replica set. This laptop has MongoDB 8.2 installed
and an unrelated service on 27017. The former laptop's 8.3.11 binary and
replica store are absent. Use a separately owned reusable local test store
with explicit identity and keep the existing service outside the test.
No WEB4 configuration or deployment has changed.
