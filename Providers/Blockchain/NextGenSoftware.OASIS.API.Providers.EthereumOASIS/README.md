# NextGen Software WEB4 OASIS API Ethereum OASIS Provider

WEB4 OASIS API Ethereum OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integtate into the OASIS.

## NFT mint contract

Minting targets the explicit OASIS `mint(address,string)` extension implemented by the maintained `Web3CoreOASIS.sol`, not an assumed generic ERC-721 mint function. `CollectionPublicKey` selects that collection; omission uses the configured contract, which must implement the same extension. The storage-only OASIS contracts do not provide it. There is no ABI-guessing fallback to `mint(address,uint256)`.

The contract allocates the native token ID. The adapter returns the full uint256 ID from the verified zero-to-recipient ERC-721 Transfer event, never a clock-derived ID. Existing authorized-wallet selection and native gas estimation apply; estimation is not proof of contract capability or successful minting. The configured OASIS extension restricts minting to its contract owner, so the authorized signing wallet must also satisfy that on-chain permission.

The metadata string is a JSON envelope containing `JSONMetaDataURL` and `JSONMetaData`. NFTManager prepares the metadata and URI. Inline JSON is included only when `StoreNFTMetaDataOnChain` is explicitly true; false or omitted keeps that field null. The URI is public. Administrative request fields, avatar/email identifiers and routing/signing controls are not serialized into this envelope. The metadata, URI, memo and recipient are captured before credential lookup/awaits. NFTManager remains the owner of batch minting and metadata preparation; this adapter does not add another batch loop.

Controlled official-SDK tests cover calldata/signing, receipt-assigned zero/max IDs and existing uncertain/reverted outcomes. These are not live-chain deployment, finality, gas-cost, durable-outbox or complete HyperDrive V2 certification. An uncertain submission must be reconciled before replay. Current evidence and open gates are maintained in `Docs/WIP/HyperDrive-Providers/EVM_RECORD_HISTORY_GAPS.md`.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version): \
https://api.oasisweb4.one

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!


## Explicit public Ethereum balance CQL profile

`EthereumPublicAccountMetadataSource` is a separate read-only adapter over configured
public mainnet addresses. It does not construct a wallet, sign, execute contracts,
scan accounts, download transaction bodies or write chain state. Canonical
EthereumOASIS wallet/owned-record storage remains separate.

The native query contract is exact equality or bounded membership on metadata
`EthereumAccountAddress`: one to eight configured lowercase0x20-byte addresses.
Unsupported name/description/content filters fail planning. A query verifies
chain ID0x1and Ethereum genesis, resolves one RPC-reported finalized block, then
reads every selected balance using its hash with requireCanonical:true. There is
no latest-block or other-endpoint fallback. Source errors fail the complete query,
including when paging would discard the faulty row. Public address-state results
are not proof that a COSMIC holon was persisted on Ethereum.

Metadata includes EthereumChainId, EthereumBlockHash, EthereumBlockNumber,
EthereumCommitment and EthereumBalanceWei. UInt256balances and block numbers are
exact decimal strings. Source IDs use NativeRecordHolonMapper with stable owner,
provider, chain-scoped source name and canonical address. All mapped rows are
public active roots with no children.

```csharp
// Reuse an explicit existing source catalogue; never pass wallet credentials.
using var source = EthereumPublicAccountMetadataSource.FromJson(configurationJson);

var activation = await source.ActivateQuerySourceAsync(cancellationToken);
if (activation.IsError)
    throw new InvalidOperationException(activation.Message, activation.Exception);

// Capture only selected catalogue addresses. Query composition performs no I/O.
var query = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.EthereumOASIS).Result)
    .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(avatarId).Result)
    .Where(row => selectedAddresses.Contains(
        CosmicQueryMetadata.Value<string>(row, "EthereumAccountAddress")));

if (query.IsError)
    throw new InvalidOperationException(query.Message);

var plan = CosmicQueryPlanner.Plan(
    query.Result,
    ProviderType.EthereumOASIS,
    new[] { new CosmicQueryTarget(ProviderType.EthereumOASIS, source.QueryCapabilities) });

if (plan.IsError)
    throw new InvalidOperationException(plan.Message);

var result = await source.ExecuteCosmicQueryAsync(
    plan.Result,
    cancellationToken: cancellationToken);

if (result.IsError)
    throw new InvalidOperationException(result.Message, result.Exception);
```

Configuration accepts exactly schema1, rpcUrl, chainId0x1, addresses and sourceOwner.
The owner is a stable nonempty mapping UUID. Root-path HTTPS RPC is required;
userinfo, query strings, fragments, credential path segments, authorization headers,
unknown/duplicate fields, signer settings and malformed catalogues are rejected.
Configuration is bounded8KiB/depth4; each RPC response256KiB/depth16. Host construction
uses a guarded anonymous HttpClient.

Scoped tests live in `TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests`.
18controlled cases and1actual mainnet CQL3/2case pass. The older UnitTests target
still has16compile errors for removed APIs; it was not disabled or certified.
For explicit native acceptance set OASIS_ETHEREUM_PUBLIC_SOURCE to the retained
configuration path and select TestCategory=EthereumPublicAccountsNative. Logs/TRX
belong in the worktree TestResults; preserve stable catalogue ownership in LocalState.

This profile is not enrolled in hosted WEB4 yet. Query-only permissions/pricing,
HTTP discovery/presets, private Mongo snapshots/federation/isolation, replicas and
browser acceptance remain separate gates. General query parity and chain writes
are not certified by public balance reads. Protocol: [Ethereum JSON-RPC](https://ethereum.org/developers/docs/apis/json-rpc/)
and [EIP-1898](https://eips.ethereum.org/EIPS/eip-1898).

WEB4 integration (task branch): CQL_ETHEREUM_PUBLIC_SOURCE_JSON registers this
adapter in the isolated CqlReadSourceProfile registry. Explicit query-only
permission and a query tariff are required independently; ordinary Ethereum
wallet/write admission remains separate. The leaf/federation compiler admits
bounded unordered address lookups, not the richer composition/query-parity set.
Four server examples can describe native3/2and Mongo-paired5/4selections; missing
Mongo disables only paired examples. Profile/admission/discovery/schema controls
pass, but native HTTP pairing and hosted/browser enrollment are still pending.
