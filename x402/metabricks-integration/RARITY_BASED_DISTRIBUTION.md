# Rarity-Based x402 Distribution Model

## 🎯 **Key Insight:**
Brick rarity should affect revenue distribution percentage.

---

## 📊 **Current Brick Distribution:**

### **Total Bricks:** 432

### **Breakdown:**
- **Regular:** ~361 bricks (83.6%)
- **Industrial:** ~60 bricks (13.9%)
- **Legendary:** 11 bricks (2.5%)

**From server.js:**
```javascript
const legendaryBricks = [78, 108, 153, 155, 185, 219, 278, 296, 397, 431, 432]; // 11 bricks
const industrialBricks = [2, 19, 23, 27, 28, 30, 33, 35, 38, 59, 68, 69, 71, 76, 77, 82, 90, 118, 125, 129, 132, 138, 140, 141, 146, 151, 155, 159, 180, 186, 205, 210, 212, 214, 218, 229, 231, 239, 248, 257, 258, 261, 275, 283, 284, 288, 292, 301, 311, 312, 314, 318, 350, 352, 356, 374, 379, 383, 403, 420]; // ~60 bricks
// Rest are regular
```

---

## 💰 **Distribution Models (Choose One):**

### **Model 1: Equal Split (Current)**
**Everyone gets the same amount, regardless of rarity**

```
Revenue: 10 SOL (50% = 5 SOL to holders)

Regular Brick: 5 SOL / 432 = 0.01157 SOL
Industrial Brick: 5 SOL / 432 = 0.01157 SOL
Legendary Brick: 5 SOL / 432 = 0.01157 SOL
```

**Pros:** Simple, fair
**Cons:** Legendary holders not rewarded for rarity

---

### **Model 2: Weighted by Rarity**
**Rare bricks get more revenue**

**Weight System:**
- Regular: 1x weight
- Industrial: 2x weight  
- Legendary: 5x weight

**Total Weight:**
```
Regular: 361 × 1 = 361
Industrial: 60 × 2 = 120
Legendary: 11 × 5 = 55
──────────────────────
Total Weight:       536
```

**Distribution:**
```
Revenue: 10 SOL (50% = 5 SOL to holders)

Regular Brick: 5 SOL × (1/536) = 0.00933 SOL
Industrial Brick: 5 SOL × (2/536) = 0.01866 SOL (2x regular)
Legendary Brick: 5 SOL × (5/536) = 0.04664 SOL (5x regular)
```

**Pros:** Rewards rarity, increases legendary value
**Cons:** More complex

---

### **Model 3: Tiered Percentages**
**Each tier gets a % of the total pool**

```
Revenue: 10 SOL (50% = 5 SOL to holders)

Regular Pool (70%): 3.5 SOL / 361 = 0.00970 SOL each
Industrial Pool (20%): 1.0 SOL / 60 = 0.01667 SOL each
Legendary Pool (10%): 0.5 SOL / 11 = 0.04545 SOL each
```

**Pros:** Guaranteed minimum per tier
**Cons:** Fixed percentages (less flexible)

---

## 🎨 **Updated Metadata Structure:**

### **Regular Brick Metadata:**

```json
{
  "name": "MetaBrick #1",
  "symbol": "MBRICK",
  "description": "MetaBrick NFT #1 (Regular). Earn passive income from AssetRail Smart Contract Generator. This brick receives proportional revenue based on rarity: Regular bricks earn 1x, Industrial earn 2x, Legendary earn 5x the base distribution rate.",
  "image": "https://gateway.pinata.cloud/ipfs/bafkreibhok44eomzkubmt3e2kzxip3w3b4pclixvgff5q7awhfa7kwlwsq",
  
  "attributes": [
    {
      "trait_type": "Brick Number",
      "value": "1"
    },
    {
      "trait_type": "Rarity",
      "value": "Regular"
    },
    {
      "trait_type": "Distribution Weight",
      "value": "1x"
    },
    {
      "trait_type": "x402 Revenue Sharing",
      "value": "Enabled"
    },
    {
      "trait_type": "Revenue Source",
      "value": "SC-Gen API"
    }
  ],
  
  "x402": {
    "enabled": true,
    "version": "1.0",
    "distributionPercentage": 50,
    "revenueSource": "AssetRail Smart Contract Generator",
    "distributionModel": "weighted",
    "weight": 1,
    "rarity": "regular",
    "treasuryAddress": "H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1",
    "totalSupply": 432,
    "webhookEndpoint": "https://metabricks-backend.herokuapp.com/api/metabricks/sc-gen-webhook"
  }
}
```

### **Industrial Brick Metadata:**

```json
{
  "x402": {
    "enabled": true,
    "distributionModel": "weighted",
    "weight": 2,  // ← 2x regular
    "rarity": "industrial"
  }
}
```

### **Legendary Brick Metadata:**

```json
{
  "x402": {
    "enabled": true,
    "distributionModel": "weighted",
    "weight": 5,  // ← 5x regular
    "rarity": "legendary"
  }
}
```

---

## 🔢 **Distribution Calculation with Weights:**

```javascript
// Calculate weighted distribution
function calculateWeightedDistribution(holders, totalAmount) {
  // Calculate total weight
  const totalWeight = holders.reduce((sum, h) => {
    const weight = getWeightForRarity(h.rarity);
    return sum + weight;
  }, 0);
  
  // Calculate per-holder amounts
  const distributions = holders.map(holder => {
    const weight = getWeightForRarity(holder.rarity);
    const share = weight / totalWeight;
    const amount = totalAmount * share;
    
    return {
      walletAddress: holder.walletAddress,
      amount: amount,
      weight: weight,
      rarity: holder.rarity
    };
  });
  
  return distributions;
}

function getWeightForRarity(rarity) {
  switch (rarity) {
    case 'legendary': return 5;
    case 'industrial': return 2;
    case 'regular':
    default: return 1;
  }
}
```

---

## 💡 **My Recommendation:**

### **Start with Model 2: Weighted by Rarity**

**Why?**
- ✅ Rewards rarity (legendary = 5x regular)
- ✅ Simple to understand
- ✅ Increases legendary brick value
- ✅ Still fair for regular holders

**Example with 100 SOL revenue:**

| Type | Count | Weight | Total Weight | Share | Per Brick |
|------|-------|--------|--------------|-------|-----------|
| Regular | 361 | 1x | 361 | 67.4% | 0.0933 SOL |
| Industrial | 60 | 2x | 120 | 22.4% | 0.1866 SOL |
| Legendary | 11 | 5x | 55 | 10.3% | 0.4664 SOL |

**Legendary bricks earn 5x more!** 🚀

---

## 📋 **What perks to keep?**

Looking at current metadata, I see perks like:
- Token Airdrop
- TGE Discount
- Mystery Perks

**Which should we keep for the new x402-enabled metadata?**

Want me to create the updated metadata upload script with your preferred:
1. Distribution model (equal vs weighted)?
2. Perks to keep?
3. Weight multipliers (1x, 2x, 5x or different)?

Let me know and I'll generate all 432 metadata files! 🎯

