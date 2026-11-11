# RoninOASIS

Ronin — EVM-compatible gaming blockchain by Sky Mavis (Axie Infinity), Chain ID 2020.

Part of the [OASIS Omniverse](https://oasisomniverse.one) HOT-swappable provider architecture.

## Wallet and storage configuration

The RPC-only constructor supports public native RON balance reads on chain ID 2020. Signed storage uses the Web3Core adapter with a compatible deployed contract.

Configure `OASIS.StorageProviders.RoninOASIS` for BootLoader with these fields.

| Field | Value |
| --- | --- |
| `RpcEndpoint` | Ronin mainnet HTTPS RPC or the dedicated Saigon HTTPS RPC |
| `ChainId` | `2020` for mainnet or `202601` for Saigon |
| `ContractAddress` | Web3Core storage contract deployed on that chain |
| `ChainPrivateKey` | Funded signer provided through protected runtime configuration |

The Saigon conformance suite requires a dedicated contract and testnet signer. Public wallet integration and signed storage acceptance are tracked separately. [Ronin network configuration](https://docs.roninchain.com/developers/smart-contracts/deploy)
