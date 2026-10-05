# Providers - OASIS Provider System

**Last Updated:** December 2025  
**Source of Truth:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Providers/IOASISProvider.cs` and provider implementations

---

## What Are Providers?

A **Provider** in OASIS is any external system that OASIS can connect to for storing, retrieving, or managing data. Providers abstract away the complexity of different systems (blockchains, databases, storage services) behind a unified interface.

**Key Concept:** OASIS treats all providers the same way, regardless of whether they're a blockchain, database, or cloud service.

---

## Provider Interface Hierarchy

### Base Interface: `IOASISProvider`

All providers implement `IOASISProvider` which provides:

```csharp
public interface IOASISProvider
{
    string ProviderName { get; set; }
    string ProviderDescription { get; set; }
    EnumValue<ProviderCategory> ProviderCategory { get; set; }
    List<EnumValue<ProviderCategory>> ProviderCategories { get; set; }
    EnumValue<ProviderType> ProviderType { get; set; }
    bool IsProviderActivated { get; set; }
    
    OASISResult<bool> ActivateProvider();
    Task<OASISResult<bool>> ActivateProviderAsync();
    OASISResult<bool> DeActivateProvider();
    Task<OASISResult<bool>> DeActivateProviderAsync();
}
```

### Specialized Interfaces

Providers can implement additional interfaces based on their capabilities:

1. **`IOASISStorageProvider`** - For data storage operations (CRUD)
   - Save/Load avatars, holons
   - Search capabilities
   - File operations

2. **`IOASISNETProvider`** - For networking capabilities
   - Network topology
   - Peer discovery
   - Messaging

3. **`IOASISSmartContractProvider`** - For smart contract operations
   - Contract compilation
   - Contract deployment
   - Contract interaction

4. **`IOASISNFTProvider`** - For NFT operations
   - NFT minting
   - NFT transfers
   - NFT metadata

---

## Provider Types

Providers are identified by the `ProviderType` enum. There are **55 provider types** defined (excluding `None`, `All`, `Default`):

### Blockchain Providers (40)
- Ethereum, Solana, Polygon, Arbitrum, Avalanche, Base, etc.
- EVM-compatible chains (Ethereum, Polygon, Arbitrum, Base, Optimism, etc.)
- Non-EVM chains (Solana, Cardano, Polkadot, Cosmos, etc.)

### Storage Providers (8)
- Databases: MongoDB, Neo4j, SQLite, SQL Server, Oracle DB
- Cloud: AWS, Azure Cosmos DB, Google Cloud

### Network Providers (7)
- IPFS, Pinata (IPFS gateway), Holochain
- Decentralized protocols: SOLID, ActivityPub, Scuttlebutt, ThreeFold

### Other Providers (5)
- PLAN, LocalFile, Urbit, etc.

**See:** [`../reference/PROVIDERS/STATUS.md`](../reference/PROVIDERS/STATUS.md) for complete list

---

## Provider Categories

Providers are categorized by `ProviderCategory`:

- **Blockchain** - Cryptocurrency and smart contract platforms
- **Storage** - Databases and file storage
- **Network** - Distributed networks and protocols
- **Cloud** - Cloud service providers
- **SmartContract** - Smart contract platforms
- **StorageAndNetwork** - Providers with both capabilities

---

## Provider Lifecycle

### 1. Registration

Providers are registered during OASIS boot via `OASISBootLoader`:

```csharp
// Providers are registered from OASIS_DNA.json configuration
OASISBootLoader.BootOASISAsync()
```

Registration loads the provider class and makes it available to the system.

### 2. Activation

Activation initializes the provider and establishes connections:

```csharp
await provider.ActivateProviderAsync();
```

**Activation typically:**
- Establishes database/network connections
- Validates credentials
- Initializes provider-specific resources
- Sets `IsProviderActivated = true`

### 3. Configuration

Provider configuration comes from `OASIS_DNA.json`:

```json
{
  "StorageProviders": {
    "MongoDBOASIS": {
      "ConnectionString": "...",
      "DBName": "..."
    }
  }
}
```

### 4. Deactivation

Providers can be deactivated to free resources:

```csharp
await provider.DeActivateProviderAsync();
```

---

## Provider Manager

The `ProviderManager` orchestrates all providers:

### Responsibilities

1. **Registration Management**
   - Maintains list of registered providers
   - Handles provider discovery

2. **Activation Control**
   - Activates/deactivates providers
   - Manages current active provider

3. **HyperDrive Integration**
   - Maintains failover provider lists
   - Maintains replication provider lists
   - Maintains load balancing provider lists

4. **Provider Selection**
   - Selects optimal provider for operations
   - Handles provider switching

### Key Methods

```csharp
ProviderManager.Instance.GetAndActivateDefaultStorageProviderAsync()
ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType)
ProviderManager.Instance.GetProviderAutoFailOverList()
ProviderManager.Instance.GetProvidersThatAreAutoReplicating()
ProviderManager.Instance.GetProviderAutoLoadBalanceList()
```

---

## Provider Configuration

### OASIS_DNA.json Structure

Each provider can have configuration in `OASIS_DNA.json`:

```json
{
  "OASIS": {
    "StorageProviders": {
      "MongoDBOASIS": {
        "ConnectionString": "mongodb://...",
        "DBName": "OASISAPI_DEV"
      },
      "EthereumOASIS": {
        "ChainPrivateKey": "0x...",
        "ChainId": 11155111,
        "ConnectionString": "https://sepolia.infura.io/v3/..."
      },
      "SolanaOASIS": {
        "WalletMnemonicWords": "...",
        "ConnectionString": "https://api.devnet.solana.com"
      }
    }
  }
}
```

### Common Configuration Properties

**Database Providers:**
- `ConnectionString` - Database connection string
- `DBName` - Database name

**Blockchain Providers:**
- `ConnectionString` - RPC endpoint URL
- `ChainPrivateKey` - Private key for signing transactions
- `ChainId` - Network chain ID
- `ContractAddress` - Smart contract address (if applicable)

**IPFS/Storage Providers:**
- `ConnectionString` - IPFS node URL or API endpoint
- API keys (for services like Pinata)

---

## HyperDrive Integration

Providers integrate with HyperDrive through ProviderManager:

### Auto-Failover

```json
"AutoFailOverProviders": "MongoDBOASIS, ArbitrumOASIS, EthereumOASIS"
```

If primary provider fails, HyperDrive tries providers in this order.

### Auto-Replication

```json
"AutoReplicationProviders": "MongoDBOASIS"
```

Data is automatically replicated to these providers (currently disabled in default config).

### Auto-Load Balancing

```json
"AutoLoadBalanceProviders": "MongoDBOASIS"
```

Requests are distributed across these providers for optimal performance.

---

## Provider Selection

Providers are selected based on:

1. **HyperDrive Configuration** - Failover, replication, load balancing lists
2. **Provider Availability** - Is the provider activated and healthy?
3. **Request Requirements** - Does the provider support the requested operation?
4. **Performance Metrics** - Latency, throughput, error rate
5. **Cost** - Transaction costs, subscription limits

---

## Creating a New Provider

To create a new provider:

1. **Add to ProviderType Enum**
   ```csharp
   // In ProviderType.cs
   YourProviderOASIS,
   ```

2. **Create Provider Class**
   ```csharp
   public class YourProviderOASIS : OASISStorageProviderBase, IOASISStorageProvider
   {
       public override Task<OASISResult<bool>> ActivateProviderAsync()
       {
           // Initialize connections, validate config
           IsProviderActivated = true;
           return Task.FromResult(new OASISResult<bool> { Result = true });
       }
       
       // Implement required interface methods
   }
   ```

3. **Add Configuration to OASIS_DNA.json**
   ```json
   "YourProviderOASIS": {
       "ConnectionString": "...",
       "ApiKey": "..."
   }
   ```

4. **Register Provider**
   - Add to provider registration list
   - Provider will be registered during boot

---

## Provider Status

**To check provider status:**

1. **At Runtime:**
   ```csharp
   var providers = ProviderManager.Instance.GetAllRegisteredProviders();
   foreach (var provider in providers)
   {
       Console.WriteLine($"{provider.ProviderName}: {provider.IsProviderActivated}");
   }
   ```

2. **Via API:**
   ```
   GET /api/provider/get-all-providers
   ```

3. **Check Logs:**
   - OASIS boot logs show provider registration and activation

---

## Common Provider Operations

### Storage Providers

- `SaveAvatarAsync()` - Save avatar data
- `LoadAvatarAsync()` - Load avatar by ID
- `SaveHolonAsync()` - Save holon data
- `LoadHolonAsync()` - Load holon by ID
- `DeleteHolonAsync()` - Delete holon
- `SearchAsync()` - Search across data

### Blockchain Providers

- `SendTransactionAsync()` - Send blockchain transaction
- `GetBalanceAsync()` - Get wallet balance
- `DeployContractAsync()` - Deploy smart contract
- `CallContractMethodAsync()` - Interact with contract

---

## Best Practices

1. **Error Handling:** Always use `OASISErrorHandling.HandleError()` for consistent error handling
2. **Async/Await:** Use async methods for all I/O operations
3. **Resource Cleanup:** Implement `IDisposable` for proper resource management
4. **Connection Management:** Pool connections where possible
5. **Timeout Handling:** Set appropriate timeouts for network operations
6. **Logging:** Log important operations and errors

---

## Related Documentation

- [Provider Status Reference](../reference/PROVIDERS/STATUS.md) - Complete provider list and status
- [HyperDrive Concept](../concepts/HYPERDRIVE.md) - How providers integrate with HyperDrive
- [Managers Concept](../concepts/MANAGERS.md) - How managers use providers
- [OASIS DNA Concept](../concepts/DNA.md) - Provider configuration

---

**Source Code:**
- Interface: `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Providers/IOASISProvider.cs`
- Manager: `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs`
- Boot Loader: `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.cs`

