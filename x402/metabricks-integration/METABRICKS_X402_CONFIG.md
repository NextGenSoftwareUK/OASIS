# MetaBricks x402 Integration

## 🎯 **The Model**

**MetaBricks NFTs** = Revenue-generating assets  
**Revenue Source** = AssetRail Smart Contract Generator API  
**Distribution** = Automatic via x402 protocol

---

## 🧱 **MetaBricks Collection Details**

**NFT Collection:**
- Name: MetaBricks
- Symbol: MBRICK
- Total Supply: 432 bricks
- Types: Regular (361), Industrial (60), Legendary (11)
- Blockchain: Solana
- Network: Devnet (testing) → Mainnet (production)

**Revenue Model:**
- Equal distribution to all 432 holders
- 90% of SC-Gen revenue to holders
- 10% to MetaBricks treasury/operations

---

## 💰 **Revenue Source: Smart Contract Generator**

**What generates revenue:**
- Developers pay to use SC-Gen API
- Charges: 0.02-0.15 SOL per operation
- Credit packs: 0.15-4.00 SOL for bulk purchases

**Payment flow:**
```
Developer buys 50 credits for 0.60 SOL
   ↓
SC-Gen backend verifies payment
   ↓
Triggers webhook to x402 service
   ↓
x402 service queries MetaBricks holders
   ↓
Distributes 0.54 SOL (90%) to 432 brick holders
   ↓
Each holder receives: 0.00125 SOL (~$0.125)
```

**At scale (100 packs/month):**
- Monthly revenue: 60 SOL ($6,000)
- To brick holders: 54 SOL ($5,400)
- Per brick: 0.125 SOL/month ($12.50/month = $150/year)
- ROI: 30%+ annual yield on 0.4 SOL brick purchase!

---

## 🔌 **Integration Architecture**

```
┌──────────────────────────────────────────────────┐
│  Smart Contract Generator API                     │
│  (Developers pay for contract generation)         │
└───────────────┬──────────────────────────────────┘
                │ Payment: 0.60 SOL
                ↓
┌──────────────────────────────────────────────────┐
│  Treasury Wallet                                  │
│  FQsRrE7pXHJg5jftcWUzqcHvUfk8AQoUviijWuiD4JFn   │
└───────────────┬──────────────────────────────────┘
                │ Webhook trigger
                ↓
┌──────────────────────────────────────────────────┐
│  OASIS x402 Service                              │
│  http://localhost:4000/api/x402/webhook          │
└───────────────┬──────────────────────────────────┘
                │ Query holders
                ↓
┌──────────────────────────────────────────────────┐
│  Solana Blockchain                               │
│  Query: Who owns MetaBricks NFTs?                │
│  Returns: 432 wallet addresses                   │
└───────────────┬──────────────────────────────────┘
                │ Create distribution tx
                ↓
┌──────────────────────────────────────────────────┐
│  Distribution Transaction                        │
│  Split 0.54 SOL among 432 holders               │
│  Each gets 0.00125 SOL                          │
└──────────────────────────────────────────────────┘
```

---

## 📝 **Configuration**

### **x402 Service Configuration**

File: `x402/backend-service/.env`

```bash
# MetaBricks Collection
METABRICKS_COLLECTION_SYMBOL=MBRICK
METABRICKS_TOTAL_SUPPLY=432
METABRICKS_DISTRIBUTION_MODEL=equal

# SC-Gen Integration
SCGEN_WEBHOOK_ENDPOINT=http://localhost:4000/api/x402/webhook
SCGEN_TREASURY=FQsRrE7pXHJg5jftcWUzqcHvUfk8AQoUviijWuiD4JFn
SCGEN_DISTRIBUTION_PERCENTAGE=90
```

### **SC-Gen Configuration**

They need to add to their `appsettings.json`:

```json
{
  "X402": {
    "DistributionWebhookUrl": "http://localhost:4000/api/x402/webhook"
  }
}
```

---

## 🔄 **Distribution Flow**

### **When Developer Buys Credits:**

```
1. Developer pays 0.60 SOL to SC-Gen
2. SC-Gen backend POST to:
   http://localhost:4000/api/x402/webhook
   {
     "signature": "SOLANA_TX_SIG",
     "amount": 0.60,
     "distributionPercentage": 90,
     "nftCollection": "METABRICKS"
   }

3. x402 service:
   - Verifies transaction on Solana ✅
   - Calculates: 0.60 × 0.9 = 0.54 SOL to distribute
   - Queries Solana for MetaBricks holders
   - Finds 432 wallet addresses
   - Calculates: 0.54 / 432 = 0.00125 SOL each
   - Creates multi-recipient transaction
   - Submits to Solana blockchain
   - All 432 holders receive payment (30 seconds)

4. Treasury Activity Feed updates:
   - Shows: ↓ Revenue received +0.54 SOL (from SC-Gen)
   - Shows: ↑ Distributed to 432 holders -0.54 SOL
```

---

## 🎯 **Value Proposition for MetaBricks Buyers**

**Before x402:**
```
Buy brick for $50 → Get perks (API access, virtual land) → That's it
```

**After x402 (with SC-Gen integration):**
```
Buy brick for $50 → Get perks + passive income from SC-Gen revenue
   ↓
Every time a developer uses SC-Gen API:
   ↓
You earn SOL automatically
   ↓
At 100 users/month: $150/year passive income
At 1,000 users/month: $1,500/year passive income
```

**This transforms MetaBricks from collectibles into income-generating assets!**

---

## 🚀 **Next Steps:**

Should I:

1. **Configure x402 service** to track MetaBricks collection?
2. **Update MetaBricks minting** to register NFTs with x402?
3. **Create webhook handler** specifically for SC-Gen payments?
4. **Set up treasury activity tracker** for MetaBricks holders?

All of the above? Let me know and I'll build it! 🔨
