# Universal Asset Bridge - Architecture Overview

**Purpose:** Technical overview of the OASIS Universal Asset Bridge  
**Audience:** Developers and technical stakeholders  
**Status:** Active Development

---

## What is the Universal Asset Bridge?

The Universal Asset Bridge enables cross-chain token swaps across 10+ blockchains using OASIS HyperDrive technology for enhanced security and reliability.

**Key Features:**
- Multi-chain support (Ethereum, Solana, Polygon, Arbitrum, Base, etc.)
- Atomic swaps with automatic rollback
- Auto-failover across multiple providers
- Real-time exchange rates
- Order tracking and status management

---

## Architecture Components

### 1. API Layer (REST Endpoints)

**Location:** `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/BridgeController.cs`

**Endpoints:**
```
POST   /api/v1/orders                    - Create bridge order
GET    /api/v1/orders/{id}/check-balance - Check order status
GET    /api/v1/exchange-rate             - Get current rate
GET    /api/v1/networks                  - List supported chains
```

**Status:** ✅ Implemented

### 2. Service Layer

**Location:** `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/BridgeService.cs`

**Responsibilities:**
- Wraps bridge manager functionality
- Handles errors and logging
- Manages Solana bridge initialization
- Provides clean interface for controllers

**Status:** ✅ Implemented (Solana only, Radix pending)

### 3. Bridge Manager (Core Logic)

**Location:** `NextGenSoftware.OASIS.API.Core.Managers.Bridge/CrossChainBridgeManager.cs`

**Responsibilities:**
- Execute atomic swaps
- Manage bridge order lifecycle
- Coordinate between source and destination chains
- Handle rollbacks on failure

**Status:** ⏳ Partially implemented (needs completion)

### 4. Provider Implementations

**Interface:** `IOASISBridge`

**Implementations:**
- **SolanaBridgeService:** ✅ Implemented
- **EthereumBridgeService:** ❌ Needs implementation
- **PolygonBridgeService:** ❌ Needs implementation
- **ArbitrumBridgeService:** ❌ Needs implementation
- **RadixBridgeService:** ❌ Needs implementation

Each provider implements:
```csharp
public interface IOASISBridge
{
    Task<OASISResult<string>> LockTokensAsync(...);
    Task<OASISResult<string>> MintTokensAsync(...);
    Task<OASISResult<string>> BurnTokensAsync(...);
    Task<OASISResult<string>> ReleaseTokensAsync(...);
}
```

### 5. Smart Contracts (On-Chain)

**Location:** `/UniversalAssetBridge/contracts/`

**Required Contracts:**
- `OASISBridge.sol` (Ethereum/EVM chains) - ❌ Not deployed
- `solana_bridge_program.rs` (Solana) - ❌ Not deployed
- `radix_bridge.scrypto` (Radix) - ❌ Not deployed

**Specifications:** ✅ Created (`bridge-specifications.json`)

**Functions:**
```solidity
function lockTokens(destinationChain, recipient, amount)
function mintTokens(orderId, recipient, amount, proof) onlyOracle
function burnTokens(amount, returnChain, returnAddress)
function releaseTokens(orderId, recipient, proof) onlyOracle
```

### 6. Oracle Service (Background Worker)

**Location:** Needs to be created

**Responsibilities:**
- Monitor all chains for TokensLocked events
- Verify locks reached finality
- Generate consensus proofs
- Execute mints on destination chains
- Update order statuses

**Status:** ❌ Not implemented

### 7. Database

**Tables Needed:**
- `bridge_orders` - Order tracking
- `bridge_transactions` - Transaction history
- `bridge_events` - Event log

**Provider:** MongoDB (configured) or PostgreSQL (recommended for financial data)

**Status:** ❌ Not implemented

---

## How a Swap Works

### Current Flow (Solana Only)

```
1. User submits swap request
         ↓
2. BridgeController.CreateOrder()
         ↓
3. BridgeService.CreateOrderAsync()
         ↓
4. CrossChainBridgeManager.CreateBridgeOrderAsync()
         ↓
5. Return order ID
```

**Current Limitation:** No actual token locking/minting happens yet (contracts not deployed).

### Target Flow (Full Implementation)

```
1. User: POST /api/v1/orders {from: SOL, to: ETH, amount: 1}
         ↓
2. API: Lock 1 SOL on Solana contract
         ↓
3. Solana: Emit TokensLocked event
         ↓
4. Oracle: Detect event, wait for finality (32 blocks)
         ↓
5. Oracle: Generate merkle proof of lock
         ↓
6. Oracle: Call mintTokens() on Ethereum contract
         ↓
7. Ethereum: Verify proof, mint equivalent ETH
         ↓
8. Database: Update order status to "completed"
         ↓
9. User: Receives ETH in their wallet
```

**Time:** 2-5 minutes (depends on block finality)

---

## What Needs to Be Built

### Priority 1: Smart Contracts (Critical)

**Without these, the bridge cannot function.**

1. Generate contracts using SmartContractGenerator API
2. Deploy to testnets (Sepolia, Devnet, Stokenet)
3. Deploy to additional EVM chains (Polygon, Arbitrum, Base)
4. Test lock/mint/burn/release functions
5. Verify oracle authorization works

**Timeline:** 1 week  
**Complexity:** Medium (using generator API)

### Priority 2: Oracle Service (Critical)

**Without this, swaps never complete.**

1. Create BridgeOracleService
2. Implement event monitoring (WebSocket subscriptions)
3. Implement consensus verification
4. Implement cross-chain execution
5. Add error handling and retries

**Timeline:** 2 weeks  
**Complexity:** High (complex logic)

### Priority 3: Database Integration (Important)

**Without this, order history is lost.**

1. Design database schema
2. Create OrderRepository
3. Integrate with BridgeService
4. Add order status tracking
5. Build admin dashboard for monitoring

**Timeline:** 1 week  
**Complexity:** Low-Medium

### Priority 4: Multi-Chain Providers (Important)

**Without these, only Solana works.**

1. Implement EthereumBridgeService
2. Implement PolygonBridgeService
3. Implement ArbitrumBridgeService
4. Test each implementation
5. Add to CrossChainBridgeManager

**Timeline:** 6 weeks (1 week per chain)  
**Complexity:** Medium (repetitive work)

### Priority 5: Testing & Audit (Critical for Mainnet)

**Without this, security risks are too high.**

1. Comprehensive unit tests
2. Integration tests across chains
3. Chaos testing (simulate failures)
4. External security audit
5. Bug bounty program

**Timeline:** 4-5 months  
**Complexity:** High

---

## Current Workaround

**For Development/Testing:**

The frontend can connect to the partially implemented API:
- Exchange rates work (CoinGecko integration)
- Order creation returns IDs
- Status checks return mock data

**Limitation:** No actual token transfers happen until contracts are deployed.

**For Production Swaps:**

Until the full bridge is complete, users would need to:
- Use existing DEX aggregators
- Use traditional bridges (with known risks)
- Wait for OASIS bridge completion

---

## Success Metrics

**MVP Success:**
- [ ] Can swap SOL ↔ ETH on testnets
- [ ] Orders complete automatically (< 5 minutes)
- [ ] Zero lost funds (100% atomic)
- [ ] Exchange rates accurate (< 1% deviation from market)

**Production Success:**
- [ ] 10+ chains supported
- [ ] $1M+ daily volume
- [ ] 99.9% success rate
- [ ] < $50 average swap cost
- [ ] Security audit passed (no critical issues)

---

## Additional Notes

### Why This is Better Than Traditional Bridges

**Traditional Bridge:**
- Single bridge contract holds all funds
- If hacked, all funds lost ($2B+ lost to bridge hacks)
- Single point of failure

**OASIS Bridge with HyperDrive:**
- Multiple providers (MongoDB, Arbitrum, Ethereum)
- If one fails, automatic failover to next
- Distributed risk (no single honeypot)
- Oracle requires multi-sig (3-of-5) for mints
- Merkle proof verification

**Still a bridge:** Yes, it still uses lock-and-mint mechanism, but with better redundancy and security than traditional bridges.

---

## Contact & Support

**Questions:** @maxgershfield on Telegram  
**Repository:** https://github.com/NextGenSoftwareUK/OASIS (max-build2 branch)  
**API:** https://api.oasisweb4.one

**This document provides complete context for continuing the Universal Asset Bridge implementation.**

---

**Last Updated:** November 6, 2025  
**Version:** 2.0
