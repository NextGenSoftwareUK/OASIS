# Payment Signature Issue - Assessment

## The Problem

**Symptom**: Payment fails with error `"expandedPrivateKey.Count"` when trying to send SOL from one avatar to another.

**What's happening**: The `SendToAvatar` endpoint successfully:
1. ✅ Gets the authenticated avatar's private key from wallet
2. ✅ Decodes base64 to get private key bytes
3. ❌ Fails when creating `Account` from the private key

## Why We're Doing This

**Root Cause**: The original `SolanaService.SendTransaction` method uses a **temporary OASIS account** (`oasisAccount`) to sign **ALL transactions**, regardless of who the actual sender is:

```csharp
// In SolanaService.cs line 158
.Build(oasisAccount);  // ← Always uses temporary account!
```

This means:
- ❌ Transactions are signed with the wrong account
- ❌ Signature verification fails
- ❌ Payments don't work

**Our Solution**: Created new endpoint `POST /api/solana/SendToAvatar/{toAvatarId}` that:
1. Gets the **actual sender's private key** from their wallet
2. Creates an `Account` from that private key
3. Signs the transaction with the **correct account**

## The Persistent Issue

**Error**: `"expandedPrivateKey.Count"`

This error occurs inside the Solnet `Account` constructor when it tries to decode/expand the private key. The constructor expects a specific format, but we're providing it in a format it can't parse.

## What We Know

### How Private Keys Are Stored

From `SolanaBridgeService.CreateAccountAsync` (line 81):
```csharp
string privateKey = Convert.ToBase64String(wallet.Account.PrivateKey);
```

- `wallet.Account.PrivateKey` is a **64-byte byte array** (32-byte seed + 32-byte public key)
- It's stored as **base64 string** in OASIS
- When decoded: sometimes 64 bytes, sometimes 66 bytes (padding?)

### Account Constructor Usage

From `SolanaOasis.cs` (line 49):
```csharp
this._oasisSolanaAccount = new(privateKey, publicKey);
```

- Both parameters are **strings**
- `privateKey` is the base64 string (as stored)
- This works in `SolanaOasis` constructor

From `SolanaOnChainFundingPublisher.cs` (line 66):
```csharp
_signerAccount = new Account(privateKeyBytes, publicKey);
```

- First parameter is **byte array**
- Second parameter is **string**
- This suggests Account has multiple constructors

## What We've Tried

1. **Base64 string directly** → `new Account(privateKey, publicKey)`
   - ❌ Failed: "expandedPrivateKey.Count"

2. **Base58 encoded seed (32 bytes)** → Extract first 32 bytes, encode to base58
   - ❌ Failed: "expandedPrivateKey.Count"

3. **Base58 encoded full keypair (64 bytes)** → Encode entire 64 bytes to base58
   - ❌ Failed: "expandedPrivateKey.Count"

4. **Wallet class from seed bytes** → `new Wallet(seedBytes).Account`
   - ⏳ Not tested yet (code updated but API not restarted)

## The Real Question

**What format does Solnet `Account` constructor actually expect?**

Looking at the error "expandedPrivateKey.Count", it seems the Account constructor is trying to:
1. Decode/expand the private key string
2. Count the bytes
3. Validate the format
4. **This validation is failing**

## Possible Solutions

### Option 1: Use Wallet Class (Current Attempt)
```csharp
var wallet = new Wallet(seedBytes);
Account senderAccount = wallet.Account;
```
- ✅ Wallet handles the key derivation
- ✅ Account is properly created
- ⚠️ Need to verify public key matches

### Option 2: Store Private Key Differently
Instead of storing `wallet.Account.PrivateKey` (64 bytes), store:
- The **seed phrase** (mnemonic) - can recreate wallet
- Or the **32-byte seed** directly (not the full keypair)

### Option 3: Use SolanaOasis Pattern
Look at how `SolanaOasis` creates accounts - it uses the base64 string directly. Maybe we need to use the same approach but ensure the format matches exactly.

### Option 4: Modify SolanaService
Instead of creating a new endpoint, modify `SolanaService.SendTransaction` to:
- Accept an optional `Account` parameter
- If provided, use that account instead of `oasisAccount`
- This would be a cleaner architectural fix

## Recommended Next Steps

1. **Test Option 1** (Wallet class) - API needs restart
2. **If that fails**, check what `wallet.Account.PrivateKey` actually contains vs what Account constructor expects
3. **Consider Option 4** - Modify SolanaService to accept account parameter (cleaner architecture)
4. **Debug the Account constructor** - Add logging to see what format it's expecting

## Key Insight

The issue is that `wallet.Account.PrivateKey` is a **64-byte keypair**, but the `Account` constructor might expect:
- A **32-byte seed** (to derive the keypair)
- Or a **specific encoding format** (base58 vs base64)

We need to understand what the Account constructor actually does with the private key string.

