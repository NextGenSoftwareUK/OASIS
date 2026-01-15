# $SERV Token Integration Brief

**Date:** January 2026  
**Objective:** Integrate $SERV tokens (OpenServ's token on Base blockchain) into OASIS agents for agent-to-agent payments

---

## Context

### Current State
- ✅ **Base Provider Exists:** `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/`
- ✅ **MNEE Payment Pattern:** Already implemented in `A2AManager-MNEE.cs` (can be used as template)
- ✅ **Generic ERC-20 Service:** `MNEEService.cs` is generic and works with any ERC-20 token
- ✅ **Wallet Creation:** Base wallet creation should work similar to Solana/Ethereum patterns
- ✅ **API Endpoints:** MNEE payment endpoints just added to `A2AController.cs` (can be duplicated for SERV)

### $SERV Token Details
- **Token:** $SERV (OpenServ token)
- **Blockchain:** Base (Coinbase L2)
- **Contract Address:** `0x40e3...E28042` (from CoinMarketCap - verify exact address)
- **Chain ID:** 8453 (Base Mainnet)
- **Type:** ERC-20 token
- **RPC URL:** `https://mainnet.base.org`

---

## Requirements

### 1. Base Wallet Creation for Agents
**What:** Enable automatic Base wallet creation when agents are created/registered

**Reference Implementation:**
- `MCP/solana-wallet-tools.ts` - Solana wallet creation pattern
- `MCP/ethereum-wallet-tools.ts` - Ethereum wallet creation pattern
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/WalletManager.cs` - Wallet creation logic

**What to Do:**
- Ensure Base provider can create wallets for agents
- Add Base wallet creation to agent registration flow (optional auto-creation)
- Verify Base wallet creation works via `WalletManager.CreateWalletAsync()` with `ProviderType.BaseOASIS`

**Files to Check/Modify:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/WalletManager.cs`
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/BaseOASIS.cs`
- Agent registration endpoints in `A2AController.cs`

### 2. $SERV Token Service (Similar to MNEE)
**What:** Create SERV token service for Base blockchain

**Key Discovery:** 
- ✅ **BaseOASIS already has generic ERC-20 transfer!** (`SendTokenAsync` method at line 3040)
- ✅ **Web3CoreOASIS pattern is identical** - both use same ERC-20 ABI pattern
- ✅ **MNEEService is a convenience wrapper** - does same thing as BaseOASIS.SendTokenAsync

**What to Do:**
- **Option A (Recommended):** Use existing `BaseOASIS.SendTokenAsync()` directly - it already supports any ERC-20 token including SERV
- **Option B:** Create `SERVService.cs` convenience wrapper (like MNEEService) for SERV-specific methods
- **Option C:** Add SERV-specific methods to BaseOASIS (like EthereumOASIS has MNEE methods)

**Recommendation:** 
- **For Transfers:** Use `BaseOASIS.SendTokenAsync()` directly (already works with any ERC-20!)
- **For Balance Queries:** Create `SERVService.cs` using MNEEService pattern (change RPC URL to Base)
- **For Convenience:** Add SERV-specific methods to BaseOASIS (like EthereumOASIS has MNEE methods)

**What's Needed:**
- SERV contract address constant: `0x40e3...E28042` (verify exact address from basescan.org)
- SERVService.cs (copy MNEEService, change RPC to Base, change contract to SERV)
- Or add methods to BaseOASIS that use SERVService internally

**Files to Create/Modify:**
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/Services/SERVService.cs` (optional - convenience wrapper)
- Or add methods directly to `BaseOASIS.cs` (like EthereumOASIS has MNEE methods)

### 3. Base Provider SERV Methods
**What:** Add SERV token methods to BaseOASIS provider

**Important:** BaseOASIS already has `SendTokenAsync()` for ERC-20 transfers (line 3040). We just need SERV-specific convenience methods.

**Reference Implementation:**
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/EthereumOASIS.cs`
  - Lines 4402-4553: `GetMNEEBalanceForAvatarAsync()`, `TransferMNEEBetweenAvatarsAsync()`, etc.
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/Services/MNEEService.cs`
  - Generic ERC-20 service that can work with any token

**What to Do:**
- **Option A:** Use `MNEEService` pattern - create `SERVService.cs` that uses Base RPC URL and SERV contract address
- **Option B:** Add methods directly to `BaseOASIS.cs`:
  - `GetSERVBalanceForAvatarAsync(Guid avatarId)` - Uses MNEEService pattern with SERV contract
  - `TransferSERVBetweenAvatarsAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)` - Uses existing `SendTokenAsync()` with SERV contract
  - `ApproveSERVAsync(Guid avatarId, string spenderAddress, decimal amount)` - ERC-20 approve
  - `GetSERVAllowanceAsync(Guid avatarId, string spenderAddress)` - ERC-20 allowance

**Key Insight:** 
- BaseOASIS.SendTokenAsync() already works with any ERC-20 token
- Just need to pass SERV contract address (`0x40e3...E28042`)
- Can reuse MNEEService pattern or add convenience methods to BaseOASIS

**Files to Create/Modify:**
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/Services/SERVService.cs` (optional - convenience wrapper)
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/BaseOASIS.cs` (add SERV methods)

### 4. A2A Manager SERV Integration
**What:** Add SERV payment methods to A2AManager (similar to MNEE)

**Reference Implementation:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/A2AManager/A2AManager-MNEE.cs`
  - `SendMNEEPaymentRequestAsync()` - Sends payment request via A2A Protocol
  - `ExecuteMNEEPaymentAsync()` - Executes actual token transfer
  - `GetAgentMNEEBalanceAsync()` - Gets agent's token balance

**What to Do:**
- Create `A2AManager-SERV.cs` with:
  - `SendSERVPaymentRequestAsync(Guid fromAgentId, Guid toAgentId, decimal amount, string description, bool autoExecute)`
  - `ExecuteSERVPaymentAsync(Guid fromAgentId, Guid toAgentId, decimal amount, Guid? paymentRequestId)`
  - `GetAgentSERVBalanceAsync(Guid agentId)`
  - `SendSERVPaymentConfirmationAsync(Guid fromAgentId, Guid toAgentId, Guid paymentRequestId, string transactionHash)`

**Files to Create:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/A2AManager/A2AManager-SERV.cs`

### 5. API Endpoints
**What:** Add SERV payment endpoints to A2AController

**Reference Implementation:**
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/A2AController.cs`
  - Lines 1163-1318: `POST /api/a2a/mnee/payment` and `GET /api/a2a/mnee/balance`
  - Request model: `SendMNEEPaymentRequest`

**What to Do:**
- Add endpoints:
  - `POST /api/a2a/serv/payment` - Send SERV payment between agents
  - `GET /api/a2a/serv/balance` - Get agent's SERV balance
- Add request model: `SendSERVPaymentRequest`

**Files to Modify:**
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/A2AController.cs`

---

## Technical Details

### Base Provider Configuration
```csharp
// Base Mainnet
var baseProvider = new BaseOASIS(
    hostUri: "https://mainnet.base.org",
    chainPrivateKey: "YOUR_PRIVATE_KEY",
    contractAddress: "YOUR_CONTRACT_ADDRESS"
);
```

### SERV Token Contract
- **Address:** `0x40e3...E28042` (verify exact address from Base explorer)
- **Standard:** ERC-20
- **Decimals:** Check contract (likely 18)
- **Network:** Base Mainnet (Chain ID: 8453)

### ERC-20 Transfer Pattern
**BaseOASIS already has this!** See `BaseOASIS.SendTokenAsync()` at line 3040.

```csharp
// BaseOASIS already implements this pattern:
var erc20Abi = "[{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",...}]";
var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.FromTokenAddress);
var transferFunction = erc20Contract.GetFunction("transfer");
var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(...);
```

**To use for SERV:**
```csharp
var servRequest = new SendWeb3TokenRequest
{
    FromTokenAddress = "0x40e3...E28042", // SERV contract address
    ToWalletAddress = toAgentWalletAddress,
    Amount = servAmount,
    FromWalletPrivateKey = fromAgentPrivateKey
};
var result = await baseProvider.SendTokenAsync(servRequest);
```

**MNEEService Pattern (Alternative):**
- Create `SERVService.cs` similar to `MNEEService.cs`
- Uses same Nethereum pattern but with Base RPC URL and SERV contract
- Provides convenience methods: `GetBalanceAsync()`, `TransferAsync()`, etc.

---

## Implementation Steps

### Step 1: Verify Base Wallet Creation
1. Test Base wallet creation via `WalletManager.CreateWalletAsync()` with `ProviderType.BaseOASIS`
2. Verify wallets are created and stored correctly
3. Test wallet address derivation

### Step 2: Create SERV Token Service
**Key Finding:** `MNEEService.cs` is generic and works with any RPC URL and contract address!

1. **Create `SERVService.cs`** (copy MNEEService pattern):
   - Location: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/Services/SERVService.cs`
   - Copy entire `MNEEService.cs` file
   - Change namespace: `EthereumOASIS.Services` → `BaseOASIS.Services`
   - Change class name: `MNEEService` → `SERVService`
   - Change constant: `MNEE_CONTRACT_ADDRESS` → `SERV_CONTRACT_ADDRESS = "0x40e3...E28042"`
   - Constructor: Change default RPC (if any) to Base RPC
   - **That's it!** Same code, different RPC + contract address

2. **Alternative:** Use `BaseOASIS.SendTokenAsync()` directly for transfers
   - Already works with any ERC-20 token
   - Just pass SERV contract address
   - For balance queries, still need SERVService (BaseOASIS.GetBalanceAsync only gets native token)

3. Test balance queries and transfers

### Step 3: Add Base Provider Methods
1. Add SERV methods to `BaseOASIS.cs`
2. Use reflection pattern (like MNEE) to avoid direct dependencies
3. Test with real Base RPC

### Step 4: Create A2A Manager Integration
1. Create `A2AManager-SERV.cs` following `A2AManager-MNEE.cs` pattern
2. Implement payment request/execution flow
3. Integrate with A2A Protocol messaging

### Step 5: Add API Endpoints
1. Add SERV payment endpoints to `A2AController.cs`
2. Add request models
3. Test endpoints

### Step 6: Testing
1. Create test agents with Base wallets
2. Test SERV balance queries
3. Test agent-to-agent SERV payments
4. Verify transactions on Base explorer

---

## Key Files Reference

### Existing Files (Use as Reference)
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/A2AManager/A2AManager-MNEE.cs` - Payment pattern
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/Services/MNEEService.cs` - **Generic ERC-20 service** (works with any RPC + contract!)
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/EthereumOASIS.cs` - MNEE methods (lines 4402-4553)
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/A2AController.cs` - MNEE endpoints (lines 1163-1318)
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/BaseOASIS.cs` - Base provider
  - **Line 3040:** `SendTokenAsync()` - Already supports ERC-20 transfers! ✅
  - **Line 3363:** `GetBalanceAsync()` - Only native token (ETH), not ERC-20 tokens
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/WalletController.cs` - Generic token endpoints (line 812)

### Files to Create/Modify
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/A2AManager/A2AManager-SERV.cs` - **NEW**
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/Services/SERVService.cs` - **NEW** (optional)
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/BaseOASIS.cs` - **MODIFY** (add SERV methods)
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/A2AController.cs` - **MODIFY** (add SERV endpoints)

---

## Important Notes

1. **Base Provider Already Has ERC-20 Transfer Support!** 
   - ✅ `BaseOASIS.SendTokenAsync()` already exists (line 3040)
   - ✅ Works with any ERC-20 token (just pass contract address)
   - ✅ Same pattern as Web3CoreOASIS and MNEEService
   - ⚠️ `BaseOASIS.GetBalanceAsync()` only gets native token (ETH), not ERC-20 tokens

2. **MNEEService is Generic!**
   - ✅ `MNEEService.cs` works with **any RPC URL** and **any contract address**
   - ✅ Just takes `rpcUrl` and `contractAddress` as parameters
   - ✅ Methods: `GetBalanceAsync()`, `TransferAsync()`, `ApproveAsync()`, `GetAllowanceAsync()`
   - **For SERV:** Create `SERVService.cs` - copy MNEEService, change:
     - RPC URL: Ethereum → Base (`https://mainnet.base.org`)
     - Default contract: MNEE → SERV (`0x40e3...E28042`)

3. **Recommended Approach:**
   - **For Transfers:** Use `BaseOASIS.SendTokenAsync()` with SERV contract address ✅
   - **For Balance Queries:** Create `SERVService.cs` (copy MNEEService pattern, use Base RPC)
   - **For Convenience:** Add SERV-specific methods to BaseOASIS (like EthereumOASIS has MNEE methods)
   - **For A2A Integration:** Create `A2AManager-SERV.cs` (copy A2AManager-MNEE.cs pattern)

4. **Wallet Creation:** Base wallets should work via existing `WalletManager` - just need to verify

5. **Verify Contract Address:** Get exact SERV contract address from Base explorer (basescan.org)
   - CoinMarketCap shows: `0x40e3...E28042` (truncated)
   - Full address needed for implementation
   - Search for "OpenServ" or "SERV" token on Base

---

## Success Criteria

✅ Agents can have Base wallets created automatically  
✅ Agents can query their $SERV token balance  
✅ Agents can send $SERV payments to other agents via A2A Protocol  
✅ Payments are executed on Base blockchain  
✅ Transaction hashes are returned and stored  
✅ API endpoints work for SERV payments  
✅ Integration follows same pattern as MNEE (for consistency)

---

**Status:** Ready for Implementation  
**Estimated Complexity:** Medium (pattern already exists, just needs adaptation)  
**Dependencies:** Base provider, WalletManager, A2A Protocol
