# NextGen Software WEB4 OASIS API Arbitrum OASIS Provider

## Verification and privacy gate

The existing `ArbitrumOASIS_Web3Core` wrapper now has actual local-EVM coverage in the retained shared runner (`Scripts/TestHosts/Run-ReusableEthereumTests.ps1`): private avatar/detail round trips and inspection of native payload bytes, plus the same V2/SQLite policy fixture used for Polygon. The fixture now uses the supplied provider's real identity rather than hardcoded Polygon metrics/selection IDs. Web3 native scenarios: 4/4 passed, zero skips (2026-10-16), on loopback chain 31337 with an explicitly deployed compatible Web3Core contract. This is not an Arbitrum One/Nova/Sepolia deployment or rollup-finality test, nor certification of all NFT/interfaces. No ABI fallback or default-provider switch was added.

`ArbitrumOASIS` is the chain-specific legacy implementation. Its current save paths serialize avatar/detail/holon JSON directly before contract submission; authenticated private-record encryption and privacy-safe searchable indexes are not established for this path. Do not enable it for private records until that implementation gap is fixed and tested. This is a source audit warning, not a runtime enforcement switch or a claim that existing publications were removed.

`ArbitrumOASIS_Web3Core` is a separate implementation requiring an explicitly configured compatible Web3Core contract. Its inherited functionality does not certify the legacy ABI, the default provider path, or any hosted Arbitrum deployment. Do not silently switch ABI/contract implementations after a failure.

Legacy unit fixtures now use loopback instead of public mainnet RPC. Identity-only test success is not native storage/policy/recovery proof. See `Docs/WIP/HyperDrive-Providers/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md` in the parent repository for the current evidence and remaining gates.


## Public Arbitrum One balance CQL profile

ArbitrumPublicAccountMetadataSource is an explicit anonymous read representation,
separate from signed ArbitrumOASIS storage. It uses the common Web3Core EVM reader
with a fixed Arbitrum descriptor: chain 0xa4b1, genesis
0x7ee576b35482195fc49205cec9af72ce14f003b9ae69f6ba0faef4514be8b442,
and the official https://arb1.arbitrum.io/rpc endpoint's fixed /rpc path.
No wallet, funding, transaction, contract execution or local node is required.

The query supports exact equality or bounded membership on ArbitrumAccountAddress
for one to eight configured lowercase 20-byte addresses. Every query checks chain
and genesis, resolves one RPC-reported finalized block and reads all selected
balances using its canonical block hash. There is no endpoint switching, retry
policy or fallback to latest. Source errors fail the complete selected query.

Metadata includes ArbitrumChainId, ArbitrumBlockHash, ArbitrumBlockNumber,
ArbitrumCommitment and ArbitrumBalanceWei. Native balance/block quantities remain
exact decimal strings. Public address-state projections are not persisted COSMIC
holons or independent verification of rollup settlement/L1 finality.

```csharp
// Reuse a stable nonsecret catalogue: schema, rpcUrl, chainId, addresses, sourceOwner.
using var source = ArbitrumPublicAccountMetadataSource.FromJson(configurationJson);

var activated = await source.ActivateQuerySourceAsync(cancellationToken);
if (activated.IsError)
    throw new InvalidOperationException(activated.Message, activated.Exception);

// Select only configured addresses; composition does not perform a chain scan.
var query = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.ArbitrumOASIS).Result)
    .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(avatarId).Result)
    .Where(row => selectedAddresses.Contains(
        CosmicQueryMetadata.Value<string>(row, "ArbitrumAccountAddress")));

if (query.IsError)
    throw new InvalidOperationException(query.Message);

var plan = CosmicQueryPlanner.Plan(
    query.Result,
    ProviderType.ArbitrumOASIS,
    new[] { new CosmicQueryTarget(ProviderType.ArbitrumOASIS, source.QueryCapabilities) });

if (plan.IsError)
    throw new InvalidOperationException(plan.Message);

var rows = await source.ExecuteCosmicQueryAsync(
    plan.Result, cancellationToken: cancellationToken);

if (rows.IsError)
    throw new InvalidOperationException(rows.Message, rows.Exception);
```

Configuration is bounded to 8 KiB/depth four; RPC responses to 256 KiB/depth 16.
Unknown/duplicate fields, signer settings, userinfo, query strings/fragments,
authorization headers and nonmatching RPC paths are rejected. Keep sourceOwner
stable when reusing a catalogue. Ethereum/Polygon retain their fixed root-path
requirements; the shared path setting does not accept arbitrary credential paths.

The initial PublicNode probe could read chain/genesis/finalized headers but lacked
the finalized account state. The official endpoint supplies that state and is the
explicit profile choice, not a runtime fallback. Native CQL 3/2 passes. EVM-family
controls/native regressions also pass; tests reuse EthereumOASIS.CqlTests and the
existing SDK/cache without installing a node.

WEB4 query-only permission/pricing, HTTP discovery, Mongo pairing/isolation,
demo presets and hosted/browser acceptance remain next. General content querying,
owned chain writes, full ORM parity and replication are not certified by these
public reads. [Arbitrum chain information](https://docs.arbitrum.io/chain-info),
[protocol readiness probe](../../../Scripts/Sandbox/Test-ArbitrumPublicMetadataCandidate.ps1).
