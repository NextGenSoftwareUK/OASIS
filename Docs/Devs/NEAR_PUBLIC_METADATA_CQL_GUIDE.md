# NEAR public account CQL

The explicit public NEAR read profile queries configured account IDs. Native CQL 3/2 is verified; published C# walkthrough and actual CLI 3/2 also pass. Remaining HTTP/Mongo and hosted/browser acceptance is tracked in [current status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md). It is separate from canonical signed storage.

## C# walkthrough

Build the existing sample with the normal bounded provider lease, then run from your checkout using the retained source catalogue:

```powershell
dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-build --no-restore -- --near-public LocalState/COSMIC/HostedNearPublicSourceCurrent/source.json
```

[NearPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/NearPublicWalkthrough.cs) queries three configured accounts, then a two-account subset through the shared [NativeKeySetWalkthrough.cs](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs), CosmicLinq and HolonManager. It prints exact balances and the pinned block height, creating no account, transaction, fixture or Mongo record. The console uses a cancellation deadline of 60 seconds based on measured native activation/full/subset run of 34 seconds; no retries or source substitution.

## Source contract

`NearPublicAccountMetadataSource.FromJson` accepts exactly schema 1, rpcUrl, networkId, accounts and sourceOwner. Mainnet only, 1–8 unique canonical lowercase NEAR account IDs, anonymous root HTTPS with no user info/query/fragment, and a nonempty stable mapping owner. The retained catalogue uses rpc.mainnet.near.org and near/wrap.near/aurora. Preserve its owner to preserve mapped OASIS IDs; it is not a private key or signing account.

Query metadata key **NearAccountId** supports equality and bounded membership over the configured catalogue. Unknown keys, arbitrary scans/content predicates and unsupported sorts fail. The entire selected source set validates before read visibility/paging; Take(0) does not suppress native failures. Errors/cancellation return no partial rows.

Each selection requires RPC-reported mainnet/synced status. Its first view_account request uses finality:final. Subsequent account requests use that returned block_hash as block_id; every result must acknowledge the same hash and height. No optimistic/latest fallback, contract execution, account/key scan or transaction is performed. Replies are capped at 256 KiB and JSON depth 16; duplicate fields and malformed native quantities fail.

Amount/locked are exact decimal UInt128 strings in yoctoNEAR. Height/storage usage are exact UInt64 JSON integers normalized to decimal metadata strings. Code/block hashes must be canonical base58 encoding of 32 bytes. These checks establish response consistency, not independent chain identity/genesis, consensus or account-state proofs. Account attribution/finality remain assertions of the configured RPC. Native account projections are not holons persisted on-chain.

Metadata: NearAccountId, NearNetworkId, NearBlockHash, NearBlockHeight, NearFinality, NearAmountYocto, NearLockedYocto, NearStorageUsageBytes and NearCodeHash. NativeRecordHolonMapper supplies stable IDs; rows are public active roots with no children. Source mutations are unsupported.

## Pending hosted integration

The read profile must have its own query permission/tariff, activation and capability admission; it must never replace canonical signed storage or become a write/replication default. Hosted configuration, Mongo-paired presets and browser acceptance remain pending. With metadata encryption enabled, the specific query key needs an explicit trusted query-policy grant; do not add it to globally unencrypted system-reserved metadata.

[Evidence and exact remaining work](../WIP/COSMIC%20ORM/NEAR_PUBLIC_SOURCE_READINESS.md). Primary protocol contracts: [NEAR query types](https://github.com/near/nearcore/blob/master/chain/jsonrpc-primitives/src/types/query.rs), [view_account](https://docs.fastnear.com/rpc/account/view-account).
