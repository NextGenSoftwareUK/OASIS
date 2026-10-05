# Shielded Transactions in Browser: Our Approach

## Question from Builder

> "Does your app actually run in the browser? I had to settle for using a transparent address and hacky workarounds for shielding on my app. Can you shield addresses in your browser app, and if so, can you share your approach?"

## Answer: Yes, We Can Shield in Browser - Here's How

### Overview

**Yes, our Zypherpunk wallet runs in the browser and performs true shielded Zcash transactions.** We don't use transparent addresses or workarounds. Here's our architecture:

## Architecture: Server-Side Shielding via RPC

### Key Insight

We don't run `zcashd` in the browser (that would be impossible). Instead, we use a **hybrid architecture**:

1. **Browser (Frontend)**: Next.js React app that handles UI and user interactions
2. **Backend API (OASIS)**: C# .NET API that connects to a `zcashd` full node via RPC
3. **Zcash Node**: Full `zcashd` node running on the server that performs actual shielded transactions

### How It Works

```
┌─────────────────┐         ┌──────────────────┐         ┌──────────────┐
│   Browser App   │  HTTP   │   OASIS API      │   RPC    │   zcashd     │
│  (Next.js/React)│ ──────> │   (.NET C#)     │ ───────> │   Full Node  │
│                 │         │                  │          │              │
│ - UI/UX         │         │ - ZcashRPCClient │          │ - Creates    │
│ - Validation    │         │ - Shielded Tx    │          │   shielded   │
│ - User Input    │         │   Builder        │          │   txs        │
└─────────────────┘         └──────────────────┘         └──────────────┘
```

### Implementation Details

#### 1. Frontend (Browser)

**File**: `zypherpunk-wallet-ui/components/privacy/ShieldedSendScreen.tsx`

```typescript
// User initiates shielded transaction from browser
const result = await privacyAPI.createShieldedTransaction({
  fromWalletAddress: wallet.walletAddress,  // z-address
  toWalletAddress: recipient,               // z-address
  amount: numAmount,
  memoText: memo,
  privacyLevel: 'maximum',
  usePartialNotes: true,
  generateViewingKey: true,
});
```

**Key Points**:
- User enters shielded addresses (z-addresses starting with `z` or `zt`)
- Frontend validates addresses are shielded format
- Sends request to OASIS API backend
- No private keys leave the browser (they're stored server-side)

#### 2. Backend API (OASIS)

**File**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ZcashOASIS/Infrastructure/Services/Zcash/ZcashRPCClient.cs`

```csharp
public async Task<OASISResult<string>> SendShieldedTransactionAsync(
    string fromAddress, 
    string toAddress, 
    decimal amount, 
    string memo = null)
{
    // Uses zcashd RPC method: z_sendmany
    var request = new
    {
        fromaddress = fromAddress,
        amounts = new[]
        {
            new { address = toAddress, amount = amount }
        },
        memo = memo,
        minconf = 1,
        fee = 0.0001m
    };
    
    var response = await SendRPCRequestAsync("z_sendmany", request);
    return response; // Returns transaction ID
}
```

**Key Points**:
- Uses `zcashd` RPC methods (`z_sendmany`, `z_exportviewingkey`)
- Connects to full Zcash node via JSON-RPC
- Handles shielded transaction creation server-side
- Returns transaction IDs to frontend

#### 3. Zcash Node (Server)

- Full `zcashd` node running on server
- Maintains shielded wallet with private keys
- Performs actual zk-SNARK proof generation
- Broadcasts transactions to Zcash network

## Why This Works

### Advantages

1. **True Shielding**: Uses actual `zcashd` RPC, so transactions are genuinely shielded
2. **Browser Compatible**: No need to run heavy node software in browser
3. **Secure**: Private keys stay on server (can be further secured with hardware wallets)
4. **Full Features**: Supports all Zcash features:
   - Shielded transactions (z-to-z)
   - Viewing keys for auditability
   - Partial notes for enhanced privacy
   - Encrypted memos

### Trade-offs

1. **Server Dependency**: Requires backend API and `zcashd` node
2. **Custodial Keys**: Private keys stored server-side (though this can be mitigated)
3. **Network Latency**: RPC calls add latency vs. pure client-side

## Comparison with "Hacky Workarounds"

### What Others Might Do (Workarounds)

❌ **Transparent Addresses**: Using `t-addresses` instead of `z-addresses`
- Not truly private
- Transaction amounts visible on blockchain
- Sender/receiver visible

❌ **Client-Side Simulation**: Trying to build transactions in browser
- Can't generate zk-SNARK proofs without full node
- Results in invalid transactions
- Requires workarounds

### What We Do (Proper Implementation)

✅ **Server-Side RPC**: Use `zcashd` RPC from backend
- True shielded transactions
- Valid zk-SNARK proofs
- Full Zcash protocol compliance

## Technical Stack

### Frontend
- **Framework**: Next.js (React)
- **Language**: TypeScript
- **API Client**: Custom API client calling OASIS backend

### Backend
- **Framework**: .NET Core (C#)
- **Provider**: `ZcashOASIS` provider
- **RPC Client**: `ZcashRPCClient` class
- **Node**: `zcashd` full node

### RPC Methods Used

1. **`z_sendmany`**: Create and send shielded transaction
2. **`z_exportviewingkey`**: Generate viewing keys for auditability
3. **`z_getbalance`**: Get shielded balance
4. **`z_listaddresses`**: List shielded addresses

## Code Examples

### Creating Shielded Transaction

**Frontend**:
```typescript
// Browser calls API
const result = await privacyAPI.createShieldedTransaction({
  fromWalletAddress: "zt1...",  // Shielded address
  toWalletAddress: "zt1...",    // Shielded address
  amount: 1.5,
  memoText: "Private message",
  privacyLevel: "maximum",
  usePartialNotes: true
});
```

**Backend**:
```csharp
// OASIS API processes request
var tx = await _zcashService.CreateShieldedTransactionAsync(
    fromAddress, 
    toAddress, 
    amount, 
    memo
);

// Uses zcashd RPC
var txId = await _rpcClient.SendShieldedTransactionAsync(
    fromAddress, 
    toAddress, 
    amount, 
    memo
);
```

## Security Considerations

### Current Implementation
- Private keys stored server-side in `zcashd` wallet
- API authenticated with JWT tokens
- RPC calls secured with username/password

### Future Enhancements (Non-Custodial)
- **Hardware Wallet Integration**: Use hardware wallets for key storage
- **Multi-Sig**: Require multiple signatures for transactions
- **Key Derivation**: Derive keys from user's seed phrase (browser-side)
- **Threshold Signatures**: Split key management across multiple parties

## Sharing This Approach

### For Other Builders

If you want to implement true shielded transactions in a browser app:

1. **Set up `zcashd` node** on your server
2. **Create RPC client** in your backend (we use C#, but any language works)
3. **Expose API endpoints** for shielded transaction operations
4. **Call from browser** via HTTP/HTTPS

### Key Requirements

- Full `zcashd` node (not light client)
- RPC access to node (JSON-RPC over HTTP)
- Backend API to proxy RPC calls
- Proper authentication/authorization

### Alternative: Light Client Libraries

If you can't run a full node, consider:
- **Zcash Light Client SDKs** (if available)
- **Third-party services** that provide shielded transaction APIs
- **WASM implementations** of Zcash protocol (experimental)

## Conclusion

**Yes, we can and do perform true shielded transactions from our browser app.** The key is using a server-side `zcashd` node via RPC, rather than trying to do everything client-side. This gives us:

- ✅ True privacy (zk-SNARK proofs)
- ✅ Browser compatibility
- ✅ Full Zcash feature support
- ✅ No workarounds or hacks

We're happy to share more details or help other builders implement similar solutions!

## Contact

For questions or collaboration:
- Check our codebase: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ZcashOASIS/`
- Review our implementation: `ZcashRPCClient.cs`
- See our frontend: `zypherpunk-wallet-ui/components/privacy/ShieldedSendScreen.tsx`

