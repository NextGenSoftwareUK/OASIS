# OASIS Deployment Scripts

This directory contains automated deployment scripts for OASIS smart contracts across all supported blockchains.

## 📁 Script Overview

### Status & Checking
- **`check-deployment-status.js`** - Check current deployment status across all chains
- **`update-dna-from-deployments.js`** - Update OASIS_DNA.json with deployed addresses

### EVM Chain Deployment
- **`deploy-evm-chain.js`** - Deploy to individual EVM chain (testnet or mainnet)
- **`deploy-all-evm-testnet.sh`** - Deploy to ALL EVM testnets
- **`deploy-all-evm-mainnet.sh`** - Deploy to ALL EVM mainnets (⚠️ expensive!)

### Move Chain Deployment
- **`deploy-aptos.sh`** - Deploy to Aptos (testnet or mainnet)
- **`deploy-sui.sh`** - Deploy to Sui (testnet or mainnet)
- **`deploy-all-move.sh`** - Deploy to ALL Move chains (Aptos + Sui)

### Configuration
- **`hardhat.config.template.js`** - Hardhat configuration template for all EVM chains

## 🚀 Quick Start

### 1. Setup Environment

```bash
# Install dependencies
npm install --save-dev hardhat @nomicfoundation/hardhat-toolbox dotenv

# Copy Hardhat config
cp scripts/hardhat.config.template.js hardhat.config.js

# Create .env file
cat > .env << EOF
DEPLOYER_PRIVATE_KEY=your-private-key-here
ETHERSCAN_API_KEY=your-etherscan-api-key
ARBISCAN_API_KEY=your-arbiscan-api-key
# ... add other API keys as needed
EOF
```

### 2. Check Current Status

```bash
node scripts/check-deployment-status.js
```

### 3. Deploy to Testnet

**Individual chain:**
```bash
node scripts/deploy-evm-chain.js ethereum testnet
node scripts/deploy-evm-chain.js arbitrum testnet
```

**All EVM testnets:**
```bash
./scripts/deploy-all-evm-testnet.sh
```

**All Move testnets:**
```bash
./scripts/deploy-all-move.sh testnet
```

### 4. Deploy to Mainnet

**⚠️ WARNING: Mainnet deployments cost real money!**

**Individual chain:**
```bash
node scripts/deploy-evm-chain.js ethereum mainnet
node scripts/deploy-evm-chain.js arbitrum mainnet
```

**All EVM mainnets (use with caution):**
```bash
./scripts/deploy-all-evm-mainnet.sh
```

**All Move mainnets:**
```bash
./scripts/deploy-all-move.sh mainnet
```

### 5. Update Configuration

After deployment, update OASIS_DNA.json:
```bash
node scripts/update-dna-from-deployments.js
```

## 📋 Usage Examples

### Deploy Individual Chain

```bash
# Testnet
node scripts/deploy-evm-chain.js sepolia
node scripts/deploy-evm-chain.js baseSepolia
node scripts/deploy-evm-chain.js arbitrum testnet

# Mainnet
node scripts/deploy-evm-chain.js ethereum mainnet
node scripts/deploy-evm-chain.js base mainnet
```

### Deploy with Verification

```bash
node scripts/deploy-evm-chain.js ethereum mainnet --verify
```

### Deploy Move Chains

```bash
# Aptos testnet
./scripts/deploy-aptos.sh testnet

# Aptos mainnet
./scripts/deploy-aptos.sh mainnet

# Sui testnet
./scripts/deploy-sui.sh testnet

# Sui mainnet
./scripts/deploy-sui.sh mainnet
```

## 📊 Deployment Tracking

All deployments are tracked in `deployed-addresses.json`:

```json
{
  "ethereum": {
    "mainnet": {
      "chain": "Ethereum",
      "network": "ethereum",
      "address": "0x...",
      "chainId": 1,
      "explorer": "https://etherscan.io/address/0x...",
      "deployedAt": "2025-01-XXT..."
    },
    "testnet": {
      "chain": "Ethereum Sepolia",
      "network": "sepolia",
      "address": "0x...",
      "chainId": 11155111,
      "explorer": "https://sepolia.etherscan.io/address/0x...",
      "deployedAt": "2025-01-XXT..."
    }
  }
}
```

## 🔧 Configuration

### Hardhat Config

Edit `hardhat.config.js` to customize:
- RPC endpoints
- Network configurations
- Gas settings
- API keys for verification

### Environment Variables

Required:
- `DEPLOYER_PRIVATE_KEY` - Private key for deployment (never commit!)

Optional (for contract verification):
- `ETHERSCAN_API_KEY`
- `ARBISCAN_API_KEY`
- `POLYGONSCAN_API_KEY`
- `BSCSCAN_API_KEY`
- `FTMSCAN_API_KEY`
- `SNOWTRACE_API_KEY`
- etc.

## 🛠️ Troubleshooting

### "Insufficient balance"
- Fund your deployer wallet with native tokens

### "RPC connection failed"
- Check RPC URL in hardhat.config.js
- Try alternative RPC endpoint
- Check network connectivity

### "Contract verification failed"
- Ensure API key is set
- Check constructor arguments match
- Wait a few minutes after deployment before verifying

### "Module not found"
- Run `npm install` to install dependencies
- Check Node.js version (v16+)

## 📚 Additional Resources

- **Deployment Guide**: See `CONTRACT_DEPLOYMENT.md`
- **Deployment Checklist**: See `DEPLOYMENT_CHECKLIST.md`
- **Status Report**: See `DEPLOYMENT_STATUS.md`

## 🔒 Security Notes

⚠️ **IMPORTANT**:
- Never commit private keys to version control
- Use environment variables for sensitive data
- Test on testnet before mainnet
- Consider multi-sig wallets for production
- Audit contracts before mainnet deployment

## 📞 Support

For issues or questions:
- Review deployment documentation
- Check provider-specific README files
- Consult chain-specific deployment guides


