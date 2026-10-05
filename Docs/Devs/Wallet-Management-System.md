# OASIS Wallet Management System

## Overview

The OASIS Wallet Management System provides a comprehensive solution for managing wallets, cryptographic keys, and wallet addresses across multiple blockchain providers. This system is designed to be secure, scalable, and easy to use for developers.

## Architecture

The wallet management system consists of three main components:

1. **WalletManager** - Core wallet management functionality
2. **KeyManager** - Cryptographic key management
3. **WalletHelper** - Generic helper with fallback chain for wallet lookup

## Components

### 1. WalletManager

The `WalletManager` is the core component for managing wallet objects and wallet addresses across different providers.

#### Key Features:
- **Wallet Address Management**: Get wallet addresses for avatars
- **Provider Support**: Works with all OASIS blockchain providers
- **Default Wallet Selection**: Automatically selects the best wallet for transactions
- **Cross-Provider Support**: Manages wallets across multiple blockchain networks

#### Main Methods:

```csharp
// Get default wallet for avatar by ID
public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByIdAsync(Guid avatarId, ProviderType providerType)

// Get default wallet for avatar by username
public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByUsernameAsync(string username, ProviderType providerType)

// Get default wallet for avatar by email
public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByEmailAsync(string email, ProviderType providerType)

// Load all provider wallets for avatar
public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id, ProviderType providerType)
```

#### Usage Example:

```csharp
// Get Ethereum wallet for avatar
var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarId, ProviderType.EthereumOASIS);

if (!walletResult.IsError && walletResult.Result != null)
{
    var wallet = walletResult.Result;
    string walletAddress = wallet.WalletAddress;
    string publicKey = wallet.PublicKey;
    // Use wallet for transactions
}
```

### 2. KeyManager

The `KeyManager` handles cryptographic keys (public and private keys) for secure operations.

#### Key Features:
- **Key Management**: Manages public and private keys
- **Security**: Private keys are encrypted and only accessible to logged-in avatar
- **Provider Support**: Works with all OASIS blockchain providers
- **Authentication**: Handles key-based authentication

#### Main Methods:

```csharp
// Get public keys for avatar
public OASISResult<List<string>> GetProviderPublicKeysForAvatarById(Guid avatarId, ProviderType providerType)

// Get private keys for avatar (only for logged-in avatar)
public OASISResult<List<string>> GetProviderPrivateKeysForAvatarById(Guid avatarId, ProviderType providerType)

// Get public keys by username
public OASISResult<List<string>> GetProviderPublicKeysForAvatarByUsername(string username, ProviderType providerType)

// Get public keys by email
public OASISResult<List<string>> GetProviderPublicKeysForAvatarByEmail(string email, ProviderType providerType)
```

#### Usage Example:

```csharp
// Get public keys for transaction verification
var publicKeysResult = KeyManager.GetProviderPublicKeysForAvatarById(avatarId, ProviderType.EthereumOASIS);

if (!publicKeysResult.IsError && publicKeysResult.Result != null)
{
    var publicKeys = publicKeysResult.Result;
    // Use public keys for verification
}

// Get private keys for transaction signing (only for logged-in avatar)
var privateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, ProviderType.EthereumOASIS);

if (!privateKeysResult.IsError && privateKeysResult.Result != null)
{
    var privateKeys = privateKeysResult.Result;
    // Use private keys for signing transactions
}
```

### 3. WalletHelper

The `WalletHelper` is a generic helper class that provides a robust fallback chain for wallet lookup across all providers.

#### Key Features:
- **Generic**: Works across all OASIS providers
- **Robust Fallback**: 3-tier fallback chain for maximum reliability
- **OASIS Standards**: Proper `OASISResult` wrappers
- **Error Handling**: Comprehensive error management
- **Future-Proof**: Easy to extend for new providers

#### Fallback Chain:
1. **WalletManager** - Direct wallet lookup (safest)
2. **Avatar.ProviderWallets** - Fallback to avatar's provider wallets
3. **HTTP Client** - Final fallback to WEB4 OASIS API

#### Main Methods:

```csharp
// Get wallet address for avatar by ID
public static async Task<OASISResult<string>> GetWalletAddressForAvatarAsync(
    WalletManager walletManager, 
    ProviderType providerType, 
    Guid avatarId, 
    HttpClient httpClient = null)

// Get wallet address for avatar by username
public static async Task<OASISResult<string>> GetWalletAddressForAvatarByUsernameAsync(
    WalletManager walletManager, 
    ProviderType providerType, 
    string username, 
    HttpClient httpClient = null)

// Get wallet address for avatar by email
public static async Task<OASISResult<string>> GetWalletAddressForAvatarByEmailAsync(
    WalletManager walletManager, 
    ProviderType providerType, 
    string email, 
    HttpClient httpClient = null)
```

#### Usage Example:

```csharp
// Get wallet address with fallback chain
var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(
    WalletManager.Instance, 
    ProviderType.EthereumOASIS, 
    avatarId, 
    httpClient);

if (!walletResult.IsError && !string.IsNullOrEmpty(walletResult.Result))
{
    string walletAddress = walletResult.Result;
    // Use wallet address for transactions
}
```

## Best Practices

### When to Use Each Component

#### 🏆 **Use WalletHelper for Wallet Addresses**
- **Best for**: Getting wallet addresses for transactions
- **Benefits**: Robust fallback, cross-provider support, error handling
- **Use Case**: Transaction sending, wallet lookup, cross-provider operations

```csharp
// ✅ RECOMMENDED: Use WalletHelper
var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(
    WalletManager, ProviderType.EthereumOASIS, avatarId, httpClient);
```

#### 🔑 **Use KeyManager for Cryptographic Keys**
- **Best for**: Signing transactions, encryption, authentication
- **Benefits**: Secure key management, encrypted storage
- **Use Case**: Transaction signing, key-based authentication

```csharp
// ✅ RECOMMENDED: Use KeyManager for keys
var privateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, providerType);
```

#### 💰 **Use WalletManager for Direct Wallet Management**
- **Best for**: When you need full wallet objects
- **Benefits**: Direct access to wallet properties
- **Use Case**: Wallet management, detailed wallet information

```csharp
// ✅ RECOMMENDED: Use WalletManager for full wallet objects
var walletResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(avatarId, providerType);
```

### Security Considerations

1. **Private Keys**: Only accessible to logged-in avatar
2. **Encryption**: Private keys are encrypted in storage
3. **Access Control**: Proper authentication required
4. **Error Handling**: Comprehensive error management

### Error Handling

All components use `OASISResult<T>` wrappers for consistent error handling:

```csharp
var result = await WalletHelper.GetWalletAddressForAvatarAsync(...);

if (!result.IsError && !string.IsNullOrEmpty(result.Result))
{
    // Success - use result.Result
}
else
{
    // Handle error - check result.Message and result.Exception
    Console.WriteLine($"Error: {result.Message}");
}
```

## Integration with Providers

The wallet management system is integrated with all OASIS blockchain providers:

- **EthereumOASIS** - Ethereum blockchain
- **SOLANAOASIS** - Solana blockchain
- **ArbitrumOASIS** - Arbitrum Layer 2
- **PolygonOASIS** - Polygon blockchain
- **OptimismOASIS** - Optimism Layer 2
- **BNBChainOASIS** - BNB Smart Chain
- **FantomOASIS** - Fantom blockchain
- **AvalancheOASIS** - Avalanche blockchain
- **EOSIOOASIS** - EOSIO blockchain
- **TRONOASIS** - TRON blockchain
- **HashgraphOASIS** - Hashgraph blockchain
- **AptosOASIS** - Aptos blockchain
- **BitcoinOASIS** - Bitcoin blockchain
- **CardanoOASIS** - Cardano blockchain
- **NEAROASIS** - NEAR Protocol
- **CosmosOASIS** - Cosmos blockchain
- **PolkadotOASIS** - Polkadot blockchain
- **ActivityPubOASIS** - ActivityPub protocol

## Related Documentation

- [OASIS Developer Index](Dev-Index.md)
- [Provider Management](Provider-Management.md)
- [Avatar Management](Avatar-Management.md)
- [Transaction Management](Transaction-Management.md)
- [Security Best Practices](Security-Best-Practices.md)

## Examples

### Complete Transaction Example

```csharp
public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(
    Guid fromAvatarId, 
    Guid toAvatarId, 
    decimal amount, 
    ProviderType providerType)
{
    var result = new OASISResult<ITransactionRespone>();
    
    try
    {
        // 1. Get sender's wallet address
        var senderWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(
            WalletManager.Instance, providerType, fromAvatarId);
        
        if (senderWalletResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to get sender wallet: {senderWalletResult.Message}");
            return result;
        }
        
        // 2. Get receiver's wallet address
        var receiverWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(
            WalletManager.Instance, providerType, toAvatarId);
        
        if (receiverWalletResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to get receiver wallet: {receiverWalletResult.Message}");
            return result;
        }
        
        // 3. Get private keys for signing
        var privateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(fromAvatarId, providerType);
        
        if (privateKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to get private keys: {privateKeysResult.Message}");
            return result;
        }
        
        // 4. Create and sign transaction
        var transaction = CreateTransaction(
            senderWalletResult.Result, 
            receiverWalletResult.Result, 
            amount, 
            privateKeysResult.Result.First());
        
        // 5. Send transaction
        var txResult = await SendTransactionToBlockchain(transaction);
        
        if (!txResult.IsError)
        {
            result.Result = txResult.Result;
            result.IsError = false;
            result.Message = "Transaction sent successfully";
        }
        else
        {
            OASISErrorHandling.HandleError(ref result, $"Transaction failed: {txResult.Message}");
        }
    }
    catch (Exception ex)
    {
        result.Exception = ex;
        OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionAsync: {ex.Message}");
    }
    
    return result;
}
```

## Troubleshooting

### Common Issues

1. **Wallet Not Found**: Check if avatar has linked wallets for the provider
2. **Private Key Access Denied**: Ensure you're using the logged-in avatar's ID
3. **Provider Not Supported**: Verify the provider type is correct
4. **Network Issues**: Check HTTP client configuration for fallback

### Debug Tips

1. **Enable Logging**: Use OASIS logging to track wallet operations
2. **Check Error Messages**: Always check `OASISResult.Message` for detailed error information
3. **Verify Provider**: Ensure the provider is properly configured
4. **Test Fallback**: Test the fallback chain by disabling primary methods

## Support

For additional support and questions:

- **GitHub Issues**: [OASIS GitHub Issues](https://github.com/NextGenSoftwareUK/OASIS/issues)
- **Documentation**: [OASIS Documentation](https://github.com/NextGenSoftwareUK/OASIS/wiki)
- **Community**: [OASIS Community](https://github.com/NextGenSoftwareUK/OASIS/discussions)

---

*Last updated: December 2024*
*Version: 1.0*
