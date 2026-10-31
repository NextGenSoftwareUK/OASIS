# Cosmos Hub public validator CQL

This explicit read representation queries configured public Cosmos Hub validators. It is separate from the canonical wallet/signed-storage provider. Native CQL and console3/2 are verified locally; local HTTP/Mongo5/4 and tenant isolation also pass. Remaining hosted/browser acceptance is tracked in [current status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md).

## Run the existing C# demo

Use the retained source catalogue; its owner determines stable source holon IDs. Do not regenerate it for each run.

```powershell
dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-build --no-restore -- --cosmos-public LocalState/COSMIC/HostedCosmosPublicSourceCurrent/source.json
```

[CosmosPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/CosmosPublicWalkthrough.cs) reads three validators, then filters to two using the shared [NativeKeySetWalkthrough.cs](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs). `CosmicLinq` calls HolonManager's planner and executor; there is no separate demo query engine. The command performs no source writes, snapshots or local database setup.

## Configuration and semantics

`CosmosPublicValidatorMetadataSource.FromJson` accepts exactly `schema:1`, `restUrl`, `chainId:"cosmoshub-4"`, `addresses` and `sourceOwner`. Use1–8 unique canonical Bech32 `cosmosvaloper` addresses and a nonempty stable owner. Endpoint must be anonymous HTTPS with root path and no query, fragment or user info. The retained catalogue explicitly selects `https://cosmoshub.api.kjnodes.com/`; no implicit failover or retry exists.

The metadata query key is **CosmosValidatorAddress**, supporting equality and bounded membership over the configured catalogue. Unknown keys and unsupported predicates fail. Paging and read visibility apply only after the complete native selection validates; an invalid final record cannot become successful partial output even with Take(0).

For each query, the source verifies the latest block's chain ID and selects the REST response's acknowledged application height (same as the block or exactly one behind). Every individual validator response must acknowledge that same state height and match the selected operator address. Response bodies are capped at256KiB with strict duplicate-field/depth validation. This is server-reported consistency, not a light client, independent settlement proof or Merkle verification.

Metadata includes CosmosChainId, CosmosBlockHash (canonical base64), CosmosBlockNumber, CosmosStateHeight, CosmosValidatorTokens (exact decimal UInt256 string), CosmosValidatorStatus and CosmosValidatorJailed. Source holons are public active roots with no children and stable IDs from the shared native mapper. They represent native validator state, not persisted COSMIC holons on the chain.

## WEB4 enrollment

The optional `CQL_COSMOS_PUBLIC_SOURCE_JSON` configuration registers a lazy query representation. Configuration alone grants no access: independent provider permission, query-only enrollment, query tariff, activation and capability admission are required. The representation must not replace canonical wallet storage or become a write/replication default. Current admission rejects unsigned and signed roles for the same provider in one factory; preserve that fence.

When metadata encryption is enabled, grant the specific query key through the configured query policy; do not add it to the global reserved unencrypted list. Local HTTP tests use two SDK-owned private Mongo snapshots. Hosted configuration, examples and visitor Mongo preparation remain pending their hosted acceptance gates; this guide does not claim Cosmos is live in the public playground.

[Readiness, failures and proving tests](../WIP/COSMIC%20ORM/COSMOS_PUBLIC_SOURCE_READINESS.md). [Cosmos SDK query contract](https://github.com/cosmos/cosmos-sdk/blob/main/proto/cosmos/staking/v1beta1/query.proto); [endpoint registry](https://github.com/cosmos/chain-registry/blob/master/cosmoshub/chain.json).

## Staged publisher and presets

`Publish-HostedCosmosPublicConfiguration.py` strictly validates the verified KJNodes catalogue and adds only the source variable, explicit query permission/tariff and four examples (native3/2 plus Mongo federation). It rejects credentials, paid reads, overlapping existing enrollment and missing encryption query-key grants. It preserves canonical DNA.

For the pending combined rollout, use the already prepared candidate from `Publish-HostedGoogleEthereumConfiguration.py with-cosmos`; it composes all five strict source validators and preserves all20new presets. Do not rerun preparation blindly or apply independent candidates from the same older snapshot. Explicit `apply-with-cosmos` exists for the reviewed matching application/Core rollout; no apply/deployment has occurred. It must never be applied to the older frozen6bb correctness batch. Public browser discovery will expose examples only after permission, pricing, activation and capability checks succeed.
