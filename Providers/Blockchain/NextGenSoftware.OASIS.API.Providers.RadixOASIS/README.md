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

