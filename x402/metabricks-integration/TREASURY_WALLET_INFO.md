# MetaBricks Treasury Wallet Configuration

## 💰 **Treasury Wallet Address:**

```
3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X
```

**This wallet:**
- Linked to Smart Contract Generator
- Receives SC-Gen revenue
- Will distribute to MetaBricks holders

---

## 🔐 **Security:**

**CRITICAL:** For production distributions, you'll need:
1. The **private key** for this wallet (stored securely)
2. Environment variable: `TREASURY_WALLET_PRIVATE_KEY`
3. Or keypair file path: `TREASURY_KEYPAIR_PATH`

---

## 📊 **Check Balance:**

### **Devnet:**
```bash
solana balance 3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X --url devnet
```

### **Mainnet:**
```bash
solana balance 3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X --url mainnet-beta
```

### **Web:**
- Devnet: https://solscan.io/account/3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X?cluster=devnet
- Mainnet: https://solscan.io/account/3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X

---

## 🔧 **Configuration:**

Add to MetaBricks backend environment:

```bash
# Treasury wallet
TREASURY_WALLET_ADDRESS=3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X
TREASURY_KEYPAIR_PATH=/path/to/treasury-keypair.json

# Or use private key directly (less secure)
TREASURY_WALLET_PRIVATE_KEY=[1,2,3,...]  # Array of bytes
```

---

## 💡 **How It Works:**

```
SC-Gen receives payment (1.0 SOL)
   ↓
Sends to treasury wallet (3BTEJ...co5X)
   ↓
SC-Gen triggers webhook → MetaBricks backend
   ↓
Backend distributes from treasury:
   • 0.5 SOL → 432 MetaBricks holders (0.001157 SOL each)
   • 0.5 SOL → Stays in treasury
```

---

## ⚠️ **Important:**

For real distributions to work, the backend needs **signing authority** for this wallet.

**Options:**
1. Store keypair file securely on Heroku
2. Use environment variable with private key
3. Use multi-sig with additional security layer

Currently configured for: **File-based keypair**

