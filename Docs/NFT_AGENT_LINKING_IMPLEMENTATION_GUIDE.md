# NFT-Agent Linking Service - Implementation & Testing Guide

**Last Updated:** January 2026  
**Status:** ✅ Implementation Complete - Ready for Testing

---

## Executive Summary

This document provides a complete guide to the NFT-Agent linking service that enables agents to be traded as NFTs. The system creates bidirectional links between agents and NFTs, allowing agent ownership to be managed through NFT transfers on blockchain marketplaces.

**Core Concept:** An NFT represents ownership of an agent. When you own the NFT, you own the agent. Transferring the NFT transfers agent ownership.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Implementation Status](#implementation-status)
3. [API Endpoints](#api-endpoints)
4. [Data Structures](#data-structures)
5. [Workflows](#workflows)
6. [Testing Guide](#testing-guide)
7. [Known Issues & Limitations](#known-issues--limitations)
8. [Future Enhancements](#future-enhancements)

---

## Architecture Overview

### Bidirectional Linking

**Agent → NFT:**
- Agent stores `NFTId` in `MetaData["NFTId"]`
- Agent stores `NFTMintAddress` in `MetaData["NFTMintAddress"]`
- Agent stores `NFTMintTransactionHash` in `MetaData["NFTMintTransactionHash"]`
- Agent stores `NFTOnChainProvider` in `MetaData["NFTOnChainProvider"]`
- Agent stores `NFTLinkedDate` in `MetaData["NFTLinkedDate"]`

**NFT → Agent:**
- NFT stores `AgentId` in `MetaData["AgentId"]`
- NFT stores `AgentCard` (JSON) in `MetaData["AgentCard"]` (for marketplace display)
- NFT stores `AgentType` = "Agent" in `MetaData["AgentType"]`
- NFT stores `AgentVersion` in `MetaData["AgentVersion"]`
- NFT stores `AgentStatus` in `MetaData["AgentStatus"]`

### Ownership Synchronization

**Two Ownership Models:**

1. **NFT-Backed Agents:**
   - Ownership comes from NFT's `CurrentOwnerAvatarId`
   - Agent's `OwnerAvatarId` is synced from NFT after transfers
   - NFT ownership is the source of truth

2. **Traditional Agents:**
   - Ownership comes from agent's `MetaData["OwnerAvatarId"]`
   - No NFT linked

**Precedence Rule:**
- If agent has `NFTId` in metadata → NFT ownership is authoritative
- If agent has no `NFTId` → Metadata ownership is authoritative

---

## Implementation Status

### ✅ Completed Components

#### 1. Core Methods (`AgentManager-NFT.cs`)

**Location:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AgentManager/AgentManager-NFT.cs`

**Methods:**
- ✅ `MintAgentNFTAsync()` - Placeholder (must be called from ONODE.WebAPI)
- ✅ `LinkNFTToAgentAsync()` - Links existing NFT to agent
- ✅ `GetAgentNFTIdAsync()` - Retrieves NFT ID from agent metadata
- ✅ `SyncAgentOwnershipFromNFTAsync()` - Placeholder (must be called from ONODE.WebAPI)
- ✅ `GetAgentOwnerWithNFTCheckAsync()` - Placeholder (must be called from ONODE.WebAPI)

#### 2. Extension Methods (`A2AManagerExtensions-NFT.cs`)

**Location:** `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/A2AManager/A2AManagerExtensions-NFT.cs`

**Methods:**
- ✅ `MintAgentOwnershipNFTAsync()` - **FULL IMPLEMENTATION**
  - Validates agent exists
  - Checks for existing NFT
  - Gets agent owner
  - Retrieves Agent Card
  - Builds NFT metadata with agent info
  - Mints NFT via NFTManager
  - Links NFT to agent
  - Returns minted NFT

- ✅ `SyncAgentOwnershipFromNFTAsync()` - **FULL IMPLEMENTATION**
  - Loads NFT by ID
  - Extracts `CurrentOwnerAvatarId` and `PreviousOwnerAvatarId`
  - Loads agent
  - Updates agent's `OwnerAvatarId` from NFT
  - Updates agent's `PreviousOwnerAvatarId` from NFT
  - Updates agent's `LastNFTTransferDate`
  - Saves agent

- ✅ `CheckAndSyncAgentOwnershipFromNFTAsync()` - **FULL IMPLEMENTATION**
  - Loads NFT by ID
  - Checks if NFT represents an agent (via `AgentId` in metadata)
  - If yes, calls `SyncAgentOwnershipFromNFTAsync()`
  - Returns success/failure

#### 3. API Endpoints (`A2AController.cs`)

**Location:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/A2AController.cs`

**Endpoints:**
- ✅ `POST /api/a2a/agent/{agentId}/mint-nft` - Mint NFT for agent
  - Authentication required
  - Verifies caller owns agent
  - Calls `A2AManager.Instance.MintAgentOwnershipNFTAsync()`
  - Returns minted NFT details

- ✅ `GET /api/a2a/agent/{agentId}/nft` - Get agent's NFT
  - Public endpoint (no auth required)
  - Gets NFT ID from agent metadata
  - Loads full NFT details
  - Returns NFT or null

#### 4. Ownership Verification (`AgentManager-Ownership.cs`)

**Location:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AgentManager/AgentManager-Ownership.cs`

**Methods:**
- ✅ `GetAgentOwnerAsync()` - Updated to acknowledge NFT-backed agents
  - Checks if agent has `NFTId` in metadata
  - Notes that NFT ownership should be authoritative (synced to metadata)
  - Falls back to metadata `OwnerAvatarId`

### ⏳ Pending Components

1. **Automatic Sync Hook:**
   - Need to hook into `NFTManager.SendNFTAsync()` to automatically call `CheckAndSyncAgentOwnershipFromNFTAsync()` after NFT transfers
   - Currently requires manual call after transfers

2. **Background Sync Job:**
   - Periodic job to check NFT ownership and sync agent ownership
   - Useful for catching transfers that happened outside OASIS API

3. **Marketplace Integration Examples:**
   - Examples for OpenSea, Magic Eden, etc.
   - Webhook handlers for marketplace events

---

## API Endpoints

### 1. Mint Agent NFT

**Endpoint:** `POST /api/a2a/agent/{agentId}/mint-nft`

**Authentication:** Required (Bearer token)

**Request Body:**
```json
{
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "MongoDBOASIS",
  "title": "My Agent NFT",
  "description": "NFT representing ownership of my agent",
  "imageUrl": "https://example.com/agent-image.png",
  "price": 100.0,
  "symbol": "AGENTNFT",
  "additionalMetadata": {
    "custom_field": "custom_value"
  }
}
```

**Response (Success):**
```json
{
  "success": true,
  "nft": {
    "id": "uuid-of-nft",
    "title": "My Agent NFT",
    "description": "NFT representing ownership of my agent",
    "currentOwnerAvatarId": "uuid-of-owner",
    "metaData": {
      "AgentId": "uuid-of-agent",
      "AgentType": "Agent",
      "AgentCard": "{...}",
      "AgentVersion": "1.0.0",
      "AgentStatus": "Available"
    },
    "web3NFTs": [
      {
        "nftMintedUsingWalletAddress": "solana-wallet-address",
        "mintTransactionHash": "transaction-hash"
      }
    ]
  },
  "message": "Agent ownership NFT minted and linked successfully. NFT ID: uuid-of-nft"
}
```

**Response (Error):**
```json
{
  "error": "Agent {agentId} already has an NFT linked. Use GetAgentNFTAsync to retrieve it."
}
```

**Prerequisites:**
- Agent must exist
- Agent must be owned by caller (verified via `GetAgentOwnerAsync()`)
- Agent must not already have an NFT linked
- Agent must have an owner (`OwnerAvatarId` in metadata)

### 2. Get Agent NFT

**Endpoint:** `GET /api/a2a/agent/{agentId}/nft`

**Authentication:** Not required (Public endpoint)

**Response (Success - NFT exists):**
```json
{
  "nft": {
    "id": "uuid-of-nft",
    "title": "My Agent NFT",
    "currentOwnerAvatarId": "uuid-of-current-owner",
    "previousOwnerAvatarId": "uuid-of-previous-owner",
    "metaData": {
      "AgentId": "uuid-of-agent",
      "AgentType": "Agent",
      "AgentCard": "{...}"
    }
  },
  "message": "NFT retrieved successfully"
}
```

**Response (Success - No NFT):**
```json
{
  "nft": null,
  "message": "Agent has no NFT linked"
}
```

**Response (Error):**
```json
{
  "error": "Agent {agentId} not found"
}
```

### 3. Sync Agent Ownership from NFT (Manual)

**Note:** This is not an API endpoint, but a method that can be called programmatically.

**Method:** `A2AManager.Instance.CheckAndSyncAgentOwnershipFromNFTAsync(nftId)`

**Usage:**
```csharp
var result = await A2AManager.Instance.CheckAndSyncAgentOwnershipFromNFTAsync(nftId);
if (result.IsError)
{
    // Handle error
}
```

**What It Does:**
1. Loads NFT by ID
2. Checks if NFT represents an agent (via `AgentId` in metadata)
3. If yes, syncs agent ownership from NFT ownership
4. Updates agent's `OwnerAvatarId`, `PreviousOwnerAvatarId`, `LastNFTTransferDate`

---

## Data Structures

### Agent Metadata (When Linked to NFT)

```json
{
  "NFTId": "uuid-of-nft",
  "NFTMintAddress": "solana-mint-address",
  "NFTMintTransactionHash": "transaction-hash",
  "NFTOnChainProvider": "SolanaOASIS",
  "NFTLinkedDate": "2025-01-15T10:00:00Z",
  "LastNFTTransferDate": "2025-01-20T15:30:00Z",
  "OwnerAvatarId": "uuid-of-current-owner",
  "PreviousOwnerAvatarId": "uuid-of-previous-owner"
}
```

### NFT Metadata (When Representing Agent)

```json
{
  "AgentId": "uuid-of-agent",
  "AgentType": "Agent",
  "AgentCard": {
    "agentId": "uuid-of-agent",
    "name": "Agent Name",
    "version": "1.0.0",
    "capabilities": {
      "services": ["data-analysis", "report-generation"],
      "skills": ["Python", "Machine Learning"],
      "status": "Available"
    },
    "connection": {
      "endpoint": "https://api.oasisplatform.world/api/a2a/jsonrpc",
      "protocol": "A2A_JSON-RPC_2.0"
    }
  },
  "AgentVersion": "1.0.0",
  "AgentStatus": "Available"
}
```

---

## Workflows

### Workflow 1: Mint NFT for Agent

```
1. User creates/owns agent
   - Agent has OwnerAvatarId in metadata
   
2. User calls POST /api/a2a/agent/{agentId}/mint-nft
   - API verifies user owns agent
   - API calls A2AManager.Instance.MintAgentOwnershipNFTAsync()
   
3. MintAgentOwnershipNFTAsync():
   - Validates agent exists and is Agent type
   - Checks agent doesn't already have NFT
   - Gets agent owner from metadata
   - Gets Agent Card
   - Builds NFT metadata with AgentId, AgentCard, etc.
   - Creates MintWeb4NFTRequest
   - Sets SendToAvatarAfterMintingId = agent owner
   - Calls NFTManager.MintNftAsync()
   
4. After minting:
   - NFT is created with agent metadata
   - NFT.CurrentOwnerAvatarId = agent owner
   - NFT is sent to agent owner
   
5. Link NFT to agent:
   - Calls AgentManager.Instance.LinkNFTToAgentAsync()
   - Stores NFTId, NFTMintAddress, NFTMintTransactionHash in agent metadata
   - Stores NFTLinkedDate
   
6. Result:
   - Agent has NFTId in metadata
   - NFT has AgentId in metadata
   - Bidirectional link established
```

### Workflow 2: NFT Transfer (Manual Sync)

```
1. NFT is transferred on-chain
   - Via marketplace (OpenSea, Magic Eden, etc.)
   - Via direct transfer
   - NFT.CurrentOwnerAvatarId is updated by blockchain
   
2. System detects transfer (manual or automatic):
   - Call A2AManager.Instance.CheckAndSyncAgentOwnershipFromNFTAsync(nftId)
   
3. CheckAndSyncAgentOwnershipFromNFTAsync():
   - Loads NFT
   - Checks if NFT.MetaData contains "AgentId"
   - If yes, extracts AgentId
   - Calls SyncAgentOwnershipFromNFTAsync(agentId, nftId)
   
4. SyncAgentOwnershipFromNFTAsync():
   - Loads NFT to get CurrentOwnerAvatarId and PreviousOwnerAvatarId
   - Loads agent
   - Updates agent.MetaData["OwnerAvatarId"] = NFT.CurrentOwnerAvatarId
   - Updates agent.MetaData["PreviousOwnerAvatarId"] = NFT.PreviousOwnerAvatarId
   - Updates agent.MetaData["LastNFTTransferDate"] = DateTime.UtcNow
   - Saves agent
   
5. Result:
   - Agent ownership synced with NFT ownership
   - New owner can now control agent
```

### Workflow 3: Get Agent Owner (NFT-Backed)

```
1. Call AgentManager.Instance.GetAgentOwnerAsync(agentId)
   
2. GetAgentOwnerAsync():
   - Loads agent
   - Checks if agent.MetaData contains "NFTId"
   
3. If NFT-backed:
   - Notes that ownership should come from NFT
   - Falls back to agent.MetaData["OwnerAvatarId"] (which should be synced)
   - Returns owner ID
   
4. If not NFT-backed:
   - Returns agent.MetaData["OwnerAvatarId"]
   
5. Result:
   - Returns current owner ID
```

---

## Testing Guide

### Test 1: Mint NFT for Agent

**Prerequisites:**
- Agent exists
- Agent is owned by authenticated user
- Agent has no existing NFT

**Steps:**
1. Authenticate and get JWT token
2. Create or get an agent ID
3. Call `POST /api/a2a/agent/{agentId}/mint-nft` with request body
4. Verify response contains NFT details
5. Verify agent metadata contains `NFTId`
6. Call `GET /api/a2a/agent/{agentId}/nft` to verify NFT is linked

**Expected Results:**
- ✅ NFT is minted successfully
- ✅ Agent metadata contains `NFTId`, `NFTMintAddress`, `NFTMintTransactionHash`
- ✅ NFT metadata contains `AgentId`, `AgentCard`, `AgentType`
- ✅ NFT `CurrentOwnerAvatarId` matches agent owner

**Test Cases:**
- ✅ Mint NFT for agent you own
- ❌ Mint NFT for agent you don't own (should fail)
- ❌ Mint NFT for agent that already has NFT (should fail)
- ❌ Mint NFT for agent with no owner (should fail)

### Test 2: Get Agent NFT

**Prerequisites:**
- Agent exists (may or may not have NFT)

**Steps:**
1. Call `GET /api/a2a/agent/{agentId}/nft`
2. Verify response

**Expected Results:**
- ✅ If agent has NFT: Returns NFT details
- ✅ If agent has no NFT: Returns `{"nft": null}`

**Test Cases:**
- ✅ Get NFT for agent with NFT linked
- ✅ Get NFT for agent without NFT (should return null)
- ❌ Get NFT for non-existent agent (should return 404)

### Test 3: Sync Ownership After NFT Transfer

**Prerequisites:**
- Agent has NFT linked
- NFT has been transferred (CurrentOwnerAvatarId changed)

**Steps:**
1. Transfer NFT on-chain (via marketplace or direct transfer)
2. Wait for blockchain confirmation
3. Call `A2AManager.Instance.CheckAndSyncAgentOwnershipFromNFTAsync(nftId)` programmatically
4. Verify agent ownership updated

**Expected Results:**
- ✅ Agent `OwnerAvatarId` matches NFT `CurrentOwnerAvatarId`
- ✅ Agent `PreviousOwnerAvatarId` matches NFT `PreviousOwnerAvatarId`
- ✅ Agent `LastNFTTransferDate` is updated

**Test Cases:**
- ✅ Sync ownership for NFT-backed agent
- ✅ Sync ownership for NFT that doesn't represent agent (should return false)
- ❌ Sync ownership for non-existent NFT (should fail)

### Test 4: Ownership Verification

**Prerequisites:**
- Agent exists (may or may not have NFT)

**Steps:**
1. Call `AgentManager.Instance.GetAgentOwnerAsync(agentId)`
2. Verify returned owner ID

**Expected Results:**
- ✅ Returns correct owner ID
- ✅ For NFT-backed agents: Returns owner from metadata (synced from NFT)
- ✅ For traditional agents: Returns owner from metadata

**Test Cases:**
- ✅ Get owner for NFT-backed agent
- ✅ Get owner for traditional agent
- ✅ Get owner for agent with no owner (should return null)

### Test 5: End-to-End Flow

**Prerequisites:**
- User account with agent
- Solana wallet configured

**Steps:**
1. **Mint NFT:**
   - Call `POST /api/a2a/agent/{agentId}/mint-nft`
   - Verify NFT minted and linked

2. **Verify Link:**
   - Call `GET /api/a2a/agent/{agentId}/nft`
   - Verify NFT details match

3. **Transfer NFT:**
   - Transfer NFT on-chain (via marketplace or direct)
   - Wait for confirmation

4. **Sync Ownership:**
   - Call `CheckAndSyncAgentOwnershipFromNFTAsync(nftId)`
   - Verify agent ownership updated

5. **Verify New Owner:**
   - Call `GetAgentOwnerAsync(agentId)`
   - Verify owner matches NFT owner

**Expected Results:**
- ✅ Complete flow works end-to-end
- ✅ Ownership transfers correctly
- ✅ All metadata is synchronized

---

## Known Issues & Limitations

### 1. Manual Sync Required

**Issue:** NFT transfers don't automatically trigger agent ownership sync.

**Current Workaround:** Manually call `CheckAndSyncAgentOwnershipFromNFTAsync()` after transfers.

**Future Fix:** Hook into `NFTManager.SendNFTAsync()` to automatically sync.

### 2. No Background Sync Job

**Issue:** Transfers that happen outside OASIS API (e.g., via marketplace) aren't automatically detected.

**Current Workaround:** Manual sync or periodic polling.

**Future Fix:** Background job to periodically check NFT ownership and sync.

### 3. Ownership Precedence

**Issue:** `GetAgentOwnerAsync()` doesn't fully verify NFT ownership on-chain.

**Current Behavior:** Falls back to metadata `OwnerAvatarId` (which should be synced).

**Future Fix:** Add on-chain verification for NFT-backed agents.

### 4. No Unlink Functionality

**Issue:** No way to unlink NFT from agent once linked.

**Current Behavior:** Agent always has NFT linked (unless manually removed from metadata).

**Future Fix:** Add `UnlinkNFTFromAgentAsync()` method with proper validation.

### 5. Multiple NFTs Per Agent

**Issue:** System doesn't prevent multiple NFTs from being linked to same agent.

**Current Behavior:** `MintAgentOwnershipNFTAsync()` checks for existing NFT, but `LinkNFTToAgentAsync()` doesn't.

**Future Fix:** Add validation to prevent multiple NFTs per agent.

---

## Future Enhancements

### 1. Automatic Sync Hook

**Implementation:**
```csharp
// In NFTManager.SendNFTAsync() after successful transfer
if (sendResult != null && sendResult.Result != null && !sendResult.IsError)
{
    var nftId = sendResult.Result.Id;
    // Fire and forget - don't block transfer
    _ = Task.Run(async () =>
    {
        await A2AManager.Instance.CheckAndSyncAgentOwnershipFromNFTAsync(nftId);
    });
}
```

### 2. Background Sync Job

**Implementation:**
- Periodic job (every 5-10 minutes)
- Query all agents with `NFTId` in metadata
- For each agent:
  - Load NFT
  - Compare NFT `CurrentOwnerAvatarId` with agent `OwnerAvatarId`
  - If different, call `SyncAgentOwnershipFromNFTAsync()`

### 3. Marketplace Webhooks

**Implementation:**
- Webhook endpoint: `POST /api/a2a/nft-transfer-webhook`
- Receives transfer events from marketplaces
- Automatically calls `CheckAndSyncAgentOwnershipFromNFTAsync()`

### 4. Enhanced Ownership Verification

**Implementation:**
- For NFT-backed agents, verify ownership on-chain
- Query blockchain for current NFT owner
- Compare with agent metadata
- Log discrepancies

### 5. Unlink Functionality

**Implementation:**
- `POST /api/a2a/agent/{agentId}/unlink-nft`
- Validates caller owns agent
- Removes NFT metadata from agent
- Optionally burns or transfers NFT

---

## Code Locations

### Core Implementation

- **AgentManager-NFT.cs:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AgentManager/AgentManager-NFT.cs`
- **A2AManagerExtensions-NFT.cs:** `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/A2AManager/A2AManagerExtensions-NFT.cs`
- **AgentManager-Ownership.cs:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AgentManager/AgentManager-Ownership.cs`

### API Endpoints

- **A2AController.cs:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/A2AController.cs`
  - `POST /api/a2a/agent/{agentId}/mint-nft` (line ~1184)
  - `GET /api/a2a/agent/{agentId}/nft` (line ~1252)

### Documentation

- **Architecture Doc:** `Docs/AGENT_NFT_TRADING_ARCHITECTURE.md`

---

## Summary

### ✅ What Works

- Minting NFT for agent
- Linking NFT to agent (bidirectional)
- Getting agent's NFT
- Syncing agent ownership from NFT (manual)
- Ownership verification (with NFT awareness)

### ⏳ What Needs Work

- Automatic sync on NFT transfer
- Background sync job
- Enhanced ownership verification
- Unlink functionality
- Multiple NFT prevention

### 🎯 Testing Priority

1. **High Priority:**
   - Test minting NFT for agent
   - Test getting agent NFT
   - Test ownership sync after transfer

2. **Medium Priority:**
   - Test end-to-end flow
   - Test error cases
   - Test edge cases

3. **Low Priority:**
   - Test performance with many agents
   - Test concurrent transfers

---

**Status:** ✅ Ready for Testing  
**Last Updated:** January 2026
