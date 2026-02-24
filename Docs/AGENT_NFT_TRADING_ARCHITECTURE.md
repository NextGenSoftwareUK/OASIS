# Agent-to-NFT Trading Architecture

## Overview

This document outlines the architecture for attaching agents to NFTs, enabling agents to be traded as NFTs on blockchain marketplaces.

## Core Concept

**An NFT represents ownership of an agent.** When you own the NFT, you own the agent. Transferring the NFT transfers agent ownership.

## Architecture Design

### 1. Bidirectional Linking

**Agent → NFT:**
- Store `NFTId` (or `NFTMintAddress`) in agent's `MetaData["NFTId"]`
- Store `NFTMintTransactionHash` in agent's `MetaData["NFTMintTransactionHash"]`
- Store `NFTOnChainProvider` in agent's `MetaData["NFTOnChainProvider"]`

**NFT → Agent:**
- Store `AgentId` in NFT's `MetaData["AgentId"]`
- Store `AgentCard` JSON in NFT's `MetaData["AgentCard"]` (optional, for marketplace display)
- Store `AgentType` in NFT's `MetaData["AgentType"]` = "Agent"

### 2. Ownership Synchronization

**Current State:**
- Agents have `OwnerAvatarId` in metadata (from agent-to-user linking)
- NFTs have `CurrentOwnerAvatarId` and `PreviousOwnerAvatarId`

**Synchronization Strategy:**
- When NFT is minted for agent: Set `NFT.CurrentOwnerAvatarId` = agent's current owner
- When NFT is transferred: Update agent's `OwnerAvatarId` to match `NFT.CurrentOwnerAvatarId`
- When agent ownership changes (via link/unlink): Update NFT's `CurrentOwnerAvatarId` (if NFT exists)

### 3. Minting Flow

```
1. User creates/owns agent
2. User mints NFT with agent metadata:
   - MetaData["AgentId"] = agent.Id
   - MetaData["AgentCard"] = JSON.stringify(agentCard)
   - SendToAvatarAfterMintingId = current owner's avatar ID
3. After minting:
   - Store NFT.Id in agent.MetaData["NFTId"]
   - Store NFT mint transaction hash in agent.MetaData["NFTMintTransactionHash"]
   - Set NFT.CurrentOwnerAvatarId = agent owner
   - Set agent.MetaData["NFTLinkedDate"] = DateTime.UtcNow
```

### 4. Transfer Flow

```
1. NFT is transferred on-chain (via marketplace or direct transfer)
2. NFT.CurrentOwnerAvatarId is updated by the system
3. System detects transfer (via event listener or polling):
   - Load agent by NFT.MetaData["AgentId"]
   - Update agent.MetaData["OwnerAvatarId"] = NFT.CurrentOwnerAvatarId
   - Update agent.MetaData["PreviousOwnerAvatarId"] = NFT.PreviousOwnerAvatarId
   - Update agent.MetaData["LastNFTTransferDate"] = DateTime.UtcNow
   - Clear agent.MetaData["OwnerLinkedDate"] (since ownership now comes from NFT)
```

### 5. Verification Flow

**Before allowing agent operations:**
```
1. Check if agent has NFTId in metadata
2. If yes:
   - Load NFT by NFTId
   - Verify NFT.CurrentOwnerAvatarId matches requesting avatar
   - If match: Allow operation
   - If no match: Deny (agent ownership is controlled by NFT)
3. If no NFT:
   - Fall back to agent.MetaData["OwnerAvatarId"] (traditional ownership)
```

## Data Structures

### Agent Metadata (when linked to NFT)
```json
{
  "NFTId": "uuid-of-nft",
  "NFTMintAddress": "solana-mint-address",
  "NFTMintTransactionHash": "transaction-hash",
  "NFTOnChainProvider": "SolanaOASIS",
  "NFTLinkedDate": "2025-01-15T10:00:00Z",
  "LastNFTTransferDate": "2025-01-20T15:30:00Z",
  "OwnerAvatarId": "uuid-of-current-owner",  // Synced from NFT
  "PreviousOwnerAvatarId": "uuid-of-previous-owner"
}
```

### NFT Metadata (when representing agent)
```json
{
  "AgentId": "uuid-of-agent",
  "AgentType": "Agent",
  "AgentCard": {
    "agentId": "uuid-of-agent",
    "name": "Agent Name",
    "capabilities": {
      "services": ["data-analysis", "report-generation"],
      "skills": ["Python", "Machine Learning"]
    },
    "connection": {
      "endpoint": "https://api.oasisweb4.com/api/a2a/jsonrpc",
      "protocol": "jsonrpc2.0"
    }
  },
  "AgentVersion": "1.0.0",
  "AgentStatus": "Available"
}
```

## Implementation Components

### 1. Mint Agent NFT Endpoint
**POST `/api/a2a/agent/{agentId}/mint-nft`**
- Validates agent exists and caller owns it
- Creates NFT with agent metadata
- Links NFT to agent
- Returns NFT details

### 2. Get Agent NFT Endpoint
**GET `/api/a2a/agent/{agentId}/nft`**
- Returns NFT information if agent is linked to NFT
- Returns null if agent is not NFT-backed

### 3. NFT Transfer Event Handler
- Listens to NFT transfer events (or polls periodically)
- Updates agent ownership when NFT transfers
- Maintains ownership history

### 4. Agent Ownership Verification
- Enhanced `GetAgentOwnerAsync` to check NFT ownership
- Enhanced authorization checks to verify NFT ownership

## Benefits

1. **Tradable Agents**: Agents can be bought/sold on NFT marketplaces
2. **Blockchain Ownership**: Ownership is verifiable on-chain
3. **Marketplace Integration**: Standard NFT marketplaces can list agents
4. **Transfer History**: Complete ownership history via NFT transfers
5. **Royalties**: NFT royalties can be configured for agent sales

## Considerations

1. **Dual Ownership Model**: 
   - NFT-backed agents: Ownership from NFT
   - Traditional agents: Ownership from metadata
   - Need clear precedence rules

2. **Transfer Detection**:
   - Real-time: Event listeners (requires infrastructure)
   - Polling: Periodic checks (simpler but delayed)
   - Hybrid: Event listeners with polling fallback

3. **Agent Operations During Transfer**:
   - Should agent be locked during transfer?
   - How to handle in-flight operations?

4. **Unlinking**:
   - Can agent be unlinked from NFT?
   - What happens to NFT? (burn, transfer, etc.)

5. **Multiple NFTs**:
   - Can one agent have multiple NFTs? (probably not)
   - Can one NFT represent multiple agents? (probably not)

## Example Use Cases

1. **Agent Marketplace**:
   - User mints NFT for their agent
   - Lists NFT on marketplace (OpenSea, Magic Eden, etc.)
   - Buyer purchases NFT
   - Agent ownership automatically transfers to buyer

2. **Agent Rental/Leasing**:
   - NFT owner can grant temporary access
   - Access controlled by NFT ownership
   - Time-limited permissions

3. **Agent Staking**:
   - Stake NFT to earn rewards
   - Agent continues operating while staked
   - Rewards distributed to NFT owner

## Next Steps

1. ✅ Implement `MintAgentNFT` endpoint - **COMPLETED**
2. ✅ Implement NFT transfer sync logic - **COMPLETED**
3. ✅ Update agent ownership verification logic - **COMPLETED**
4. ⏳ Add NFT transfer hook to automatically sync ownership
5. ⏳ Create marketplace integration examples

## Implementation Status

### ✅ Completed
- `AgentManager-NFT.cs` - Core NFT linking methods
- `A2AManagerNFTExtensions.MintAgentOwnershipNFTAsync` - Mint NFT for agent
- `A2AManagerNFTExtensions.SyncAgentOwnershipFromNFTAsync` - Sync ownership after transfer
- `A2AManagerNFTExtensions.CheckAndSyncAgentOwnershipFromNFTAsync` - Auto-detect and sync
- `POST /api/a2a/agent/{agentId}/mint-nft` - API endpoint to mint agent NFT
- `GET /api/a2a/agent/{agentId}/nft` - API endpoint to get agent NFT
- Updated `GetAgentOwnerAsync` to support NFT-backed agents

### ⏳ Pending
- Hook into `NFTManager.SendNFTAsync` to automatically call `CheckAndSyncAgentOwnershipFromNFTAsync` after transfers
- Background job to periodically check NFT ownership and sync agent ownership
- Marketplace integration examples

## Usage

### Minting an Agent NFT
```http
POST /api/a2a/agent/{agentId}/mint-nft
Authorization: Bearer {token}

{
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "MongoDBOASIS",
  "title": "My Agent NFT",
  "description": "NFT representing ownership of my agent",
  "price": 100.0,
  "symbol": "AGENTNFT"
}
```

### Getting Agent NFT
```http
GET /api/a2a/agent/{agentId}/nft
```

### Syncing Ownership After NFT Transfer
After an NFT is transferred (on-chain), call:
```csharp
await A2AManager.Instance.CheckAndSyncAgentOwnershipFromNFTAsync(nftId);
```

This will:
1. Load the NFT
2. Check if it represents an agent (via metadata)
3. Sync the agent's ownership to match the NFT's current owner

### Automatic Sync Hook
To automatically sync ownership when NFTs are transferred, add this to `NFTManager.SendNFTAsync` after a successful transfer:

```csharp
// After successful NFT transfer
if (sendResult != null && sendResult.Result != null && !sendResult.IsError)
{
    // ... existing code ...
    
    // Check if this NFT represents an agent and sync ownership
    var nftId = /* get NFT ID from request or result */;
    await A2AManager.Instance.CheckAndSyncAgentOwnershipFromNFTAsync(nftId);
}
```
