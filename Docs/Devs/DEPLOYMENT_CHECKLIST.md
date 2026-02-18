# OASIS Contract Deployment Checklist

This comprehensive checklist guides you through deploying OASIS contracts to all supported blockchains.

## 📋 Pre-Deployment Checklist

### Environment Setup

- [ ] **Node.js v16+ installed**
  ```bash
  node --version  # Should be v16 or higher
  ```

- [ ] **Hardhat installed globally or in project**
  ```bash
  npm install --save-dev hardhat @nomicfoundation/hardhat-toolbox
  ```

- [ ] **Aptos CLI installed** (for Aptos deployment)
  ```bash
  curl -fsSL https://aptos.dev/scripts/install_cli.py | python3
  ```

- [ ] **Sui CLI installed** (for Sui deployment)
  ```bash
  cargo install --locked --git https://github.com/MystenLabs/sui.git --branch main sui
  ```

- [ ] **Environment variables configured**
  - Create `.env` file in project root
  - Set `DEPLOYER_PRIVATE_KEY=your-private-key`
  - Set API keys for contract verification (optional):
    - `ETHERSCAN_API_KEY`
    - `ARBISCAN_API_KEY`
    - `POLYGONSCAN_API_KEY`
    - `BSCSCAN_API_KEY`
    - etc.

- [ ] **Deployer wallets created and funded**
  - [ ] Ethereum/Arbitrum/Optimism/Base: Funded with ETH
  - [ ] Polygon: Funded with MATIC
  - [ ] BNB Chain: Funded with BNB
  - [ ] Fantom: Funded with FTM
  - [ ] Avalanche: Funded with AVAX
  - [ ] Aptos: Funded with APT
  - [ ] Sui: Funded with SUI
  - [ ] Other chains: Funded with native tokens

- [ ] **Hardhat config created**
  ```bash
  cp scripts/hardhat.config.template.js hardhat.config.js
  # Edit hardhat.config.js with your RPC URLs
  ```

- [ ] **Contract source verified**
  - [ ] OASIS.sol exists in contract directories
  - [ ] Move contracts exist for Aptos/Sui
  - [ ] Contracts compile without errors

## 🧪 Testnet Deployment

### EVM Testnets

- [ ] **Ethereum Sepolia**
  ```bash
  node scripts/deploy-evm-chain.js sepolia
  ```

- [ ] **Arbitrum Sepolia**
  ```bash
  node scripts/deploy-evm-chain.js arbitrumSepolia
  ```

- [ ] **Optimism Sepolia**
  ```bash
  node scripts/deploy-evm-chain.js optimismSepolia
  ```

- [ ] **Base Sepolia**
  ```bash
  node scripts/deploy-evm-chain.js baseSepolia
  ```

- [ ] **Polygon Amoy**
  ```bash
  node scripts/deploy-evm-chain.js amoy
  ```

- [ ] **BNB Chain Testnet**
  ```bash
  node scripts/deploy-evm-chain.js bnbTestnet
  ```

- [ ] **Fantom Testnet**
  ```bash
  node scripts/deploy-evm-chain.js fantomTestnet
  ```

- [ ] **Avalanche Fuji**
  ```bash
  node scripts/deploy-evm-chain.js fuji
  ```

- [ ] **Rootstock Testnet**
  ```bash
  node scripts/deploy-evm-chain.js rootstockTestnet
  ```

- [ ] **zkSync Era Testnet**
  ```bash
  node scripts/deploy-evm-chain.js zkSyncTestnet
  ```

- [ ] **Linea Testnet**
  ```bash
  node scripts/deploy-evm-chain.js lineaTestnet
  ```

- [ ] **Scroll Sepolia**
  ```bash
  node scripts/deploy-evm-chain.js scrollSepolia
  ```

**OR deploy all testnets at once:**
```bash
./scripts/deploy-all-evm-testnet.sh
```

### Move Testnets

- [ ] **Aptos Testnet**
  ```bash
  ./scripts/deploy-aptos.sh testnet
  ```

- [ ] **Sui Testnet**
  ```bash
  ./scripts/deploy-sui.sh testnet
  ```

**OR deploy all Move testnets:**
```bash
./scripts/deploy-all-move.sh testnet
```

### Testnet Verification

- [ ] **All contracts deployed successfully**
  ```bash
  node scripts/check-deployment-status.js
  ```

- [ ] **Contracts verified on block explorers**
  - Check each contract on respective testnet explorer
  - Verify contract code matches source

- [ ] **Integration tests pass on testnet**
  ```bash
  # Run provider-specific integration tests
  dotnet test Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.*.IntegrationTests
  ```

- [ ] **Test CRUD operations**
  - [ ] Save avatar
  - [ ] Load avatar
  - [ ] Save holon
  - [ ] Load holon
  - [ ] Search operations

- [ ] **Update testnet addresses in DNA**
  ```bash
  node scripts/update-dna-from-deployments.js
  ```

## 🚀 Mainnet Deployment

### ⚠️ Mainnet Deployment Warning

**Before deploying to mainnet:**
- [ ] All testnet deployments successful
- [ ] All testnet tests passing
- [ ] Contracts audited (recommended)
- [ ] Multi-sig wallet set up (recommended)
- [ ] Gas fees budgeted
- [ ] Deployment plan documented

### EVM Mainnets

- [ ] **Ethereum Mainnet**
  ```bash
  node scripts/deploy-evm-chain.js ethereum mainnet
  ```

- [ ] **Arbitrum One**
  ```bash
  node scripts/deploy-evm-chain.js arbitrum mainnet
  ```

- [ ] **Optimism**
  ```bash
  node scripts/deploy-evm-chain.js optimism mainnet
  ```

- [ ] **Base**
  ```bash
  node scripts/deploy-evm-chain.js base mainnet
  ```

- [ ] **Polygon**
  ```bash
  node scripts/deploy-evm-chain.js polygon mainnet
  ```

- [ ] **BNB Chain**
  ```bash
  node scripts/deploy-evm-chain.js bnb mainnet
  ```

- [ ] **Fantom**
  ```bash
  node scripts/deploy-evm-chain.js fantom mainnet
  ```

- [ ] **Avalanche C-Chain**
  ```bash
  node scripts/deploy-evm-chain.js avalanche mainnet
  ```

- [ ] **Rootstock**
  ```bash
  node scripts/deploy-evm-chain.js rootstock mainnet
  ```

- [ ] **zkSync Era**
  ```bash
  node scripts/deploy-evm-chain.js zkSync mainnet
  ```

- [ ] **Linea**
  ```bash
  node scripts/deploy-evm-chain.js linea mainnet
  ```

- [ ] **Scroll**
  ```bash
  node scripts/deploy-evm-chain.js scroll mainnet
  ```

**OR deploy all mainnets at once (⚠️ WARNING: Expensive!):**
```bash
./scripts/deploy-all-evm-mainnet.sh
```

### Move Mainnets

- [ ] **Aptos Mainnet**
  ```bash
  ./scripts/deploy-aptos.sh mainnet
  ```

- [ ] **Sui Mainnet**
  ```bash
  ./scripts/deploy-sui.sh mainnet
  ```

**OR deploy all Move mainnets:**
```bash
./scripts/deploy-all-move.sh mainnet
```

### Mainnet Verification

- [ ] **All contracts deployed successfully**
  ```bash
  node scripts/check-deployment-status.js
  ```

- [ ] **Contracts verified on block explorers**
  - [ ] Ethereum: https://etherscan.io
  - [ ] Arbitrum: https://arbiscan.io
  - [ ] Optimism: https://optimistic.etherscan.io
  - [ ] Base: https://basescan.org
  - [ ] Polygon: https://polygonscan.com
  - [ ] BNB Chain: https://bscscan.com
  - [ ] Fantom: https://ftmscan.com
  - [ ] Avalanche: https://snowtrace.io
  - [ ] zkSync: https://explorer.zksync.io
  - [ ] Linea: https://lineascan.build
  - [ ] Scroll: https://scrollscan.com
  - [ ] Aptos: https://explorer.aptoslabs.com
  - [ ] Sui: https://suiexplorer.com

- [ ] **Update OASIS_DNA.json with mainnet addresses**
  ```bash
  node scripts/update-dna-from-deployments.js
  ```

- [ ] **Update deployed-addresses.json**
  - Verify all addresses are recorded
  - Include transaction hashes
  - Include deployment dates

- [ ] **Documentation updated**
  - [ ] CONTRACT_DEPLOYMENT.md updated with addresses
  - [ ] DEPLOYMENT_STATUS.md updated
  - [ ] README files updated

- [ ] **Integration tests pass on mainnet**
  ```bash
  # Run smoke tests (be careful with mainnet!)
  dotnet test Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.*.IntegrationTests
  ```

## 📊 Post-Deployment

### Verification & Testing

- [ ] **All providers activated successfully**
- [ ] **Avatar CRUD operations work**
- [ ] **Holon CRUD operations work**
- [ ] **Search operations work**
- [ ] **NFT operations work** (if applicable)
- [ ] **Bridge operations work** (if applicable)

### Documentation

- [ ] **Contract addresses documented**
  - [ ] In OASIS_DNA.json
  - [ ] In deployed-addresses.json
  - [ ] In CONTRACT_DEPLOYMENT.md
  - [ ] In provider README files

- [ ] **Deployment dates recorded**
- [ ] **Transaction hashes recorded**
- [ ] **Deployer addresses recorded**
- [ ] **Gas costs documented**

### Monitoring

- [ ] **Set up monitoring** (optional)
  - [ ] Contract event monitoring
  - [ ] Error tracking
  - [ ] Usage analytics

- [ ] **Set up alerts** (optional)
  - [ ] Contract upgrade alerts
  - [ ] Error rate alerts
  - [ ] Gas price alerts

## 🔒 Security Checklist

- [ ] **Private keys secured**
  - [ ] Never committed to version control
  - [ ] Stored in secure key management system
  - [ ] Access restricted to authorized personnel

- [ ] **Multi-sig wallets used** (recommended for mainnet)
- [ ] **Contracts audited** (recommended for mainnet)
- [ ] **Upgrade mechanisms reviewed**
- [ ] **Access controls verified**
- [ ] **Emergency procedures documented**

## 📝 Deployment Log Template

For each deployment, record:

```
Chain: [Chain Name]
Network: [Mainnet/Testnet]
Date: [YYYY-MM-DD]
Deployer: [Address]
Contract Address: [0x...]
Transaction Hash: [0x...]
Gas Used: [Number]
Gas Price: [Number]
Total Cost: [Amount + Currency]
Explorer: [URL]
Notes: [Any relevant notes]
```

## 🆘 Troubleshooting

### Common Issues

1. **Insufficient Balance**
   - Solution: Fund deployer wallet with native tokens

2. **RPC Connection Failed**
   - Solution: Check RPC URL, try alternative RPC endpoint

3. **Gas Estimation Failed**
   - Solution: Increase gas limit, check contract size

4. **Contract Verification Failed**
   - Solution: Ensure constructor arguments match, check API key

5. **Compilation Errors**
   - Solution: Check Solidity version, verify contract dependencies

## 📞 Support

- Review `CONTRACT_DEPLOYMENT.md` for detailed instructions
- Check provider-specific README files
- Review contract source code
- Consult chain-specific documentation

---

**Last Updated**: 2025-01-XX  
**Status**: Ready for deployment


