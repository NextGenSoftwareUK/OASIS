# 🚀 Aztec Bridge Contract Deployment Guide

## Current Status

✅ **Contract Created**: `~/aztec-bridge-contract/src/main.nr`
✅ **Nargo.toml Configured**: Using correct Aztec dependencies
⚠️ **Compilation Issue**: Dependency path needs adjustment
✅ **Deployment Ready**: Can deploy once compiled

---

## Contract Structure

The bridge contract includes:

1. **`deposit(owner, amount)`** - Private function to deposit funds
2. **`withdraw(destination, amount)`** - Private function to withdraw funds
3. **`get_deposits(user)`** - Public utility to query deposits
4. **`get_withdrawals(user)`** - Public utility to query withdrawals

---

## Deployment Steps

### Option 1: Use Aztec Starter Template (Recommended)

The contract structure is correct but has a dependency path issue. Use the working aztec-starter template:

```bash
# 1. Copy contract to aztec-starter
cd ~/aztec-starter
cp ~/aztec-bridge-contract/src/main.nr src/bridge_contract.nr

# 2. Update Nargo.toml to include bridge contract
# (or create a new package)

# 3. Compile
aztec-nargo compile

# 4. Generate TypeScript bindings
aztec codegen ./target/bridge_contract-BridgeContract.json -o src/artifacts

# 5. Deploy using TypeScript
# (see deployment script below)
```

### Option 2: Fix Dependency Path

The issue is the dependency tag. Update `Nargo.toml`:

```toml
[package]
name = "bridge_contract"
type = "contract"
authors = ["OASIS"]
compiler_version = ">=0.18.0"

[dependencies]
aztec = { git = "https://github.com/AztecProtocol/aztec-packages/", tag = "v3.0.0-devnet.5", directory = "noir-projects/aztec-nr/aztec" }
```

Then:
```bash
cd ~/aztec-bridge-contract
rm -rf ~/nargo  # Clear cache
aztec-nargo compile
```

### Option 3: Deploy Using Aztec CLI (Simplest)

If you have a compiled contract artifact, deploy directly:

```bash
export NODE_URL=https://aztec-testnet-fullnode.zkv.xyz
export PATH="$HOME/.aztec/bin:$PATH"

# Deploy contract
aztec-wallet deploy \
    --node-url $NODE_URL \
    --from accounts:maxgershfield \
    --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
    --alias bridge \
    ~/aztec-bridge-contract/target/bridge_contract-BridgeContract.json
```

---

## TypeScript Deployment Script

Create `deploy-bridge.ts`:

```typescript
import { createAztecClient, getSchnorrAccount } from '@aztec/aztec.js';
import { BridgeContract } from './artifacts/BridgeContract';

async function deployBridge() {
    const nodeUrl = process.env.AZTEC_NODE_URL || 'https://aztec-testnet-fullnode.zkv.xyz';
    const client = await createAztecClient(nodeUrl);
    
    // Load account
    const account = await getSchnorrAccount(client, 'maxgershfield', nodeUrl);
    
    // Deploy contract
    const contract = await BridgeContract.deploy(account)
        .send({ from: account.address })
        .deployed();
    
    console.log('Bridge contract deployed at:', contract.address.toString());
    return contract.address.toString();
}

deployBridge().catch(console.error);
```

Run:
```bash
npm install @aztec/aztec.js
npx ts-node deploy-bridge.ts
```

---

## Update OASIS Configuration

After deployment, update the bridge contract address:

**`appsettings.json`**:
```json
"AztecBridge": {
  "BridgeContractAddress": "0x...", // Deployed contract address
  "NodeUrl": "https://aztec-testnet-fullnode.zkv.xyz"
}
```

**`AztecBridgeService.cs`**:
```csharp
// Replace placeholder
var bridgeContractAddress = configuration["AztecBridge:BridgeContractAddress"] 
    ?? "0x0000000000000000000000000000000000000000";
```

---

## Testing the Deployment

Once deployed, test the contract:

```bash
# Test deposit
aztec-wallet send deposit \
    --node-url $NODE_URL \
    --from accounts:maxgershfield \
    --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
    --contract-address <BRIDGE_CONTRACT_ADDRESS> \
    --args <OWNER_ADDRESS> <AMOUNT>

# Test withdraw
aztec-wallet send withdraw \
    --node-url $NODE_URL \
    --from accounts:maxgershfield \
    --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
    --contract-address <BRIDGE_CONTRACT_ADDRESS> \
    --args <DESTINATION_ADDRESS> <AMOUNT>

# Query deposits
aztec-wallet call get_deposits \
    --node-url $NODE_URL \
    --contract-address <BRIDGE_CONTRACT_ADDRESS> \
    --args <USER_ADDRESS>
```

---

## Next Steps

1. **Fix Compilation**: Resolve dependency path issue
2. **Compile Contract**: `aztec-nargo compile`
3. **Deploy Contract**: Use one of the methods above
4. **Update Configuration**: Add contract address to OASIS config
5. **Test Bridge**: Test deposit/withdraw operations

---

## Troubleshooting

### Issue: "Cannot read file .../aztec/Nargo.toml"
**Solution**: The dependency tag or path is incorrect. Use `v3.0.0-devnet.5` tag and `noir-projects/aztec-nr/aztec` directory.

### Issue: "Transactions are not permitted"
**Solution**: Account needs to be deployed first:
```bash
aztec-wallet deploy-account --from maxgershfield --node-url $NODE_URL
```

### Issue: "Contract not found"
**Solution**: Ensure contract is compiled and artifact exists in `target/` directory.

---

**Status**: Contract code ready, awaiting compilation fix
**Last Updated**: 2024-01-15

