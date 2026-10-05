# OASIS Managers Complete Guide

## Overview

The OASIS system provides a comprehensive set of managers that handle different aspects of the platform. This guide covers all managers, their use cases, best practices, and when to use each one.

## Core Managers

### 1. AvatarManager

The `AvatarManager` is the central manager for all avatar-related operations in the OASIS system.

#### Key Features:
- **Avatar Lifecycle**: Create, read, update, delete avatars
- **Authentication**: Login, logout, session management
- **Profile Management**: Avatar details, preferences, settings
- **Provider Integration**: Works with all OASIS providers
- **Data Persistence**: Saves avatar data across multiple storage providers

#### Main Methods:

```csharp
// Avatar Creation & Management
public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id)
public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username)
public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email)

// Avatar Authentication
public async Task<OASISResult<IAvatar>> LoginAsync(string username, string password)
public async Task<OASISResult<IAvatar>> LoginByEmailAsync(string email, string password)
public async Task<OASISResult<bool>> LogoutAsync()

// Avatar Details
public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id)
public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username)
```

#### Usage Example:

```csharp
// Create a new avatar
var avatar = new Avatar
{
    Username = "john_doe",
    Email = "john@example.com",
    FirstName = "John",
    LastName = "Doe"
};

var saveResult = await AvatarManager.Instance.SaveAvatarAsync(avatar);

if (!saveResult.IsError && saveResult.Result != null)
{
    Console.WriteLine($"Avatar created with ID: {saveResult.Result.Id}");
}

// Load avatar by username
var loadResult = await AvatarManager.Instance.LoadAvatarAsync("john_doe");

if (!loadResult.IsError && loadResult.Result != null)
{
    var loadedAvatar = loadResult.Result;
    Console.WriteLine($"Loaded avatar: {loadedAvatar.Username}");
}
```

### 2. WalletManager

The `WalletManager` handles all wallet-related operations across multiple blockchain providers.

#### Key Features:
- **Multi-Provider Support**: Works with all blockchain providers
- **Wallet Management**: Create, manage, and retrieve wallets
- **Default Wallet Selection**: Automatically selects the best wallet
- **Cross-Provider Operations**: Manage wallets across different blockchains

#### Main Methods:

```csharp
// Wallet Management
public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByIdAsync(Guid avatarId, ProviderType providerType)
public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id, ProviderType providerType)

// Wallet Operations
public async Task<OASISResult<Guid>> CreateWalletAsync(Guid avatarId, ProviderType providerType)
public async Task<OASISResult<bool>> DeleteWalletAsync(Guid walletId, ProviderType providerType)
```

#### Usage Example:

```csharp
// Get default Ethereum wallet for avatar
var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarId, ProviderType.EthereumOASIS);

if (!walletResult.IsError && walletResult.Result != null)
{
    var wallet = walletResult.Result;
    Console.WriteLine($"Wallet Address: {wallet.WalletAddress}");
    Console.WriteLine($"Public Key: {wallet.PublicKey}");
}
```

### 3. KeyManager

The `KeyManager` handles cryptographic keys for secure operations.

#### Key Features:
- **Key Management**: Manages public and private keys
- **Security**: Private keys are encrypted and secure
- **Provider Support**: Works with all blockchain providers
- **Authentication**: Key-based authentication

#### Main Methods:

```csharp
// Public Key Management
public OASISResult<List<string>> GetProviderPublicKeysForAvatarById(Guid avatarId, ProviderType providerType)
public OASISResult<List<string>> GetProviderPublicKeysForAvatarByUsername(string username, ProviderType providerType)

// Private Key Management (secure)
public OASISResult<List<string>> GetProviderPrivateKeysForAvatarById(Guid avatarId, ProviderType providerType)
public OASISResult<List<string>> GetProviderPrivateKeysForAvatarByUsername(string username, ProviderType providerType)
```

#### Usage Example:

```csharp
// Get public keys for verification
var publicKeysResult = KeyManager.GetProviderPublicKeysForAvatarById(avatarId, ProviderType.EthereumOASIS);

if (!publicKeysResult.IsError && publicKeysResult.Result != null)
{
    var publicKeys = publicKeysResult.Result;
    // Use public keys for verification
}

// Get private keys for signing (only for logged-in avatar)
var privateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, ProviderType.EthereumOASIS);

if (!privateKeysResult.IsError && privateKeysResult.Result != null)
{
    var privateKeys = privateKeysResult.Result;
    // Use private keys for signing transactions
}
```

## Manager Comparison & Best Practices

### When to Use Each Manager

#### 🏆 **AvatarManager - User Management**
- **Use for**: User accounts, profiles, authentication
- **Best when**: Managing user data, login/logout, profile updates
- **Benefits**: Centralized user management, authentication, profile data

#### 💰 **WalletManager - Wallet Operations**
- **Use for**: Wallet addresses, wallet management, transactions
- **Best when**: Getting wallet addresses, managing wallets
- **Benefits**: Multi-provider support, wallet management

#### 🔑 **KeyManager - Cryptographic Operations**
- **Use for**: Signing transactions, encryption, authentication
- **Best when**: Need cryptographic keys for security operations
- **Benefits**: Secure key management, encrypted storage

### Best Practices

#### ✅ **For User Operations**
```csharp
// ✅ RECOMMENDED: Use AvatarManager for user operations
var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username);
var saveResult = await AvatarManager.Instance.SaveAvatarAsync(avatar);
```

#### ✅ **For Wallet Operations**
```csharp
// ✅ RECOMMENDED: Use WalletManager for wallet operations
var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarId, providerType);
```

#### ✅ **For Cryptographic Operations**
```csharp
// ✅ RECOMMENDED: Use KeyManager for cryptographic operations
var privateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, providerType);
```

## Security Considerations

### 🔒 **Key Security**
- Private keys are encrypted in storage
- Only accessible to logged-in avatar
- Proper authentication required

### 🔒 **Wallet Security**
- Wallet addresses are public
- Private keys are encrypted
- Secure key management

### 🔒 **Avatar Security**
- User data is protected
- Authentication required
- Secure session management

## Error Handling

All managers use `OASISResult<T>` wrappers for consistent error handling:

```csharp
var result = await AvatarManager.Instance.LoadAvatarAsync(username);

if (!result.IsError && result.Result != null)
{
    // Success - use result.Result
    var avatar = result.Result;
}
else
{
    // Handle error - check result.Message and result.Exception
    Console.WriteLine($"Error: {result.Message}");
    if (result.Exception != null)
    {
        Console.WriteLine($"Exception: {result.Exception.Message}");
    }
}
```

## Integration Examples

### Complete User Registration Flow

```csharp
public async Task<OASISResult<IAvatar>> RegisterUserAsync(string username, string email, string password)
{
    var result = new OASISResult<IAvatar>();
    
    try
    {
        // 1. Create avatar
        var avatar = new Avatar
        {
            Username = username,
            Email = email,
            Password = password
        };
        
        var saveResult = await AvatarManager.Instance.SaveAvatarAsync(avatar);
        
        if (saveResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to create avatar: {saveResult.Message}");
            return result;
        }
        
        // 2. Create default wallet
        var walletResult = await WalletManager.Instance.CreateWalletAsync(avatar.Id, ProviderType.EthereumOASIS);
        
        if (walletResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to create wallet: {walletResult.Message}");
            return result;
        }
        
        // 3. Success
        result.Result = saveResult.Result;
        result.IsError = false;
        result.Message = "User registered successfully";
    }
    catch (Exception ex)
    {
        result.Exception = ex;
        OASISErrorHandling.HandleError(ref result, $"Error in RegisterUserAsync: {ex.Message}");
    }
    
    return result;
}
```

## Related Documentation

- [Wallet Management System](Wallet-Management-System.md)
- [Provider Management](Provider-Management.md)
- [Transaction Management](Transaction-Management.md)
- [Security Best Practices](Security-Best-Practices.md)

---

*Last updated: December 2024*
*Version: 1.0*
