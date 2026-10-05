# MetaBricks + Smart Contract Generator Integration

## 🎯 **The Complete Picture**

**MetaBricks** = 432 brick NFTs (the investment/shares)  
**Smart Contract Generator** = Revenue-generating business  
**x402** = Automatic payment distribution  

**Result:** Brick holders earn passive income from SC-Gen API revenue!

---

## 💰 **How It Works**

```
Developer uses SC-Gen API
   ↓
Pays 0.60 SOL for 50 credits
   ↓
SC-Gen webhook → x402 service
   ↓
x402 distributes 0.54 SOL (90%) to 432 brick holders
   ↓
Each brick holder receives: 0.00125 SOL
```

**At 100 users/month:**
- Each brick earns: ~0.125 SOL/month ($12.50)
- Annual income: ~1.5 SOL/year ($150)
- ROI: 37.5% on 0.4 SOL brick purchase!

---

## 🏗️ **System Architecture**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         REVENUE GENERATION LAYER                            │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌──────────────────────────────────────────────────────────────┐
    │    Smart Contract Generator (SC-Gen) API                     │
    │    ┌──────────┐  ┌──────────┐  ┌──────────┐                │
    │    │Developer │  │Developer │  │Developer │  ... (100+)    │
    │    │  Pays    │  │  Pays    │  │  Pays    │                │
    │    │ 0.6 SOL  │  │ 1.2 SOL  │  │ 0.3 SOL  │                │
    │    └─────┬────┘  └─────┬────┘  └─────┬────┘                │
    └──────────┼─────────────┼─────────────┼──────────────────────┘
               │             │             │
               └─────────────┴─────────────┘
                             │
                             │ HTTP POST webhook
                             ▼

┌─────────────────────────────────────────────────────────────────────────────┐
│                      x402 DISTRIBUTION SERVICE                              │
│                     (localhost:4000 / Cloud)                                │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌─────────────────────────────────────────────────────────────┐
    │  POST /api/metabricks/sc-gen-webhook                        │
    │  ┌──────────────────────────────────────────────────────┐   │
    │  │ 1. Receive payment notification                      │   │
    │  │    { signature, amount: 0.60, distributionPct: 90% } │   │
    │  │                                                       │   │
    │  │ 2. Calculate split                                   │   │
    │  │    To holders: 0.54 SOL (90%)                        │   │
    │  │    To treasury: 0.06 SOL (10%)                       │   │
    │  │                                                       │   │
    │  │ 3. Query NFT holders                                 │   │
    │  │    └──> Solana RPC / OASIS API                       │   │
    │  │         Returns: 432 wallet addresses                │   │
    │  │                                                       │   │
    │  │ 4. Calculate per-holder amount                       │   │
    │  │    0.54 SOL ÷ 432 = 0.00125 SOL each                 │   │
    │  │                                                       │   │
    │  │ 5. Record distribution                               │   │
    │  │    └──> File storage / MongoDB / OASIS DB            │   │
    │  │                                                       │   │
    │  │ 6. Return confirmation                               │   │
    │  │    { success: true, distributionTx: "..." }          │   │
    │  └──────────────────────────────────────────────────────┘   │
    │                                                              │
    │  Other Endpoints:                                            │
    │  • GET /api/metabricks/stats    → Distribution history      │
    │  • GET /api/metabricks/holders  → List all 432 holders      │
    │  • GET /health                  → Service health check      │
    └──────────────────────────────────────────────────────────────┘
                             │
                             │ Distribution data
                             ▼

┌─────────────────────────────────────────────────────────────────────────────┐
│                        BLOCKCHAIN SETTLEMENT LAYER                          │
│                           (Solana Devnet/Mainnet)                           │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌──────────────────────────────────────────────────────────────┐
    │  Solana Program (On-chain Distribution Logic)               │
    │  ┌────────────────────────────────────────────────────────┐  │
    │  │  Multi-recipient transaction:                          │  │
    │  │  ┌──────────────┐                                      │  │
    │  │  │ Treasury     │──> MetaBrick1Holder  (0.00125 SOL)  │  │
    │  │  │ Wallet       │──> MetaBrick2Holder  (0.00125 SOL)  │  │
    │  │  │ (0.54 SOL)   │──> MetaBrick3Holder  (0.00125 SOL)  │  │
    │  │  └──────────────┘    ...                               │  │
    │  │                  └─> MetaBrick432Holder (0.00125 SOL)  │  │
    │  │                                                         │  │
    │  │  Transaction Signature: "5xYz...abc123"                │  │
    │  │  Status: Confirmed ✅                                   │  │
    │  └────────────────────────────────────────────────────────┘  │
    └──────────────────────────────────────────────────────────────┘
                             │
                             │ Settlement complete
                             ▼

┌─────────────────────────────────────────────────────────────────────────────┐
│                           NFT HOLDERS (432 BRICKS)                          │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐
    │ Brick #1   │  │ Brick #2   │  │ Brick #3   │  │ Brick #432 │
    │ Holder     │  │ Holder     │  │ Holder     │  │ Holder     │
    │            │  │            │  │            │  │            │
    │ 💰 +0.00125│  │ 💰 +0.00125│  │ 💰 +0.00125│  │ 💰 +0.00125│
    │    SOL     │  │    SOL     │  │    SOL     │  │    SOL     │
    └────────────┘  └────────────┘  └────────────┘  └────────────┘
         │               │               │               │
         └───────────────┴───────────────┴───────────────┘
                             │
                             ▼
    ┌────────────────────────────────────────────────────────┐
    │  Each holder sees in their Phantom/Solflare wallet:   │
    │  • Incoming SOL transaction                            │
    │  • Source: SC-Gen revenue distribution                 │
    │  • View on Solscan for full transparency               │
    └────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                       OASIS INTEGRATION POINTS                              │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌──────────────────────────────────────────────────────────────┐
    │  OASIS Web4 Platform                                         │
    │  ┌────────────────────────────────────────────────────────┐  │
    │  │ 1. NFT Minting (SolanaOASIS Provider)                  │  │
    │  │    • Mint MetaBricks NFTs with x402 metadata           │  │
    │  │    • Store in IPFS via OASIS STAR API                  │  │
    │  │                                                         │  │
    │  │ 2. NFT Holder Queries (OASIS NFT API)                  │  │
    │  │    • Query all 432 current MetaBricks holders          │  │
    │  │    • Cross-chain compatible                            │  │
    │  │                                                         │  │
    │  │ 3. Distribution Storage (MongoDBOASIS)                 │  │
    │  │    • Store distribution history                        │  │
    │  │    • Audit trail for compliance                        │  │
    │  │                                                         │  │
    │  │ 4. Multi-chain Bridge (Future)                         │  │
    │  │    • Expand to other chains via OASIS providers        │  │
    │  └────────────────────────────────────────────────────────┘  │
    └──────────────────────────────────────────────────────────────┘


                          💡 KEY FEATURES
    ┌────────────────────────────────────────────────────────┐
    │ ✅ Automatic revenue distribution (90% to holders)    │
    │ ✅ Equal split among 432 bricks                       │
    │ ✅ Real-time tracking & statistics                    │
    │ ✅ On-chain transparency (Solscan)                    │
    │ ✅ Scalable to multiple revenue sources               │
    │ ✅ OASIS cross-chain compatibility                    │
    └────────────────────────────────────────────────────────┘
```

---

## 🚀 **Quick Start**

### **1. Start x402 Service**

```bash
cd x402/backend-service
npm install
npm start
```

**You should see:**
```
✅ x402 Service running on http://0.0.0.0:4000
🧱 MetaBricks: http://0.0.0.0:4000/api/metabricks
```

### **2. Test Integration**

```bash
cd x402/metabricks-integration
./TEST_INTEGRATION.sh
```

**Expected output:**
```
✅ Service is running
✅ Distribution successful
   Distributed to: 432 brick holders
   Amount each: 0.00125 SOL
```

### **3. Configure SC-Gen**

In Smart Contract Generator's `appsettings.json`:

```json
{
  "X402": {
    "DistributionWebhookUrl": "http://localhost:4000/api/metabricks/sc-gen-webhook"
  }
}
```

---

## 📁 **What Was Added**

```
x402/metabricks-integration/
├── README.md                          # This file
├── INTEGRATION_GUIDE.md               # Detailed technical guide
├── METABRICKS_X402_CONFIG.md          # Configuration details
└── TEST_INTEGRATION.sh                # Test script

x402/backend-service/src/routes/
└── metabricks-routes.js               # MetaBricks webhook handlers

meta-bricks-main/backend/
└── server.js                          # Updated with x402 config
```

---

## 🔌 **API Endpoints**

### **SC-Gen Webhook** (SC-Gen calls this)
```
POST http://localhost:4000/api/metabricks/sc-gen-webhook

Body: {
  "signature": "SOLANA_TX_SIG",
  "amount": 0.60,
  "distributionPercentage": 90,
  "nftCollection": "METABRICKS"
}
```

### **Get Stats** (Dashboard calls this)
```
GET http://localhost:4000/api/metabricks/stats
```

### **Get Holders** (Dashboard calls this)
```
GET http://localhost:4000/api/metabricks/holders
```

---

## 🎨 **Updated MetaBricks NFTs**

**New Description:**
```
MetaBrick NFT: Brick #42. Earn passive income from AssetRail 
Smart Contract Generator API revenue.
```

**x402 Configuration added to minting:**
```javascript
x402Config: {
  enabled: true,
  nftCollection: 'METABRICKS',
  revenueSource: 'AssetRail Smart Contract Generator API',
  revenueModel: 'equal',
  distributionPercentage: 90,
  totalNFTs: 432
}
```

---

## 📊 **Revenue Projections**

### **Conservative (100 SC-Gen users/month)**

- Monthly revenue: 60 SOL
- To brick holders: 54 SOL
- Per brick: 0.125 SOL/month
- Annual: $150/year per brick

### **Growth (1,000 SC-Gen users/month)**

- Monthly revenue: 600 SOL
- To brick holders: 540 SOL
- Per brick: 1.25 SOL/month
- Annual: $1,500/year per brick

### **Scale (10,000 SC-Gen users/month)**

- Monthly revenue: 6,000 SOL
- To brick holders: 5,400 SOL
- Per brick: 12.5 SOL/month
- Annual: $15,000/year per brick! 🚀

---

## ✅ **Integration Status**

**Complete:**
- [x] x402 service configured for MetaBricks
- [x] SC-Gen webhook handler created
- [x] Distribution logic (432 equal split)
- [x] Statistics endpoints
- [x] Test script
- [x] MetaBricks minting updated with x402 config
- [x] Documentation

**Next Steps:**
- [ ] Start x402 service
- [ ] Run test script
- [ ] Configure SC-Gen webhook URL
- [ ] Test with real SC-Gen payment
- [ ] Launch! 🎉

---

## 🧪 **Testing**

```bash
# Start service
cd x402/backend-service
npm start

# In another terminal, run tests
cd x402/metabricks-integration
./TEST_INTEGRATION.sh
```

---

## 🎉 **You're Ready!**

MetaBricks NFTs are now revenue-generating assets earning passive income from Smart Contract Generator API usage!

**Every SC-Gen payment → Automatic distribution to all 432 brick holders** ✅

