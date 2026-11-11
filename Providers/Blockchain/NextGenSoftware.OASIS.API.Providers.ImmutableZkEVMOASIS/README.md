# ImmutableZkEVMOASIS

Immutable zkEVM — EVM-compatible gaming L2 (Chain ID 13371) with native NFT support.

Part of the [OASIS Omniverse](https://oasisomniverse.one) HOT-swappable provider architecture.

## Wallet and storage configuration

The RPC-only constructor supports native IMX balance reads on mainnet. Signed storage uses a compatible deployed Web3Core contract.

Configure `OASIS.StorageProviders.ImmutableZkEVMOASIS` for BootLoader with these fields.

| Field | Value |
| --- | --- |
| `RpcEndpoint` | `https://rpc.immutable.com` for mainnet or `https://rpc.testnet.immutable.com` for testnet |
| `ChainId` | `13371` for mainnet or `13473` for testnet |
| `ContractAddress` | Web3Core storage contract deployed on that chain |
| `ChainPrivateKey` | Funded signer supplied through protected runtime configuration |

The signed conformance suite requires a dedicated testnet contract with zero avatar, avatar detail and holon records. It verifies those counts before writes and after cleanup. [Immutable testnet configuration](https://docs.immutable.com/build/unreal/operational-guides/launch-new-collection)
