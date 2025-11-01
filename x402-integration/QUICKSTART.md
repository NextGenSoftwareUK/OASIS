# OASIS x402 Integration - Quick Start Guide

Get up and running with revenue-generating NFTs in 5 minutes!

---

## 🚀 **Option 1: Test with Demo Frontend (Easiest)**

1. **Open the demo:**
```bash
open demo-frontend.html
```

2. **Connect your Phantom wallet**

3. **Mint an x402-enabled NFT:**
   - Enter your wallet address
   - Give your NFT a name
   - Select revenue model (equal split recommended)
   - Click "Mint x402 NFT"

4. **Test payment distribution:**
   - Switch to "Test Distribution" tab
   - Enter your NFT mint address
   - Set amount (e.g., 1 SOL)
   - Click "Distribute Payment"

**Done!** You just created an NFT that automatically pays its holders.

---

## 🔧 **Option 2: Integrate with Your App**

### **Step 1: Install**
```bash
npm install @oasis/x402-integration
```

### **Step 2: Mint NFT with x402**
```typescript
import axios from 'axios';

const response = await axios.post('https://api.oasis.one/api/mint-nft-x402', {
  walletAddress: 'YourSolanaWallet',
  brickName: 'Revenue Share NFT #1',
  imageUrl: 'https://ipfs.io/your-image.png',
  paymentNetwork: 'solana',
  
  // Enable x402 revenue distribution
  x402Config: {
    enabled: true,
    paymentEndpoint: 'https://api.yourservice.com/x402/revenue',
    revenueModel: 'equal'
  }
});

console.log('NFT Minted:', response.data.nft.mintAddress);
console.log('x402 URL:', response.data.x402.paymentUrl);
```

### **Step 3: Handle x402 Webhooks**
```typescript
import { X402PaymentDistributor } from '@oasis/x402-integration';

const distributor = new X402PaymentDistributor({
  solanaRpcUrl: 'https://api.devnet.solana.com',
  oasisApiUrl: 'https://api.oasis.one',
  oasisApiKey: 'your-api-key'
});

// In your Express server
app.post('/api/x402/webhook', async (req, res) => {
  const result = await distributor.handleX402Payment(req.body);
  res.json({ success: true, result });
});
```

**That's it!** Payments now automatically distribute to NFT holders.

---

## 🎵 **Option 3: Real-World Example (Music NFT)**

```typescript
// Artist mints 1,000 NFTs for album
const albumNFT = await axios.post('/api/mint-nft-x402', {
  walletAddress: artistWallet,
  brickName: 'Album Revenue Share Token',
  imageUrl: 'https://ipfs.io/album-cover.png',
  paymentNetwork: 'solana',
  
  x402Config: {
    enabled: true,
    paymentEndpoint: 'https://streaming-api.com/x402/revenue',
    revenueModel: 'equal',
    metadata: {
      artist: 'Artist Name',
      album: 'Album Title',
      releaseDate: '2026-01-01'
    }
  }
});

// Fans buy NFTs on marketplace
// ... NFT sales logic ...

// Each month, streaming platform sends payment:
// POST https://api.oasis.one/api/x402/webhook
// {
//   "amount": 10000000000,  // 10 SOL
//   "metadata": {
//     "nftMintAddress": "ABC123...",
//     "serviceType": "streaming_revenue"
//   }
// }

// OASIS automatically distributes:
// 10 SOL / 1,000 holders = 0.01 SOL each (~$10)
// Payment arrives in holder wallets within 30 seconds!
```

---

## 🏠 **Option 4: Real Estate Rental Income**

```typescript
// Tokenize property
const propertyNFT = await axios.post('/api/mint-nft-x402', {
  walletAddress: trustWallet,
  brickName: '123 Main St - Property Share',
  imageUrl: 'https://ipfs.io/property-photo.jpg',
  paymentNetwork: 'solana',
  
  x402Config: {
    enabled: true,
    paymentEndpoint: 'https://property-trust.com/x402/rental',
    revenueModel: 'equal',
    metadata: {
      propertyAddress: '123 Main St, City, State',
      monthlyRent: 5000,
      totalTokens: 1000
    }
  }
});

// Monthly rent distribution:
// $5,000 / 1,000 tokens = $5 per holder per month
// Automatic via x402!
```

---

## 📊 **Check Distribution Stats**

```typescript
// Get payment statistics
const stats = await axios.get(
  'https://api.oasis.one/api/x402/stats/YourNFTMintAddress'
);

console.log('Total Distributed:', stats.data.totalDistributed, 'SOL');
console.log('Distribution Count:', stats.data.distributionCount);
console.log('Current Holders:', stats.data.holderCount);
console.log('Avg per Distribution:', stats.data.averagePerDistribution);
```

---

## 🧪 **Testing Locally**

```bash
# 1. Clone the repo
git clone https://github.com/oasis-platform/x402-integration
cd x402-integration

# 2. Install dependencies
npm install

# 3. Configure environment
cp .env.example .env
# Edit .env with your settings

# 4. Run development server
npm run dev

# 5. Test in another terminal
curl -X POST http://localhost:3000/api/x402/distribute-test \
  -H "Content-Type: application/json" \
  -d '{"nftMintAddress": "MOCK_NFT", "amount": 0.1}'
```

---

## ⚡ **Key Concepts**

### **x402 Payment Endpoint**
- URL where revenue sources send payments
- Example: `https://api.yourservice.com/x402/revenue/nft123`
- Webhook triggers OASIS distributor

### **Revenue Models**
- **Equal:** All holders get same amount
- **Weighted:** Based on token holdings
- **Creator Split:** Fixed % to creator, rest to holders

### **Distribution Flow**
```
Payment → x402 Endpoint → OASIS Webhook → Query Holders → 
Calculate Splits → Execute Solana Transfer → Complete in 30s
```

---

## 🆘 **Troubleshooting**

**NFT minting fails:**
- Check wallet has enough SOL for gas
- Verify OASIS API key is valid
- Ensure Solana RPC endpoint is working

**Distribution not happening:**
- Verify webhook URL is publicly accessible
- Check x402 signature validation
- Review server logs for errors

**Holders not receiving payments:**
- Confirm holder addresses are valid
- Check treasury wallet has sufficient funds
- Verify token account exists for SPL tokens

---

## 📚 **Next Steps**

1. **Read full docs:** `README.md`
2. **View examples:** `example-usage.ts`
3. **Review pitch:** `X402_HACKATHON_PITCH_DECK.html`
4. **Deploy to prod:** Follow deployment guide in README

---

## 🔗 **Useful Links**

- **Full Documentation:** README.md
- **API Reference:** Check OASIS_API_COMPLETE_ENDPOINTS_SUMMARY.md
- **x402 Protocol:** https://solana.com/x402
- **OASIS Platform:** https://oasis.one

---

## 💡 **Pro Tips**

1. **Start with testnet:** Use Solana devnet for testing
2. **Monitor gas costs:** Set alerts for treasury wallet balance
3. **Test with small amounts:** Try 0.01 SOL distributions first
4. **Use webhooks wisely:** Validate signatures in production
5. **Track analytics:** Use built-in stats API to monitor distributions

---

## 🎉 **You're Ready!**

You now have everything needed to create revenue-generating NFTs with automatic payment distribution.

**Questions?** hackathon@oasis.one  
**Issues?** github.com/oasis-platform/x402-integration/issues

---

**Built for x402 Solana Hackathon 2025** 🚀

