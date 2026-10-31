# Base, Avalanche and BNB Chain public balance CQL

These explicit read representations reuse the common EVM reader and HolonManager/LINQ pipeline. Native CQL 3/2 is verified for each. Published C# walkthroughs and actual CLI3/2 also pass for all three. Remaining HTTP/Mongo, hosted and browser acceptance is tracked in [current status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md); no full ORM or signed-storage parity is implied.

## Readable C# walkthroughs

Build the existing console under the bounded provider lease, then select an existing retained catalogue:

```powershell
dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-build --no-restore -- --base-public LocalState/COSMIC/HostedBasePublicSourceCurrent/source.json

dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-build --no-restore -- --avalanche-public LocalState/COSMIC/HostedAvalanchePublicSourceCurrent/source.json

dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-build --no-restore -- --bnb-public LocalState/COSMIC/HostedBnbPublicSourceCurrent/source.json
```

The spaced/commented [Base](../../Examples/CosmicCqlDemo/BasePublicWalkthrough.cs), [Avalanche](../../Examples/CosmicCqlDemo/AvalanchePublicWalkthrough.cs) and [BNB Chain](../../Examples/CosmicCqlDemo/BnbPublicWalkthrough.cs) walkthroughs select three configured addresses, then a two-address subset. [NativeKeySetWalkthrough](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs) contains the shared Contains→bounded metadata membership→CosmicLinq→HolonManager query. No account, wallet, transaction or Mongo fixture is created. Preserve existing mapping owners; regenerating them changes source holon IDs.

## Explicit native profiles

| Provider | Class | Metadata query key | Native network/path |
|---|---|---|---|
| BaseOASIS | BasePublicAccountMetadataSource | BaseAccountAddress | 0x2105, root RPC path |
| AvalancheOASIS | AvalanchePublicAccountMetadataSource | AvalancheAccountAddress | 0xa86a, /ext/bc/C/rpc |
| BNBChainOASIS | BnbPublicAccountMetadataSource | BNBChainAccountAddress | 0x38, root RPC path |

Each accepts the existing exact anonymous schema: schema1/rpcUrl/chainId/addresses/sourceOwner. One to eight unique lowercase20byte addresses, stable nonempty source owner, credential-free HTTPS, fixed descriptor chain/genesis/path. No source mutation, secret fields, wallet defaults or endpoint fallback. Canonical signed-provider DNA is separate.

Query capabilities are equality/membership over the configured address catalogue, bounded paging and read visibility. Arbitrary content/name/description scans, ordering or unsupported operators fail at planning. Every selected native response validates before visibility/paging, including empty pages; no partial rows escape.

Per query, the reader checks chain ID and pinned genesis, selects a reported finalized block and reads all balances using that exact blockHash with requireCanonical:true. Unsupported hash-pinned semantics fail rather than downgrading to latest or numeric-height reads. Quantities remain exact decimal UInt256 strings; metadata includes the network-specific AccountAddress/ChainId/BlockHash/BlockNumber/Commitment/BalanceWei fields. Amounts are in the smallest units of each chain's native currency (ETH, AVAX or BNB), not decimal coin amounts; the shared suffix does not promise a shared currency or conversion.

These are RPC-reported canonical/finality assertions, not independent consensus, L1 settlement or rollup proof. They expose native account state, not evidence of COSMIC holons persisted on-chain. No contract call, transaction body or hidden storage scan is required.

## Remaining integration

Separate read permission/tariff, activation and capability admission are required before hosted enrollment. These profiles grant no signed saves or replication authority. With metadata encryption enabled, allow the specific application query key through trusted policy; do not add it to the global reserved unencrypted metadata list. C# console and native acceptance do not prove HTTP, visitor preparation, replicas or browser behavior.

[Evidence and next steps](../WIP/COSMIC%20ORM/EVM_NEXT_THREE_SOURCE_STATUS.md).
