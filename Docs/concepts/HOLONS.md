# Holons - OASIS Universal Data Model

**Last Updated:** December 2025  
**Source of Truth:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/HolonBase.cs` and `IHolon.cs`

---

## What is a Holon?

A **Holon** is OASIS's universal data model - a self-contained unit of data that works across all providers. The term "Holon" comes from the concept that it is both a whole (complete entity) and a part (can be part of larger structures).

**Key Concept:** Holons are provider-agnostic. The same data structure works with Ethereum, Solana, MongoDB, IPFS, and any other provider.

---

## Holon Structure

### Core Properties (from IHolonBase)

Every Holon has these fundamental properties:

```csharp
public interface IHolonBase : IAuditBase
{
    Guid Id { get; set; }                           // Unique identifier (GUID)
    string Name { get; set; }                       // Human-readable name
    string Description { get; set; }                // Description
    HolonType HolonType { get; set; }               // Type of holon
    bool IsActive { get; set; }                     // Active/inactive flag
    Dictionary<string, object> MetaData { get; set; } // Custom metadata (key-value pairs)
}
```

### Provider-Specific Properties

Holons track provider-specific information:

```csharp
// Provider-specific storage keys
Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
// Example: { ProviderType.EthereumOASIS: "0x1234...", ProviderType.MongoDBOASIS: "507f1f77bcf86cd799439011" }

// Provider-specific metadata
Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
// Example: { ProviderType.SolanaOASIS: { "programId": "...", "account": "..." } }

// Which provider was this holon originally created on
EnumValue<ProviderType> CreatedProviderType { get; set; }

// Which provider is this instance currently saved on
EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
```

### Versioning Properties

Holons support versioning:

```csharp
Guid PreviousVersionId { get; set; }
Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }
IHolon Original { get; set; }  // Reference to original holon
```

### Audit Properties (from IAuditBase)

Holons inherit audit properties:

```csharp
Guid CreatedByAvatarId { get; set; }
DateTime CreatedDate { get; set; }
Guid ModifiedByAvatarId { get; set; }
DateTime ModifiedDate { get; set; }
Guid DeletedByAvatarId { get; set; }
DateTime DeletedDate { get; set; }
```

### Advanced Properties (from IHolon)

Higher-level holons can have additional properties:

```csharp
GlobalHolonData GlobalHolonData { get; set; }  // Global data across all OASIS instances
IList<INode> Nodes { get; set; }               // Connected nodes

// Parent relationships (for STAR system)
Guid ParentOmniverseId { get; set; }
Guid ParentMultiverseId { get; set; }
Guid ParentUniverseId { get; set; }
// ... more parent relationships
```

---

## Holon Types

Holons are categorized by `HolonType` enum:

- `Holon` - Generic holon
- `Avatar` - User identity
- `Provider` - Provider configuration
- `Quest` - Quest/mission
- `Mission` - Mission
- `Planet` - Planet (STAR system)
- `Star` - Star (STAR system)
- ... (many more types for STAR system)

---

## Provider Agnostic Design

### How Provider Agnosticism Works

1. **Universal Structure:** Same data model works with all providers
2. **Provider Keys:** Each provider stores holon with its own unique key
3. **Metadata:** Provider-specific metadata stored separately
4. **Abstraction:** Managers handle provider differences, not holons

### Example: Same Holon, Different Providers

```csharp
// Create a holon
var holon = new Holon
{
    Name = "My Data",
    Description = "Test data",
    MetaData = new Dictionary<string, object> { { "value", 123 } }
};

// Save to MongoDB - gets MongoDB ObjectId
await holonManager.SaveHolonAsync(holon);
// holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "507f1f77bcf86cd799439011"

// Save to Ethereum - gets transaction hash
await holonManager.SaveHolonAsync(holon, ProviderType.EthereumOASIS);
// holon.ProviderUniqueStorageKey[ProviderType.EthereumOASIS] = "0x1234..."

// Same holon object, different provider keys
```

---

## Holon Lifecycle

### 1. Creation

```csharp
var holon = new Holon
{
    Name = "My Holon",
    Description = "A new holon",
    HolonType = HolonType.Holon,
    IsActive = true,
    MetaData = new Dictionary<string, object>()
};
// IsNewHolon = true (automatically set)
```

### 2. Saving

```csharp
// Save to default provider
var result = await holonManager.SaveHolonAsync(holon);

// Save to specific provider
var result = await holonManager.SaveHolonAsync(holon, ProviderType.EthereumOASIS);

// After save:
// - IsNewHolon = false
// - ProviderUniqueStorageKey[providerType] = provider-specific key
// - InstanceSavedOnProviderType = providerType
// - CreatedProviderType = providerType (if first save)
```

### 3. Loading

```csharp
// Load by ID (tries providers in failover order)
var holon = await holonManager.LoadHolonAsync(holonId);

// Load by provider-specific key
var holon = await holonManager.LoadHolonAsync(
    providerKey, 
    ProviderType.MongoDBOASIS
);
```

### 4. Updating

```csharp
holon.Name = "Updated Name";
holon.IsChanged = true;  // Automatically set when properties change
await holonManager.SaveHolonAsync(holon);
```

### 5. Deleting

```csharp
await holonManager.DeleteHolonAsync(holonId);
// Sets DeletedDate, DeletedByAvatarId
```

---

## Metadata System

Holons have two types of metadata:

### 1. Global Metadata

```csharp
holon.MetaData["key"] = "value";
```

**Characteristics:**
- Stored on all providers
- Same across all provider instances
- Use for cross-provider data

### 2. Provider-Specific Metadata

```csharp
holon.ProviderMetaData[ProviderType.EthereumOASIS]["gasUsed"] = "21000";
holon.ProviderMetaData[ProviderType.SolanaOASIS]["slot"] = "12345";
```

**Characteristics:**
- Provider-specific information
- Only stored on that provider
- Use for provider-specific details

---

## Provider Key Management

### ProviderUniqueStorageKey Dictionary

Each provider uses different key formats:

| Provider | Key Format | Example |
|----------|------------|---------|
| MongoDB | ObjectId (string) | `"507f1f77bcf86cd799439011"` |
| Ethereum | Transaction Hash | `"0x1234..."` |
| Solana | Account Address | `"6rF4zzvuBgM5RgftahPQHuPfp9WmVLYkGn44CkbRijfv"` |
| IPFS | IPFS Hash | `"QmXoypizjW3WknFiJnKLwHCnL72vedxjQkDDP1mXWo6uco"` |

### Accessing Provider Keys

```csharp
// Get key for specific provider
if (holon.ProviderUniqueStorageKey.ContainsKey(ProviderType.MongoDBOASIS))
{
    var mongoKey = holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
}

// Get all provider keys
foreach (var kvp in holon.ProviderUniqueStorageKey)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

---

## GlobalHolonData

`GlobalHolonData` provides data that's shared across all OASIS instances (for ONET network):

```csharp
public class GlobalHolonData
{
    // Data shared across all OASIS nodes
    // Used for ONET network synchronization
}
```

---

## Holon Inheritance

### Class Hierarchy

```
IHolonBase (interface)
    ↓
HolonBase (abstract class)
    ↓
SemanticHolon (class)
    ↓
Holon (concrete class)
```

### Specialized Holons

- **Avatar** - Extends Holon for user identity
- **CelestialHolon** - For STAR system (planets, stars, etc.)
- **ProviderHolon** - For provider configuration

---

## Best Practices

### 1. Use Appropriate HolonType

```csharp
holon.HolonType = HolonType.Avatar;  // Don't use generic HolonType.Holon
```

### 2. Set Metadata Appropriately

```csharp
// Use global metadata for cross-provider data
holon.MetaData["importantValue"] = 123;

// Use provider metadata for provider-specific data
holon.ProviderMetaData[ProviderType.EthereumOASIS]["blockNumber"] = "15000000";
```

### 3. Handle Provider Keys

```csharp
// Always check if provider key exists before using
if (holon.ProviderUniqueStorageKey.ContainsKey(providerType))
{
    var key = holon.ProviderUniqueStorageKey[providerType];
    // Use key to load from specific provider
}
```

### 4. Use Descriptive Names

```csharp
holon.Name = "User Profile";  // Clear, descriptive name
holon.Description = "Stores user profile information";  // Helpful description
```

### 5. Set Active Flag

```csharp
holon.IsActive = true;  // Mark active holons appropriately
```

---

## Common Patterns

### Pattern 1: Create and Save

```csharp
var holon = new Holon
{
    Name = "My Data",
    Description = "Test holon",
    HolonType = HolonType.Holon,
    IsActive = true,
    MetaData = new Dictionary<string, object>
    {
        { "customField", "value" }
    }
};

var result = await holonManager.SaveHolonAsync(holon);
if (!result.IsError)
{
    Console.WriteLine($"Saved with ID: {result.Result.Id}");
}
```

### Pattern 2: Load and Update

```csharp
var result = await holonManager.LoadHolonAsync(holonId);
if (!result.IsError && result.Result != null)
{
    var holon = result.Result;
    holon.Description = "Updated description";
    await holonManager.SaveHolonAsync(holon);
}
```

### Pattern 3: Multi-Provider Access

```csharp
var holon = await holonManager.LoadHolonAsync(holonId);

// Check which providers have this holon
foreach (var providerKey in holon.ProviderUniqueStorageKey)
{
    Console.WriteLine($"Available on {providerKey.Key}: {providerKey.Value}");
}

// Load from specific provider
if (holon.ProviderUniqueStorageKey.ContainsKey(ProviderType.MongoDBOASIS))
{
    var mongoKey = holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
    var fromMongo = await holonManager.LoadHolonAsync(mongoKey, ProviderType.MongoDBOASIS);
}
```

---

## Relationship to Other Concepts

### Avatars

Avatars are specialized Holons:

```csharp
public class Avatar : Holon, IAvatar
{
    // Avatar-specific properties
    public string Username { get; set; }
    public string Email { get; set; }
    // ... more avatar properties
}
```

### Managers

Managers operate on Holons:

- **HolonManager** - CRUD operations on holons
- **AvatarManager** - Avatar-specific operations (extends HolonManager)
- **SearchManager** - Search across holons

### Providers

Providers store Holons:

- Each provider implements `SaveHolonAsync()`, `LoadHolonAsync()`, etc.
- Providers convert holons to provider-specific format
- Providers return holons in OASIS format

---

## Related Documentation

- [Providers Concept](../concepts/PROVIDERS.md) - How providers store holons
- [Managers Concept](../concepts/MANAGERS.md) - How managers work with holons
- [Avatars Concept](../concepts/AVATARS.md) - Avatars as specialized holons
- [HyperDrive Concept](../concepts/HYPERDRIVE.md) - How HyperDrive routes holon operations

---

## Source Code References

**Core Classes:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Holons/IHolonBase.cs`
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/HolonBase.cs`
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/Holon.cs`
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Holons/IHolon.cs`

**Manager:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/HolonManager/HolonManager.cs`

