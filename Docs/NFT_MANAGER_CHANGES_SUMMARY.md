# NFT Manager Changes Summary: max-build4 vs master

**Date:** January 2026  
**Branch:** max-build4 vs master

---

## Overview

Major NFT functionality has been added to support **Agent-to-Agent (A2A) protocol** and **Agent ownership via NFTs**. The changes span across multiple layers:

1. **API.Core** - Core agent NFT interfaces and basic methods
2. **ONODE.Core** - Extension methods with full NFTManager integration
3. **ONODE.WebAPI** - REST API endpoints for NFT operations

---

## Key Files Changed

### 1. `AgentManager-NFT.cs` (NEW - 304 lines)
**Location:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AgentManager/`

**Purpose:** Core agent NFT management methods (without NFTManager dependency)

**Key Methods:**
- `MintAgentNFTAsync()` - **Note:** Returns error indicating it must be called from ONODE.WebAPI
- `LinkNFTToAgentAsync()` - Links existing NFT to agent metadata
- `GetAgentNFTIdAsync()` - Retrieves NFT ID from agent metadata
- `SyncAgentOwnershipFromNFTAsync()` - **Note:** Placeholder, requires NFTManager
- `GetAgentOwnerWithNFTCheckAsync()` - Gets owner with NFT ownership check

**Important Notes:**
- Methods that require `NFTManager` are placeholders
- Actual implementation is in `ONODE.Core` extension methods
- This separation avoids circular dependencies (API.Core can't reference ONODE.Core)

---

### 2. `A2AManager-NFT.cs` (NEW - 29 lines)
**Location:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/A2AManager/`

**Purpose:** Placeholder file noting that NFT methods are implemented as extension methods

**Key Points:**
- All NFT functionality is in `ONODE.Core` extension methods
- See: `ONODE.Core/Managers/A2AManager/A2AManagerExtensions-NFT.cs`

---

### 3. `A2AManagerExtensions-NFT.cs` (NEW - 587 lines)
**Location:** `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/A2AManager/`

**Purpose:** Full NFT functionality using NFTManager (requires ONODE.Core)

**Key Methods:**

#### Reputation & Certificates
- `CreateAgentReputationNFTAsync()` - Creates reputation NFT for agent
- `CreateServiceCompletionCertificateAsync()` - Creates service completion certificate NFT
- `CreateAchievementBadgeAsync()` - Creates achievement badge NFT

#### Agent Ownership NFTs
- `MintAgentOwnershipNFTAsync()` - **Main method** for minting agent ownership NFT
  - Validates agent exists and is Agent type
  - Checks agent doesn't already have NFT
  - Gets current owner from agent metadata
  - Retrieves Agent Card for NFT metadata
  - Creates `MintWeb4NFTRequest` with agent info
  - Mints NFT using `NFTManager.MintNftAsync()`
  - Links NFT to agent via `AgentManager.LinkNFTToAgentAsync()`
  - Sets `SendToAvatarAfterMintingId` to current owner

#### Ownership Synchronization
- `SyncAgentOwnershipFromNFTAsync()` - **Critical method** for syncing ownership
  - Loads NFT to get `CurrentOwnerAvatarId` and `PreviousOwnerAvatarId`
  - Updates agent metadata:
    - `OwnerAvatarId` = new owner (REPLACES old owner)
    - `PreviousOwnerAvatarId` = previous owner (PRESERVES old owner)
    - `LastNFTTransferDate` = current timestamp
  - Saves agent with updated ownership

- `CheckAndSyncAgentOwnershipFromNFTAsync()` - Auto-sync helper
  - Checks if NFT represents an agent (checks metadata for `AgentId`)
  - If yes, calls `SyncAgentOwnershipFromNFTAsync()`
  - **Use this after NFT transfers** to automatically update agent ownership

**NFTManager Usage:**
```csharp
// All methods use NFTManager from ONODE.Core
var nftManager = new NFTManager(avatarId, ProviderManager.Instance.OASISDNA);
var nftResult = await nftManager.MintNftAsync(mintRequest);
var nftResult = await nftManager.LoadWeb4NftAsync(nftId);
```

---

### 4. `A2AController.cs` (NEW - 1,789 lines)
**Location:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/`

**Purpose:** REST API endpoints for A2A protocol and NFT operations

**NFT-Related Endpoints:**

#### Reputation & Certificates
- `POST /api/a2a/nft/reputation` - Create reputation NFT
- `POST /api/a2a/nft/service-certificate` - Create service certificate NFT

#### Agent Ownership NFTs
- `POST /api/a2a/agent/{agentId}/mint-nft` - **Main endpoint** for minting agent ownership NFT
  - Requires authentication
  - Verifies caller owns the agent
  - Calls `A2AManager.Instance.MintAgentOwnershipNFTAsync()`
  - Returns minted NFT details

- `GET /api/a2a/agent/{agentId}/nft` - Get NFT for agent
  - Public endpoint (no auth required)
  - Gets NFT ID from agent metadata
  - Loads NFT using `NFTManager.LoadWeb4NftAsync()`
  - Returns full NFT details

**Request Model:**
```csharp
public class MintAgentNFTRequest
{
    public ProviderType? OnChainProvider { get; set; }      // Default: SolanaOASIS
    public ProviderType? OffChainProvider { get; set; }     // Default: MongoDBOASIS
    public string Title { get; set; }                        // Default: "{AgentName} NFT"
    public string Description { get; set; }                  // Default: agent description
    public string ImageUrl { get; set; }
    public decimal? Price { get; set; }                      // Default: 0
    public string Symbol { get; set; }                       // Default: "AGENTNFT"
    public Dictionary<string, object> AdditionalMetadata { get; set; }
}
```

---

### 5. `NftController.cs` (MODIFIED - 8 lines)
**Location:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/`

**Changes:**
- Updated `SearchWeb4NFTsAsync()` to include `MetaKeyValuePairMatchMode` parameter
- Updated `SearchWeb4GeoNFTsAsync()` to include `MetaKeyValuePairMatchMode` parameter
- Updated `SearchWeb4NFTCollectionsAsync()` to include `MetaKeyValuePairMatchMode` parameter

**Before:**
```csharp
return await NFTManager.SearchWeb4NFTsAsync(searchTerm, avatarId, searchOnlyForCurrentAvatar, providerType);
```

**After:**
```csharp
return await NFTManager.SearchWeb4NFTsAsync(searchTerm, avatarId, null, MetaKeyValuePairMatchMode.All, searchOnlyForCurrentAvatar, providerType);
```

---

## NFT Metadata Structure

### Agent Ownership NFT Metadata
```json
{
  "AgentId": "guid-of-agent",
  "AgentType": "Agent",
  "AgentCard": "{serialized AgentCard JSON}",
  "AgentVersion": "1.0.0",
  "AgentStatus": "Available",
  ...additionalMetadata
}
```

### Agent Metadata (after NFT linking)
```json
{
  "NFTId": "guid-of-nft",
  "NFTMintAddress": "solana-mint-address",
  "NFTMintTransactionHash": "transaction-hash",
  "NFTOnChainProvider": "SolanaOASIS",
  "NFTLinkedDate": "2026-01-11T12:00:00Z",
  "OwnerAvatarId": "guid-of-current-owner",
  "PreviousOwnerAvatarId": "guid-of-previous-owner",
  "LastNFTTransferDate": "2026-01-11T12:00:00Z"
}
```

---

## Ownership Flow

### 1. Minting Agent Ownership NFT
```
1. Agent must exist and be Agent type
2. Agent must have owner (OwnerAvatarId in metadata)
3. Agent must NOT already have NFT
4. Mint NFT with agent metadata
5. Link NFT to agent (store NFTId in agent metadata)
6. Send NFT to current owner
```

### 2. NFT Transfer → Agent Ownership Sync
```
1. NFT is transferred (ownership changes on blockchain)
2. NFT.CurrentOwnerAvatarId = new owner
3. NFT.PreviousOwnerAvatarId = old owner
4. Call SyncAgentOwnershipFromNFTAsync(agentId, nftId)
   - Updates agent.OwnerAvatarId = NFT.CurrentOwnerAvatarId
   - Updates agent.PreviousOwnerAvatarId = NFT.PreviousOwnerAvatarId
   - Saves agent
```

### 3. Auto-Sync After Transfer
```
1. After any NFT transfer, call CheckAndSyncAgentOwnershipFromNFTAsync(nftId)
2. Checks if NFT.MetaData contains "AgentId"
3. If yes, automatically syncs agent ownership
```

---

## Key Implementation Details

### NFTManager Integration
- All NFT operations use `NFTManager` from `ONODE.Core`
- `NFTManager` requires `OASISDNA` for provider configuration
- Created via: `new NFTManager(avatarId, ProviderManager.Instance.OASISDNA)`

### Default NFT Settings
- **OnChainProvider:** `SolanaOASIS`
- **OffChainProvider:** `MongoDBOASIS`
- **NFTStandardType:** `ERC1155` (for ownership NFTs)
- **StoreNFTMetaDataOnChain:** `false` (stored off-chain)
- **NFTOffChainMetaType:** `OASIS`
- **WaitTillNFTMinted:** `true`
- **WaitForNFTToMintInSeconds:** `60`

### Ownership Preservation
- **OwnerAvatarId** is REPLACED when NFT transfers
- **PreviousOwnerAvatarId** is PRESERVED (keeps history)
- **LastNFTTransferDate** tracks when ownership changed

---

## API Endpoints Summary

| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/api/a2a/agent/{agentId}/mint-nft` | POST | Yes | Mint ownership NFT for agent |
| `/api/a2a/agent/{agentId}/nft` | GET | No | Get NFT for agent |
| `/api/a2a/nft/reputation` | POST | Yes | Create reputation NFT |
| `/api/a2a/nft/service-certificate` | POST | Yes | Create service certificate NFT |

---

## Dependencies

### Required
- `NFTManager` (from ONODE.Core)
- `AgentManager` (from API.Core)
- `AvatarManager` (from API.Core)
- `ProviderManager` (for OASISDNA)

### Architecture
- **API.Core** - Core interfaces and basic methods (no NFTManager)
- **ONODE.Core** - Extension methods with full NFTManager integration
- **ONODE.WebAPI** - REST API endpoints

This separation avoids circular dependencies while providing full functionality where NFTManager is available.

---

## Testing Considerations

1. **Agent must exist** and be `AvatarType.Agent`
2. **Agent must have owner** before minting NFT
3. **Agent must NOT already have NFT** (check `NFTId` in metadata)
4. **Provider must be registered/activated** (SolanaOASIS, MongoDBOASIS)
5. **Authentication required** for minting endpoints
6. **Ownership verification** required (caller must own agent)

---

**Status:** ✅ Analysis Complete  
**Last Updated:** January 2026
