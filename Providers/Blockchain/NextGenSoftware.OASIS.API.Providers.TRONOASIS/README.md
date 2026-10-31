# NextGen Software WEB4 OASIS API TRON OASIS Provider

WEB4 OASIS API TRON OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integtate into the OASIS.

## Native NFT identity and mint contract

TRON NFT descriptors expose `NFTStandardType.TRC721` (serialized numeric value `6`) and `OnChainProvider.TRONOASIS` through one shared descriptor factory. Existing standard values `0`–`5` are unchanged. This identifies the adapter's expected TRC721 ABI; it is not a complete contract-conformance audit or a claim that every indexed asset is discoverable.

For native minting, omit `NFTStandardType` or explicitly select `TRC721`; explicit Ethereum/Solana/Sui/Hedera/CosmWasm standards are rejected before wallet lookup. `OnChainProvider` may be omitted, `Default`, or `TRONOASIS`; contradictory explicit chains are rejected. Older clients that explicitly sent `ERC721` must now select `TRC721` or omit the optional setting. Update clients sharing the Core enum before consuming the new string value.

The existing explicit `mint(address,uint256,string)` collection extension still requires one token, a minter avatar, a metadata URI, and `MetaData` entries `MintFunctionSignature`, `TokenAddress`, and decimal `TokenId`. This change does not implement royalties, metadata freezing, cross-collection completeness, off-chain metadata fetching, or coordinator authorization. No implicit mint ABI or NFT identity is invented.

Mint recipients may be an explicit address or an avatar ID, username, email, or a consistent combination. Existing WalletManager lookups resolve only supplied selectors. All must resolve to the same exact canonical TRON wallet address; conflicting selectors, failed lookups and missing wallets stop before native signing, never silently mint to the minter. With no recipient selector, the existing minter-wallet default is retained. The shared `NftMintRecipient` coordinator is also used by Sui, which supplies its own address-equivalence comparison. Selection does not grant mint authority, fetch credentials, or authorize fees. Recipient lookups can incur storage-provider reads; hosted admission/accounting remains a separate verification gate.

[Official TRC721 specification](https://developers.tron.network/docs/trc-721) describes TRON's ERC721-compatible interface and TVM-specific behavior. Verification scope and unresolved native transaction/fee/release gates are recorded in `Docs/WIP/HyperDrive-Providers/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md` in the parent repository. Unit/gRPC fixture success is not full native or hosted provider certification.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version): \
https://api.oasisweb4.one

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!
