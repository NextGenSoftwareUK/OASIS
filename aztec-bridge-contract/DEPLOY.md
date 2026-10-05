# Quick Deployment Guide

## Current Status

✅ Contract code written: `src/main.nr`
⚠️ Compilation blocked by dependency path issue
✅ Deployment script ready: `deploy-aztec-bridge.sh`

## Quick Fix & Deploy

### Step 1: Fix Nargo.toml

```bash
cd ~/aztec-bridge-contract
cat > Nargo.toml << 'EOF'
[package]
name = "bridge_contract"
type = "contract"
authors = ["OASIS"]
compiler_version = ">=0.18.0"

[dependencies]
aztec = { git = "https://github.com/AztecProtocol/aztec-packages/", tag = "v3.0.0-devnet.5", directory = "noir-projects/aztec-nr/aztec" }
EOF
```

### Step 2: Compile

```bash
rm -rf ~/nargo  # Clear cache
export PATH="$HOME/.aztec/bin:$PATH"
aztec-nargo compile
```

### Step 3: Deploy

```bash
export NODE_URL=https://aztec-testnet-fullnode.zkv.xyz
export PATH="$HOME/.aztec/bin:$PATH"

aztec-wallet deploy \
    --node-url $NODE_URL \
    --from accounts:maxgershfield \
    --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
    --alias bridge \
    target/bridge_contract-BridgeContract.json
```

### Step 4: Update OASIS Config

After deployment, copy the contract address and update:
- `appsettings.json`: Add `BridgeContractAddress`
- `AztecBridgeService.cs`: Use config value instead of placeholder

---

**Note**: If compilation still fails, use the aztec-starter template approach (see main deployment guide).

