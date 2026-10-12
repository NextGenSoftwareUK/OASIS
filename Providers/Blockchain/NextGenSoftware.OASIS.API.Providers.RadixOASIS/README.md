# NextGenSoftware.OASIS.API.Providers.RadixOASIS

## Overview

The **RadixOASIS Provider** is a WEB4 OASIS API provider that enables seamless integration with the Radix blockchain network. This provider supports:

- ✅ Native Radix blockchain operations
- ✅ Cross-chain bridge capabilities (SOL ↔ XRD)
- ✅ Account creation and management
- ✅ Balance checking and transactions
- ✅ Smart contract interactions
- ✅ XRD token operations

## Features

### Blockchain Integration
- Full Radix DLT support via Radix Engine Toolkit
- MainNet and StokNet (testnet) support
- Native XRD token handling
- Transaction manifest creation and execution

### Bridge Operations
- Cross-chain asset transfers
- Exchange rate integration
- Atomic swap capabilities
- Transaction rollback support

### Account Management
- Create new Radix accounts with seed phrases
- Restore accounts from seed phrases
- Secure key management
- Balance queries

## Installation

Add the package reference to your project:

```xml
<PackageReference Include="NextGenSoftware.OASIS.API.Providers.RadixOASIS" Version="1.0.0" />
```

## Usage

### Initializing the Provider

```csharp
var radixProvider = new RadixOASIS(
    hostUri: "https://stokenet.radixdlt.com",
    networkId: 2, // StokNet
    accountAddress: "your_account_address",
    privateKey: "your_private_key"
);

await radixProvider.ActivateProviderAsync();
```

### Creating an Account

```csharp
var result = await radixProvider.CreateAccountAsync();
if (!result.IsError)
{
    var (publicKey, privateKey, seedPhrase) = result.Result;
    Console.WriteLine($"New Account Created: {publicKey}");
}
```

### Checking Balance

```csharp
var balance = await radixProvider.GetAccountBalanceAsync("account_address");
if (!balance.IsError)
{
    Console.WriteLine($"Balance: {balance.Result} XRD");
}
```

### Making a Transfer

```csharp
var transferResult = await radixProvider.DepositAsync(
    amount: 10.0m,
    receiverAccountAddress: "receiver_address"
);
```

## Bridge Operations

The RadixOASIS provider integrates with the OASIS Bridge Manager for cross-chain operations:

```csharp
var bridgeManager = new CrossChainBridgeManager(solanaProvider, radixProvider);

var orderRequest = new CreateBridgeOrderRequest
{
    FromToken = "SOL",
    ToToken = "XRD",
    Amount = 1.5m,
    DestinationAddress = "radix_address",
    UserId = userId
};

var order = await bridgeManager.CreateBridgeOrderAsync(orderRequest);
```

## On-ledger storage (OasisStorage component)

By default RadixOASIS delegates avatar and holon storage to the other OASIS providers. Set `OasisBlueprintAddress` and
avatars, avatar details and holons are stored **on the Radix ledger** instead, as JSON in the `OasisStorage` Scrypto
component (`contracts/`).

### What it stores

| Operation | Component method |
|---|---|
| `SaveAvatarAsync` / `LoadAvatarAsync` / `LoadAvatarByUsernameAsync` / `LoadAvatarByEmailAsync` / `DeleteAvatarAsync` | `save_avatar`, `get_avatar*`, `delete_avatar` |
| `SaveAvatarDetailAsync` / `LoadAvatarDetailAsync` | `save_avatar_detail`, `get_avatar_detail` |
| `SaveHolonAsync` / `LoadHolonAsync` (id or provider key) / `DeleteHolonAsync` | `save_holon`, `get_holon*`, `delete_holon` |

Entities are keyed by their OASIS id (GUID string); the Radix provider key of a stored entity is that id. Saves create
or replace and keep the username/email/provider-key indexes in step. Soft deletes mark the avatar deleted; hard
deletes remove it and its indexes. Operations that need enumeration (load-all, search, export, children by parent)
still use the delegated path.

Reads go through the Gateway (`/state/entity/details` for the component's key-value store addresses, then
`/state/key-value-store/data`). Writes are V1 transactions that lock the fee from `AccountAddress`, prove the owner
badge and call the component; the provider waits until they commit.

### Security

Reads are public. Every write requires a proof of the component's **owner badge**, minted once by `instantiate`, so
only the account holding it (the provider's `AccountAddress`) can change data.

### Deploy

1. Build: `scrypto build` in `contracts/` (Scrypto 1.4, current stable Rust; the wasm32 C deps need `clang`).
   This produces `oasis_storage.wasm` and `oasis_storage.rpd`.
2. Publish the package and call `OasisStorage::instantiate` from the account RadixOASIS will use. The badge is deposited
   into that account.
3. Configure the provider:

```json
"RadixOASIS": {
  "HostUri": "https://stokenet.radixdlt.com",
  "NetworkId": 2,
  "AccountAddress": "account_tdx_2_...",
  "PrivateKey": "<ed25519 hex>",
  "OasisBlueprintAddress": "component_tdx_2_...",
  "OasisStorageOwnerBadgeAddress": "resource_tdx_2_..."
}
```

`StokenetOnLedgerStorageTests` (integration tests) does all of this programmatically on Stokenet: faucet-funded
throwaway account, publish, instantiate, then save/load/delete through the provider. Run it with
`RADIX_STOKENET_LIVE=1` and `RADIX_OASIS_STORAGE_PACKAGE_DIR` pointing at the built `.wasm`/`.rpd`.

### Tests

- `contracts/tests/oasis_storage_tests.rs`: ledger-simulator tests (`cargo test --release` in `contracts/`).
- `RadixOasisStorageClientTests` (unit tests): Gateway parsing and lookups against a fake Gateway.
- `StokenetOnLedgerStorageTests` (integration tests): live end-to-end on Stokenet (opt-in, see above).

## Configuration

### Network Types
- **MainNet** (NetworkId: 1): Production Radix network
- **StokNet** (NetworkId: 2): Testnet for development

### Environment Variables
```env
RADIX_HOST_URI=https://stokenet.radixdlt.com
RADIX_NETWORK_ID=2
RADIX_ACCOUNT_ADDRESS=account_tdx_2_...
RADIX_PRIVATE_KEY=...
```

## Architecture

The provider follows the OASIS architecture pattern:

```
RadixOASIS (Provider)
├── Infrastructure
│   ├── Services
│   │   └── RadixService (Core Radix operations)
│   ├── Repositories
│   │   └── RadixRepository (Data access)
│   └── Entities
│       └── DTOs (Data transfer objects)
├── Extensions
│   └── Mapping extensions
└── Contracts
    └── IOASISBridge implementation
```

## Dependencies

- **RadixEngineToolkit**: Core Radix SDK
- **RadixDlt.CoreApiSdk**: Radix Core API
- **RadixDlt.NetworkGateway.GatewayApiSdk**: Gateway API
- **NextGenSoftware.OASIS.API.Core**: OASIS Core API

## Cross-Chain Support

This provider works seamlessly with:
- **SolanaOASIS**: For SOL ↔ XRD bridges
- **EthereumOASIS**: For ETH ↔ XRD bridges (future)
- **Other OASIS Providers**: Via the unified bridge interface

## License

MIT License - Copyright © NextGen Software Ltd 2025

## Links

- [OASIS API Repository](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK)
- [Radix DLT](https://www.radixdlt.com/)
- [Radix Engine Toolkit](https://github.com/radixdlt/radix-engine-toolkit)
- [OASIS Documentation](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/tree/master/Docs)

## Support

For issues and questions:
- GitHub Issues: [OASIS API Issues](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/issues)
- Email: support@nextgensoftware.co.uk

