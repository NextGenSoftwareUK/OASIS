# 🧪 X402 Payment Modal - Testing Guide

**Status:** ✅ Integrated and Ready  
**Last Updated:** November 2, 2025

---

## ✅ What Was Fixed

**Problem:** "Non-base58 character" error  
**Cause:** Placeholder address `'YOUR_SOLANA_ADDRESS_HERE'` is not a valid Solana address  
**Fix:** Updated to valid address `FQsRrE7pXHJg5jftcWUzqcHvUfk8AQoUviijWuiD4JFn`

---

## 🎯 Step-by-Step Test Instructions

### **1. Prepare Your Wallet**

**Switch Phantom to Devnet:**
1. Open Phantom wallet extension
2. Click Settings ⚙️  
3. Developer Settings
4. Network → **Devnet**

**Get Devnet SOL (Free):**
```bash
# Via web faucet
https://faucet.solana.com/

# Or via CLI
solana airdrop 2 YOUR_PHANTOM_ADDRESS --url devnet
```

### **2. Open the UI**

```
http://localhost:3001/generate/template
```

### **3. Trigger Payment Modal**

1. Click the **"Generate Contract"** button (purple button)
2. The API will return **402 Payment Required**
3. **PaymentModal should appear** as an overlay

### **4. What You Should See in the Modal**

```
┌──────────────────────────────────────┐
│  Complete Payment                  × │
├──────────────────────────────────────┤
│                                      │
│  Operation:        Generate          │
│  Blockchain:       Rust              │
│  Amount:           0.02 SOL          │
│                                      │
│  💎 Revenue Distribution             │
│  90% → 10,000 NFT holders           │
│  Each holder receives: 0.0000018 SOL│
│  Want to earn? Mint an NFT           │
│                                      │
│  [Connect Phantom Wallet]            │
│                                      │
│  Powered by x402 protocol            │
└──────────────────────────────────────┘
```

### **5. Complete Payment**

1. Click **"Connect Phantom Wallet"**
   - Phantom popup appears
   - Click "Connect"
   - Modal updates to show:
     - ✅ Wallet Connected
     - Your wallet address
     - Your balance
     - **"Pay 0.02 SOL"** button

2. Click **"Pay 0.02 SOL"**
   - Phantom opens transaction approval
   - Shows: Transfer 0.02 SOL to treasury
   - Click "Approve"

3. **Payment Processing:**
   - Modal shows: "Processing Payment..." (spinner)
   - Frontend verifies transaction on Solana
   - Backend verifies and issues JWT token

4. **Success:**
   - ✅ Payment Complete! (green checkmark)
   - Link to view transaction on Solana Explorer
   - Modal auto-closes after 2 seconds
   - **Contract generation starts automatically**

---

## 🐛 Troubleshooting

### **Modal Doesn't Appear**

**Check browser console:**
```javascript
// Open DevTools (F12) → Console
// Look for errors like:
// - "PaymentRequiredError"
// - "402"
```

**Verify API is returning 402:**
```bash
curl -X POST http://localhost:5000/api/v1/contracts/generate \
  -F "Language=Rust" \
  -F "JsonFile=@/dev/null"

# Should return:
# {
#   "error": "Payment Required",
#   "pricing": { ... }
# }
```

### **"Non-base58 character" Error**

**Fix Applied:** Updated recipient address to valid Solana pubkey  
**If still occurs:** Hard refresh the browser (Cmd+Shift+R) to reload JavaScript

### **"Wallet not connected" Error**

1. Make sure Phantom is installed
2. Make sure Phantom is on **Devnet** (not mainnet)
3. Click "Connect Phantom Wallet" in modal first

### **"Insufficient balance" Error**

Get more devnet SOL:
```bash
solana airdrop 2 YOUR_ADDRESS --url devnet
```

---

## 🔍 Verify Integration

**Check these files were updated:**

1. ✅ `lib/x402-payment.ts` - Recipient address set
2. ✅ `app/generate/template/page.tsx` - PaymentModal imported and integrated
3. ✅ `lib/api-client.ts` - Payment token support added
4. ✅ Backend `appsettings.json` - RequirePayment: true

**Check services are running:**
```bash
curl http://localhost:5000/health
# Should return: {"status":"healthy",...}

curl -s -o /dev/null -w "%{http_code}" http://localhost:3001
# Should return: 200
```

---

## 📝 Expected Full Flow

1. **Generate button** → 402 Payment Required → **Modal appears** ✅
2. **Connect wallet** → Phantom popup → Approve → **Wallet connected** ✅
3. **Pay 0.02 SOL** → Phantom transaction → Approve → **Payment sent** ✅
4. **Verification** → On-chain check → JWT issued → **Token received** ✅
5. **Auto-retry** → Generate with token → **Contract generated** ✅

---

## 🎯 **Try Again Now!**

The "Non-base58 character" error is fixed. Refresh your browser and try again:

**http://localhost:3001/generate/template**

The payment modal should now work perfectly! 🚀

