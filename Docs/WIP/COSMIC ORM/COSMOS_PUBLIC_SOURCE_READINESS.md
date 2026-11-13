# Cosmos blockchain public-read readiness

31 October 2026. Native CQL and C# console3/2 verified on explicitly selected KJNodes. Local HTTP/federation/isolation now verified; hosted/browser acceptance remains incomplete.

- Anonymous PublicNode REST returned HTTP403 before a body. No retry/fallback was performed.
- Explicitly selected CryptoCrew REST endpoint from the Cosmos chain registry: `https://rest.cosmoshub-main.ccvalidators.com`.
- Initial attempt to query the latest block height returned HTTP500. The block endpoint advertised application state one height behind the block. The corrected probe explicitly selects that advertised state height and requires every staking response to acknowledge the same height; it never retries at latest.
- Five bounded reads passed: latest block, one page limited to three validators, then three individual validator records. Chain `cosmoshub-4`; block33667800, application state33667799. No wallet, transaction, account, node installation or genesis download.
- This is server-reported state consistency, not independently verified consensus or Merkle proofs. Historical retention and endpoint availability are not guaranteed.

Implemented task-only: `CosmosPublicValidatorMetadataSource` in the existing Cosmos provider. It admits only point/eight-key-set queries over configured Bech32-checked validator addresses, validates every response height/identity, rejects partial pages even with Take(0), preserves exact UInt256 token strings and uses the shared stable native mapper. No canonical signed storage registration changed.

Controlled protocol/query suite: **16 passed, zero skips**. Initial tests used a static array expression unsupported by the query expression compiler; changed the test to the documented captured local selection. Native CQL: **one failed, zero skips**, HTTP502 on latest block; independent Python probe also returned502. Earlier REST five-read success is historical, not current availability. No retry/fallback was added.

Next: add HTTP+Mongo5/4/isolation and hosted integration; native3/2 is now verified on KJNodes. Preserve the current source owner if changing only the endpoint. Source representation design remains using a fixed configured address set, exact numeric strings, consistent pinned state, strict schema/identity guards and capability checks. Add negative protocol tests and real native3/2, then HTTP+Mongo5/4/isolation before hosted registration. Do not advertise Cosmos as live yet.

Historical successful probe evidence (subsequent probe overwrote the regenerable file; hash below identifies the historical bytes): `TestResults/CosmosPublicSourceCandidateCurrent/readiness-cryptocrew.json`; SHA256 `cbe557f6e025f00841a211a5f9c9bde1bc740e386675c60c5dad14b9f4212026`. Raw responses are not retained in Docs. Recreate with `python Scripts/Sandbox/Probe-CosmosPublicReadiness.py --endpoint https://rest.cosmoshub-main.ccvalidators.com`; observations change with chain height.

Primary contracts: [staking query proto](https://github.com/cosmos/cosmos-sdk/blob/main/proto/cosmos/staking/v1beta1/query.proto), [Cosmos Hub chain registry](https://github.com/cosmos/chain-registry/blob/master/cosmoshub/chain.json). Runtime admission remains unchanged.

- Current evidence `TestResults\CosmosPublicSourceCandidateCurrent\provider-controls.trx`: SHA256 `8b456c2a54d96b3656875de4ffce46255bae139de91abf04ebd509c55a4cf763`, 26413 B.

- Current evidence `TestResults\CosmosPublicSourceCandidateCurrent\provider-all.trx`: SHA256 `530ffc8b5882ccdec03b76fd5d8f59b9ccaee52c89f50b2297804dd5e433f969`, 30093 B.

- Current evidence `TestResults\CosmosPublicSourceCandidateCurrent\readiness-cryptocrew.json`: SHA256 `afa81ecc91aae991af460e7636dd7761ce345ac304648b3bcac7f80da923934e`, 297 B.

## C# walkthrough (native local acceptance verified)

The existing `Examples/CosmicCqlDemo` console now has a spaced, commented Cosmos walkthrough. It uses the same `NativeKeySetWalkthrough` → `CosmicLinq` → `HolonManager` pipeline as other key-set sources, not a separate query engine. It reads the existing source configuration and creates no synthetic records or database fixtures.

```powershell
dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-build --no-restore -- --cosmos-public LocalState/COSMIC/HostedCosmosPublicSourceCurrent/source.json
```

The intended result is three validators, then a filtered two-validator selection. The retained configuration now explicitly selects KJNodes. CryptoCrew failure evidence remains preserved. This local demo is verified; hosted/federation acceptance remains pending. The native acceptance test now executes the published walkthrough source verbatim, so it will exercise manager/LINQ execution as well as the source when the endpoint is available.

Additional explicit endpoint checks: registry-listed Lava REST returned404, StakerHouse returned403. Neither was silently selected or written into the source catalogue. No endpoint fallback or retry added. Their bounded JSON failure summaries are in `TestResults/CosmosPublicSourceCandidateCurrent/readiness-{lava,stakerhouse}.json`.

Preview validation: console build succeeded, **17 focused controls passed, zero skips**, including the published shared manager/LINQ walkthrough returning3/2 with stable IDs against controlled native responses. Historical note: this was before the explicit KJNodes selection below; the full native walkthrough subsequently passed.

- `TestResults/CosmosPublicSourceCandidateCurrent/console-controls.trx`: 27982 B; SHA256 `bfb6448ec5f31fb0c7bcc1606e9b2b5be7aefb2948799ec7e7cdc2ab9dd73037`.

- `TestResults/CosmosPublicSourceCandidateCurrent/readiness-lava.json`: 279 B; SHA256 `4d12d74a8f74b722d0666bf264ad08c4b5c094313d3518d8d8d86a4f63f35509`.

- `TestResults/CosmosPublicSourceCandidateCurrent/readiness-stakerhouse.json`: 289 B; SHA256 `1aad73d35f51b545d317b49de5d41c84defd02d9f743363b4ca4d243085dcea9`.

- `TestResults/CosmosPublicSourceCandidateCurrent/console-build.log`: 835023 B; SHA256 `1f6505de86cb1b502da322d8751fe60a4a16d730010219db8841f7b7f35d2c1b`.

## Native acceptance milestone: KJNodes

Explicitly selected registry-listed `https://cosmoshub.api.kjnodes.com/`. Bounded readiness passed5reads at state33667879. Only restUrl changed in the existing retained catalogue; the three addresses and stable mapping owner are unchanged. No fallback, retry, source writes or new environment.

The published Cosmos walkthrough passed the **one native test, zero skips**, executing actual CQL through HolonManager/LINQ and returning3full/2filtered validators. The real `--cosmos-public` CLI also succeeded with3/2. Tokens remain exact decimal strings, state heights acknowledged per native response. This proves server-reported state consistency, not independent chain verification or hosted availability.

- `TestResults/CosmosPublicSourceCandidateCurrent/readiness-kjnodes.json`: 1974 B; SHA256 `6269923a68e961ccfb081328cf6767b3f670cf1c5336bba1bb3475ab85806b51`.

- `TestResults/CosmosPublicSourceCandidateCurrent/console-native.trx`: 2872 B; SHA256 `6cbabbd6381cb3d12c9f5745bbb21e03bc9e39113e04d8c923c555b48920d48e`.

- `TestResults/CosmosPublicSourceCandidateCurrent/console-native-cli.log`: 1229 B; SHA256 `9c61a83d551a096bcae26658deb08ca43bb1e5078e50b59aac13015a4ea4aa39`.

## HTTP federation acceptance milestone

`CqlCosmosMongoNativeTests` passed1test/0skips using shipping leaf/federation endpoints on loopback Kestrel, real KJNodes and retained Mongo8.3.11. Native3/2, owner pairs5/4, foreign-private0 and foreign-public-pair3;9validated query receipts. Two SDK create-only saves use server-mapped private identities with original operations persisted before mutation. Every returned ID, provenance, owner and visibility is checked; source configuration remains unchanged.

Exact marker/nonce/2record checks passed before the owned `cosmic_cql_cosmos_http_demo` database was dropped; absence asserted. Mongo process stopped, no listener27029. Retained original intent585B,2ACKs108B each, cleaned70B (871B total) live under `LocalState/COSMIC/CosmosMongoHttpCurrent`. Do not reseed/replay the cleaned fixture. Controlled bearer/usage authority is not production login/billing, hosted replication or browser proof.

API15-provider admission/preset/discovery controls2pass; affected Core read-tariff controls9pass. Explicit source permission, price and query-only registration required; signed-write fence retained. Lazy `CQL_COSMOS_PUBLIC_SOURCE_JSON` registration added but no hosted configuration applied.

Next: strict publisher+joint config and four presets, browser source links, matching hosted deployment, then actual hosted preparation/isolation/replica/browser acceptance. [Maintained source guide](../../Devs/COSMOS_PUBLIC_METADATA_CQL_GUIDE.md).

- `TestResults/CosmosPublicSourceCandidateCurrent/cosmos-mongo-http.trx`: 3359 B; SHA256 `d7d3479657a4d2cd92c192419c60ad81bab07134c19d29365d4d19197eae1901`.

- `TestResults/CosmosPublicSourceCandidateCurrent/cosmos-mongo-native-summary.json`: 225 B; SHA256 `c1734958aa317f661e9dbc7187a20c446aef7d2a303ed738785295f58684bed0`.

- `TestResults/CosmosPublicSourceCandidateCurrent/api-controls.trx`: 4998 B; SHA256 `1b8007b5261e0cbe29d5711a0ecc6c023eabb256fe957b44c963b3f141974695`.

- `TestResults/CosmosPublicSourceCandidateCurrent/core-controls.trx`: 15968 B; SHA256 `a7203f66f4a5a26a1c1bb9c732637f3197f93007d90df7896d854ee5a0443759`.

## Hosted configuration and client staging

Strict Cosmos publisher3tests pass (including malformed/paid/overlap cases and both encryption policies); combined five-source publisher6tests pass. Cosmos gets native3/2 and Mongo-paired5/4 presets. Existing DNA/unrelated variables are unchanged. Independent candidates from an old snapshot must not overwrite shared permission/examples: use the joint Google/Ethereum/Polygon/Arbitrum/Cosmos candidate.

Protected joint snapshot/candidate prepared, not applied: `LocalState/COSMIC/HostedSourceRolloutCurrent/google-ethereum-public-polygon-arbitrum-cosmos-{before,candidate}-variables.dpapi`,67,094/80,454B,7values/20presets. Requires the matching application/Core169e402; never apply to frozen6bb. No hosted mutation/deployment occurred.

Frontend task branch `work/cosmic-cql`, commit84e27e5: local Cosmos guide/CLI/source links and generic private snapshot preparation coverage;52client tests pass. Repeat preparation0writes/2existing, server-derived private IDs and Mongo-only writes checked. No provider-specific production JS branch added. Public main remains unchanged. Hosted preparation/replica/browser checks remain open.

Next: matching scheduled hosted rollout, source availability, actual private visitor preparation/reuse/isolation and replica acceptance, then browser acceptance. This replaces the earlier next-step note to create the publisher/presets.

- `TestResults/CosmosPublicSourceCandidateCurrent/config-controls.log`: 106 B; SHA256 `383a12fba9b5b327054a24ce7e5739569c4f0b6621facd981f2d6a4271063b3c`.

- `TestResults/CosmosPublicSourceCandidateCurrent/combined-config-controls.log`: 109 B; SHA256 `ba9882e9ac323861c4e47fd88f712e12a56b4bd76a6d541faec02026dfe5f209`.

- `TestResults/CosmosPublicSourceCandidateCurrent/frontend-controls.log`: 5045 B; SHA256 `cfea90ab5ebc87a12ef3dcc707c1479d0b58ee149a3e81091379820ca0fb372d`.
