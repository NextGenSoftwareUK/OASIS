Superseded by [current guide](../../../Devs/NEAR_PUBLIC_METADATA_CQL_GUIDE.md); historical deployment notes, not current readiness.

# NEAR public account CQL

The explicit public NEAR read profile queries configured account IDs. Native CQL 3/2 is verified; published C# walkthrough and actual CLI 3/2 also pass. Original-cohort local HTTP/Mongo 5/4 and isolation also pass. Remaining hosted/browser acceptance is tracked in [current status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md). It is separate from canonical signed storage.

## C# walkthrough

Build the existing sample with the normal bounded provider lease, then run from your checkout using the retained source catalogue:

```powershell
dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-build --no-restore -- --near-public LocalState/COSMIC/HostedNearPublicSourceCurrent/source.json
```

[NearPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/NearPublicWalkthrough.cs) queries three configured accounts, then a two-account subset through the shared [NativeKeySetWalkthrough.cs](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs), CosmicLinq and HolonManager. It prints exact balances and the pinned block height, creating no account, transaction, fixture or Mongo record. The console uses a cancellation deadline of 60 seconds based on the earlier slow endpoint run of 34 seconds; FastNear now completes the native test in about one second. No retries or source substitution.

## Source contract

`NearPublicAccountMetadataSource.FromJson` accepts exactly schema 1, rpcUrl, networkId, accounts and sourceOwner. Mainnet only, 1–8 unique canonical lowercase NEAR account IDs, anonymous root HTTPS with no user info/query/fragment, and a nonempty stable mapping owner. The retained catalogue explicitly uses rpc.mainnet.fastnear.com and near/wrap.near/aurora. Preserve its owner to preserve mapped OASIS IDs; it is not a private key or signing account.

Query metadata key **NearAccountId** supports equality and bounded membership over the configured catalogue. Unknown keys, arbitrary scans/content predicates and unsupported sorts fail. The entire selected source set validates before read visibility/paging; Take(0) does not suppress native failures. Errors/cancellation return no partial rows.

Each selection requires RPC-reported mainnet/synced status. Its first view_account request uses finality:final. Subsequent account requests use that returned block_hash as block_id; every result must acknowledge the same hash and height. No optimistic/latest fallback, contract execution, account/key scan or transaction is performed. Replies are capped at 256 KiB and JSON depth 16; duplicate fields and malformed native quantities fail.

Amount/locked are exact decimal UInt128 strings in yoctoNEAR. Height/storage usage are exact UInt64 JSON integers normalized to decimal metadata strings. Code/block hashes must be canonical base58 encoding of 32 bytes. These checks establish response consistency, not independent chain identity/genesis, consensus or account-state proofs. Account attribution/finality remain assertions of the configured RPC. Native account projections are not holons persisted on-chain.

Metadata: NearAccountId, NearNetworkId, NearBlockHash, NearBlockHeight, NearFinality, NearAmountYocto, NearLockedYocto, NearStorageUsageBytes and NearCodeHash. NativeRecordHolonMapper supplies stable IDs; rows are public active roots with no children. Source mutations are unsupported.

## Pending hosted integration

Optional `CQL_NEAR_PUBLIC_SOURCE_JSON` lazily registers the separate read representation. The profile must still have its own query permission/tariff, activation and capability admission; it must never replace canonical signed storage or become a write/replication default. Hosted configuration, visitor Mongo preparation and browser acceptance remain pending. Local HTTP pairs 5/4 are verified after reusing the original two acknowledged private snapshots without resaving. With metadata encryption enabled, the specific query key needs an explicit trusted query-policy grant; do not add it to globally unencrypted system-reserved metadata.

[Evidence and exact remaining work](../WIP/COSMIC%20ORM/NEAR_PUBLIC_SOURCE_READINESS.md). Primary protocol contracts: [NEAR query types](https://github.com/near/nearcore/blob/master/chain/jsonrpc-primitives/src/types/query.rs), [view_account](https://docs.fastnear.com/rpc/account/view-account).

The older rpc.mainnet.near.org source exceeded WEB4's fixed five-second leaf-query limit and produced HTTP 504. The explicit FastNear selection resolves this observed demo issue; shipping deadlines were not relaxed. The original failed fixture and acknowledgements were preserved, read-only continuation verified every original private copy before exact-owned cleanup, and no writes were replayed.

## Staged hosted configuration

`Publish-HostedNearPublicConfiguration.py` accepts only the verified free FastNear catalogue. It adds the source variable, independent query permission/tariff and four presets; credentials, paid reads, existing roles/examples and missing encryption key grants are rejected. Canonical DNA remains unchanged.

The pending seven-profile rollout uses the already prepared `Publish-HostedGoogleEthereumConfiguration.py with-near` candidate:9values/28new presets through strict source validators. Do not prepare blindly over existing files or apply standalone candidates from one old snapshot. Explicit `apply-with-near` exists for the reviewed matching application/Core0eb3449 rollout; no apply/deployment occurred. Never apply to frozen6bb. Source discovery must pass permissions, prices, activation and capability checks before exposing live examples.
