# Solana Payment Error Report - expandedPrivateKey.Count

## Problem Summary
When attempting to send Solana payments using the OASIS API, we encounter a persistent error: `"expandedPrivateKey.Count"` when trying to create a `Solnet.Wallet.Account` from a stored private key.

## Context
- **Project**: MNEE Hackathon Submission - Autonomous AI Agent Payment Network
- **Technology Stack**: C# (.NET), Solnet library, OASIS API
- **Goal**: Enable autonomous AI agents to send SOL payments using wallets generated at registration

## Error Details

### Error Message
```
Error sending payment: expandedPrivateKey.Count
```

### Where It Occurs
- **File**: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/SolanaController.cs`
- **Method**: `SendToAvatar` endpoint
- **Line**: When creating `Account` or `Wallet` from stored private key

### Code Location
```csharp
// Attempting to create Account from stored private key
var privateKey = privateKeysResult.Result.First(); // Base64 string
byte[] privateKeyBytes = Convert.FromBase64String(privateKey); // 64 bytes

// All of these approaches fail with the same error:

// Approach 1: Account(string, string)
Account senderAccount = new Account(privateKey, fromWalletResult.Result);
// ❌ Error: expandedPrivateKey.Count

// Approach 2: Wallet(byte[])
byte[] seedBytes = new byte[32];
Array.Copy(privateKeyBytes, 0, seedBytes, 0, 32);
var wallet = new Wallet(seedBytes);
Account senderAccount = wallet.Account;
// ❌ Error: expandedPrivateKey.Count

// Approach 3: Account(byte[], string) - doesn't compile
Account senderAccount = new Account(privateKeyBytes, fromWalletResult.Result);
// ❌ Compilation error: cannot convert from 'byte[]' to 'string'
```

## How Private Keys Are Stored

### Wallet Creation (Working)
```csharp
// In SolanaBridgeService.CreateAccountAsync
Mnemonic mnemonic = new(WordList.English, WordCount.Twelve);
Wallet wallet = new(mnemonic);

string publicKey = wallet.Account.PublicKey;
string privateKey = Convert.ToBase64String(wallet.Account.PrivateKey); // 64-byte keypair
string seedPhrase = string.Join(" ", mnemonic.Words);

// Stored in OASIS KeyManager:
// - Private key: Base64 string of 64-byte keypair (32-byte seed + 32-byte public key)
// - Public key: Base58 string
// - Seed phrase: 12-word mnemonic (NOT currently stored)
```

### Private Key Format
- **Stored format**: Base64 encoded string
- **Decoded bytes**: 64 bytes total
  - First 32 bytes: Seed (used to derive keypair)
  - Next 32 bytes: Public key
- **Example**: `wallet.Account.PrivateKey` is a `byte[]` of length 64

## What We've Tried

### 1. Account Constructor with Base64 String
```csharp
Account senderAccount = new Account(privateKey, fromWalletResult.Result);
```
- **Result**: ❌ Runtime error: `expandedPrivateKey.Count`
- **Theory**: Account constructor tries to decode/expand the base64 string and count bytes, but format doesn't match

### 2. Account Constructor with Base58 Encoded Seed
```csharp
byte[] seedBytes = new byte[32];
Array.Copy(privateKeyBytes, 0, seedBytes, 0, 32);
string seedBase58 = SimpleBase.Base58.Bitcoin.Encode(seedBytes);
Account senderAccount = new Account(seedBase58, fromWalletResult.Result);
```
- **Result**: ❌ Runtime error: `expandedPrivateKey.Count`

### 3. Wallet Constructor with Seed Bytes
```csharp
byte[] seedBytes = new byte[32];
Array.Copy(privateKeyBytes, 0, seedBytes, 0, 32);
var wallet = new Wallet(seedBytes);
Account senderAccount = wallet.Account;
```
- **Result**: ❌ Runtime error: `expandedPrivateKey.Count`
- **Note**: Wallet constructor also fails with same error

### 4. Account Constructor with Byte Array
```csharp
Account senderAccount = new Account(privateKeyBytes, fromWalletResult.Result);
```
- **Result**: ❌ Compilation error: `cannot convert from 'byte[]' to 'string'`
- **Note**: Account constructor only accepts `(string, string)`, not `(byte[], string)`

## Reference Implementation (Working)

### JavaScript/TypeScript (x402 project)
```javascript
// This works in x402 project
const { Keypair } = require('@solana/web3.js');
const bs58 = require('bs58');

// Parse secret key (supports JSON array or base58 string)
let secretKey;
if (rawKey.startsWith('[')) {
  secretKey = Uint8Array.from(JSON.parse(rawKey));
} else {
  secretKey = Uint8Array.from(bs58.decode(rawKey));
}

// Create keypair from raw bytes - THIS WORKS
const keypair = Keypair.fromSecretKey(secretKey);
```

### C# Reference (SolanaOnChainFundingPublisher.cs)
```csharp
// This code exists in our codebase and supposedly works
byte[] privateKeyBytes = Convert.FromBase64String(privateKey);
_signerAccount = new Account(privateKeyBytes, publicKey);
```
- **Problem**: This constructor doesn't exist in our Solnet version
- **Compilation error**: `cannot convert from 'byte[]' to 'string'`

## Environment Details

### Solnet Library
- **Package**: `Solnet.Wallet`
- **Version**: Need to verify (see investigation below)
- **Namespace**: `Solnet.Wallet`

### .NET Version
- **Framework**: .NET (need to verify exact version)
- **Project**: `NextGenSoftware.OASIS.API.ONODE.WebAPI`

## Questions for Investigation

1. **What format does Solnet `Account` constructor actually expect?**
   - The error "expandedPrivateKey.Count" suggests it's trying to decode/expand the private key string
   - What format should the private key string be in?

2. **Why does `Account(byte[], string)` not exist in our version?**
   - `SolanaOnChainFundingPublisher` uses this constructor successfully
   - Is there a version mismatch?

3. **What's the correct way to recreate an Account from stored private key?**
   - Should we store the mnemonic instead?
   - Should we use a different format for the private key?

4. **Is there a way to sign transactions directly with raw bytes?**
   - Can we bypass the Account constructor entirely?

## Expected Behavior

When a payment is initiated:
1. Retrieve the sender's private key from OASIS KeyManager ✅ (works)
2. Decode base64 to get private key bytes ✅ (works)
3. Create `Account` from private key ❌ (fails here)
4. Use `Account` to sign transaction
5. Send signed transaction to Solana network

## Current Workaround Attempts

We've modified `SolanaService.SendTransaction` to accept an optional `Account` parameter:
```csharp
public async Task<OASISResult<SendTransactionResult>> SendTransaction(
    SendTransactionRequest sendTransactionRequest, 
    Account signerAccount = null)
{
    Account accountToUse = signerAccount ?? oasisAccount;
    // ... build and sign transaction
}
```

This architecture is correct, but we can't create the `signerAccount` from the stored private key.

## Additional Context

- The wallet generation works perfectly - keys are created and stored correctly
- The public key matches what we expect
- The issue is purely in recreating the `Account` object from the stored private key
- We need to use the actual keys generated at wallet creation (not a temporary account)

## Files Involved

1. `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/SolanaController.cs`
   - `SendToAvatar` method (lines ~115-210)

2. `/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/Infrastructure/Services/Solana/SolanaService.cs`
   - `SendTransaction` method (modified to accept optional Account)

3. `/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/Infrastructure/Services/Solana/SolanaBridgeService.cs`
   - `CreateAccountAsync` method (creates and stores keys)

## Next Steps Needed

1. Determine correct format for `Account` constructor
2. Verify Solnet library version and available constructors
3. Find working example of recreating Account from stored private key in C#/Solnet
4. Consider alternative: store mnemonic and recreate wallet from seed phrase

