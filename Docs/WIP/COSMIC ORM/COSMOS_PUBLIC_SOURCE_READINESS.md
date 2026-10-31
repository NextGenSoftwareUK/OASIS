# Cosmos blockchain public-read readiness

31 October 2026. Initial REST feasibility verified; CQL native acceptance is failing with endpoint HTTP502. HTTP/federation/browser acceptance remains incomplete.

- Anonymous PublicNode REST returned HTTP403 before a body. No retry/fallback was performed.
- Explicitly selected CryptoCrew REST endpoint from the Cosmos chain registry: `https://rest.cosmoshub-main.ccvalidators.com`.
- Initial attempt to query the latest block height returned HTTP500. The block endpoint advertised application state one height behind the block. The corrected probe explicitly selects that advertised state height and requires every staking response to acknowledge the same height; it never retries at latest.
- Five bounded reads passed: latest block, one page limited to three validators, then three individual validator records. Chain `cosmoshub-4`; block33667800, application state33667799. No wallet, transaction, account, node installation or genesis download.
- This is server-reported state consistency, not independently verified consensus or Merkle proofs. Historical retention and endpoint availability are not guaranteed.

Implemented task-only: `CosmosPublicValidatorMetadataSource` in the existing Cosmos provider. It admits only point/eight-key-set queries over configured Bech32-checked validator addresses, validates every response height/identity, rejects partial pages even with Take(0), preserves exact UInt256 token strings and uses the shared stable native mapper. No canonical signed storage registration changed.

Controlled protocol/query suite: **16 passed, zero skips**. Initial tests used a static array expression unsupported by the query expression compiler; changed the test to the documented captured local selection. Native CQL: **one failed, zero skips**, HTTP502 on latest block; independent Python probe also returned502. Earlier REST five-read success is historical, not current availability. No retry/fallback was added.

Next: obtain current native3/2 acceptance on an explicitly selected functioning free endpoint, then HTTP+Mongo5/4/isolation and hosted integration. Preserve the current source owner if changing only the endpoint. Source representation design remains using a fixed configured address set, exact numeric strings, consistent pinned state, strict schema/identity guards and capability checks. Add negative protocol tests and real native3/2, then HTTP+Mongo5/4/isolation before hosted registration. Do not advertise Cosmos as live yet.

Historical successful probe evidence (subsequent probe overwrote the regenerable file; hash below identifies the historical bytes): `TestResults/CosmosPublicSourceCandidateCurrent/readiness-cryptocrew.json`; SHA256 `cbe557f6e025f00841a211a5f9c9bde1bc740e386675c60c5dad14b9f4212026`. Raw responses are not retained in Docs. Recreate with `python Scripts/Sandbox/Probe-CosmosPublicReadiness.py --endpoint https://rest.cosmoshub-main.ccvalidators.com`; observations change with chain height.

Primary contracts: [staking query proto](https://github.com/cosmos/cosmos-sdk/blob/main/proto/cosmos/staking/v1beta1/query.proto), [Cosmos Hub chain registry](https://github.com/cosmos/chain-registry/blob/master/cosmoshub/chain.json). Runtime admission remains unchanged.

- Current evidence `TestResults\CosmosPublicSourceCandidateCurrent\provider-controls.trx`: SHA256 `8b456c2a54d96b3656875de4ffce46255bae139de91abf04ebd509c55a4cf763`, 26413 B.

- Current evidence `TestResults\CosmosPublicSourceCandidateCurrent\provider-all.trx`: SHA256 `530ffc8b5882ccdec03b76fd5d8f59b9ccaee52c89f50b2297804dd5e433f969`, 30093 B.

- Current evidence `TestResults\CosmosPublicSourceCandidateCurrent\readiness-cryptocrew.json`: SHA256 `afa81ecc91aae991af460e7636dd7761ce345ac304648b3bcac7f80da923934e`, 297 B.
