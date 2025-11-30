# Aztec Account Deployment - Testnet Read-Only Issue

## Problem

The Aztec testnet node (`https://aztec-testnet-fullnode.zkv.xyz`) is **read-only** and returns:
```
Error: Invalid tx: Transactions are not permitted
```

This blocks:
- ❌ Account deployment
- ❌ Contract deployment

## What We've Accomplished

✅ **Contract Compiled**: `target/bridge_contract-BridgeContract.json`
✅ **Verification Keys Generated**: Using `aztec-postprocess-contract`
   - Keys in: `target/cache/*.vk` files
   - Functions: `deposit` and `withdraw` have VKs
✅ **Proof Generation Works**: The proving system generates proofs successfully
❌ **Transaction Submission Fails**: Testnet rejects all transactions

## Solutions

### Option 1: Use Local Aztec Sandbox (Recommended)

Run a local Aztec node for development:

```bash
# Start local Aztec sandbox
aztec-sandbox

# Then deploy with local PXE
aztec-wallet create-account --rpc-url http://localhost:8080 --alias maxgershfield
aztec-wallet deploy-account --rpc-url http://localhost:8080 --from accounts:maxgershfield
aztec-wallet deploy --rpc-url http://localhost:8080 --from accounts:maxgershfield target/bridge_contract-BridgeContract.json
```

### Option 2: Wait for Testnet to Allow Transactions

The testnet might be temporarily disabled. Check:
- Aztec Discord/Telegram for testnet status
- Aztec documentation for testnet availability
- Try again later

### Option 3: Use Different Testnet Endpoint

Check if there's another testnet endpoint that allows transactions:
- Devnet endpoints
- Alternative testnet URLs
- Sandbox endpoints

### Option 4: Use Aztec.js SDK Directly

Deploy using TypeScript/JavaScript SDK instead of CLI:

```typescript
import { createAztecClient, getSchnorrAccount } from '@aztec/aztec.js';

// Connect to local sandbox or different endpoint
const client = await createAztecClient('http://localhost:8080');
const account = await getSchnorrAccount(client, 'maxgershfield', 'http://localhost:8080');

// Deploy account
await account.deploy().wait();

// Deploy contract
const contract = await BridgeContractContractArtifact.deploy(account).send().deployed();
```

## Current Status

- ✅ Contract ready to deploy
- ✅ Verification keys generated
- ⚠️ Blocked by testnet read-only mode
- 🔄 Need local sandbox or alternative endpoint

## Next Steps

1. **Set up local Aztec sandbox** (if available)
2. **Check for alternative testnet endpoints**
3. **Wait for testnet to allow transactions**
4. **Use TypeScript SDK with local node**

---

**Note**: The contract and verification keys are ready. We just need a writable Aztec node to deploy to.

