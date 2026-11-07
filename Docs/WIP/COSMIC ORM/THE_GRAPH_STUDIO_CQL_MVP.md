# The Graph Studio CQL demo profile

The reviewed profile reads indexing status for three public Studio deployments:
ADEXTO on Arbitrum One and Base, and Klerosboard on Gnosis. Each request uses
one fixed `_meta` GraphQL query. The response must match its reviewed deployment
CID, report healthy indexing and include a positive block height/timestamp and
canonical block hash. Metadata records retain the configured network label and
indexer-reported block identity. This is an indexer observation, not an
independent chain-consensus verification.

The Graph describes Studio endpoints as rate-limited testing endpoints and
provides an API-key gateway for production.
[Official endpoint guidance](https://thegraph.com/docs/en/subgraphs/querying/from-an-application/).
This demo profile uses the public Studio endpoints. Production gateway access,
arbitrary entity queries and general GraphQL translation remain separate work.

## Verified on 7 November 2026

- Twelve controlled provider cases passed, including exact routes, foreign-ID
  rejection without an HTTP request, unhealthy/malformed/oversized responses
  and refusal to publish partial results.
- One real CQL schedule returned all three deployments and a two-record subset.
- WEB4 registration/read-only parser/tariff admission passed32 cases, including
  four new Graph cases. No source is enrolled without explicit owner settings.
- One real loopback WEB4 HTTP/Mongo schedule passed with nine receipts:
  native3/2, Mongo federation5/4, foreign-private0 and foreign-public3.
  Two private copies were saved through the real Mongo SDK. Their exact owned
  test database was removed after ownership/count checks. The retained Mongo
  service stopped gracefully with native exit12 and WiredTiger closed.

The HTTP fixture uses controlled local authentication and metering. It does
not prove deployed signup/billing, HyperDrive replication or browser interaction.
The graph source code was unchanged during these checks. The Core task tariff
change is24467833a36f6d671958b4cf34f65ff98f1cff03; native HTTP used the
frozen outputs from that verified source tree. The deployment image must be
rebuilt from its final scoped commit.

## Query scope and examples

Current native CQL supports explicit reviewed IDs with equality/membership and
bounded paging. It projects the network, deployment CID and indexed block
metadata. Metadata/name filtering, arbitrary subgraph entities, writes and full
ORM parity are not part of this profile.

The readable [C# test/example](../../../Providers/Social/TestProjects/NextGenSoftware.OASIS.API.Providers.TheGraphOASIS.UnitTests/TheGraphStudioStatusQueryTests.cs)
shows the same LINQ membership expression for three/two-record selections and
checks public ownership, metadata and provider-native keys.
`Scripts/TestHosts/Run-CqlReviewedPublicMongoHttp.ps1` reuses the existing
Mongo8.3.11 store and frozen WEB4 outputs. It refuses a retained fixture intent
or process log. Do not rerun the accepted native cohort to regenerate reports.

## Evidence

Reports remain under `TestResults/TheGraphStudioCurrent/`. Durable fixture,
save, nine response and cleanup receipts are under
`LocalState/COSMIC/ReviewedPublicMongoHttpQualification/TheGraphOASIS/`.

| Report | SHA-256 |
| --- | --- |
| `controlled.trx` | `144d6f1c1beb2229a91419bb953692dd5db94d38e793f7abae4130f2b8a5258a` |
| `native.trx` | `b0f53a13b74244c4c5478a4af42be4382c21ae8f7593c28a7bf955033e5adb6a` |
| `api-admission.trx` | `aac20050a9beb4a823fb5a678c0afb561b759d40b2052cdbb0f3a0089d3c998d` |
| `web4-mongo-native.trx` | `76be3a170f3436330dc0152a1298662f256bb0891a71a0e449507fa008163822` |

## Next gates

Prepare a scoped release from current Development9c6e9300. Preserve the active
GUN/OrbitDB runtimes: use `teaching_sidecars=true` during image qualification.
Carry only the Graph source, WEB4 registration/compiler/tariff and proving tests,
plus the verified local acceptance cleanup fence. Do not deploy the broad task
branch or unrelated provider drafts. Configure a canonical owner through
`CQL_THE_GRAPH_STUDIO_QUERY_SOURCE_OWNER`, fixed public catalogue and native/
Mongo presets only after the exact image passes and deployment is verified.
Hosted native/federation/privacy/replica acceptance and browser proof remain open.

## Build output retention

The finished app, unit-test, provider and provider-test bin/obj directories total
1,981,209,912 bytes. Automatic tool policy rejected their exact scoped cleanup,
returning only blocked-by-policy. They remain retained and recorded in the
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md). No runtime was installed.
