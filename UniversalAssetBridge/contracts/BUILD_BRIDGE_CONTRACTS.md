# Building OASIS Bridge Contracts

## Overview

This document explains how to generate, compile, and deploy the OASIS Bridge contracts for all supported chains using the SmartContractGenerator API.

---

## Prerequisites

**1. Contract Generator API Running:**
```bash
cd /Volumes/Storage/OASIS_CLEAN/SmartContractGenerator/src/SmartContractGen/ScGen.API
dotnet run
```

The API will be available at: `http://localhost:5000`

**2. Required Tools Installed:**
- **Solidity:** `solc` compiler for Ethereum contracts
- **Rust + Anchor:** For Solana programs
- **Scrypto:** For Radix components

---

## Step 1: Generate Contracts

### Ethereum/EVM Chains (Solidity)

Generate the Solidity bridge contract:

```bash
curl -X POST "http://localhost:5000/api/v1/contracts/generate" \
  -F 'Language=Ethereum' \
  -F 'JsonFile=@/Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/bridge-specifications.json' \
  -o /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/OASISBridge.sol
```

This creates a Solidity contract that can be deployed to:
- Ethereum
- Polygon
- Arbitrum
- Base
- Optimism
- BSC
- Avalanche
- Fantom

### Solana (Rust/Anchor)

Generate the Solana bridge program:

```bash
curl -X POST "http://localhost:5000/api/v1/contracts/generate" \
  -F 'Language=Rust' \
  -F 'JsonFile=@/Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/bridge-specifications.json' \
  -o /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/solana-bridge-program.zip
```

Extract and build:
```bash
cd /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/
unzip solana-bridge-program.zip
cd solana-bridge-program
anchor build
```

### Radix (Scrypto)

Generate the Radix bridge component:

```bash
curl -X POST "http://localhost:5000/api/v1/contracts/generate" \
  -F 'Language=Scrypto' \
  -F 'JsonFile=@/Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/bridge-specifications.json' \
  -o /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/radix-bridge-component.zip
```

Extract and build:
```bash
cd /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/
unzip radix-bridge-component.zip
cd radix-bridge-component
scrypto build
```

---

## Step 2: Compile Contracts

### Ethereum/EVM

```bash
curl -X POST "http://localhost:5000/api/v1/contracts/compile" \
  -F 'Language=Ethereum' \
  -F 'Source=@/Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/OASISBridge.sol' \
  -o /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/OASISBridge-compiled.zip
```

Extract the compiled files:
```bash
cd /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/
unzip OASISBridge-compiled.zip
# This creates OASISBridge.abi and OASISBridge.bin
```

### Solana

```bash
cd /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/solana-bridge-program
anchor build
# Creates target/deploy/solana_bridge_program.so
```

### Radix

```bash
cd /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/radix-bridge-component
scrypto build
# Creates target/wasm32-unknown-unknown/release/radix_bridge.wasm
```

---

## Step 3: Deploy to Testnets

### Ethereum Sepolia

```bash
curl -X POST "http://localhost:5000/api/v1/contracts/deploy" \
  -F 'Language=Ethereum' \
  -F 'Schema=@/Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/OASISBridge.abi' \
  -F 'CompiledContractFile=@/Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/OASISBridge.bin'
```

**Response:**
```json
{
  "transactionHash": "0x...",
  "contractAddress": "0x...",
  "network": "sepolia"
}
```

### Solana Devnet

```bash
curl -X POST "http://localhost:5000/api/v1/contracts/deploy" \
  -F 'Language=Rust' \
  -F 'CompiledContractFile=@/Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/solana-bridge-program/target/deploy/solana_bridge_program.so'
```

**Response:**
```json
{
  "programId": "...",
  "transactionSignature": "...",
  "network": "devnet"
}
```

### Radix Testnet (Stokenet)

```bash
curl -X POST "http://localhost:5000/api/v1/contracts/deploy" \
  -F 'Language=Scrypto' \
  -F 'CompiledContractFile=@/Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts/radix-bridge-component/target/wasm32-unknown-unknown/release/radix_bridge.wasm'
```

**Response:**
```json
{
  "componentAddress": "component_...",
  "packageAddress": "package_...",
  "transactionId": "...",
  "network": "stokenet"
}
```

---

## Step 4: Deploy to All EVM Chains

For each EVM chain, deploy the same compiled Solidity contract:

**Polygon Mumbai:**
```bash
# Update appsettings.json to point to Polygon RPC
curl -X POST "http://localhost:5000/api/v1/contracts/deploy" \
  -F 'Language=Ethereum' \
  -F 'Schema=@OASISBridge.abi' \
  -F 'CompiledContractFile=@OASISBridge.bin'
```

**Arbitrum Sepolia:**
```bash
# Update appsettings.json to point to Arbitrum RPC
curl -X POST "http://localhost:5000/api/v1/contracts/deploy" \
  -F 'Language=Ethereum' \
  -F 'Schema=@OASISBridge.abi' \
  -F 'CompiledContractFile=@OASISBridge.bin'
```

Repeat for:
- Base Goerli
- Optimism Goerli
- BSC Testnet
- Avalanche Fuji
- Fantom Testnet

---

## Contract Addresses Registry

After deployment, save all contract addresses to a registry file:

```json
{
  "bridges": {
    "ethereum": {
      "testnet": {
        "network": "sepolia",
        "address": "0x...",
        "deployedAt": "2025-11-06T...",
        "txHash": "0x..."
      },
      "mainnet": {
        "network": "mainnet",
        "address": null,
        "deployedAt": null
      }
    },
    "solana": {
      "testnet": {
        "network": "devnet",
        "programId": "...",
        "deployedAt": "2025-11-06T...",
        "signature": "..."
      },
      "mainnet": {
        "network": "mainnet-beta",
        "programId": null
      }
    },
    "polygon": {
      "testnet": {
        "network": "mumbai",
        "address": "0x...",
        "deployedAt": "2025-11-06T...",
        "txHash": "0x..."
      }
    },
    "arbitrum": {
      "testnet": {
        "network": "sepolia",
        "address": "0x...",
        "deployedAt": "2025-11-06T...",
        "txHash": "0x..."
      }
    },
    "radix": {
      "testnet": {
        "network": "stokenet",
        "componentAddress": "component_...",
        "packageAddress": "package_...",
        "deployedAt": "2025-11-06T..."
      }
    }
  }
}
```

---

## Next Steps After Deployment

### 1. Update OASIS Provider Configurations

Update each blockchain provider (EthereumOASIS, SolanaOASIS, etc.) with the deployed contract addresses:

**File:** `OASIS_DNA_devnet.json`
```json
{
  "EthereumOASIS": {
    "BridgeContractAddress": "0x...",
    "ChainPrivateKey": "...",
    "ConnectionString": "https://sepolia.infura.io/v3/..."
  },
  "SolanaOASIS": {
    "BridgeProgramId": "...",
    "PrivateKey": "...",
    "ConnectionString": "https://api.devnet.solana.com"
  }
}
```

### 2. Authorize Oracle Addresses

Each bridge contract needs to authorize the OASIS oracle (HyperDrive) to mint/release tokens:

**Ethereum:**
```solidity
// Call this on deployed contract
bridge.authorizeRelayer(oracleAddress);
```

**Solana:**
```bash
# Update authority in program
anchor run authorize-oracle --provider.cluster devnet
```

### 3. Test Bridge Flow

**Test on testnets:**
1. Lock 1 SOL on Solana
2. Verify `TokensLocked` event emitted
3. Oracle detects event via HyperDrive
4. Oracle calls `mintTokens` on Ethereum
5. Verify tokens received on Ethereum
6. Reverse: Burn on Ethereum, release on Solana

### 4. Security Audit

Before mainnet:
- **Internal review:** Code review by team
- **External audit:** Hire security firm ($50K-$200K)
- **Bug bounty:** Launch on Immunefi ($50K reserve)
- **Testnet period:** Run for 2-3 months minimum

---

## Automated Deployment Script

Create a bash script to deploy to all chains:

```bash
#!/bin/bash
# deploy-all-bridges.sh

CONTRACTS_DIR="/Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/contracts"
API_URL="http://localhost:5000/api/v1/contracts"

# Step 1: Generate contracts
echo "Generating contracts..."
curl -X POST "$API_URL/generate" \
  -F "Language=Ethereum" \
  -F "JsonFile=@$CONTRACTS_DIR/bridge-specifications.json" \
  -o "$CONTRACTS_DIR/OASISBridge.sol"

# Step 2: Compile Ethereum contract
echo "Compiling Solidity..."
curl -X POST "$API_URL/compile" \
  -F "Language=Ethereum" \
  -F "Source=@$CONTRACTS_DIR/OASISBridge.sol" \
  -o "$CONTRACTS_DIR/compiled.zip"

cd $CONTRACTS_DIR
unzip -o compiled.zip

# Step 3: Deploy to all EVM chains
CHAINS=("ethereum" "polygon" "arbitrum" "base" "optimism" "bsc" "avalanche" "fantom")

for chain in "${CHAINS[@]}"; do
  echo "Deploying to $chain..."
  # Update config for each chain
  # Deploy
  curl -X POST "$API_URL/deploy" \
    -F "Language=Ethereum" \
    -F "Schema=@$CONTRACTS_DIR/OASISBridge.abi" \
    -F "CompiledContractFile=@$CONTRACTS_DIR/OASISBridge.bin" \
    > "$CONTRACTS_DIR/deployed-$chain.json"
done

echo "Deployment complete!"
```

---

## Monitoring & Maintenance

### Event Monitoring

HyperDrive needs to monitor these events on all chains:

```
TokensLocked → Trigger mint on destination
TokensBurned → Trigger release on source
TokensMinted → Update internal state
TokensReleased → Update internal state
```

### Health Checks

Regular checks every 10 minutes:
- Contract balance matches internal records
- No stuck orders (> 1 hour old and pending)
- Oracle authority still valid
- All chains responding

### Emergency Procedures

If security issue detected:
1. Call `pause()` on all contracts immediately
2. Investigate issue
3. Deploy fix if needed
4. Resume with `unpause()`

---

## Estimated Timeline

- **Contract Generation:** 1 hour
- **Compilation & Testing:** 1 day
- **Testnet Deployment:** 1 day
- **Integration with HyperDrive:** 1 week
- **Testing Period:** 2-3 months
- **Security Audit:** 4-8 weeks
- **Mainnet Deployment:** 1 week

**Total: 4-5 months to production**

---

## Next: Start Generation

To begin, start the contract generator API:

```bash
cd /Volumes/Storage/OASIS_CLEAN/SmartContractGenerator/src/SmartContractGen/ScGen.API
dotnet run
```

Then run the generation commands above.

