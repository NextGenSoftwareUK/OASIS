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

## Core storage (OasisKeyValueStore component)

RadixOASIS stores avatars, avatar details and holons **on the Radix ledger** through the shared
`KeyValueStorageProviderBase` (the same record, index, version and deletion semantics as every other key-value
provider). The backend is one `OasisKeyValueStore` Scrypto component (`contracts/src/oasis_key_value_store.rs`,
a `KeyValueStore<String, String>`):

- **Writes** are notarised V1 transactions calling `put` / `delete`, signed by `PrivateKey` (secp256k1). The component
  is owned by that key's signature badge, so the signature alone authorises a write, and the key's account pays the fee.
  A write is acknowledged only when the node's Core API reports the intent `CommittedSuccess`; `CommittedFailure`,
  `PermanentRejection`, a rejected submit or an unresolved status within the timeout are errors (`RadixTransactionException`).
- **Reads** come from committed state through the node's Engine State API (`kv-store/entry`, `kv-store/iterator`).
- **Privacy**: ledger state is public, so every value is compressed and encrypted (`PrivateKeyValueBackend`,
  32-byte `StorageEncryptionKey`) before it is written. Keys are OASIS record keys (kinds and GUIDs, or encoded
  username/email index entries).

### Configuration

```csharp
var provider = new RadixOASIS(new RadixOASISConfig
{
    NetworkId = 240,                        // 1 mainnet, 2 stokenet, 240 localnet
    NetworkName = "localnet",               // logical network name the node expects
    CoreApiUri = "http://127.0.0.1:3333/core/",
    EngineStateApiUri = "http://127.0.0.1:3336/engine-state/",
    PrivateKey = "<32-byte secp256k1 key, hex>",
    KeyValueStoreComponentAddress = "component_loc1...",
    StorageEncryptionKey = "<32-byte key, hex>"
});
```

All six storage settings go together; a partial set is rejected at construction. The bridge-only constructor
`RadixOASIS(hostUri, networkId, accountAddress, privateKey)` leaves storage unconfigured: storage calls and activation
fail with an explicit configuration error, while `RadixBridgeService`, `OracleNode` and `ChainObserver` still work.
The node needs its local transaction execution index (on by default) for transaction status.

### Deploy

Build the package with the reproducible builder (`docker run --rm -v "$PWD/contracts:/src" radixdlt/scrypto-builder:v1.3.1`),
then with `RadixLedgerClient`: `PublishPackageAsync(wasm, rpd)` and `InstantiateKeyValueStoreAsync(package)` from the
key RadixOASIS will sign with. On a test network `FundFromFaucetAsync()` funds that key's account.

### Tests

- `RadixNativeStorageConformanceTests` (integration tests, `-p:NativeStorageConformance=true`): the shared 26-check
  core storage suite in both sync and async modes against a real local Babylon network started by
  `fixture/start-radix-fixture.sh` (babylon-node v1.4.0.0, single validator, fresh genesis with faucet); each mode uses
  its own funded key, package and component. `RadixNativeStoragePersistenceTests` checks a record survives a node restart.
  CI: the `Radix` entry of `.github/workflows/popular-providers-native-storage.yml`.
- `RadixKeyValueStorageConfigurationTests` (unit tests): configuration contract without a network.
- TestHarness: `dotnet run -- localnet-storage` against the fixture's `OASIS_RADIX_*` variables.

The older `OasisStorage` blueprint (`contracts/src/oasis_storage.rs`) is still in the package but RadixOASIS no
longer uses it.

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

