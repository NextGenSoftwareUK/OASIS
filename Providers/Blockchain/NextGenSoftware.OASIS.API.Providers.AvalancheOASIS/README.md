# Avalanche OASIS Provider

## Overview
The **Avalanche OASIS Provider** enables seamless integration between the OASIS ecosystem and the Avalanche blockchain network. This provider implements the full OASIS storage and blockchain interfaces, allowing avatars, holons, and NFTs to be stored and managed on Avalanche's high-performance C-Chain (Contract Chain).

## Features

✨ **Full OASIS Integration**
- Avatar management (create, read, update, delete)
- Avatar detail storage
- Holon (data object) storage
- NFT minting and transfers
- Transaction management

⚡ **Avalanche Network Support**
- C-Chain (EVM-compatible) integration
- High throughput (4,500+ TPS)
- Sub-second finality
- Low transaction fees

🔐 **Security Features**
- Wallet integration via WalletManager
- Private key management
- Transaction signing and verification

## Installation

Add the Avalanche OASIS Provider to your project:

```bash
dotnet add package NextGenSoftware.OASIS.API.Providers.AvalancheOASIS
```

## Configuration

### Initialize Provider

```csharp
using NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;

// Avalanche Mainnet (C-Chain)
var avalancheProvider = new AvalancheOASIS(
    hostUri: "https://api.avax.network/ext/bc/C/rpc",
    chainPrivateKey: "YOUR_PRIVATE_KEY",
    chainId: 43114,
    contractAddress: "YOUR_CONTRACT_ADDRESS"
);

// Avalanche Testnet (Fuji C-Chain)
var avalancheProviderTestnet = new AvalancheOASIS(
    hostUri: "https://api.avax-test.network/ext/bc/C/rpc",
    chainPrivateKey: "YOUR_PRIVATE_KEY",
    chainId: 43113,
    contractAddress: "YOUR_CONTRACT_ADDRESS"
);

await avalancheProvider.ActivateProviderAsync();
```

## Network Details

### Mainnet (C-Chain)
- **RPC URL**: `https://api.avax.network/ext/bc/C/rpc`
- **Chain ID**: 43114
- **Explorer**: https://snowtrace.io
- **Currency**: AVAX

### Testnet (Fuji C-Chain)
- **RPC URL**: `https://api.avax-test.network/ext/bc/C/rpc`
- **Chain ID**: 43113
- **Explorer**: https://testnet.snowtrace.io
- **Faucet**: https://faucet.avax.network

## Usage Examples

### Save Avatar
```csharp
var avatar = new Avatar
{
    Id = Guid.NewGuid(),
    Username = "avalanche_user",
    Email = "user@example.com"
};

var result = await avalancheProvider.SaveAvatarAsync(avatar);
if (!result.IsError)
{
    Console.WriteLine($"Avatar saved: {result.Result.Id}");
}
```

### Load Avatar
```csharp
var result = await avalancheProvider.LoadAvatarAsync(avatarId);
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
    MemoText = "My OASIS NFT on Avalanche"
};

var result = await avalancheProvider.MintNFTAsync(mintRequest);
if (!result.IsError)
{
    Console.WriteLine($"NFT Minted: {result.Result.TransactionResult}");
}
```

### Send Transaction
```csharp
var txRequest = new WalletTransactionRequest
{
    ToWalletAddress = "0x...",
    Amount = 1.5m // 1.5 AVAX
};

var result = await avalancheProvider.SendTransactionAsync(txRequest);
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

- **Transaction Speed**: Sub-second finality
- **Cost**: ~0.001 AVAX per transaction
- **Throughput**: 4,500+ TPS on C-Chain
- **Scalability**: Subnet support for custom chains

## Advantages

🚀 **High Performance**
- Fastest blockchain for smart contracts
- Sub-second transaction finality
- Scalable subnet architecture

💰 **Cost Effective**
- Low gas fees
- Predictable costs
- Energy efficient (PoS)

🔗 **Ecosystem**
- Growing DeFi ecosystem
- EVM compatibility
- Cross-chain bridges

## License
MIT License - Copyright © NextGen Software Ltd 2025

## Support
- Documentation: https://docs.avax.network
- OASIS Docs: https://oasis.earth
- Issues: https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/issues



