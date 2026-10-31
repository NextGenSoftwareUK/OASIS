# Cosmos blockchain public-read readiness

31 October 2026. Feasibility verified; provider/CQL/HTTP/federation/browser acceptance remains incomplete.

- Anonymous PublicNode REST returned HTTP403 before a body. No retry/fallback was performed.
- Explicitly selected CryptoCrew REST endpoint from the Cosmos chain registry: `https://rest.cosmoshub-main.ccvalidators.com`.
- Initial attempt to query the latest block height returned HTTP500. The block endpoint advertised application state one height behind the block. The corrected probe explicitly selects that advertised state height and requires every staking response to acknowledge the same height; it never retries at latest.
- Five bounded reads passed: latest block, one page limited to three validators, then three individual validator records. Chain `cosmoshub-4`; block33667800, application state33667799. No wallet, transaction, account, node installation or genesis download.
- This is server-reported state consistency, not independently verified consensus or Merkle proofs. Historical retention and endpoint availability are not guaranteed.

Next: implement a separate explicit Cosmos validator read profile using a fixed configured address set, exact numeric strings, consistent pinned state, strict schema/identity guards and capability checks. Add negative protocol tests and real native3/2, then HTTP+Mongo5/4/isolation before hosted registration. Do not advertise Cosmos as live yet.

Evidence: `TestResults/CosmosPublicSourceCandidateCurrent/readiness-cryptocrew.json`; SHA256 `cbe557f6e025f00841a211a5f9c9bde1bc740e386675c60c5dad14b9f4212026`. Raw responses are not retained in Docs. Recreate with `python Scripts/Sandbox/Probe-CosmosPublicReadiness.py --endpoint https://rest.cosmoshub-main.ccvalidators.com`; observations change with chain height.

Primary contracts: [staking query proto](https://github.com/cosmos/cosmos-sdk/blob/main/proto/cosmos/staking/v1beta1/query.proto), [Cosmos Hub chain registry](https://github.com/cosmos/chain-registry/blob/master/cosmoshub/chain.json). Runtime admission remains unchanged.
