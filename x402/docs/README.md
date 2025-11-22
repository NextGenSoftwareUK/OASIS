# OASIS x402 Integration

**Revenue-Generating NFTs on Solana**

Automatic payment distribution to NFT holders using x402 protocol and OASIS cross-chain infrastructure.

---

## 🎯 Overview

This project enables **automatic revenue distribution** to NFT holders whenever payments are made via x402 protocol. Perfect for:

- 🎵 Music streaming revenue sharing
- 🏠 Real estate rental income distribution
- 🔌 API usage revenue sharing
- 🎬 Content creator ad revenue distribution
- 🎮 Gaming item revenue sharing

**Key Features:**
- ✅ Automatic payment distribution via x402 webhooks
- ✅ Ultra-low cost ($0.001 per recipient)
- ✅ Fast distribution (5-30 seconds)
- ✅ Built-in holder tracking
- ✅ Analytics & reporting
- ✅ Cross-chain ready (via OASIS)

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   REVENUE SOURCE                         │
│     (Spotify / Rentals / API Usage / Ad Platform)       │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│              x402 PAYMENT ENDPOINT                       │
│      https://api.yourservice.com/x402/pay/nft           │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│          OASIS X402 PAYMENT DISTRIBUTOR                  │
│  • Query NFT holders from Solana                        │
│  • Calculate distribution amounts                       │
│  • Execute multi-recipient transaction                  │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│              SOLANA BLOCKCHAIN                           │
│  • Transfer SOL/USDC to each holder                     │
│  • Confirmation in 5-30 seconds                         │
│  • Ultra-low fees ($0.001 per holder)                  │
└─────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Start

### Prerequisites

```bash
node >= 18.x
npm >= 9.x
Solana CLI (optional, for local testing)
```

### Installation

```bash
# Clone the repository
git clone https://github.com/oasis-platform/x402-integration
cd x402-integration

# Install dependencies
npm install

# Configure environment
cp .env.example .env
# Edit .env with your credentials
```

### Environment Variables

```env
# Solana Configuration
SOLANA_RPC_URL=https://api.devnet.solana.com
SOLANA_NETWORK=devnet

# OASIS API Configuration
OASIS_API_URL=https://api.oasis.one
OASIS_API_KEY=your_api_key_here

# x402 Configuration
X402_WEBHOOK_URL=https://your-domain.com/api/x402/webhook
X402_WEBHOOK_SECRET=your_webhook_secret
```

---

## 📖 Usage Examples

### Example 1: Mint NFT with x402 Revenue Sharing

```typescript
import axios from 'axios';

const response = await axios.post('https://api.oasis.one/api/mint-nft-x402', {
  // Standard NFT fields
  walletAddress: 'YourSolanaWalletAddress',
  brickName: 'Music Revenue Share NFT #1',
  imageUrl: 'https://ipfs.io/album-cover.png',
  paymentNetwork: 'solana',
  
  // x402 configuration
  x402Config: {
    enabled: true,
    paymentEndpoint: 'https://api.yourservice.com/x402/revenue',
    revenueModel: 'equal', // Options: 'equal', 'weighted', 'creator-split'
  }
});

console.log('NFT Minted:', response.data.nft.mintAddress);
console.log('x402 URL:', response.data.x402.paymentUrl);
```

### Example 2: Simulate Payment Distribution (Testing)

```typescript
// Test payment distribution
const response = await axios.post('https://api.oasis.one/api/x402/distribute-test', {
  nftMintAddress: 'YourNFTMintAddress',
  amount: 1.0 // 1 SOL
});

console.log(`Distributed to ${response.data.result.recipients} holders`);
console.log(`Each holder received: ${response.data.result.amountPerHolder} SOL`);
```

### Example 3: Get Payment Statistics

```typescript
const stats = await axios.get(
  'https://api.oasis.one/api/x402/stats/YourNFTMintAddress'
);

console.log('Total Distributed:', stats.data.totalDistributed, 'SOL');
console.log('Distribution Count:', stats.data.distributionCount);
console.log('Holder Count:', stats.data.holderCount);
```

---

## 🎵 Real-World Use Case: Music Streaming NFT

### Scenario
Artist releases album, wants fans to share in streaming revenue.

### Implementation

```typescript
// 1. Artist mints 1,000 NFTs
const mintResponse = await axios.post('/api/mint-nft-x402', {
  walletAddress: artistWallet,
  brickName: 'Album Revenue Share Token',
  x402Config: {
    enabled: true,
    paymentEndpoint: 'https://streaming-api.com/x402/revenue',
    revenueModel: 'equal'
  }
});

// 2. Fans buy NFTs (each = 1 share of revenue)
// ... NFT marketplace integration ...

// 3. Monthly streaming revenue: $10,000
// Streaming platform sends payment via x402:
POST https://api.oasis.one/api/x402/webhook
{
  "amount": 10000000000, // 10 SOL (assuming 1 SOL = $1000)
  "currency": "SOL",
  "metadata": {
    "nftMintAddress": "ABC123...",
    "serviceType": "streaming_revenue"
  }
}

// 4. OASIS automatically distributes:
// - 1,000 NFT holders
// - 10 SOL / 1,000 = 0.01 SOL per holder
// - ~$10 per holder per month

// 5. Each holder receives payment in their Solana wallet!
```

---

## 🏠 Real Estate Rental Income Example

```typescript
// Tokenize property with 3,500 tokens
const propertyNFT = await axios.post('/api/mint-nft-x402', {
  walletAddress: trustWallet,
  brickName: '123 Sunset Blvd - Property Share #1',
  x402Config: {
    enabled: true,
    paymentEndpoint: 'https://property-trust.com/x402/rental',
    revenueModel: 'equal',
    metadata: {
      propertyAddress: '123 Sunset Blvd, Beverly Hills, CA',
      monthlyRent: 7875,
      totalTokens: 3500
    }
  }
});

// Monthly rental income automatically distributed
// $7,875 / 3,500 tokens = $2.25 per token holder
```

---

## 🔌 API Revenue Sharing Example

```typescript
// Developer offers API access via NFT
const apiNFT = await axios.post('/api/mint-nft-x402', {
  walletAddress: developerWallet,
  brickName: 'Premium API Access Token',
  x402Config: {
    enabled: true,
    paymentEndpoint: 'https://your-api.com/x402/usage',
    revenueModel: 'equal',
    metadata: {
      accessTier: 'premium',
      rateLimitPerDay: 100000,
      paymentPerRequest: 0.00001 // $0.00001 per API call
    }
  }
});

// Every API call generates micro-payment via x402
// Revenue automatically split among all NFT holders
```

---

## 🛠️ Technical Components

### 1. X402PaymentDistributor.ts
Core service that handles payment distribution logic.

**Key Methods:**
- `registerNFTForX402()` - Register NFT with x402 endpoint
- `handleX402Payment()` - Process incoming payments
- `getNFTHolders()` - Query current holders
- `distributePayments()` - Execute multi-recipient transfers

### 2. x402-oasis-middleware.ts
Express.js middleware that integrates with your existing OASIS API.

**Endpoints:**
- `POST /api/mint-nft-x402` - Mint NFT with x402 support
- `POST /api/x402/webhook` - Receive x402 payment notifications
- `GET /api/x402/stats/:nft` - Get payment statistics
- `POST /api/x402/distribute-test` - Test distribution

### 3. example-usage.ts
Complete examples for all use cases.

---

## 📊 Performance Metrics

| Metric | Value |
|--------|-------|
| Distribution Speed | 5-30 seconds |
| Cost per Recipient | ~$0.001 |
| Supported Recipients | Unlimited |
| Uptime | 99.9% (Solana network) |
| Supported Currencies | SOL, USDC |

---

## 🧪 Testing

### Run Tests

```bash
# Unit tests
npm test

# Integration tests
npm run test:integration

# E2E tests (requires Solana devnet)
npm run test:e2e
```

### Manual Testing

```bash
# Start local OASIS API server
npm run dev

# In another terminal, test payment distribution
curl -X POST http://localhost:3000/api/x402/distribute-test \
  -H "Content-Type: application/json" \
  -d '{
    "nftMintAddress": "MOCK_NFT_123",
    "amount": 0.1
  }'
```

---

## 📁 Project Structure

```
x402-integration/
├── X402PaymentDistributor.ts    # Core distribution service
├── x402-oasis-middleware.ts     # Express.js middleware
├── example-usage.ts              # Usage examples
├── X402_HACKATHON_PITCH_DECK.html  # Pitch deck
├── X402_ONE_PAGER.md            # Executive summary
├── README.md                     # This file
├── package.json
├── tsconfig.json
└── tests/
    ├── distributor.test.ts
    ├── middleware.test.ts
    └── integration.test.ts
```

---

## 🚀 Deployment

### Deploy to Production

```bash
# Build TypeScript
npm run build

# Deploy to your preferred platform
# Railway
railway up

# Or Heroku
git push heroku main

# Or Docker
docker build -t oasis-x402 .
docker run -p 3000:3000 oasis-x402
```

### Webhook Configuration

1. Set up public HTTPS endpoint for webhooks
2. Configure x402 webhook URL in your payment provider
3. Verify webhook signature for security

```typescript
// Webhook signature validation (production)
function validateX402Signature(signature: string, payload: any): boolean {
  const expectedSignature = crypto
    .createHmac('sha256', process.env.X402_WEBHOOK_SECRET!)
    .update(JSON.stringify(payload))
    .digest('hex');
  return signature === expectedSignature;
}
```

---

## 🔒 Security Considerations

1. **Webhook Signature Validation:** Always validate x402 webhook signatures in production
2. **Rate Limiting:** Implement rate limiting on payment endpoints
3. **Wallet Security:** Use secure key management for treasury wallet
4. **Error Handling:** Graceful failure with retry mechanisms
5. **Monitoring:** Set up alerts for failed distributions

---

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## 📄 License

MIT License - See [LICENSE](LICENSE) for details.

---

## 🔗 Links

- **OASIS Platform:** https://oasis.one
- **x402 Protocol:** https://solana.com/x402
- **Documentation:** https://docs.oasis.one/x402
- **Live Demo:** https://x402.oasis.one
- **GitHub:** https://github.com/oasis-platform/x402-integration

---

## 📞 Support

- **Email:** support@oasis.one
- **Discord:** discord.gg/oasis
- **Twitter:** @oasis_web4

---

## 🏆 Hackathon Submission

**Built for:** x402 Solana Hackathon 2025  
**Team:** OASIS Platform  
**Submission Date:** January 2026

**What We Built:**
- ✅ Complete x402 payment distribution system
- ✅ OASIS API integration
- ✅ Multiple real-world use cases
- ✅ Production-ready code
- ✅ Comprehensive documentation
- ✅ Live demo

**Innovation:**
First implementation of x402 protocol for automatic NFT revenue distribution, enabling cash-flowing digital assets.

---

**Made with 💚 by the OASIS team**

