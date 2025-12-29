# Wallet Stack Fix Plan - OASIS vs Privy.io Decision

**Date:** December 21, 2025  
**Goal:** Get OASIS wallet stack working reliably to evaluate against Privy.io

---

## Current Status

### ❌ Issues Found

1. **405 Method Not Allowed** error when calling wallet generation
   - Endpoint: `POST /api/keys/generate_keypair_for_provider/{providerType}`
   - Likely cause: Authentication token issue or endpoint routing problem

2. **Token Extraction Failure**
   - OASIS API authenticate endpoint returns nested response structure
   - Token extraction may not be handling all response formats correctly

3. **Wallet Balance Endpoint** - 500 errors (needs investigation)

---

## Implementation Plan

### Phase 1: Fix Authentication & Token Management

**Issue:** OASIS API token extraction may not handle all response formats

**Fix:**
1. Enhance token extraction in `oasis-token-manager.service.ts` to handle all possible response structures:
   - `.result.Result.jwtToken`
   - `.result.jwtToken`
   - `.jwtToken`
   - `.token`
   - `.data.token`
   
2. Add comprehensive logging to trace token extraction

3. Test authentication flow end-to-end

**Files to Modify:**
- `src/services/oasis-token-manager.service.ts`

---

### Phase 2: Verify Wallet Generation Endpoints

**Test Directly Against OASIS API:**

1. **Test Keypair Generation:**
   ```bash
   POST /api/keys/generate_keypair_for_provider/SolanaOASIS
   Headers: Authorization: Bearer <token>
   ```

2. **Test Private Key Linking:**
   ```bash
   POST /api/keys/link_provider_private_key_to_avatar_by_id
   Body: {
     "AvatarID": "<avatarId>",
     "ProviderType": "SolanaOASIS",
     "ProviderKey": "<privateKey>"
   }
   ```

3. **Test Public Key Linking:**
   ```bash
   POST /api/keys/link_provider_public_key_to_avatar_by_id
   Body: {
     "WalletId": "<walletId>",
     "AvatarID": "<avatarId>",
     "ProviderType": "SolanaOASIS",
     "ProviderKey": "<publicKey>",
     "WalletAddress": "<walletAddress>"
   }
   ```

**Expected Results:**
- All three endpoints should return 200 OK
- Response should contain wallet ID after linking private key
- Wallet should be complete after linking public key

---

### Phase 3: Fix Wallet Generation Flow

**Current Implementation Issues:**

1. **Error Handling:**
   - 405 errors not being properly diagnosed
   - Need better error messages showing actual API response

2. **Response Parsing:**
   - OASIS API responses have nested structures
   - Need robust parsing for all possible formats

3. **Logging:**
   - Add detailed logging at each step
   - Log request/response for debugging

**Files to Modify:**
- `src/services/oasis-wallet.service.ts`
- Enhance error handling and logging

---

### Phase 4: Test Wallet Balance & Sync

**Test Balance Endpoint:**
```bash
GET /api/wallet/balance
Headers: Authorization: Bearer <token>
```

**Expected:**
- Returns list of wallets with balances
- Handles empty wallet list gracefully

**Fix if needed:**
- `src/wallet/wallet.controller.ts` - `getBalances()` method
- `src/services/oasis-wallet.service.ts` - `getBalance()` method

---

### Phase 5: Create Comprehensive Test Suite

**Create test script:** `scripts/test-wallet-stack.sh`

**Test Scenarios:**
1. ✅ Authentication & token refresh
2. ✅ Generate Solana wallet
3. ✅ Generate Ethereum wallet
4. ✅ Get wallet balances
5. ✅ Sync balances
6. ✅ Set default wallet
7. ✅ Get wallet by ID
8. ✅ Error handling (invalid provider, missing avatar, etc.)

---

### Phase 6: Compare with Privy.io

**OASIS Wallet Stack Capabilities:**

✅ **What We Have:**
- Multi-chain wallet generation (Solana, Ethereum, etc.)
- Key management via OASIS Keys API
- Wallet linking to Avatars
- Balance tracking
- Transaction history (via OASIS)
- Integration with existing OASIS authentication

❓ **What We Need to Verify:**
- Reliability of wallet generation
- Error handling robustness
- Transaction signing capabilities
- User onboarding experience
- Security of key storage

**Privy.io Capabilities (from their description):**
- Complete wallet stack
- Key management
- User onboarding
- Transaction signing
- Whitelabel wallets
- Programmable API

**Decision Criteria:**
1. **Reliability** - Does OASIS wallet generation work consistently?
2. **User Experience** - Can users easily create and use wallets?
3. **Security** - Are keys stored securely?
4. **Maintenance** - How much effort to maintain vs. using Privy.io?
5. **Cost** - OASIS is already integrated, Privy.io is a new service
6. **Control** - Do we want to own the wallet infrastructure?

---

## Testing Checklist

### Basic Wallet Operations
- [ ] Generate Solana wallet successfully
- [ ] Generate Ethereum wallet successfully
- [ ] Get wallet balance
- [ ] Sync wallet balance
- [ ] Set default wallet
- [ ] List all wallets for avatar

### Error Scenarios
- [ ] Invalid provider type handling
- [ ] Missing avatar ID handling
- [ ] Invalid authentication token handling
- [ ] Network failure handling
- [ ] OASIS API errors (500, 404, etc.)

### Integration Tests
- [ ] Complete flow: Register → Generate Wallet → Get Balance
- [ ] Multiple wallets per avatar
- [ ] Wallet switching/default wallet
- [ ] Balance sync after transactions

---

## Next Steps

1. **Immediate:** Fix authentication token extraction
2. **Next:** Test wallet generation directly against OASIS API
3. **Then:** Fix any endpoint/response parsing issues
4. **Finally:** Run comprehensive test suite and document results

---

## Success Criteria

The OASIS wallet stack is considered "working" when:

1. ✅ Wallet generation succeeds >95% of the time
2. ✅ Error messages are clear and actionable
3. ✅ All wallet operations (generate, balance, sync) work reliably
4. ✅ Test suite passes all scenarios
5. ✅ Documentation is complete

If we can meet these criteria, OASIS is a viable alternative to Privy.io.

---

## Files to Review/Modify

1. `src/services/oasis-token-manager.service.ts` - Token extraction
2. `src/services/oasis-wallet.service.ts` - Wallet generation flow
3. `src/wallet/wallet.controller.ts` - API endpoints
4. `scripts/test-wallet-stack.sh` - Test script (to be created)

---

**Last Updated:** December 21, 2025


