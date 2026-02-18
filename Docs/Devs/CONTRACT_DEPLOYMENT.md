# OASIS Smart Contract Deployment Guide

This comprehensive guide provides instructions for deploying OASIS smart contracts to **ALL** supported blockchain providers.

## Overview

OASIS supports multiple blockchain ecosystems, each requiring different deployment approaches:

- **EVM-Compatible Chains**: Solidity contracts (Ethereum, Arbitrum, Optimism, Base, Polygon, BNB Chain, Fantom, Avalanche, Rootstock, TRON, zkSync, Linea, Scroll, TON)
- **Move-Based Chains**: Move contracts (Aptos, Sui)
- **Cosmos Ecosystem**: CosmWasm contracts (Cosmos Hub, Osmosis, etc.)
- **Polkadot Ecosystem**: ink! contracts (Polkadot, Kusama, parachains)
- **Other Chains**: Cardano (Plutus), NEAR (Rust/WASM), Solana (Rust/BPF), EOSIO (C++), Bitcoin (OP_RETURN), XRPL (Memos), Hedera (Solidity/File Service)

---

## Table of Contents

1. [EVM-Compatible Chains](#evm-compatible-chains)
2. [Move-Based Chains (Aptos, Sui)](#move-based-chains)
3. [Cosmos Ecosystem (CosmWasm)](#cosmos-ecosystem)
4. [Polkadot Ecosystem (ink!)](#polkadot-ecosystem)
5. [Solana (Rust/BPF)](#solana)
6. [Cardano (Plutus)](#cardano)
7. [NEAR (Rust/WASM)](#near)
8. [EOSIO/Telos (C++)](#eosio)
9. [Bitcoin (OP_RETURN)](#bitcoin)
10. [XRPL (Transaction Memos)](#xrpl)
11. [Hedera Hashgraph](#hedera)
12. [TRON](#tron)
13. [Verification & Testing](#verification)
14. [Security Best Practices](#security)

---

## EVM-Compatible Chains

**Chains**: Ethereum, Arbitrum, Optimism, Base, Polygon, BNB Chain, Fantom, Avalanche, Rootstock, TRON, zkSync Era, Linea, Scroll, TON EVM

**Contract Language**: Solidity ^0.8.19

**Contract Source**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.OptimismOASIS/contracts/OASIS.sol`

### Prerequisites

1. **Deployer Wallet**: Wallet with native tokens (ETH, MATIC, BNB, etc.) for gas fees
2. **Deployment Tool**: Hardhat, Truffle, or Remix IDE
3. **Node.js**: v16+ for Hardhat/Truffle

### Deployment Steps

#### 1. Install Dependencies

```bash
npm install --save-dev hardhat @nomicfoundation/hardhat-toolbox
npm install --save-dev @openzeppelin/contracts  # If using OpenZeppelin
```

#### 2. Configure Networks

Create `hardhat.config.js`:

```javascript
require("@nomicfoundation/hardhat-toolbox");

module.exports = {
  solidity: {
    version: "0.8.19",
    settings: {
      optimizer: {
        enabled: true,
        runs: 200
      }
    }
  },
  networks: {
    // Ethereum Mainnet
    ethereum: {
      url: process.env.ETHEREUM_RPC_URL || "https://eth.llamarpc.com",
      chainId: 1,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY]
    },
    // Arbitrum
    arbitrum: {
      url: process.env.ARBITRUM_RPC_URL || "https://arb1.arbitrum.io/rpc",
      chainId: 42161,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY]
    },
    // Optimism
    optimism: {
      url: process.env.OPTIMISM_RPC_URL || "https://mainnet.optimism.io",
      chainId: 10,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY]
    },
    // Base
    base: {
      url: process.env.BASE_RPC_URL || "https://mainnet.base.org",
      chainId: 8453,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY]
    },
    // Polygon
    polygon: {
      url: process.env.POLYGON_RPC_URL || "https://polygon-rpc.com",
      chainId: 137,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY]
    },
    // BNB Chain
    bnb: {
      url: process.env.BNB_RPC_URL || "https://bsc-dataseed.binance.org",
      chainId: 56,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY]
    },
    // Fantom
    fantom: {
      url: process.env.FANTOM_RPC_URL || "https://rpc.ftm.tools",
      chainId: 250,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY]
    },
    // Avalanche
    avalanche: {
      url: process.env.AVALANCHE_RPC_URL || "https://api.avax.network/ext/bc/C/rpc",
      chainId: 43114,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY]
    },
    // zkSync Era
    zkSync: {
      url: process.env.ZKSYNC_RPC_URL || "https://mainnet.era.zksync.io",
      chainId: 324,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY],
      zksync: true  // Requires @matterlabs/hardhat-zksync
    },
    // Linea
    linea: {
      url: process.env.LINEA_RPC_URL || "https://rpc.linea.build",
      chainId: 59144,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY]
    },
    // Scroll
    scroll: {
      url: process.env.SCROLL_RPC_URL || "https://rpc.scroll.io",
      chainId: 534352,
      accounts: [process.env.DEPLOYER_PRIVATE_KEY]
    }
  }
};
```

#### 3. Deploy Script

Create `scripts/deploy.js`:

```javascript
const hre = require("hardhat");

async function main() {
  const OASIS = await hre.ethers.getContractFactory("OASIS");
  const oasis = await OASIS.deploy();
  await oasis.waitForDeployment();
  
  const address = await oasis.getAddress();
  console.log("OASIS deployed to:", address);
  console.log("Network:", hre.network.name);
  console.log("Chain ID:", (await hre.ethers.provider.getNetwork()).chainId);
  
  // Verify on block explorer (optional)
  if (hre.network.name !== "hardhat") {
    await hre.run("verify:verify", {
      address: address,
      constructorArguments: []
    });
  }
}

main()
  .then(() => process.exit(0))
  .catch((error) => {
    console.error(error);
    process.exit(1);
  });
```

#### 4. Deploy

```bash
# Set private key
export DEPLOYER_PRIVATE_KEY="your-private-key-here"

# Deploy to specific network
npx hardhat run scripts/deploy.js --network ethereum
npx hardhat run scripts/deploy.js --network arbitrum
npx hardhat run scripts/deploy.js --network optimism
# ... etc for each chain
```

#### 5. Update Configuration

Update `OASIS_DNA.json` with deployed addresses:

```json
{
  "StorageProviders": {
    "EthereumOASIS": {
      "ContractAddress": "0xYourDeployedAddress",
      "ChainPrivateKey": "your-private-key",
      "ChainId": 1
    },
    "ArbitrumOASIS": {
      "ContractAddress": "0xYourDeployedAddress",
      "ChainPrivateKey": "your-private-key",
      "ChainId": 42161
    }
    // ... etc
  }
}
```

---

## Move-Based Chains

### Aptos

**Contract Language**: Move

**Contract Source**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AptosOASIS/contracts/Oasis.move`

#### Prerequisites

```bash
# Install Aptos CLI
curl -fsSL "https://aptos.dev/scripts/install_cli.py" | python3
```

#### Deployment Steps

1. **Initialize Project**:
   ```bash
   aptos init --network mainnet
   ```

2. **Compile Contract**:
   ```bash
   aptos move compile --named-addresses oasis=your-account-address
   ```

3. **Publish Module**:
   ```bash
   aptos move publish \
     --named-addresses oasis=your-account-address \
     --assume-yes
   ```

4. **Update Configuration**:
   ```json
   "AptosOASIS": {
     "RpcEndpoint": "https://fullnode.mainnet.aptoslabs.com",
     "ContractAddress": "your-account-address",
     "Network": "mainnet"
   }
   ```

**Module Address Format**: `{your-account-address}::oasis::oasis`

### Sui

**Contract Language**: Sui Move

**Contract Source**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/contracts/oasis.move`

#### Prerequisites

```bash
# Install Sui CLI
cargo install --locked --git https://github.com/MystenLabs/sui.git --branch main sui
```

#### Deployment Steps

1. **Initialize Project**:
   ```bash
   sui client new-address ed25519
   sui client active-address
   ```

2. **Publish Package**:
   ```bash
   sui client publish \
     --gas-budget 100000000 \
     --json
   ```

3. **Get Package ID**:
   ```bash
   sui client objects --address $(sui client active-address)
   ```

4. **Update Configuration**:
   ```json
   "SuiOASIS": {
     "RpcEndpoint": "https://fullnode.mainnet.sui.io:443",
     "ContractAddress": "0xYourPackageId",
     "Network": "mainnet"
   }
   ```

---

## Cosmos Ecosystem

**Contract Language**: CosmWasm (Rust)

**Contract Source**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/contracts/`

### Prerequisites

```bash
# Install Rust
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh

# Add wasm32 target
rustup target add wasm32-unknown-unknown

# Install wasm-opt
cargo install wasm-opt
```

### Deployment Steps

1. **Build Contract**:
   ```bash
   cd Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/contracts/oasis
   cargo wasm
   ```

2. **Optimize WASM**:
   ```bash
   wasm-opt -Os target/wasm32-unknown-unknown/release/oasis.wasm \
     -o oasis-optimized.wasm
   ```

3. **Store Contract**:
   ```bash
   wasmd tx wasm store oasis-optimized.wasm \
     --from mykey \
     --gas auto \
     --gas-adjustment 1.3 \
     --chain-id cosmoshub-4
   ```

4. **Instantiate Contract**:
   ```bash
   wasmd tx wasm instantiate <code_id> '{}' \
     --from mykey \
     --label "oasis" \
     --admin $(wasmd keys show mykey -a) \
     --gas auto \
     --chain-id cosmoshub-4
   ```

5. **Update Configuration**:
   ```json
   "CosmosBlockChainOASIS": {
     "RpcEndpoint": "https://cosmos-rpc.polkachu.com",
     "ContractAddress": "cosmos1...",
     "ChainId": "cosmoshub-4"
   }
   ```

---

## Polkadot Ecosystem

**Contract Language**: ink! (Rust)

**Contract Source**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/contracts/`

### Prerequisites

```bash
# Install Rust
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh

# Install cargo-contract
cargo install cargo-contract --force
```

### Deployment Steps

1. **Build Contract**:
   ```bash
   cd Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/contracts/oasis
   cargo contract build --release
   ```

2. **Upload Contract** (via Polkadot.js Apps):
   - Go to https://polkadot.js.org/apps
   - Navigate to Developer > Contracts
   - Upload the `.contract` file
   - Note the code hash

3. **Instantiate Contract**:
   - Use Polkadot.js Apps or CLI
   - Instantiate with constructor parameters
   - Note the contract address

4. **Update Configuration**:
   ```json
   "PolkadotOASIS": {
     "RpcEndpoint": "wss://rpc.polkadot.io",
     "ContractAddress": "5...",
     "Network": "mainnet"
   }
   ```

---

## Solana

**Contract Language**: Rust (BPF)

**Contract Source**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/Contracts/OASISStorage.sol`

### Prerequisites

```bash
# Install Solana CLI
sh -c "$(curl -sSfL https://release.solana.com/stable/install)"

# Install Anchor framework
cargo install --git https://github.com/coral-xyz/anchor avm --locked --force
avm install latest
avm use latest
```

### Deployment Steps

1. **Build Program**:
   ```bash
   anchor build
   ```

2. **Deploy Program**:
   ```bash
   solana program deploy \
     target/deploy/oasis_storage.so \
     --url mainnet-beta
   ```

3. **Update Configuration**:
   ```json
   "SolanaOASIS": {
     "ConnectionString": "https://api.mainnet-beta.solana.com",
     "ProgramId": "YourProgramId",
     "PublicKey": "YourWalletPublicKey"
   }
   ```

---

## Cardano

**Contract Language**: Plutus (Haskell)

**Storage Method**: Native tokens or Plutus smart contracts

### Configuration

CardanoOASIS uses native transactions with metadata. No contract deployment needed, but configure:

```json
"CardanoOASIS": {
  "RpcEndpoint": "https://cardano-mainnet.blockfrost.io/api/v0",
  "NetworkId": "mainnet",
  "ApiKey": "YOUR_BLOCKFROST_API_KEY"
}
```

---

## NEAR

**Contract Language**: Rust (compiled to WASM)

### Prerequisites

```bash
# Install NEAR CLI
npm install -g near-cli
```

### Deployment Steps

1. **Build Contract**:
   ```bash
   cd contracts/oasis
   cargo build --target wasm32-unknown-unknown --release
   ```

2. **Deploy Contract**:
   ```bash
   near deploy \
     --wasmFile target/wasm32-unknown-unknown/release/oasis.wasm \
     --accountId oasis.your-account.near \
     --networkId mainnet
   ```

3. **Update Configuration**:
   ```json
   "NEAROASIS": {
     "RpcEndpoint": "https://rpc.mainnet.near.org",
     "ContractAddress": "oasis.your-account.near",
     "Network": "mainnet"
   }
   ```

---

## EOSIO

**Contract Language**: C++

**Chains**: EOSIO, Telos, SEEDS

### Prerequisites

```bash
# Install EOSIO CDT
git clone https://github.com/EOSIO/eosio.cdt
cd eosio.cdt
./build.sh
```

### Deployment Steps

1. **Compile Contract**:
   ```bash
   eosio-cpp -o oasis.wasm oasis.cpp --abigen
   ```

2. **Create Account**:
   ```bash
   cleos create account eosio oasis EOS6MRyAjQq8ud7hVNYcfnVPJqcVpscN5So8BhtHuGYqET5GDW5CV
   ```

3. **Deploy Contract**:
   ```bash
   cleos set contract oasis /path/to/contract oasis.wasm oasis.abi
   ```

4. **Update Configuration**:
   ```json
   "EOSIOOASIS": {
     "AccountName": "oasis",
     "ConnectionString": "https://eos.greymass.com",
     "ChainId": "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906"
   }
   ```

---

## Bitcoin

**Storage Method**: OP_RETURN transactions (no smart contracts)

### Configuration

BitcoinOASIS uses OP_RETURN to embed data. No contract deployment needed:

```json
"BitcoinOASIS": {
  "RpcEndpoint": "https://api.blockcypher.com/v1/btc/main",
  "Network": "mainnet"
}
```

**Note**: Ensure your Bitcoin node supports OP_RETURN transactions.

---

## XRPL

**Storage Method**: Transaction memos (no smart contracts)

### Configuration

XRPLOASIS uses transaction memos to store data:

```json
"XRPLOASIS": {
  "RpcEndpoint": "https://s1.ripple.com:51234",
  "ArchiveAccount": "rYourXRPLAccountAddress",
  "Network": "mainnet"
}
```

**Setup Steps**:
1. Create an XRPL account (or use existing)
2. Fund it with minimum XRP reserve (~10 XRP)
3. Set `ArchiveAccount` in DNA config
4. Ensure avatars have XRPL wallets configured

---

## Hedera Hashgraph

**Storage Options**: 
1. **Hedera File Service** (recommended for data storage)
2. **Hedera Smart Contract Service** (Solidity contracts)

### Option 1: File Service (No Contract)

```json
"HashgraphOASIS": {
  "RpcEndpoint": "https://mainnet-public.mirrornode.hedera.com",
  "Network": "mainnet",
  "AccountId": "0.0.xxxxx",
  "PrivateKey": "your-private-key"
}
```

### Option 2: Smart Contract Service

Deploy Solidity contract similar to EVM chains:

```bash
# Use Hedera SDK or Remix IDE
# Deploy to Hedera Smart Contract Service
```

---

## TRON

**Contract Language**: Solidity (TRON-compatible)

**Contract Source**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TRONOASIS/Contracts/OASISStorage.sol`

### Prerequisites

```bash
# Install TronBox (Truffle for TRON)
npm install -g tronbox
```

### Deployment Steps

1. **Configure TronBox**:
   ```javascript
   // tronbox.js
   module.exports = {
     networks: {
       mainnet: {
         privateKey: process.env.TRON_PRIVATE_KEY,
         userFeePercentage: 100,
         feeLimit: 1000 * 1e6,
         fullHost: "https://api.trongrid.io"
       }
     }
   };
   ```

2. **Deploy**:
   ```bash
   tronbox migrate --network mainnet
   ```

3. **Update Configuration**:
   ```json
   "TRONOASIS": {
     "RpcEndpoint": "https://api.trongrid.io",
     "ContractAddress": "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t",
     "Network": "mainnet"
   }
   ```

---

## Verification & Testing

### Block Explorers

Verify contracts on chain-specific explorers:

| Chain | Explorer URL |
|-------|-------------|
| Ethereum | https://etherscan.io |
| Arbitrum | https://arbiscan.io |
| Optimism | https://optimistic.etherscan.io |
| Base | https://basescan.org |
| Polygon | https://polygonscan.com |
| BNB Chain | https://bscscan.com |
| Fantom | https://ftmscan.com |
| Avalanche | https://snowtrace.io |
| zkSync | https://explorer.zksync.io |
| Linea | https://lineascan.build |
| Scroll | https://scrollscan.com |
| Aptos | https://explorer.aptoslabs.com |
| Sui | https://suiexplorer.com |
| Cosmos | https://www.mintscan.io/cosmos |
| Polkadot | https://polkascan.io |
| Solana | https://explorer.solana.com |
| NEAR | https://explorer.near.org |
| TRON | https://tronscan.org |

### Test Harnesses

Run provider-specific test harnesses:

```bash
# Example: Test zkSync provider
cd Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.ZkSyncOASIS.TestHarness
dotnet run
```

### Integration Tests

Run integration tests to verify CRUD operations:

```bash
dotnet test Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.ZkSyncOASIS.IntegrationTests
```

---

## Security Best Practices

### General

- **Never commit private keys** to version control
- **Use environment variables** for sensitive data
- **Test on testnet first** before mainnet deployment
- **Use multi-sig wallets** for production deployments
- **Consider upgradeable contracts** if contract changes are expected
- **Audit contracts** before mainnet deployment

### Chain-Specific

- **EVM**: Use OpenZeppelin libraries for security
- **Move**: Leverage Move's built-in security features
- **CosmWasm**: Follow CosmWasm security guidelines
- **ink!**: Use ink! security best practices
- **Solana**: Follow Solana program security guidelines

### Key Management

- Use hardware wallets for production deployments
- Implement key rotation policies
- Use separate keys for testnet and mainnet
- Store keys in secure key management systems (AWS KMS, HashiCorp Vault, etc.)

---

## Contract Address Registry

After deployment, record all contract addresses:

| Provider | Network | Contract Address | Deployer | Transaction Hash | Deployed Date |
|----------|---------|------------------|----------|------------------|--------------|
| EthereumOASIS | Ethereum Mainnet | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| ArbitrumOASIS | Arbitrum One | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| OptimismOASIS | Optimism | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| BaseOASIS | Base | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| PolygonOASIS | Polygon | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| BNBChainOASIS | BNB Chain | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| FantomOASIS | Fantom | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| AvalancheOASIS | Avalanche C-Chain | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| ZkSyncOASIS | zkSync Era | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| LineaOASIS | Linea | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| ScrollOASIS | Scroll | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| AptosOASIS | Aptos | `{address}::oasis::oasis` | `{address}` | `0x...` | YYYY-MM-DD |
| SuiOASIS | Sui | `0x...` | `0x...` | `0x...` | YYYY-MM-DD |
| CosmosBlockChainOASIS | Cosmos Hub | `cosmos1...` | `cosmos1...` | `0x...` | YYYY-MM-DD |
| PolkadotOASIS | Polkadot | `5...` | `5...` | `0x...` | YYYY-MM-DD |
| SolanaOASIS | Solana Mainnet | `...` | `...` | `...` | YYYY-MM-DD |
| NEAROASIS | NEAR | `oasis.near` | `...` | `...` | YYYY-MM-DD |
| EOSIOOASIS | EOS | `oasis` | `...` | `...` | YYYY-MM-DD |
| TRONOASIS | TRON | `TR...` | `T...` | `...` | YYYY-MM-DD |

---

## Support & Resources

### Documentation

- Provider-specific README files in each provider directory
- Contract source code in `contracts/` directories
- Chain-specific deployment guides

### Community

- OASIS GitHub: [Link to repo]
- Discord/Slack: [Link to community]
- Documentation: [Link to docs]

### Chain-Specific Resources

- **Ethereum**: https://ethereum.org/developers
- **Aptos**: https://aptos.dev
- **Sui**: https://docs.sui.io
- **Cosmos**: https://docs.cosmos.network
- **Polkadot**: https://docs.polkadot.network
- **Solana**: https://docs.solana.com

---

## Troubleshooting

### Common Issues

1. **Gas Estimation Failed**: Ensure deployer wallet has sufficient native tokens
2. **Contract Verification Failed**: Check constructor arguments match deployment
3. **Network Connection Issues**: Verify RPC endpoint is accessible
4. **Compilation Errors**: Ensure contract language version matches chain requirements

### Getting Help

- Check provider-specific error logs
- Review chain-specific documentation
- Consult OASIS community channels
- Open GitHub issue with deployment details (redact sensitive info)

---

**Last Updated**: 2025-01-XX  
**Maintained By**: OASIS Development Team
