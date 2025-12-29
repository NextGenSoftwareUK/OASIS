# Managers - OASIS Manager System

**Last Updated:** December 2025  
**Detailed Documentation:** See `Docs/Devs/OASIS-Managers-Complete-Guide.md`, `OASIS-Managers-Part2.md`, `OASIS-Managers-Part3.md`

---

## What Are Managers?

**Managers** are high-level APIs that provide domain-specific functionality in OASIS. They abstract away provider complexity and provide a clean, unified interface for common operations.

**Key Concept:** You use managers, not providers directly. Managers handle the complexity of provider selection, HyperDrive routing, and error handling.

---

## Manager Architecture

### Base Class: OASISManager

All managers inherit from `OASISManager`:

```csharp
public abstract class OASISManager
{
    public OASISDNA OASISDNA { get; set; }
    public event OASISManagerError OnOASISManagerError;
    
    protected OASISManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null)
    {
        // Initializes provider and DNA
    }
}
```

### Singleton Pattern

Most managers use singleton pattern:

```csharp
public static AvatarManager Instance
{
    get
    {
        if (_instance == null)
            _instance = new AvatarManager(...);
        return _instance;
    }
}
```

---

## Core Managers

### 1. AvatarManager

**Purpose:** User identity and account management

**Key Operations:**
- Create, read, update, delete avatars
- Authentication (login, logout)
- Avatar details management
- Profile management

**Example:**
```csharp
// Create avatar
var avatar = new Avatar { Username = "user", Email = "user@example.com" };
var result = await AvatarManager.Instance.SaveAvatarAsync(avatar);

// Load avatar
var avatar = await AvatarManager.Instance.LoadAvatarAsync(avatarId);
```

**See:** `Docs/Devs/OASIS-Managers-Complete-Guide.md` for detailed API

---

### 2. HolonManager

**Purpose:** Data storage and retrieval (CRUD operations on holons)

**Key Operations:**
- Save, load, delete holons
- Load holons for parent
- Search holons
- Holon hierarchy management

**Example:**
```csharp
// Save holon
var holon = new Holon { Name = "My Data", Description = "Test" };
var result = await HolonManager.Instance.SaveHolonAsync(holon);

// Load holon
var holon = await HolonManager.Instance.LoadHolonAsync(holonId);
```

**See:** `Docs/Devs/OASIS-Managers-Part2.md` for detailed API

---

### 3. WalletManager

**Purpose:** Multi-chain wallet management

**Key Operations:**
- Get avatar wallets
- Create/delete wallets
- Wallet address management
- Cross-chain wallet operations

**Example:**
```csharp
// Get default wallet
var wallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(
    avatarId, 
    ProviderType.EthereumOASIS
);
```

**See:** `Docs/Devs/Wallet-Management-System.md` for detailed API

---

### 4. KeyManager

**Purpose:** Cryptographic key management

**Key Operations:**
- Generate keypairs
- Link keys to providers
- Retrieve provider keys
- Key lifecycle management

**Example:**
```csharp
// Generate keypair
var result = await KeyManager.Instance.GenerateKeyPairAsync(ProviderType.EthereumOASIS);
```

---

### 5. NFTManager

**Purpose:** Cross-chain NFT operations

**Key Operations:**
- Mint NFTs
- Transfer NFTs
- NFT metadata management
- Ownership tracking

**See:** `Docs/Devs/OASIS-Managers-Part2.md` for detailed API

---

### 6. SearchManager

**Purpose:** Universal search across all providers

**Key Operations:**
- Search holons
- Search avatars
- Cross-provider search
- Advanced search with filters

---

### 7. KarmaManager

**Purpose:** Digital reputation system

**Key Operations:**
- Add/remove karma
- Get karma for avatar
- Karma leaderboards
- Karma history

---

## Additional Managers

### Social & Communication
- **MessagingManager** - Avatar-to-avatar messaging
- **ChatManager** - Real-time chat
- **SocialManager** - Social network operations

### Gaming & Gamification
- **MissionManager** - Mission/quest management
- **QuestManager** - Quest system
- **CompetitionManager** - Leaderboards and competitions
- **GiftsManager** - Gift system
- **AchievementManager** - Achievement system

### Content & Media
- **FilesManager** - File upload/download
- **VideoManager** - Video operations

### System & Configuration
- **SettingsManager** - Settings management
- **StatsManager** - Statistics and analytics
- **MapManager** - Mapping and location services

### Specialized
- **SuperStarManager** - STAR system operations
- **EggsManager** - STAR system eggs
- **ProviderManager** - Provider management (internal)

---

## How Managers Work

### Request Flow

```
1. Application calls Manager method
      ↓
2. Manager validates input
      ↓
3. Manager calls ProviderManager/HyperDrive
      ↓
4. HyperDrive selects optimal provider
      ↓
5. Provider executes operation
      ↓
6. Result flows back through chain
      ↓
7. Manager returns result to application
```

### HyperDrive Integration

Managers automatically benefit from HyperDrive:

- **Auto-Failover:** If provider fails, automatically tries next
- **Auto-Replication:** If enabled, data replicated automatically
- **Load Balancing:** Requests distributed across providers

**Example:**
```csharp
// Manager call automatically uses HyperDrive
await HolonManager.Instance.SaveHolonAsync(holon);

// Behind the scenes:
// 1. HolonManager calls ProviderManager
// 2. ProviderManager uses HyperDrive
// 3. HyperDrive selects provider (or uses failover)
// 4. Data saved to provider
// 5. If replication enabled, replicates to other providers
```

---

## Manager Best Practices

### 1. Use Managers, Not Providers Directly

```csharp
// ✅ Good: Use manager
var avatar = await AvatarManager.Instance.LoadAvatarAsync(avatarId);

// ❌ Bad: Direct provider access (unless absolutely necessary)
var provider = ProviderManager.Instance.CurrentStorageProvider;
var avatar = await provider.LoadAvatarAsync(...);
```

### 2. Handle Errors

```csharp
var result = await AvatarManager.Instance.SaveAvatarAsync(avatar);
if (result.IsError)
{
    Console.WriteLine($"Error: {result.Message}");
    // Handle error
}
else
{
    // Use result.Result
}
```

### 3. Use Async/Await

```csharp
// ✅ Good: Async
var avatar = await AvatarManager.Instance.LoadAvatarAsync(avatarId);

// ❌ Bad: Blocking
var avatar = AvatarManager.Instance.LoadAvatarAsync(avatarId).Result;
```

### 4. Check for Null Results

```csharp
var result = await HolonManager.Instance.LoadHolonAsync(holonId);
if (!result.IsError && result.Result != null)
{
    var holon = result.Result;
    // Use holon
}
```

---

## Manager vs Provider

### When to Use Managers

- **Normal application operations** - Saving/loading data, user management
- **Domain-specific operations** - Avatars, wallets, NFTs
- **Cross-provider operations** - Want automatic failover/replication

### When to Use Providers Directly

- **Provider-specific features** - Features only available on specific provider
- **Advanced configuration** - Fine-grained provider control
- **Internal OASIS code** - Core system operations

---

## Manager Source Code

**Base Class:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASISManager.cs`

**Manager Implementations:**
- `AvatarManager` - `OASIS Architecture/.../Managers/AvatarManager/`
- `HolonManager` - `OASIS Architecture/.../Managers/HolonManager/`
- `WalletManager` - `OASIS Architecture/.../Managers/WalletManager.cs`
- `KeyManager` - `OASIS Architecture/.../Managers/KeyManager.cs`
- ... (see `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/` directory)

---

## Related Documentation

- **Detailed Manager Documentation:** 
  - `Docs/Devs/OASIS-Managers-Complete-Guide.md`
  - `Docs/Devs/OASIS-Managers-Part2.md`
  - `Docs/Devs/OASIS-Managers-Part3.md`
- [Providers Concept](./PROVIDERS.md) - How managers use providers
- [HyperDrive Concept](./HYPERDRIVE.md) - How managers benefit from HyperDrive
- [Holons Concept](./HOLONS.md) - Data model managers work with

---

**This is a concise overview. For detailed API documentation, see the referenced manager guides in `Docs/Devs/`.**

