# x402 Treasury Wallet Feature - User Guide

## ✅ **Feature Added: User-Controlled Treasury Wallets**

Users can now enter their own Solana wallet for x402 revenue distributions! This makes the system **fully decentralized** and **trustless**.

---

## 🎨 **What Users Will See**

### **Step 4: x402 Revenue Sharing**

```
┌──────────────────────────────────────────────────────┐
│ 💰 Enable x402 Revenue Sharing           [✓ Enabled] │
│ Automatically distribute payments to NFT holders      │
└──────────────────────────────────────────────────────┘

Distribution Model:
┌─────────────┬─────────────┬─────────────┐
│ ⚖️ [ACTIVE] │ 📊          │ 🎨          │
│ Equal Split │ Weighted    │ Creator     │
└─────────────┴─────────────┴─────────────┘

x402 Payment Endpoint URL:
┌──────────────────────────────────────────────────────┐
│ https://api.oasisweb4.one/x402/revenue              │
└──────────────────────────────────────────────────────┘
[Auto-generate OASIS endpoint]

💎 Treasury Wallet (Optional):
┌──────────────────────────────────────────────────────┐
│ Enter your Solana wallet address                     │
└──────────────────────────────────────────────────────┘
Revenue will be sent to this wallet. Distributions 
will be made FROM this wallet. Leave empty to use 
OASIS Web4 platform treasury.

[🦊 Use Connected Wallet (Phantom)]  [Clear]

┌──────────────────────────────────────────────────────┐
│ ☑️ Pre-authorize automatic distributions              │
│                                                       │
│ ✅ Distributions will happen automatically without   │
│ requiring your approval each time. You'll sign one   │
│ authorization transaction during minting.            │
└──────────────────────────────────────────────────────┘

✨ Configuration Preview:
  Revenue Model: Equal Split
  Distribution: realtime
  Endpoint: https://api.oasisweb4.one/...
  Treasury: ABC1...xyz9 ✨ NEW
  Auto-distribute: Yes ✨ NEW
```

---

## 🔄 **How It Works**

### **Option 1: User Provides Treasury Wallet**

**User Flow:**
```
1. User navigates to x402 config step
   ↓
2. User enables x402 toggle
   ↓
3. User clicks "🦊 Use Connected Wallet (Phantom)"
   ↓
4. Phantom popup: "Connect wallet?"
   ↓
5. User approves
   ↓
6. Wallet address auto-fills: "ABC123...xyz789"
   ↓
7. User checks "Pre-authorize automatic distributions"
   ↓
8. User proceeds to mint
   ↓
9. During minting: One additional Phantom popup
   "Authorize OASIS Web4 to distribute up to 100 SOL/month"
   ↓
10. User approves authorization
   ↓
11. NFT minted with user's treasury wallet configured
```

**Result:**
- ✅ User controls their own funds
- ✅ Revenue comes to THEIR wallet
- ✅ Pre-authorized distributions are automatic
- ✅ No need to trust OASIS Web4 platform
- ✅ Fully decentralized!

### **Option 2: Platform Treasury (Default)**

**User Flow:**
```
1. User leaves treasury wallet field empty
   ↓
2. System uses OASIS Web4 platform treasury
   ↓
3. Simpler setup, no extra steps
```

**Result:**
- ✅ Simpler for users
- ✅ No wallet management needed
- ⚠️ Platform holds funds (centralized)

---

## 💻 **Technical Implementation**

### **What Gets Stored:**

**NFT Metadata (MongoDB):**
```json
{
  "nftMintAddress": "ABC123...",
  "x402Config": {
    "enabled": true,
    "paymentEndpoint": "https://api.oasisweb4.one/x402/revenue",
    "revenueModel": "equal",
    "treasuryWallet": "ABC123xyz789...",  // ✨ User's wallet
    "preAuthorizeDistributions": true,     // ✨ Auto-approve
    "metadata": {
      "contentType": "music",
      "distributionFrequency": "realtime"
    }
  }
}
```

**On-Chain (Solana Program):**
```rust
// Program Derived Address (PDA) for authorization
pub struct DistributionAuthorization {
    pub treasury_wallet: Pubkey,        // User's wallet
    pub nft_mint: Pubkey,               // NFT mint address
    pub max_amount_per_month: u64,      // Authorization limit
    pub authorized_distributor: Pubkey, // OASIS Web4 program
    pub bump: u8,
}

// User signs this once during minting
// OASIS Web4 can then distribute automatically within limits
```

---

## 🔐 **Two Distribution Modes**

### **Mode A: Manual Approval (Default)**

**When distribution happens:**
```
1. Revenue arrives ($1,000)
   ↓
2. x402 webhook triggered
   ↓
3. Backend creates distribution transaction
   ↓
4. Frontend shows Phantom popup:
   "Approve distribution of 1.0 SOL to 250 holders?"
   [Approve] [Reject]
   ↓
5. User clicks [Approve]
   ↓
6. Transaction signed and submitted
   ↓
7. Distribution complete!
```

**Pros:**
- ✅ User approves each distribution
- ✅ Maximum control
- ✅ Can review before approving

**Cons:**
- ⚠️ User must be online
- ⚠️ Requires manual action
- ⚠️ Not truly "automatic"

### **Mode B: Pre-Authorized (Recommended)** ⭐

**One-time setup (during minting):**
```
1. User mints NFT with x402 + treasury wallet
   ↓
2. Additional Phantom popup appears:
   "Authorize OASIS Web4 to distribute revenue?"
   • Max 100 SOL per month
   • Valid for 1 year
   • Can revoke anytime
   [Authorize] [Cancel]
   ↓
3. User clicks [Authorize]
   ↓
4. Authorization stored on-chain (PDA)
   ↓
5. Setup complete!
```

**When distributions happen:**
```
1. Revenue arrives ($1,000)
   ↓
2. x402 webhook triggered
   ↓
3. Backend checks: Is pre-authorized? ✅ Yes
   ↓
4. Distribution executes automatically
   ↓
5. No user interaction needed!
   ↓
6. Complete in 30 seconds
```

**Pros:**
- ✅ Truly automatic
- ✅ User doesn't need to be online
- ✅ Still decentralized (user sets limits)
- ✅ Can revoke authorization anytime

**Cons:**
- ⚠️ Slightly more complex initial setup
- ⚠️ Requires on-chain authorization program

---

## 🎯 **What the Payload Looks Like**

### **With User Treasury Wallet:**

```json
{
  "Title": "My Music NFT",
  "Symbol": "MUSIC",
  "OnChainProvider": { "value": 3, "name": "SolanaOASIS" },
  "ImageUrl": "https://ipfs.io/album.png",
  "SendToAddressAfterMinting": "UserWallet123...",
  
  "x402Config": {
    "enabled": true,
    "paymentEndpoint": "https://api.oasisweb4.one/x402/revenue",
    "revenueModel": "equal",
    
    // ✨ NEW FIELDS:
    "treasuryWallet": "ABC123xyz789...",  // User's wallet
    "preAuthorizeDistributions": true,     // Auto-approve
    
    "metadata": {
      "contentType": "music",
      "distributionFrequency": "realtime",
      "revenueSharePercentage": 100
    }
  }
}
```

---

## 🔄 **Distribution Flow with User Treasury**

### **Scenario: Music NFT with 1,000 holders**

**Month 1:**
```
Revenue Source (Spotify): $10,000 earned
   ↓
Sends payment to x402 endpoint:
POST https://api.oasisweb4.one/x402/webhook
{
  "amount": 10000000000,  // 10 SOL
  "targetWallet": "ABC123xyz789...",  // User's treasury
  "metadata": { "nftMintAddress": "MUSIC_NFT_123" }
}
   ↓
OASIS Web4 Backend:
- Receives webhook
- Verifies: treasuryWallet = "ABC123xyz789..."
- Checks: preAuthorizeDistributions = true ✅
- Queries Solana: 1,000 current holders
- Calculates: 10 SOL / 1,000 = 0.01 SOL each
   ↓
Creates transaction:
FROM: ABC123xyz789... (user's treasury wallet)
TO:   [1,000 holder addresses]
AMOUNT: 0.01 SOL each
   ↓
Signs with pre-authorization (PDA):
- User authorized OASIS Web4 during minting
- OASIS Web4 can sign on behalf of user (within limits)
- Uses program derived address (PDA) for security
   ↓
Submits to Solana blockchain
   ↓
5-30 seconds later:
- All 1,000 holders receive 0.01 SOL
- Total cost: ~$1 in fees
- User's wallet balance: -10 SOL (revenue distributed)
```

**User Experience:**
- Revenue generated → Holders paid automatically
- User sees transaction in wallet history
- No action required from user! ✅

---

## 🛡️ **Security: How Pre-Authorization Works**

### **Solana Program Derived Address (PDA):**

```rust
// On-chain authorization account
#[account]
pub struct DistributionAuth {
    pub owner: Pubkey,              // User's wallet
    pub nft_mint: Pubkey,           // Which NFT this applies to
    pub authorized_program: Pubkey, // OASIS Web4 distributor program
    pub max_per_month: u64,         // Safety limit (e.g., 100 SOL)
    pub max_per_distribution: u64,  // Safety limit (e.g., 10 SOL)
    pub valid_until: i64,           // Expiration timestamp
    pub total_distributed: u64,     // Track usage
    pub bump: u8,
}

// User creates this during minting:
pub fn authorize_distributions(
    ctx: Context<AuthorizeDistributions>,
    max_per_month: u64,
    max_per_distribution: u64,
    valid_until: i64,
) -> Result<()> {
    let auth = &mut ctx.accounts.authorization;
    
    auth.owner = ctx.accounts.owner.key();
    auth.nft_mint = ctx.accounts.nft_mint.key();
    auth.authorized_program = ctx.accounts.distributor_program.key();
    auth.max_per_month = max_per_month;
    auth.max_per_distribution = max_per_distribution;
    auth.valid_until = valid_until;
    auth.total_distributed = 0;
    auth.bump = ctx.bumps.authorization;
    
    msg!("Distribution authorized for NFT: {}", auth.nft_mint);
    Ok(())
}

// OASIS Web4 uses this to distribute without user signature:
pub fn distribute_with_authorization(
    ctx: Context<DistributeAuthorized>,
    amount: u64,
) -> Result<()> {
    let auth = &mut ctx.accounts.authorization;
    
    // Verify authorization is valid
    require!(
        Clock::get()?.unix_timestamp < auth.valid_until,
        ErrorCode::AuthorizationExpired
    );
    
    // Verify within limits
    require!(
        amount <= auth.max_per_distribution,
        ErrorCode::ExceedsDistributionLimit
    );
    
    // Check monthly limit
    require!(
        auth.total_distributed + amount <= auth.max_per_month,
        ErrorCode::ExceedsMonthlyLimit
    );
    
    // Execute distribution
    // ... transfer logic ...
    
    // Update tracking
    auth.total_distributed += amount;
    
    Ok(())
}
```

**Safety Features:**
- ✅ Time-limited (expires after 1 year)
- ✅ Amount-limited (max 100 SOL/month)
- ✅ Per-distribution limit (max 10 SOL/distribution)
- ✅ Revocable (user can cancel anytime)
- ✅ Tracked (all distributions logged on-chain)

---

## 🎯 **What Users See in Step 4**

### **New Fields Added:**

**1. Treasury Wallet Input:**
```
💎 Treasury Wallet (Optional)
┌──────────────────────────────────────────────────────┐
│                                                       │
└──────────────────────────────────────────────────────┘
Revenue will be sent to this wallet. Distributions 
will be made FROM this wallet. Leave empty to use 
OASIS Web4 platform treasury.

[🦊 Use Connected Wallet (Phantom)]  [Clear]
```

**When user clicks "Use Connected Wallet":**
```
1. Phantom popup appears: "Connect wallet?"
2. User approves
3. Field auto-fills: "ABC123xyz789..."
4. ✅ Wallet connected!
```

**2. Pre-Authorization Checkbox:**

```
☑️ Pre-authorize automatic distributions

✅ Distributions will happen automatically without 
requiring your approval each time. You'll sign one 
authorization transaction during minting.
```

**Or if unchecked:**
```
☐ Pre-authorize automatic distributions

Distributions will require your approval via Phantom 
popup each time. More secure but requires you to be 
online.
```

---

## 📊 **Configuration Preview Updates**

**Preview box now shows:**
```
✨ Configuration Preview

Revenue Model:    Equal Split
Distribution:     realtime
Endpoint:         https://api.oasisweb4.one/...
Treasury:         ABC1...xyz9  ✨ NEW
Auto-distribute:  Yes          ✨ NEW
```

---

## 🎬 **Step 5: Review & Mint**

**Summary section shows:**
```
Summary:
┌──────────────────────────────────────┐
│ Title: My Music NFT                  │
│ Symbol: MUSIC                        │
│ x402 Revenue Sharing: equal          │
│ Treasury Wallet: ABC1...xyz9 ✨ NEW  │
└──────────────────────────────────────┘
```

**Enhanced x402 status box:**
```
┌──────────────────────────────────────────────────────┐
│ 💰 x402 Revenue Sharing Enabled                      │
│                                                       │
│ This NFT will automatically distribute payments to   │
│ all holders when revenue is generated. Payments      │
│ sent to api.oasisweb4.one/x402/revenue will trigger │
│ automatic distribution using the equal model.        │
│ ─────────────────────────────────────────────────── │
│ Treasury Wallet: ABC123xyz...xyz789                  │
│ ✅ Pre-authorized for automatic distributions        │
└──────────────────────────────────────────────────────┘
```

---

## 🚀 **User Experience Scenarios**

### **Scenario A: Artist Using Own Wallet**

**Emma the Artist:**
```
1. Emma wants to create music NFTs for her album
2. She navigates to Step 4 (x402 config)
3. Enables x402 revenue sharing
4. Selects "Equal Split" - fans share revenue equally
5. Clicks "Use Connected Wallet" - Phantom auto-fills her address
6. Checks "Pre-authorize" - wants automatic distributions
7. Reviews: Sees her wallet listed (ABC1...xyz9)
8. Mints 1,000 NFTs
9. Phantom pops up: "Authorize OASIS Web4 to distribute up to 100 SOL/month?"
10. Emma approves
11. NFTs minted successfully!

Monthly revenue:
- Spotify sends $10,000 to Emma's wallet
- OASIS Web4 automatically distributes to 1,000 fans
- Each fan gets $10
- Emma doesn't need to do anything!
- Revenue flows automatically from her wallet to fans
```

**Benefits for Emma:**
- ✅ She controls her own wallet
- ✅ Revenue comes to HER first
- ✅ Automatic distributions (no manual work)
- ✅ Can set safety limits
- ✅ Can revoke authorization anytime

### **Scenario B: Developer Using Platform Treasury**

**Mike the Developer:**
```
1. Mike wants to create API access NFTs
2. Navigates to Step 4 (x402 config)
3. Enables x402 revenue sharing
4. Leaves treasury wallet EMPTY (uses platform)
5. Reviews: Sees "Treasury: OASIS Web4 Platform"
6. Mints NFTs

Monthly revenue:
- API usage generates $1,000
- Sent to OASIS Web4 platform treasury
- OASIS Web4 distributes to holders
- Simple, no wallet management needed
```

**Benefits for Mike:**
- ✅ Simpler setup (no wallet config)
- ✅ No authorization needed
- ✅ Still automatic distributions
- ⚠️ Trusts OASIS Web4 platform

---

## 🔧 **Backend Handling**

### **When Minting:**

```typescript
// Backend receives mint request with treasury wallet
app.post('/api/mint-nft-x402', async (req, res) => {
  const { x402Config, ...mintData } = req.body;
  
  // Mint NFT
  const nft = await mintNFT(mintData);
  
  if (x402Config.enabled) {
    // Register x402 config
    await registerX402Config({
      nftMintAddress: nft.mintAddress,
      paymentEndpoint: x402Config.paymentEndpoint,
      revenueModel: x402Config.revenueModel,
      treasuryWallet: x402Config.treasuryWallet || PLATFORM_TREASURY,
      preAuthorized: x402Config.preAuthorizeDistributions
    });
    
    // If user wants pre-authorization
    if (x402Config.treasuryWallet && x402Config.preAuthorizeDistributions) {
      // Create authorization transaction
      const authTx = await createAuthorizationTransaction({
        userWallet: x402Config.treasuryWallet,
        nftMint: nft.mintAddress,
        maxPerMonth: 100 * LAMPORTS_PER_SOL,
        validFor: 365 * 24 * 60 * 60  // 1 year
      });
      
      // Return to frontend for user to sign
      return res.json({
        success: true,
        nft: nft,
        x402: { registered: true },
        requiresAuthorization: true,
        authorizationTx: authTx  // Frontend will prompt user to sign
      });
    }
    
    return res.json({ success: true, nft, x402: { registered: true } });
  }
});
```

### **When Distributing:**

```typescript
// Backend handles x402 webhook
app.post('/api/x402/webhook', async (req, res) => {
  const { amount, metadata } = req.body;
  
  // Get NFT config
  const nftConfig = await getX402Config(metadata.nftMintAddress);
  
  // Get holders
  const holders = await getNFTHolders(metadata.nftMintAddress);
  
  // Calculate distribution
  const amountPerHolder = amount / holders.length;
  
  // Check if pre-authorized
  if (nftConfig.preAuthorized) {
    // Distribute using PDA (no user signature needed)
    const signature = await distributeWithPDA({
      treasuryWallet: nftConfig.treasuryWallet,
      holders: holders,
      amountPerHolder: amountPerHolder,
      nftMint: metadata.nftMintAddress
    });
    
    return res.json({ 
      success: true, 
      signature,
      method: 'pre-authorized'
    });
  } else {
    // Request user signature
    const tx = await createDistributionTransaction({
      from: nftConfig.treasuryWallet || PLATFORM_TREASURY,
      holders: holders,
      amountPerHolder: amountPerHolder
    });
    
    // Send to frontend for signature
    // OR notify user to approve
    return res.json({ 
      success: 'pending',
      requiresSignature: true,
      transaction: tx
    });
  }
});
```

---

## 📱 **Frontend Integration**

### **In mint-review-panel.tsx:**

**After minting, if authorization needed:**
```typescript
const response = await call(endpoint, {
  method: "POST",
  body: JSON.stringify(payload),
});

// Check if authorization required
if (response.requiresAuthorization && response.authorizationTx) {
  // Show Phantom popup for authorization
  const authTx = response.authorizationTx;
  
  if (window.solana) {
    const signedTx = await window.solana.signTransaction(authTx);
    
    // Submit authorization
    await call('/api/x402/submit-authorization', {
      method: 'POST',
      body: JSON.stringify({ signedTx })
    });
    
    alert('✅ Pre-authorization complete! Distributions will now happen automatically.');
  }
}
```

---

## 🎨 **Visual Examples**

### **With Treasury Wallet Configured:**

**Review Screen shows:**
```
┌──────────────────────────────────────────────────────┐
│ 💰 x402 Revenue Sharing Enabled                      │
│                                                       │
│ This NFT will automatically distribute payments to   │
│ all holders when revenue is generated.               │
│                                                       │
│ ─────────────────────────────────────────────────── │
│ Treasury Wallet: ABC123xyz...xyz789                  │
│ ✅ Pre-authorized for automatic distributions        │
│                                                       │
│ Revenue will flow FROM your wallet to all NFT        │
│ holders. You approved distributions up to 100 SOL    │
│ per month. You can revoke this authorization at      │
│ any time from your dashboard.                        │
└──────────────────────────────────────────────────────┘
```

### **Without Treasury Wallet (Platform Default):**

```
┌──────────────────────────────────────────────────────┐
│ 💰 x402 Revenue Sharing Enabled                      │
│                                                       │
│ This NFT will automatically distribute payments to   │
│ all holders when revenue is generated.               │
│                                                       │
│ ─────────────────────────────────────────────────── │
│ Treasury: OASIS Web4 Platform                        │
│ ℹ️  Using platform-managed treasury for simplicity   │
└──────────────────────────────────────────────────────┘
```

---

## ✅ **Integration Complete!**

### **What You Can Now Do:**

**✅ Users can:**
- Enter their own Solana wallet address
- Click "Use Connected Wallet" to auto-fill from Phantom
- Choose automatic vs manual approval
- See treasury wallet in review screen
- Full decentralized control!

**✅ System stores:**
- Treasury wallet address
- Pre-authorization preference
- Displays in payload
- Ready for backend processing

**✅ Next step:**
- Backend uses this info to distribute FROM user's wallet
- Either auto-approved (PDA) or requests signature

---

## 🎉 **Benefits of This Approach**

### **Decentralization:**
- ✅ User controls their own funds
- ✅ No platform custody
- ✅ Trustless operation
- ✅ Can revoke anytime

### **User Experience:**
- ✅ One-click wallet connection (Phantom)
- ✅ Clear preview of settings
- ✅ Choose auto vs manual approval
- ✅ Transparent about what happens

### **Security:**
- ✅ On-chain authorization with limits
- ✅ Time-limited (expires)
- ✅ Amount-limited (safety caps)
- ✅ Revocable (user can cancel)

---

## 🚀 **Test It Now**

```bash
cd "/Volumes/Storage 2/OASIS_CLEAN/nft-mint-frontend"
npm run dev
```

**What to test:**
1. Navigate to Step 4 (x402 config)
2. Enable x402
3. Click "🦊 Use Connected Wallet"
4. Phantom should prompt to connect
5. Address auto-fills in field
6. Check "Pre-authorize" checkbox
7. See preview update with your wallet
8. Proceed to Step 5 (Review)
9. See treasury wallet in summary
10. Check payload JSON - should include treasuryWallet & preAuthorizeDistributions

---

## 📊 **Payload Example**

```json
{
  "Title": "My Music NFT",
  "x402Config": {
    "enabled": true,
    "paymentEndpoint": "https://api.oasisweb4.one/x402/revenue",
    "revenueModel": "equal",
    "treasuryWallet": "ABC123xyz789abc...",      // ✨ User's wallet
    "preAuthorizeDistributions": true,            // ✨ Auto-approve
    "metadata": {
      "contentType": "music"
    }
  }
}
```

---

**Feature complete! Ready to test!** 🎉

Next: Let's tackle the revenue distribution question. Want me to create the revenue source integration guide?

