# 🔐 Enabling Aztec Transactions - Complete Guide

## Problem

The Aztec testnet node (`https://aztec-testnet-fullnode.zkv.xyz`) returns:
```
Error: Invalid tx: Transactions are not permitted
```

This means the testnet node is **read-only** and doesn't accept transaction submissions.

---

## Solutions

### Solution 1: Use Aztec CLI for Transactions (✅ Implemented)

We've created `AztecCLIService` that uses the Aztec CLI (`aztec-wallet`) to submit real transactions.

**How it works:**
1. The service executes `aztec-wallet send` commands
2. Aztec CLI handles proof generation and transaction submission
3. Returns real transaction hashes

**Requirements:**
- Aztec CLI installed (✅ Already done)
- Account created (✅ Already done - `maxgershfield`)
- Bridge contract deployed (⚠️ **Need to deploy**)

**Usage:**
```csharp
var cliService = new AztecCLIService();
var txResult = await cliService.SendTransactionAsync(
    accountAlias: "maxgershfield",
    contractAddress: bridgeContractAddress,
    functionName: "deposit",
    functionArgs: new object[] { receiverAddress, amount }
);
```

---

### Solution 2: Deploy Bridge Contract First

Before transactions work, we need to deploy the bridge contract to Aztec testnet.

**Steps:**

1. **Write Bridge Contract** (Noir):
   ```noir
   // bridge_contract.nr
   contract BridgeContract {
       fn deposit(owner: AztecAddress, amount: Field) -> Field {
           // Create private note
           // Return note ID
       }
       
       fn withdraw(note_id: Field, destination: AztecAddress) -> Field {
           // Withdraw from private note
           // Return transaction hash
       }
   }
   ```

2. **Compile Contract**:
   ```bash
   aztec-nargo compile
   ```

3. **Deploy Contract**:
   ```bash
   aztec-wallet deploy \
       --node-url $NODE_URL \
       --from accounts:maxgershfield \
       --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
       --alias bridge \
       BridgeContract \
       --args <constructor_args>
   ```

4. **Update OASIS Configuration**:
   ```json
   "AztecBridge": {
     "BridgeContractAddress": "0x...",
     "NodeUrl": "https://aztec-testnet-fullnode.zkv.xyz"
   }
   ```

---

### Solution 3: Use Local Aztec Sandbox (For Development)

For local development and testing, use Aztec Sandbox instead of testnet.

**Setup:**

1. **Start Local Aztec Node** (if available):
   ```bash
   # Aztec sandbox runs via Docker
   # Check Aztec docs for sandbox setup
   ```

2. **Update Configuration**:
   ```json
   "AztecBridge": {
     "NodeUrl": "http://localhost:8080",
     "PxeUrl": "http://localhost:8080"
   }
   ```

**Note**: Aztec Sandbox may require additional setup. Check Aztec documentation for latest sandbox instructions.

---

### Solution 4: Integrate Aztec.js SDK (For Production)

For production, integrate Aztec.js SDK via Node.js service bridge.

**Architecture:**
```
OASIS .NET API → Node.js Bridge Service → Aztec.js SDK → Aztec Network
```

**Implementation:**

1. **Create Node.js Service**:
   ```javascript
   // aztec-bridge-service.js
   const { createAztecClient } = require('@aztec/aztec.js');
   
   async function sendTransaction(contractAddress, functionName, args) {
       const client = await createAztecClient(nodeUrl);
       const contract = await client.getContract(contractAddress);
       const tx = await contract.methods[functionName](...args).send();
       return await tx.wait();
   }
   ```

2. **Call from .NET**:
   ```csharp
   // Use Process.Start or HTTP API to call Node.js service
   ```

---

## Current Implementation Status

### ✅ What's Working

1. **Account Created**: `maxgershfield` account exists
2. **AztecCLIService**: Created to submit transactions via CLI
3. **AztecTestnetClient**: Can query account info and transaction status
4. **Bridge Service**: Updated to use CLI service

### ⚠️ What's Needed

1. **Bridge Contract Deployment**: 
   - Need to write/compile/deploy bridge contract
   - Contract address needed in configuration

2. **Transaction Testing**:
   - Once contract is deployed, test deposit/withdraw
   - Verify transaction hashes are returned

3. **Account Deployment** (if needed):
   - Account might need to be deployed before sending transactions
   - Try: `aztec-wallet deploy-account --from maxgershfield`

---

## Next Steps

### Immediate (To Get Transactions Working)

1. **Try Account Deployment Again**:
   ```bash
   export NODE_URL=https://aztec-testnet-fullnode.zkv.xyz
   aztec-wallet deploy-account \
       --node-url $NODE_URL \
       --from maxgershfield \
       --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
       --register-class
   ```

2. **If Deployment Still Fails**:
   - The testnet might be temporarily read-only
   - Try using a different testnet endpoint
   - Or wait for testnet to allow transactions

3. **Deploy Bridge Contract**:
   - Write bridge contract in Noir
   - Compile and deploy to testnet
   - Update configuration with contract address

### Alternative: Use Aztec.js SDK

If CLI approach doesn't work, create a Node.js bridge service:

1. Create Node.js service that uses Aztec.js
2. Expose HTTP API for transaction submission
3. Call from .NET via HTTP client

---

## Testing Transactions

Once bridge contract is deployed:

```csharp
// Test deposit
var depositResult = await aztecBridge.DepositAsync(
    amount: 0.5m,
    receiverAccountAddress: "0x09d16dbfac70e06fc61cbd984190ac9d385131f1011faeb436da4e17eaa2a686"
);

// Test withdrawal
var withdrawResult = await aztecBridge.WithdrawAsync(
    amount: 0.5m,
    senderAccountAddress: "0x09d16dbfac70e06fc61cbd984190ac9d385131f1011faeb436da4e17eaa2a686",
    senderPrivateKey: "0x13b1096a8b708a4788ce8ff9189b85e17b0846dc9df676361a24eb618b1c50de"
);
```

---

## Troubleshooting

### Issue: "Transactions are not permitted"
**Solution**: 
- Testnet node is read-only
- Try account deployment first
- Or use local sandbox for development

### Issue: "Account not found"
**Solution**:
- Account needs to be deployed: `aztec-wallet deploy-account --from maxgershfield`

### Issue: "Contract not found"
**Solution**:
- Bridge contract needs to be deployed first
- Update configuration with contract address

### Issue: "CLI not found"
**Solution**:
- Ensure Aztec CLI is in PATH: `export PATH="$HOME/.aztec/bin:$PATH"`
- Or specify full path in `AztecCLIService` constructor

---

## Summary

**Current Status**: 
- ✅ Account created: `maxgershfield`
- ✅ CLI service implemented
- ✅ Bridge service updated
- ⚠️ Bridge contract needs deployment
- ⚠️ Account deployment pending (testnet restrictions)

**To Enable Transactions**:
1. Deploy bridge contract to Aztec testnet
2. Update configuration with contract address
3. Test deposit/withdraw operations
4. If testnet still doesn't allow transactions, use local sandbox

---

**Last Updated**: 2024-01-15
**Status**: CLI integration complete, awaiting contract deployment

