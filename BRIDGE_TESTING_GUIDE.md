# Bridge Testing Guide

## 🎯 Quick Start: Testing with Testnet SOL

You have **5 SOL on Solana devnet**. Here's how to test the bridge:

## Option 1: SOL ↔ ETH Swap (Recommended - Easiest)

### Step 1: Get Sepolia ETH

**Faucets:**
1. **QuickNode Faucet** (Recommended):
   - URL: https://faucet.quicknode.com/ethereum/sepolia
   - Requires: QuickNode account (free)
   - Amount: 0.5 ETH per day

2. **Alchemy Sepolia Faucet**:
   - URL: https://sepoliafaucet.com/
   - Requires: Alchemy account (free)
   - Amount: 0.5 ETH per day

3. **PoW Faucet** (No account needed):
   - URL: https://sepolia-faucet.pk910.de/
   - Requires: Mining (proof of work)
   - Amount: 0.05 ETH per request

### Step 2: Get Your Ethereum Address

Your Ethereum wallet address should be in the UI. If not, check:
```bash
# Get your Ethereum wallet address
curl -k -X GET "https://localhost:5004/api/wallet/avatar/YOUR_AVATAR_ID/wallets" \
  -H "Authorization: Bearer $OASIS_TOKEN" | jq '.result.EthereumOASIS[0].walletAddress'
```

### Step 3: Test the Swap

1. Open the swap screen in the UI
2. Select:
   - **From**: SOL (Solana)
   - **To**: ETH (Ethereum)
3. Enter amount (e.g., 0.1 SOL)
4. Enter your Ethereum address as destination
5. Click "Create swap order"

## Option 2: Test Private Bridges

### Zcash Testnet (TAZ)

**Faucets:**
- https://faucet.zcashcommunity.com/ (if available)
- Discord: https://discord.gg/zcash
- Channel: `#zcash-testnet`

**Note**: Zcash testnet requires Unified Addresses (u1...) which we're currently generating as transparent addresses (tm...). This may need additional work.

### Starknet Testnet

**Faucets:**
- https://starknet-faucet.vercel.app/
- https://faucet.goerli.starknet.io/

**Note**: Requires Starknet wallet address (0x... format, 66 chars)

### Aztec Testnet

**Faucets:**
- Check Aztec documentation: https://docs.aztec.network/
- May require running a local node

## 🧪 Testing Script

Use the test script we created:

```bash
# Test bridge swap
./test-bridge-swap.sh
```

This will:
1. Get exchange rate (SOL → ETH)
2. Create a swap order
3. Check order balance

## 📊 Current Bridge Status

| Pair | Status | Testnet Available |
|------|--------|-------------------|
| SOL ↔ ETH | ✅ Ready | ✅ Yes (Devnet + Sepolia) |
| SOL ↔ XRD | ⚠️ Placeholder | ❌ No |
| Zcash ↔ Aztec | ✅ Ready | ⚠️ Limited |
| Zcash ↔ Miden | ✅ Ready | ⚠️ Limited |
| Zcash ↔ Starknet | ✅ Ready | ⚠️ Limited |

## 💡 Recommended Testing Flow

1. **Get Sepolia ETH** (easiest option)
2. **Test SOL → ETH swap** via UI
3. **Verify order creation** in API logs
4. **Check order status** via API
5. **Test ETH → SOL swap** (if you have ETH)

## 🔗 Useful Links

- **Solana Explorer**: https://explorer.solana.com/?cluster=devnet
- **Ethereum Sepolia Explorer**: https://sepolia.etherscan.io/
- **Bridge API Docs**: See `BRIDGE_API_GUIDE.md`
- **Test Script**: `./test-bridge-swap.sh`

## ⚠️ Notes

- Bridge orders are created but execution depends on bridge service availability
- Some swaps may require additional setup (RPC endpoints, technical accounts)
- Testnet tokens have no real value - perfect for testing!
