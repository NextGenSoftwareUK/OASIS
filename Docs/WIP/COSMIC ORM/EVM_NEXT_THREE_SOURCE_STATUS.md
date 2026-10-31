# Base / Avalanche / BNB Chain public CQL milestone

31 October 2026. Three native read adapters verified; C# console, HTTP/Mongo, hosted configuration and browser acceptance remain open. Work is limited to new demo scopes; existing green EVM code is unchanged and legacy classes were not edited.

| Provider | Native endpoint | Chain ID | Exact RPC path | Proof |
|---|---|---|---|---|
| BaseOASIS | https://mainnet.base.org/ |0x2105| / |18controls + native3/2 |
| AvalancheOASIS | https://api.avax.network/ext/bc/C/rpc |0xa86a| /ext/bc/C/rpc |18controls + native3/2 |
| BNBChainOASIS | https://bsc-dataseed.bnbchain.org/ |0x38| / |18controls + native3/2 |

All three use thin immutable descriptors over the existing unchanged EvmPublicAccountMetadataSource: fixed provider/chain/genesis/metadata/path, explicit anonymous config, one reported finalized canonical hash per selection, exact UInt256 balance strings, complete validation before visibility/paging, no source writes/signing/scans/implicit fallback or latest downgrade. Base/Avalanche genesis pins were observed at the official endpoint; BNB pin also matches the canonical BSC source. These checks do not independently verify consensus or L2 settlement.

Source classes: BasePublicAccountMetadataSource, AvalanchePublicAccountMetadataSource and BnbPublicAccountMetadataSource in their existing provider projects. Metadata keys: BaseAccountAddress, AvalancheAccountAddress and BNBChainAccountAddress. Native balances are source base units (BaseETH, AVAX, BNB respectively), not a shared currency or units-conversion promise. Public address-state projections are not stored COSMIC holons on-chain.

Controlled54tests/0skips cover point/membership/paging/budget, chain/genesis/hash/number/envelope failures, canonical block binding, malformed final records/Take(0), oversized/duplicate replies, cancellation/inactive/unknown keys, unsupported content scans/writes and forbidden credential configuration. Three actual CQL native tests pass/0skips, each returning3accounts and a2account subset with preserved native keys and one block hash per selection. Existing Ethereum CQL test host reused; only the three new classes selected. No green source tests rerun, shared implementation changed or new test environment created.

Persistent catalogues use the same existing schema and3public addresses, each with its own stable mapping owner under LocalState/COSMIC/HostedBasePublicSourceCurrent, HostedAvalanchePublicSourceCurrent and HostedBnbPublicSourceCurrent. Preserve these owners for stable mapped IDs. No wallet/node/account/contract/database installed or created, no gas or source writes. Existing SDK/cache/project references reused.

Next: readable C# walkthroughs via shared HolonManager/LINQ, native verbatim samples/actualCLI, then one HTTP+Mongo5/4/isolation gate per source with original operation journals and exact-owned cleanup. Batch the three catalogue/admission changes; strict source publishers/joint staging and browser links follow. Do not advertise them as hosted before those gates.

Reproduce protocol qualification with `Scripts/Sandbox/Test-EvmBreadthReadiness.ps1 -Network Base` (or Avalanche/BNBChain). Native CQL tests use the existing EthereumOASIS.CqlTests project and the matching explicit source env variables; do not replay other native fixtures.

Primary endpoints/protocol: [Base JSON-RPC endpoint (probed)](https://mainnet.base.org/), [Avalanche primary network](https://build.avax.network/docs/primary-network), [BNB official RPC endpoints](https://docs.bnbchain.org/bnb-smart-chain/developers/json_rpc/json-rpc-endpoint/), [canonical BSC genesis](https://github.com/bnb-chain/bsc/blob/master/params/config.go).

- `TestResults/EvmBreadthReadinessCurrent/Base.json`: 2548 B; SHA256 `604cf9584b69d15533225380f5f88f80b1a5ea35167c516c1c171b49c8693c44`.

- `TestResults/EvmBreadthReadinessCurrent/Avalanche.json`: 2557 B; SHA256 `0c17f3d1c366af5d2d68aedc35b00798e038338389b7e81f3eca1c5aa47facda`.

- `TestResults/EvmBreadthReadinessCurrent/BNBChain.json`: 2569 B; SHA256 `974da1224c7fdcfc8ba72863b92ef369d3d92d4c134d15f3ef151611f41881cb`.

- `TestResults/EvmBreadthReadinessCurrent/provider-controls.trx`: 84036 B; SHA256 `ee48ecf157e134a86916c4e74cd226d66108bf8ad81975216ee7ef9f90555eac`.

- `TestResults/EvmBreadthReadinessCurrent/provider-native.trx`: 6129 B; SHA256 `3d735cab83e9df279b1893b3e7996289d59ade88d6722327ece8d804d0131bb2`.
