# Radix Gateway API Research for Read-Only Component Method Calls

## Overview

This document summarizes research into the Radix Gateway API to determine the best approach for implementing read-only component method calls (e.g., `get_avatar`, `get_holon`) in the RadixOASIS provider.

## Problem Statement

Radix Scrypto components expose methods that can be called to read state (e.g., `get_avatar(id: u64) -> Option<String>`). However, unlike write operations which use transactions, read-only operations need a different approach:

1. **Transaction-based approach**: Submit a transaction that calls the read-only method (inefficient, costs fees)
2. **State query approach**: Query component state directly via Gateway API (efficient, but requires parsing)
3. **Transaction preview/stakes approach**: Use transaction preview endpoints to simulate the call (needs return value extraction)

## Gateway API Endpoints

### Base URLs
- **Mainnet**: `https://mainnet.radixdlt.com`
- **Stokenet (Testnet)**: `https://stokenet.radixdlt.com`

### Documentation Links
- **Full Gateway API Documentation**: https://radix-babylon-gateway-api.redoc.ly/
- **TypeScript Gateway SDK**: https://www.npmjs.com/package/@radixdlt/babylon-gateway-api-sdk
- **Network APIs Overview**: https://docs.radixdlt.com/docs/network-apis

### Known State Query Endpoints

#### 1. Account Balance
- **Endpoint**: `/core/lts/state/account-fungible-resource-balance`
- **Method**: POST
- **Payload**:
  ```json
  {
    "network": "stokenet" | "mainnet",
    "account_address": "account_tdx_...",
    "resource_address": "resource_tdx_..."
  }
  ```
- **Used in**: `RadixService.GetAccountBalanceAsync()`

#### 2. Entity Details (Confirmed)
- **Endpoint**: `/state/entity/details` (Gateway API)
- **Full Path**: `/state/entity/details` (not `/core/lts/` prefix like some other endpoints)
- **Purpose**: Query entity state (components, accounts, resources)
- **Request Format**:
  ```json
  {
    "addresses": ["component_address_here"],
    "at_ledger_state": { "state_version": 1000 }  // Optional: for historical state
  }
  ```
- **Response**: Returns entity details which differ per entity type (component, account, resource, etc.)
- **Status**: ✅ Confirmed in Gateway API documentation
- **Documentation**: https://radix-babylon-gateway-api.redoc.ly/

#### 3. Transaction Stakes (Preview)
- **Endpoint**: `/core/lts/transaction/stakes`
- **Purpose**: Preview transaction execution without committing
- **Payload**:
  ```json
  {
    "network": "stokenet" | "mainnet",
    "notarized_transaction_hex": "...",
    "signer_public_keys": ["..."]
  }
  ```
- **Status**: Currently attempted in `CallComponentMethodAsync()`, but return value extraction not implemented

### Transaction Endpoints

#### Transaction Submit
- **Endpoint**: `/core/lts/transaction/submit`
- **Method**: POST
- **Payload**:
  ```json
  {
    "network": "stokenet" | "mainnet",
    "notarized_transaction_hex": "...",
    "force_recalculate": true
  }
  ```
- **Used in**: `RadixComponentService.CallComponentMethodTransactionAsync()`

## Approaches for Read-Only Component Method Calls

### Approach 1: Direct State Query (Recommended for Future)

**Concept**: Query component's KeyValueStore state directly and parse it.

**Pros**:
- Efficient (no transaction fees)
- Fast (direct state access)
- No transaction overhead

**Cons**:
- Requires understanding component's internal state structure
- Must parse KeyValueStore binary/JSON format
- Component state structure may change with blueprint updates

**Implementation Steps**:
1. Query `/core/lts/state/entity/details` (or similar) with component address
2. Parse component state to extract KeyValueStore entries
3. Find the relevant entry by key (e.g., entity ID)
4. Deserialize the stored JSON string

**Status**: ⏳ Not yet implemented - needs Gateway API endpoint research

**Research Needed**:
- Check Gateway API documentation for entity state endpoints
- Understand entity state response structure
- Document KeyValueStore state representation format

### Approach 2: Transaction Preview/Stakes (Current Workaround)

**Concept**: Build a transaction that calls the read-only method, then use the stakes endpoint to preview it without committing.

**Pros**:
- Uses existing transaction infrastructure
- Can see method return values in preview
- No actual transaction fees (preview only)

**Cons**:
- Requires building and signing a transaction (even if not committed)
- Complex return value extraction from preview response
- May be slower than direct state query

**Implementation Steps**:
1. Build transaction manifest with read-only method call
2. Sign transaction with dummy/read-only account
3. Call `/core/lts/transaction/stakes` endpoint
4. Parse response to extract method return value
5. Extract JSON string from return value

**Status**: 🔄 Partially implemented - transaction building works, but return value extraction needs work

**Research Needed**:
- Document stakes endpoint response format
- Understand how method return values are represented
- Determine SBOR decoding requirements
- Test with actual component method calls

### Approach 3: Dedicated Read-Only Transaction Account

**Concept**: Use a small transaction account that only calls read-only methods and pays minimal fees.

**Pros**:
- Simple implementation
- Guaranteed to work (uses standard transaction flow)
- Can reuse existing transaction infrastructure

**Cons**:
- Costs transaction fees (even if minimal)
- Slower than state queries
- Requires account with XRD balance for fees
- Not ideal for high-frequency reads

**Status**: ⏸️ Not recommended for production, but viable as fallback

## Current Implementation Status

### `CallComponentMethodAsync()` (Read-Only)

**Location**: `RadixComponentService.cs`

**Current State**:
- ✅ Transaction manifest building
- ✅ Transaction signing with dummy key
- ✅ Calling `/core/lts/transaction/stakes` endpoint
- ❌ Return value extraction from stakes response
- ❌ JSON string parsing

**Next Steps**:
1. Research stakes endpoint response format in Gateway API docs
2. Implement return value extraction logic
3. Parse SBOR/JSON return value to string
4. Test with actual component method calls

## Research Findings

### Gateway API Capabilities
- Provides state queries for accounts, components, and resources
- Supports transaction preview, submission, and status tracking
- Includes historical data access
- Has both Core API (low-level) and Gateway API (higher-level abstraction)

### Key Discovery: `/state/entity/details` Endpoint
- **Confirmed**: This endpoint exists and is the main endpoint for querying entity state
- **Usage**: Takes a list of entity addresses and returns details for those entities
- **Component State**: For components, this should return component state including KeyValueStore entries
- **Response Format**: Response schema differs per entity type (component responses include state information)
- **Documentation Location**: https://radix-babylon-gateway-api.redoc.ly/ (State Endpoints section)

### Gateway API Structure
- Gateway API uses `/state/` prefix (not `/core/lts/state/`)
- Core API uses `/core/lts/` prefix (e.g., `/core/lts/state/account-fungible-resource-balance`)
- Different APIs have different endpoint structures

### Key Resources
- **Official Docs**: https://docs.radixdlt.com/docs/network-apis
- **Gateway API Docs**: https://radix-babylon-gateway-api.redoc.ly/
- **Gateway SDK**: https://www.npmjs.com/package/@radixdlt/babylon-gateway-api-sdk

### Gateway API Providers
- **Radix Foundation**: Rate-limited public endpoints
- **RadixAPI**: Enhanced endpoints and higher rate limits
- **NowNodes**: Gateway API support with various pricing plans

## Recommended Path Forward

### Short Term (Immediate)
1. **Complete Transaction Stakes Approach**:
   - Review Gateway API documentation for stakes endpoint response format
   - Implement return value extraction
   - Test with deployed component

### Medium Term (Next Phase)
2. **Implement Direct State Query**:
   - Research `/core/lts/state/entity/details` or similar endpoint in Gateway API docs
   - Implement component state parsing
   - Parse KeyValueStore entries directly
   - Fallback to transaction stakes if state query fails

### Long Term (Optimization)
3. **Optimize for Production**:
   - Cache component state queries
   - Implement efficient state parsing
   - Add error handling and retries
   - Performance testing and optimization

## Next Actions

1. **Review Gateway API Documentation**:
   - Open https://radix-babylon-gateway-api.redoc.ly/
   - Search for "state" endpoints
   - Look for entity details or component state queries
   - Document endpoint formats and response structures

2. **Investigate Transaction Stakes Response**:
   - Find stakes endpoint documentation
   - Understand response structure
   - Determine how method return values are encoded
   - Plan SBOR decoding strategy

3. **Test with Actual Component**:
   - Deploy test component to Stokenet
   - Test state query endpoints
   - Test transaction stakes endpoint with read-only method call
   - Validate return value extraction

## Notes

- The Gateway API is rate-limited on public endpoints
- Consider using third-party providers for production
- Self-hosting Gateway is an option for high-demand applications
- Component state queries are more efficient than transaction-based reads
- Transaction stakes endpoint exists for previewing transactions
- Gateway API provides higher-level abstraction than Core API
