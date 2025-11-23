# Miden Testnet - Quick Reference

## 🚀 Quick Setup (5 Minutes)

### 1. Install Wallet
- **Official Site**: https://miden.fi/
- **Testnet Info**: https://miden.xyz/testnet
- Install browser extension from official site
- Create wallet → Save recovery phrase

### 2. Get Tokens
- Visit: https://faucet.testnet.miden.io/
- Enter your address
- Click "Send Public Note"
- Claim in wallet (Receive → Claim)

### 3. Configure OASIS
```bash
export MIDEN_API_URL="https://testnet.miden.xyz"
export MIDEN_WALLET_ADDRESS="miden1your_address"
```

## 📋 Important URLs

- **Official Website**: https://miden.fi/
- **Testnet Page**: https://miden.xyz/testnet
- **Faucet**: https://faucet.testnet.miden.io/
- **Docs**: https://docs.miden.xyz/
- **GitHub**: https://github.com/0xPolygonMiden

## 🔑 Key Information

- **Network**: Miden Testnet
- **API URL**: `https://testnet.miden.xyz`
- **Address Format**: `miden1...`
- **Note Types**: Public (visible) or Private (hidden)

## ⚠️ Security

- Save recovery phrase offline
- Testnet tokens have no value
- Never share private keys
- Always verify testnet vs mainnet

## ✅ Checklist

- [ ] Wallet installed
- [ ] Wallet created
- [ ] Recovery phrase saved
- [ ] Address copied
- [ ] Tokens requested from faucet
- [ ] Tokens claimed in wallet
- [ ] Balance verified
- [ ] Environment variables set
- [ ] OASIS configured

## 🧪 Test Commands

```bash
# Test API connection
curl https://testnet.miden.xyz/health

# Run setup script
./zypherpunk/setup-miden-testnet.sh
```

