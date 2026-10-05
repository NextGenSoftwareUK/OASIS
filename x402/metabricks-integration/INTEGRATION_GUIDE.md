# MetaBricks + Smart Contract Generator x402 Integration

## 🎯 **Complete Integration Guide**

This guide shows how MetaBricks NFTs receive revenue from the Smart Contract Generator API.

---

## 🏗️ **Architecture**

```
┌──────────────────────────────────────┐
│   Smart Contract Generator API        │
│   Developers pay for API usage        │
└─────────────┬────────────────────────┘
              │ Payment: 0.60 SOL
              ↓
┌──────────────────────────────────────┐
│   SC-Gen Backend                      │
│   Verifies payment                    │
└─────────────┬────────────────────────┘
              │ POST webhook
              ↓
┌──────────────────────────────────────┐
│   x402 Service                        │
│   http://localhost:4000               │
│   /api/metabricks/sc-gen-webhook      │
└─────────────┬────────────────────────┘
              │ Query holders
              ↓
┌──────────────────────────────────────┐
│   Solana Blockchain                   │
│   Get all 432 MetaBricks holders      │
└─────────────┬────────────────────────┘
              │ Distribute
              ↓
┌──────────────────────────────────────┐
│   432 MetaBricks Holders              │
│   Each receives equal share           │
└──────────────────────────────────────┘
```

---

## 🚀 **Setup Instructions**

### **Step 1: Start x402 Service**

```bash
cd x402/backend-service
npm install
npm start
```

**You should see:**
```
🚀 Starting OASIS x402 Service...
📦 Using file storage
✅ X402Service initialized
✅ MetaBricks routes mounted at /api/metabricks
🚀 x402 Service running on http://0.0.0.0:4000
```

### **Step 2: Configure Smart Contract Generator**

In SC-Gen's `appsettings.json`:

```json
{
  "X402": {
    "DistributionWebhookUrl": "http://localhost:4000/api/metabricks/sc-gen-webhook"
  }
}
```

### **Step 3: Test the Integration**

```bash
# Simulate a credit purchase
curl -X POST http://localhost:4000/api/metabricks/sc-gen-webhook \
  -H "Content-Type: application/json" \
  -d '{
    "signature": "TEST_TX_12345",
    "amount": 0.60,
    "operation": "purchase-credits",
    "distributionPercentage": 90,
    "nftCollection": "METABRICKS",
    "metadata": {
      "packName": "Developer",
      "credits": 50,
      "userWallet": "DevWallet123..."
    }
  }'
```

**Expected response:**
```json
{
  "success": true,
  "message": "Revenue distributed to MetaBricks holders",
  "distribution": {
    "totalAmount": 0.54,
    "holders": 432,
    "amountPerHolder": 0.00125,
    "distributionTx": "metabricks_dist_...",
    "fundingTx": "TEST_TX_12345",
    "timestamp": 1730545890
  }
}
```

---

## 📊 **Revenue Distribution Math**

### **Example: Developer Pack Purchase**

```
Developer buys 50 credits for 0.60 SOL
   ↓
SC-Gen sends webhook:
{
  "amount": 0.60,
  "distributionPercentage": 90
}
   ↓
x402 calculates:
- To distribute: 0.60 × 0.9 = 0.54 SOL
- To treasury: 0.60 × 0.1 = 0.06 SOL
- Per brick: 0.54 / 432 = 0.00125 SOL
   ↓
Each MetaBricks holder receives: 0.00125 SOL
```

### **Monthly Revenue Example (100 users)**

```
100 developers buy Developer packs
   ↓
Revenue: 100 × 0.60 SOL = 60 SOL
   ↓
To distribute: 60 × 0.9 = 54 SOL
   ↓
Per brick holder: 54 / 432 = 0.125 SOL/month

Annual: 0.125 × 12 = 1.5 SOL/year
Value: ~$150/year (at $100/SOL)
ROI: 37.5% on 0.4 SOL brick purchase!
```

---

## 🎨 **MetaBricks NFT Updates**

### **Updated Description:**

**Before:**
```
MetaBrick NFT: Brick #42
```

**After:**
```
MetaBrick NFT: Brick #42. Earn passive income from AssetRail Smart 
Contract Generator API revenue.
```

**This tells buyers they'll earn ongoing revenue!**

### **NFT Metadata Enhancement**

Add x402 attributes to metadata:

```json
{
  "attributes": [
    {
      "trait_type": "Revenue Share",
      "value": "1/432 (0.23%)"
    },
    {
      "trait_type": "Revenue Source",
      "value": "SC-Gen API"
    },
    {
      "trait_type": "Distribution",
      "value": "Automatic (x402)"
    }
  ],
  "properties": {
    "x402_enabled": true,
    "collection": "METABRICKS",
    "revenueModel": "equal_distribution",
    "distributionPercentage": 90
  }
}
```

---

## 🔌 **API Endpoints**

### **For SC-Gen to Call:**

**Distribution Webhook:**
```
POST http://localhost:4000/api/metabricks/sc-gen-webhook
```

**Test Distribution:**
```bash
curl -X POST http://localhost:4000/api/metabricks/sc-gen-webhook \
  -H "Content-Type: application/json" \
  -d '{"signature":"TEST","amount":0.60,"distributionPercentage":90,"nftCollection":"METABRICKS"}'
```

### **For MetaBricks Dashboard:**

**Get Statistics:**
```
GET http://localhost:4000/api/metabricks/stats
```

**Response:**
```json
{
  "success": true,
  "stats": {
    "collectionSymbol": "MBRICK",
    "totalBricks": 432,
    "totalDistributed": 54.0,
    "distributionCount": 10,
    "averagePerDistribution": 5.4,
    "recentDistributions": [...]
  }
}
```

**Get Current Holders:**
```
GET http://localhost:4000/api/metabricks/holders
```

---

## 💎 **Value Proposition for MetaBricks Buyers**

### **Before Integration:**
```
Buy MetaBrick for $50
   ↓
Get perks:
- API access
- Virtual land
- AR experiences
   ↓
That's it (one-time value)
```

### **After Integration:**
```
Buy MetaBrick for $50
   ↓
Get perks + Passive Income:
- API access
- Virtual land
- AR experiences
- PLUS: Share of SC-Gen revenue
   ↓
Ongoing value:
- $12.50/month at 100 API users
- $125/month at 1,000 API users
- $1,250/month at 10,000 API users
   ↓
MetaBricks become investment assets!
```

---

## 📈 **Revenue Projections for MetaBricks Holders**

### **Year 1 (Conservative - 100 SC-Gen users/month)**

| Month | SC-Gen Users | Revenue | To Holders | Per Brick |
|-------|--------------|---------|------------|-----------|
| 1-3   | 50          | 30 SOL  | 27 SOL     | 0.0625 SOL |
| 4-6   | 100         | 60 SOL  | 54 SOL     | 0.125 SOL  |
| 7-9   | 150         | 90 SOL  | 81 SOL     | 0.1875 SOL |
| 10-12 | 200         | 120 SOL | 108 SOL    | 0.25 SOL   |

**Year 1 Total per brick:** ~0.625 SOL ($62.50)  
**ROI:** 156% on 0.4 SOL investment!

### **Year 2 (Growth - 1,000 users/month)**

**Monthly:**
- Revenue: 600 SOL
- To holders: 540 SOL
- Per brick: 1.25 SOL/month

**Annual:**
- Per brick: 15 SOL/year ($1,500)
- ROI: 3,750%! 🚀

---

## 🧪 **Testing**

### **Test 1: Webhook Reception**

```bash
# Test that x402 service receives webhooks
curl -X POST http://localhost:4000/api/metabricks/sc-gen-webhook \
  -H "Content-Type: application/json" \
  -d '{
    "signature": "TEST_TX_001",
    "amount": 0.60,
    "operation": "purchase-credits",
    "distributionPercentage": 90,
    "nftCollection": "METABRICKS",
    "metadata": {
      "packName": "Developer",
      "credits": 50
    }
  }'
```

### **Test 2: View Stats**

```bash
curl http://localhost:4000/api/metabricks/stats
```

### **Test 3: Check Holders**

```bash
curl http://localhost:4000/api/metabricks/holders
```

---

## 🎬 **Demo Flow**

### **For Hackathon Presentation:**

**1. Show MetaBricks**
> "This is MetaBricks - 432 unique brick NFTs. But they're not just collectibles..."

**2. Show SC-Gen**
> "We built a Smart Contract Generator API. Developers pay to generate contracts. This generates real revenue."

**3. Show Connection**
> "Every time a developer uses our API, 90% of the payment automatically distributes to all 432 MetaBricks holders."

**4. Live Demo**
- Developer buys 50 credits (0.60 SOL)
- Show webhook trigger
- Show distribution to 432 holders
- Each brick holder sees: +0.00125 SOL

**5. Show Treasury Activity**
- Open MetaBricks dashboard
- Click "Treasury Activity"
- See: ↓ Revenue from SC-Gen +0.54 SOL
- See: ↑ Distributed to 432 holders -0.54 SOL

**6. Impact**
> "MetaBricks transform from collectibles into revenue-generating assets. At scale, each brick could earn $1,500/year passive income. This is the future of NFTs."

---

## ✅ **Integration Checklist**

**Backend:**
- [x] MetaBricks routes created
- [x] SC-Gen webhook handler
- [x] Distribution logic (432 equal split)
- [x] Statistics endpoint
- [x] Holders query endpoint
- [ ] Mount routes in server
- [ ] Test with SC-Gen

**Frontend:**
- [ ] Update MetaBricks dashboard
- [ ] Show SC-Gen revenue in treasury feed
- [ ] Display estimated earnings
- [ ] Show distribution history

**SC-Gen:**
- [ ] Update webhook URL in appsettings.json
- [ ] Test payment flow
- [ ] Verify webhook triggers
- [ ] Monitor distributions

**Testing:**
- [ ] Test webhook reception
- [ ] Test distribution calculation
- [ ] Test with multiple payments
- [ ] Verify all holders receive funds

---

## 🎉 **Ready to Integrate!**

The x402 service now has:
- ✅ MetaBricks-specific routes
- ✅ SC-Gen webhook handler
- ✅ 432 holder distribution logic
- ✅ Statistics tracking

**Next:** Mount the routes and test the complete flow! 🚀

