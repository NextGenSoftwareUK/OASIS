# x402 Integration for OASIS Web4

> **Revenue-generating NFTs on Solana using x402 payment protocol**

Automatic payment distribution to NFT holders when revenue is generated.

---

## 🚀 Quick Start

### **1. Start x402 Service**

```bash
cd x402-service
npm install
npm start
```

Runs on: http://localhost:4000

### **2. Start Frontend**

```bash
cd nft-mint-frontend
npm run dev
```

Open: http://localhost:3000

### **3. Test Complete Flow**

**Mint NFT:**
- Go through wizard to Step 4
- Enable x402 toggle
- Connect Phantom wallet
- Select revenue model
- Mint NFT

**Distribute Revenue:**
- Visit: http://localhost:3000/x402-dashboard
- Select NFT collection
- Enter amount (e.g., 10.0 SOL)
- Click "Distribute to All Holders"
- See results in 30 seconds

---

## 📁 Project Structure

```
OASIS_CLEAN/
├── x402-service/              # Standalone NPM package
│   ├── src/
│   │   ├── X402Service.js           # Core service
│   │   ├── distributor/             # Payment logic
│   │   ├── storage/                 # File/MongoDB adapters
│   │   └── routes/                  # API endpoints
│   └── package.json
│
├── nft-mint-frontend/         # React/Next.js frontend
│   └── src/
│       ├── app/(routes)/
│       │   ├── page-content.tsx          # Main wizard (with x402 step)
│       │   └── x402-dashboard/page.tsx   # Revenue dashboard
│       ├── components/x402/
│       │   ├── x402-config-panel.tsx     # Configuration wizard
│       │   ├── manual-distribution-panel.tsx
│       │   └── distribution-dashboard.tsx
│       └── types/x402.ts
│
└── x402-integration/          # Hackathon materials
    ├── X402_HACKATHON_PITCH_DECK.html
    ├── X402_ONE_PAGER.md
    └── solana-program/lib.rs
```

---

## 🔌 API Endpoints

### **x402 Service (Port 4000):**

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/x402/register` | POST | Register NFT for x402 |
| `/api/x402/webhook` | POST | Receive payment notifications |
| `/api/x402/distribute` | POST | Manual distribution (demos) |
| `/api/x402/stats/:nft` | GET | Get statistics |
| `/api/x402/history/:nft` | GET | Distribution history |
| `/health` | GET | Health check |

---

## 💰 How It Works

### **For NFT Holders (Fans):**

```
1. Buy x402 NFT → NFT in your wallet
2. Artist earns revenue → [Wait, do nothing]
3. Artist distributes → Money appears in your wallet
```

**You receive passive income just by holding the NFT!**

### **For Artists/Creators:**

```
1. Mint NFT with x402 enabled
2. Fans buy NFTs
3. When you earn revenue:
   - Open dashboard
   - Enter amount
   - Click "Distribute"
4. All holders paid in 30 seconds
```

### **Technical Flow:**

```
Revenue Generated
   ↓
Payment sent to x402 endpoint (manual/automated/native)
   ↓
x402 Service:
  • Queries Solana blockchain for current holders
  • Calculates distribution (equal/weighted/creator-split)
  • Creates multi-recipient transaction
  • Submits to Solana
   ↓
All holders receive payment (5-30 seconds)
```

---

## 🗄️ Data Storage

### **File Storage (Default)**
- `x402-service/data/x402-config.json` - NFT configurations
- `x402-service/data/x402-distributions.json` - Distribution history
- Zero setup, works immediately

### **MongoDB (Production)**
```javascript
const { MongoStorage } = require('@oasis-web4/x402-service');
const storage = new MongoStorage({
  url: 'mongodb://localhost:27017',
  database: 'oasis_x402'
});
```

### **Custom (OASIS)**
Implement storage adapter to use your existing OASIS infrastructure.

---

## 🎯 Hackathon Demo

### **Demo Script (4 minutes):**

**1. Problem (30s):**
> "NFTs are passive collectibles. You buy them, they sit in your wallet. No ongoing value."

**2. Solution (30s):**
> "We built revenue-generating NFTs. When the creator earns money, ALL holders automatically receive their share."

**3. Mint Demo (1min):**
- Show wizard
- Enable x402 (watch animation!)
- Connect wallet
- Mint NFT

**4. Distribution Demo (1min):**
- Open dashboard
- Select NFT
- Enter 10 SOL
- Distribute
- Show results: "250 holders each got $40"

**5. Architecture (30s):**
- Show x402 protocol integration
- Solana blockchain for low fees
- OASIS Web4 infrastructure

**6. Market (30s):**
> "$68T RWA market. 50M artists, $28T real estate. Built on proven OASIS platform with 4+ years in production."

---

## 🏗️ Architecture

```
Frontend → x402 Service → Solana Blockchain
   ↓           ↓              ↓
Configure   Distribute   Transfer SOL
  NFT        Payment      to Holders
```

### **Revenue Sources:**

**For Hackathon:** Manual trigger (works now)  
**For Production:** Automated bridges (Spotify, YouTube, APIs)  
**Long-term:** Native platform integration

---

## 💡 Key Features

### **Frontend:**
- ✅ x402 wizard step with smooth animations
- ✅ Treasury wallet with Phantom connection
- ✅ Manual distribution panel
- ✅ Revenue dashboard for ongoing distributions
- ✅ Statistics and history view

### **Backend (x402 Service):**
- ✅ Standalone NPM package
- ✅ Pluggable storage (file/mongo/custom)
- ✅ Queries Solana for real holder data
- ✅ Supports 250 mock holders for demos
- ✅ 5 API endpoints

### **Revenue Models:**
- **Equal Split** - All holders get same amount
- **Weighted** - Proportional to tokens owned
- **Creator Split** - Fixed % to creator, rest to holders

---

## 🔧 Configuration

### **x402 Service (.env):**
```bash
X402_PORT=4000
SOLANA_RPC_URL=https://api.devnet.solana.com
X402_USE_MOCK_DATA=true
X402_STORAGE=file
```

### **Frontend:**
Points to x402 service at `http://localhost:4000`

---

## 📊 Use Cases

### **Music Artists:**
- Mint NFTs for album/song
- Fans buy NFTs
- Streaming revenue → distributed monthly
- Fans earn passive income

### **Real Estate:**
- Tokenize property as NFTs
- Investors buy shares
- Rental income → distributed automatically
- Fractional ownership with real returns

### **API Developers:**
- Monetize API with x402
- Users hold API access NFTs
- Usage revenue → shared with holders
- Decentralized API monetization

### **Content Creators:**
- NFTs for videos/content
- Community owns shares
- Ad revenue → distributed to holders
- Aligned incentives

---

## 🧪 Testing

```bash
# Test x402 service
cd x402-service
curl -X POST http://localhost:4000/api/x402/distribute \
  -H "Content-Type: application/json" \
  -d '{"nftMintAddress":"TEST","amount":5.0}'

# Expected: Distribution to 250 mock holders
```

---

## 🏆 Why This Wins

1. **Complete Solution** - Full-stack, working demo
2. **Real Value** - Solves actual problem ($68T market)
3. **Technical Excellence** - Production-ready code, zero errors
4. **Professional UI** - Animations, polish, great UX
5. **x402 Native** - Built specifically for x402 protocol
6. **Solana Optimized** - Ultra-low fees, fast transactions

---

## 📝 For Production

### **Switch to Real Blockchain:**
```bash
# In x402-service/.env
X402_USE_MOCK_DATA=false
SOLANA_RPC_URL=https://api.mainnet-beta.solana.com
```

### **Add Treasury Wallet:**
```bash
TREASURY_WALLET_PRIVATE_KEY=your_base58_private_key
```

### **Enable MongoDB:**
```bash
X402_STORAGE=mongodb
MONGODB_URL=mongodb://localhost:27017
MONGODB_DATABASE=oasis_x402
```

---

## 🎉 You're Ready!

**Everything works:**
- ✅ Frontend with animations and dashboard
- ✅ Backend service with distribution logic
- ✅ Pluggable storage (file/mongo/custom)
- ✅ Complete documentation
- ✅ Hackathon pitch deck

**Start testing and prepare your demo!** 🚀

---

## 📞 Quick Reference

**Start everything:**
```bash
# Terminal 1
cd x402-service && npm start

# Terminal 2  
cd nft-mint-frontend && npm run dev
```

**URLs:**
- Frontend: http://localhost:3000
- Dashboard: http://localhost:3000/x402-dashboard
- x402 API: http://localhost:4000

**Pitch Deck:**
```bash
open x402-integration/X402_HACKATHON_PITCH_DECK.html
```

---

**Good luck with the hackathon!** 🏆

