# Bridge Wallet Architecture - Current State & Improvements

## 🔍 Current Architecture Analysis

### How It Currently Works

**Current Implementation:**
The bridge uses a **bridge pool model** where:

1. **Withdraw Operation** (`WithdrawAsync`):
   - Takes `senderAccountAddress` and `senderPrivateKey` as parameters
   - **However**: The Zcash implementation ignores the private key: `_ = senderPrivateKey; // Zcash daemon signs transactions internally`
   - Falls back to `_bridgePoolAddress` if no address provided
   - Uses bridge pool for withdrawals

2. **Deposit Operation** (`DepositAsync`):
   - Takes `receiverAccountAddress` (user's destination address)
   - Creates transaction from bridge pool to user's address
   - User receives funds directly to their address

### Current Limitations

**❌ Issue: Requires Bridge Pool Custody**
- Assets must be in the bridge pool first
- User would need to transfer to bridge pool before bridging
- This is a **custodial model** (not ideal for privacy/decentralization)

**Current Flow:**
```
User Wallet → Bridge Pool → Bridge Operation → Destination Chain → User Wallet
     ↑                                                                    ↓
     └────────────────── Requires pre-transfer ──────────────────────────┘
```

---

## ✅ Ideal Architecture (Non-Custodial)

### What We Should Support

**Option 1: Direct User Wallet Signing** (Recommended)
- User signs transaction from their own wallet
- Bridge orchestrates but never holds funds
- True non-custodial operation

**Option 2: User-Initiated Lock**
- User creates lock transaction on source chain
- Bridge detects lock and mints on destination
- User maintains custody throughout

### Recommended Flow

```
User Wallet (Zcash) → User Signs Lock Tx → Bridge Detects → Mint on Destination → User Wallet (Aztec/Miden)
     ↑                                                                                        ↓
     └─────────────────────── No custody, user maintains control ────────────────────────────┘
```

---

## 🛠️ Implementation Options

### Option A: Wallet Connect / Signature-Based (Best UX)

**How It Works:**
1. User connects their wallet (Zcash, Aztec, Miden)
2. User initiates bridge from their wallet
3. User signs transaction with their private key
4. Bridge orchestrates the cross-chain operation
5. User receives funds directly to their destination wallet

**Benefits:**
- ✅ Non-custodial
- ✅ User maintains control
- ✅ No pre-transfer needed
- ✅ Better privacy (no bridge pool exposure)

**Implementation:**
```csharp
// User provides their wallet address and signs transaction
var request = new CreateBridgeOrderRequest
{
    FromToken = "ZEC",
    ToToken = "MIDEN",
    Amount = 1.0m,
    FromAddress = "zs1user_address", // User's Zcash address
    DestinationAddress = "mtst1user_address", // User's Miden address
    UserId = userId,
    // User signs transaction with their wallet
    UserSignedTransaction = signedTx, // From wallet extension
    EnableViewingKeyAudit = true
};
```

### Option B: Lock & Mint Pattern (More Decentralized)

**How It Works:**
1. User creates lock transaction on source chain (signed by user)
2. Bridge oracle detects lock event
3. Bridge verifies lock via viewing key (for Zcash)
4. Bridge mints on destination chain
5. User receives funds directly

**Benefits:**
- ✅ Fully non-custodial
- ✅ User initiates everything
- ✅ Bridge only orchestrates
- ✅ No bridge pool needed

**Implementation:**
```csharp
// Step 1: User locks funds (user signs)
var lockTx = await zcashProvider.CreateShieldedTransactionAsync(
    fromAddress: "zs1user_address",
    toAddress: "zs1bridge_lock_address", // Bridge lock address
    amount: 1.0m,
    memo: "bridge:miden:mtst1user_address"
);

// Step 2: Bridge detects lock and mints
var bridgeOrder = await bridgeManager.CreateBridgeOrderFromLockAsync(
    lockTransactionHash: lockTx.Result,
    destinationAddress: "mtst1user_address"
);
```

### Option C: Hybrid Model (Current + Improvements)

**How It Works:**
1. Support both models:
   - **Custodial**: User transfers to bridge pool (current)
   - **Non-Custodial**: User signs directly (new)

**Benefits:**
- ✅ Backward compatible
- ✅ User choice
- ✅ Gradual migration

---

## 📋 Recommended Implementation Plan

### Phase 1: Add User Signature Support

**Update `CreateBridgeOrderRequest`:**
```csharp
public class CreateBridgeOrderRequest
{
    // ... existing fields ...
    
    /// <summary>
    /// User-signed transaction for non-custodial operations
    /// </summary>
    public string UserSignedTransaction { get; set; } = string.Empty;
    
    /// <summary>
    /// User's private key (encrypted, for signing)
    /// </summary>
    public string EncryptedPrivateKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to use non-custodial mode (user signs)
    /// </summary>
    public bool UseNonCustodialMode { get; set; } = false;
}
```

### Phase 2: Update Bridge Services

**ZcashBridgeService:**
```csharp
public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(
    decimal amount, 
    string senderAccountAddress, 
    string senderPrivateKey)
{
    // If non-custodial mode and private key provided
    if (!string.IsNullOrEmpty(senderPrivateKey))
    {
        // User signs transaction from their wallet
        return await _rpcClient.SendShieldedTransactionAsync(
            fromAddress: senderAccountAddress,
            toAddress: _bridgePoolAddress,
            amount: amount,
            privateKey: senderPrivateKey, // User's key
            memo: "bridge_lock"
        );
    }
    
    // Fallback to bridge pool (custodial)
    return await _rpcClient.SendShieldedTransactionAsync(
        senderAccountAddress ?? _bridgePoolAddress,
        _bridgePoolAddress,
        amount,
        "withdrawal"
    );
}
```

### Phase 3: Wallet UI Integration

**Update Wallet UI to:**
1. Show bridge option for user's wallets
2. Allow user to select source wallet (their own)
3. Request user signature for transaction
4. Show bridge progress
5. Display destination wallet (user's own)

---

## 🎯 Current Answer to Your Question

### **Current State: YES, Requires Pre-Transfer**

**Current Implementation:**
- ❌ Assets must be in bridge pool first
- ❌ User needs to transfer to bridge pool before bridging
- ❌ This is a custodial model

**Why:**
- Bridge services use `_bridgePoolAddress` as fallback
- `WithdrawAsync` ignores user's private key
- Bridge pool holds funds during operation

### **Ideal State: NO, Direct Wallet Support**

**Recommended Implementation:**
- ✅ User signs transaction from their own wallet
- ✅ No pre-transfer needed
- ✅ Non-custodial operation
- ✅ User maintains control

---

## 🚀 Quick Win: Wallet UI Enhancement

### Current Wallet UI Flow

**What Users See:**
1. Go to Bridge page
2. Select tokens to bridge
3. Enter amounts
4. **Problem**: Must have assets in OASIS wallet first

### Improved Wallet UI Flow

**What Users Should See:**
1. Go to Bridge page
2. **Connect External Wallet** (Zcash, Aztec, Miden)
3. Select source wallet (their own)
4. Select destination wallet (their own)
5. Enter amount
6. **Sign Transaction** (with their wallet)
7. Bridge orchestrates
8. Funds arrive in destination wallet

**Benefits:**
- ✅ No pre-transfer needed
- ✅ User maintains custody
- ✅ Better privacy
- ✅ More decentralized

---

## 📝 Summary

### Current Architecture
- **Model**: Custodial (bridge pool)
- **Requirement**: Assets must be in bridge pool first
- **User Experience**: Transfer → Bridge → Receive

### Recommended Architecture
- **Model**: Non-custodial (user signs)
- **Requirement**: User signs transaction from their wallet
- **User Experience**: Sign → Bridge → Receive (no pre-transfer)

### Implementation Priority
1. **High**: Add user signature support to bridge services
2. **High**: Update wallet UI to support external wallet connection
3. **Medium**: Add lock & mint pattern for fully decentralized option
4. **Low**: Maintain bridge pool as fallback option

---

**Next Steps:**
1. Update bridge services to accept user-signed transactions
2. Add wallet connection UI for external wallets
3. Implement signature verification
4. Test non-custodial flow end-to-end

