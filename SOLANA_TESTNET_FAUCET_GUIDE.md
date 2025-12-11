# Solana Testnet Faucet Guide

## Your Wallet Address
```
268i3KPxGjA4Eif6BK2HLfpbXqGuSbQxvCfw5e5Ka9F5Qk2bgVm
```

## 🌐 Method 1: Official Solana Web Faucet (Easiest)

1. Go to: **https://faucet.solana.com**
2. Paste your address: `268i3KPxGjA4Eif6BK2HLfpbXqGuSbQxvCfw5e5Ka9F5Qk2bgVm`
3. Select **Devnet** network
4. Click "Airdrop 2 SOL"
5. Wait a few seconds for confirmation

## 🔧 Method 2: Solana CLI (If Installed)

```bash
# Check if Solana CLI is installed
solana --version

# Request airdrop (2 SOL)
solana airdrop 2 268i3KPxGjA4Eif6BK2HLfpbXqGuSbQxvCfw5e5Ka9F5Qk2bgVm --url devnet

# Check balance
solana balance 268i3KPxGjA4Eif6BK2HLfpbXqGuSbQxvCfw5e5Ka9F5Qk2bgVm --url devnet
```

## 🐍 Method 3: Python Script

```python
from solana.rpc.api import Client

client = Client('https://api.devnet.solana.com')
address = '268i3KPxGjA4Eif6BK2HLfpbXqGuSbQxvCfw5e5Ka9F5Qk2bgVm'

# Request 2 SOL
result = client.request_airdrop(address, 2_000_000_000)
print(f"Transaction: {result.value}")
```

## 📱 Method 4: Discord Faucet

1. Join Solana Discord: **https://discord.gg/solana**
2. Go to the `#devnet-faucet` channel
3. Type: `!faucet 268i3KPxGjA4Eif6BK2HLfpbXqGuSbQxvCfw5e5Ka9F5Qk2bgVm`
4. Wait for bot confirmation

## 🌍 Method 5: Alternative Faucets

### SolFaucet
- URL: **https://solfaucet.com/**
- Paste your address and request SOL

### QuickNode Faucet
- URL: **https://faucet.quicknode.com/solana/devnet**
- Requires QuickNode account (free)

## ✅ Verify Your Balance

After receiving SOL, verify your balance:

### Using Solana CLI:
```bash
solana balance 268i3KPxGjA4Eif6BK2HLfpbXqGuSbQxvCfw5e5Ka9F5Qk2bgVm --url devnet
```

### Using Solana Explorer:
Visit: **https://explorer.solana.com/address/268i3KPxGjA4Eif6BK2HLfpbXqGuSbQxvCfw5e5Ka9F5Qk2bgVm?cluster=devnet**

### Using API:
```bash
curl "https://api.devnet.solana.com" \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "getBalance",
    "params": ["268i3KPxGjA4Eif6BK2HLfpbXqGuSbQxvCfw5e5Ka9F5Qk2bgVm"]
  }'
```

## ⚠️ Notes

- **Rate Limits**: Most faucets have rate limits (usually 1 request per 24 hours)
- **Network**: Make sure you're using **Devnet**, not Mainnet
- **Amount**: Typically 1-2 SOL per request
- **Confirmation**: Transactions usually confirm within 1-2 seconds on devnet

## 🔗 Quick Links

- **Official Faucet**: https://faucet.solana.com
- **Solana Explorer**: https://explorer.solana.com/?cluster=devnet
- **Solana Docs**: https://docs.solana.com/developing/test-validator
- **Discord**: https://discord.gg/solana

