# Base OASIS Provider

## Overview
The **Base OASIS Provider** enables seamless integration between the OASIS ecosystem and Base, Coinbase's Layer 2 network built on Optimism. This provider implements the full OASIS storage and blockchain interfaces, allowing avatars, holons, and NFTs to be stored and managed on Base's secure, low-cost, and developer-friendly blockchain.

## Features

✨ **Full OASIS Integration**
- Avatar management (create, read, update, delete)
- Avatar detail storage
- Holon (data object) storage
- NFT minting and transfers
- Transaction management

⚡ **Base Network Support**
- Optimistic rollup technology
- Ethereum security
- Low gas fees
- Fast transactions

🔐 **Security Features**
- Wallet integration via WalletManager
- Private key management
- Transaction signing and verification
- Backed by Coinbase

## Installation

Add the Base OASIS Provider to your project:

```bash
dotnet add package NextGenSoftware.OASIS.API.Providers.BaseOASIS
```

## Configuration

### Initialize Provider

```csharp
using NextGenSoftware.OASIS.API.Providers.BaseOASIS;

// Base Mainnet
var baseProvider = new BaseOASIS(
    hostUri: "https://mainnet.base.org",
    chainPrivateKey: "YOUR_PRIVATE_KEY",
    contractAddress: "YOUR_CONTRACT_ADDRESS"
);

// Base Sepolia Testnet
var baseProviderTestnet = new BaseOASIS(
    hostUri: "https://sepolia.base.org",
    chainPrivateKey: "YOUR_PRIVATE_KEY",
    contractAddress: "YOUR_CONTRACT_ADDRESS"
);

await baseProvider.ActivateProviderAsync();
```

## Network Details

### Mainnet
- **RPC URL**: `https://mainnet.base.org`
- **Chain ID**: 8453
- **Explorer**: https://basescan.org
- **Currency**: ETH
- **Bridge**: https://bridge.base.org

### Sepolia Testnet
- **RPC URL**: `https://sepolia.base.org`
- **Chain ID**: 84532
- **Explorer**: https://sepolia.basescan.org
- **Faucet**: https://www.coinbase.com/faucets/base-ethereum-goerli-faucet

## Usage Examples

### Save Avatar
```csharp
var avatar = new Avatar
{
    Id = Guid.NewGuid(),
    Username = "base_user",
    Email = "user@example.com"
};

var result = await baseProvider.SaveAvatarAsync(avatar);
if (!result.IsError)
{
    Console.WriteLine($"Avatar saved on Base: {result.Result.Id}");
}
```

### Load Avatar
```csharp
var result = await baseProvider.LoadAvatarAsync(avatarId);
if (!result.IsError)
{
    Console.WriteLine($"Loaded: {result.Result.Username}");
}
```

### Mint NFT
```csharp
var mintRequest = new MintNFTTransactionRequest
{
    MintWalletAddress = "0x...",
    JSONMetaDataURL = "ipfs://...",
    MemoText = "My OASIS NFT on Base"
};

var result = await baseProvider.MintNFTAsync(mintRequest);
if (!result.IsError)
{
    Console.WriteLine($"NFT Minted on Base: {result.Result.TransactionResult}");
}
```

### Send Transaction
```csharp
var txRequest = new WalletTransactionRequest
{
    ToWalletAddress = "0x...",
    Amount = 0.01m // 0.01 ETH
};

var result = await baseProvider.SendTransactionAsync(txRequest);
```

## Smart Contract ABI

The provider uses a standard OASIS smart contract with the following functions:
- `CreateAvatar(uint256, string, string)`
- `CreateAvatarDetail(uint256, string, string)`
- `CreateHolon(uint256, string, string)`
- `GetAvatarById(uint256)`
- `GetHolonById(uint256)`
- `mint(address, string)` - NFT minting
- `sendNFT(...)` - NFT transfers

## Performance

- **Transaction Speed**: 2 seconds
- **Cost**: ~10-100x cheaper than Ethereum L1
- **Finality**: 1-2 seconds (soft), ~1 week (hard on Ethereum)
- **Throughput**: Scales with Ethereum

## Advantages

🚀 **Developer Friendly**
- EVM compatible
- Easy migration from Ethereum
- Comprehensive tooling
- Coinbase support

💰 **Cost Effective**
- Ultra-low gas fees
- Predictable costs
- Ethereum security

🔗 **Ecosystem**
- Coinbase integration
- Growing DeFi ecosystem
- NFT marketplaces
- Cross-chain bridges

## Why Base?

- **Backed by Coinbase**: Enterprise-grade reliability
- **Optimistic Rollup**: Ethereum security with L2 speed
- **Low Fees**: Affordable for all users
- **Developer Tools**: Full Ethereum tooling support
- **Onboarding**: Easy fiat on-ramps via Coinbase

## License
MIT License - Copyright © NextGen Software Ltd 2025

## Support
- Documentation: https://docs.base.org
- OASIS Docs: https://oasis.earth
- Base Discord: https://base.org/discord
- Issues: https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/issues



