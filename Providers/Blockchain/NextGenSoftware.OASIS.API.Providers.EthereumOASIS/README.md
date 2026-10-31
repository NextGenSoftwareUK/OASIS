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
