# OASIS Deployment Quick Start Guide

Get your OASIS contracts deployed in minutes! 🚀

## ⚡ Quick Setup (5 minutes)

### 1. Install Dependencies

```bash
# Install Node.js dependencies
npm install

# Copy Hardhat config
cp scripts/hardhat.config.template.js hardhat.config.js

# Create .env file
cp scripts/.env.example .env
# Edit .env and add your DEPLOYER_PRIVATE_KEY
```

### 2. Set Your Private Key

**Linux/Mac:**
```bash
export DEPLOYER_PRIVATE_KEY=your-private-key-here
```

**Windows (PowerShell):**
```powershell
$env:DEPLOYER_PRIVATE_KEY="your-private-key-here"
```

**Or use .env file:**
```bash
# Edit .env file
DEPLOYER_PRIVATE_KEY=your-private-key-here
```

### 3. Check Current Status

```bash
node scripts/check-deployment-status.js
```

## 🎯 Deployment Options

### Option 1: Use Master Script (Recommended)

**Linux/Mac:**
```bash
./scripts/deploy-master.sh
```

**Windows (PowerShell):**
```powershell
.\scripts\deploy-master.ps1
```

This provides an interactive menu for all deployment options.

### Option 2: Deploy All Testnets

**Linux/Mac:**
```bash
./scripts/deploy-all-evm-testnet.sh
```

**Windows (PowerShell):**
```powershell
.\scripts\deploy-all-evm-testnet.ps1
```

### Option 3: Deploy Individual Chain

```bash
# Testnet
node scripts/deploy-evm-chain.js sepolia
node scripts/deploy-evm-chain.js baseSepolia

# Mainnet (⚠️ costs real money!)
node scripts/deploy-evm-chain.js ethereum mainnet
node scripts/deploy-evm-chain.js base mainnet
```

### Option 4: Use npm Scripts

```bash
# Check status
npm run check-status

# Deploy individual chains
npm run deploy:ethereum:testnet
npm run deploy:base:mainnet

# Deploy all testnets
npm run deploy:all:testnet

# Deploy Move chains
npm run deploy:aptos:testnet
npm run deploy:sui:mainnet
```

## 📋 Common Commands

### Check Deployment Status
```bash
node scripts/check-deployment-status.js
```

### Update DNA Configuration
```bash
node scripts/update-dna-from-deployments.js
```

### Deploy to Specific Chain

**EVM Chains:**
```bash
# Testnet
node scripts/deploy-evm-chain.js <chain-name>

# Mainnet
node scripts/deploy-evm-chain.js <chain-name> mainnet
```

**Move Chains:**
```bash
# Aptos
./scripts/deploy-aptos.sh testnet
./scripts/deploy-aptos.sh mainnet

# Sui
./scripts/deploy-sui.sh testnet
./scripts/deploy-sui.sh mainnet
```

## 🔗 Available Chains

### EVM Testnets
- `sepolia` - Ethereum Sepolia
- `arbitrumSepolia` - Arbitrum Sepolia
- `optimismSepolia` - Optimism Sepolia
- `baseSepolia` - Base Sepolia
- `amoy` - Polygon Amoy
- `bnbTestnet` - BNB Chain Testnet
- `fantomTestnet` - Fantom Testnet
- `fuji` - Avalanche Fuji
- `rootstockTestnet` - Rootstock Testnet
- `zkSyncTestnet` - zkSync Era Testnet
- `lineaTestnet` - Linea Testnet
- `scrollSepolia` - Scroll Sepolia

### EVM Mainnets
- `ethereum` - Ethereum Mainnet
- `arbitrum` - Arbitrum One
- `optimism` - Optimism
- `base` - Base
- `polygon` - Polygon
- `bnb` - BNB Chain
- `fantom` - Fantom
- `avalanche` - Avalanche C-Chain
- `rootstock` - Rootstock
- `zkSync` - zkSync Era
- `linea` - Linea
- `scroll` - Scroll

## ⚠️ Important Notes

1. **Test on Testnet First**: Always deploy to testnet before mainnet
2. **Fund Your Wallet**: Ensure deployer wallet has native tokens for gas
3. **Private Keys**: Never commit private keys to version control
4. **Gas Costs**: Mainnet deployments cost real money (estimate $100-500 total)
5. **Verification**: Contracts are automatically verified if API keys are set

## 🆘 Troubleshooting

### "Insufficient balance"
- Fund your deployer wallet with native tokens

### "RPC connection failed"
- Check RPC URL in hardhat.config.js
- Try alternative RPC endpoint

### "Module not found"
- Run `npm install` to install dependencies

### Scripts not executable (Linux/Mac)
```bash
chmod +x scripts/*.sh
```

## 📚 Next Steps

After deployment:
1. ✅ Check deployment status
2. ✅ Update OASIS_DNA.json
3. ✅ Verify contracts on block explorers
4. ✅ Run integration tests
5. ✅ Update documentation

## 📖 Full Documentation

- **Deployment Guide**: `CONTRACT_DEPLOYMENT.md`
- **Deployment Checklist**: `DEPLOYMENT_CHECKLIST.md`
- **Status Report**: `DEPLOYMENT_STATUS.md`
- **Scripts README**: `scripts/README.md`

---

**Ready to deploy?** Start with the master script:
```bash
./scripts/deploy-master.sh
```


