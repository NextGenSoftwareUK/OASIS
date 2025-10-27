# BaseOASIS Provider

This provider integrates Base blockchain functionality into the OASIS API platform.

## Overview

BaseOASIS provides blockchain storage and NFT capabilities for the Base network, which is Coinbase's Layer 2 solution built on Optimism's OP Stack.

## Features

- **Avatar Management**: Store and retrieve avatar data on Base blockchain
- **NFT Operations**: Mint, transfer, and manage NFTs on Base
- **Transaction Support**: Send transactions and interact with smart contracts
- **Smart Contract Integration**: Deploy and interact with OASIS smart contracts

## Network Configuration

### Mainnet
- **Chain ID**: 8453
- **RPC URL**: `https://mainnet.base.org`
- **Explorer**: https://basescan.org

### Testnet (Sepolia)
- **Chain ID**: 84532
- **RPC URL**: `https://sepolia.base.org`
- **Explorer**: https://sepolia.basescan.org

## Setup

1. **Deploy Smart Contract**: Deploy the OASIS smart contract to Base network
2. **Configure Provider**: Update the DNA.json with your contract address and private key
3. **Initialize**: Use the provider in your OASIS application

## Configuration Example

```json
{
  "ProviderName": "BaseOASIS",
  "ProviderDescription": "Base Blockchain Provider",
  "ProviderType": "BaseOASIS",
  "ProviderCategory": "StorageAndNetwork",
  "IsEnabled": true,
  "Priority": 1,
  "CustomParams": "hostUri=https://mainnet.base.org;chainPrivateKey=your-private-key;chainId=8453;contractAddress=your-contract-address"
}
```

## Usage

```csharp
// Initialize provider
var provider = new BaseOASIS(
    hostUri: "https://mainnet.base.org",
    chainPrivateKey: "your-private-key",
    chainId: 8453,
    contractAddress: "your-contract-address"
);

// Activate provider
var result = await provider.ActivateProviderAsync();

// Use provider for OASIS operations
```

## Dependencies

- Nethereum.Web3
- Nethereum.Contracts
- NextGenSoftware.OASIS.API.Core
- NextGenSoftware.OASIS.Common

## Testing

Run the test harness to verify the provider is working correctly:

```bash
dotnet run --project NextGenSoftware.OASIS.API.Providers.BaseOASIS.TestHarness
```

## Security Notes

- Never commit private keys to version control
- Use environment variables for sensitive configuration
- Test on Base Sepolia testnet before deploying to mainnet
- Ensure you have sufficient ETH for gas fees

## Base Network Information

Base is a Layer 2 blockchain built on Optimism's OP Stack, providing:
- Low transaction costs
- Fast transaction finality
- EVM compatibility
- Native USDC support

For more information about Base, visit: https://base.org


