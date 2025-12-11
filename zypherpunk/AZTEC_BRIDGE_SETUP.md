# 🔐 Aztec Bridge Configuration Guide

## Overview

This guide covers setting up the Aztec bridge for the OASIS Universal Asset Bridge. The Aztec bridge enables private cross-chain transfers between Zcash and Aztec networks.

**Note**: Aztec Connect (the DeFi privacy bridge) was sunset in March 2023, but Aztec Network continues development. For the hackathon, we'll configure for:
1. **Local Development** - Using Aztec SDK starter for testing
2. **Future Integration** - Ready for Aztec Network's next-generation products

---

## Configuration Options

### Option 1: Local Development (Recommended for Hackathon)

Use the Aztec SDK starter for local development and testing.

#### Step 1: Clone Aztec SDK Starter
```bash
cd /Volumes/Storage/OASIS_CLEAN
git clone https://github.com/AztecProtocol/aztec-sdk-starter.git
cd aztec-sdk-starter
```

#### Step 2: Install Dependencies
```bash
yarn install
# or
npm install
```

#### Step 3: Configure Environment
```bash
cp .env.example .env
```

Edit `.env` and add:
```env
ETHEREUM_PRIVATE_KEY=your_ethereum_private_key_here
# or
MNEMONIC=your twelve word mnemonic phrase here
```

#### Step 4: Run Local Aztec Node
```bash
# Start local sequencer (if available)
yarn start
# or
npm start
```

#### Step 5: Update OASIS Configuration
In `appsettings.json`, set:
```json
"AztecBridge": {
  "ApiUrl": "http://localhost:8080",
  "ApiKey": null,
  "Network": "sandbox",
  "UseLocalDevelopment": true
}
```

---

### Option 2: Aztec Mainnet Fork (For Testing)

Connect to Aztec's mainnet fork for testing without running a local node.

#### Configuration
```json
"AztecBridge": {
  "ApiUrl": "https://api.aztec.network/aztec-connect-testnet/falafel",
  "ApiKey": null,
  "Network": "testnet",
  "UseLocalDevelopment": false,
  "SandboxUrl": "https://api.aztec.network/sandbox"
}
```

#### MetaMask Configuration
For testing with MetaMask, add Aztec Mainnet Fork:
- **Network Name**: `Aztec Mainnet Fork`
- **RPC URL**: `https://mainnet-fork.aztec.network`
- **Chain ID**: `677868`
- **Currency Symbol**: `ETH`
- **Rollup Provider URL**: `https://api.aztec.network/aztec-connect-testnet/falafel`
- **Block Explorer**: `https://aztec-connect-testnet-explorer.aztec.network`

---

### Option 3: Mock/Placeholder Mode (For Demo)

For hackathon demos where full Aztec integration isn't required, the bridge can run in mock mode.

#### Configuration
```json
"AztecBridge": {
  "ApiUrl": "http://localhost:8080",
  "ApiKey": null,
  "Network": "sandbox",
  "UseLocalDevelopment": true,
  "MockMode": true
}
```

**Note**: The current implementation already includes placeholder responses for development. Mock mode will:
- Return success responses for bridge operations
- Generate placeholder transaction IDs
- Store bridge events as Holons in OASIS
- Allow testing of the bridge workflow without a live Aztec node

---

## Current Implementation Status

### ✅ Implemented Features

1. **AztecAPIClient**
   - HTTP client for Aztec API calls
   - Supports GET and POST requests
   - Optional API key authentication
   - Error handling and response parsing

2. **AztecBridgeService**
   - Private note creation
   - STARK proof generation (placeholder)
   - Bridge deposit/withdraw operations
   - Event synchronization
   - Holon storage for bridge events

3. **Bridge Operations**
   - `DepositFromZcashAsync` - Deposit ZEC to Aztec
   - `WithdrawToZcashAsync` - Withdraw from Aztec to Zcash
   - `SyncBridgeEventAsync` - Sync bridge events
   - `CreatePrivateNoteAsync` - Create private notes
   - `GenerateProofAsync` - Generate STARK proofs

### ⚠️ Pending Integration

1. **Aztec SDK Integration**
   - Full Aztec SDK integration required for production
   - Currently using placeholder/mock responses
   - Need to integrate with Aztec's proof system

2. **Smart Contract Deployment**
   - Bridge contracts need to be deployed
   - Private state management contracts
   - Integration with Aztec's rollup

3. **Proof Verification**
   - Real STARK proof generation
   - Proof verification against Aztec network
   - Integration with Aztec's proving system

---

## API Endpoints (Expected)

The Aztec bridge expects the following API endpoints:

### Bridge Operations
- `POST /bridge/deposit` - Deposit funds to Aztec
- `POST /bridge/withdraw` - Withdraw funds from Aztec
- `GET /bridge/events/{eventId}` - Get bridge event status

### Private Notes
- `POST /notes` - Create a private note
- `GET /notes/{noteId}` - Get note details

### Proofs
- `POST /proofs` - Generate a STARK proof
- `GET /proofs/{proofId}` - Get proof details

---

## Testing the Aztec Bridge

### 1. Test Bridge Connection
```bash
curl http://localhost:8080/health
```

### 2. Test Private Note Creation
```bash
curl -X POST http://localhost:8080/notes \
  -H "Content-Type: application/json" \
  -d '{
    "value": 0.5,
    "owner": "aztec-test-address"
  }'
```

### 3. Test Bridge Deposit
```bash
curl -X POST http://localhost:8080/bridge/deposit \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 0.5,
    "zcashTxId": "test-zcash-tx-123",
    "noteId": "note-id-here",
    "owner": "aztec-test-address"
  }'
```

### 4. Test via OASIS Bridge API
```bash
curl -X POST http://localhost:5000/api/v1/orders/private \
  -H "Content-Type: application/json" \
  -d '{
    "fromToken": "ZEC",
    "toToken": "AZTEC",
    "amount": 0.5,
    "fromAddress": "zt1test123",
    "toAddress": "aztec-test-address",
    "fromChain": "Zcash",
    "toChain": "Aztec"
  }'
```

---

## Integration with OASIS Bridge

The Aztec bridge is integrated into the OASIS Universal Asset Bridge:

1. **BridgeService.cs** initializes `AztecBridgeService`
2. **CrossChainBridgeManager** handles ZEC ↔ AZTEC swaps
3. **Private bridge operations** include:
   - Viewing key audit trail
   - STARK proof verification
   - Private note management

### Configuration in BridgeService
```csharp
var aztecApiUrl = configuration["AztecBridge:ApiUrl"] ?? "http://localhost:8080";
var aztecApiKey = configuration["AztecBridge:ApiKey"];
var aztecBridge = new AztecBridgeService(new AztecAPIClient(aztecApiUrl, aztecApiKey));
```

---

## Next Steps for Full Integration

1. **Set Up Aztec SDK**
   ```bash
   git clone https://github.com/AztecProtocol/aztec-sdk-starter.git
   cd aztec-sdk-starter
   yarn install
   ```

2. **Deploy Bridge Contracts**
   - Use Aztec's contract deployment tools
   - Deploy to Aztec sandbox/testnet
   - Configure contract addresses in OASIS

3. **Integrate Proof System**
   - Connect to Aztec's proving system
   - Generate real STARK proofs
   - Verify proofs on Aztec network

4. **Test End-to-End**
   - Test Zcash → Aztec deposits
   - Test Aztec → Zcash withdrawals
   - Verify privacy features
   - Test viewing key audit trail

---

## Troubleshooting

### Issue: Aztec API Not Responding
**Solution**: 
- Check if local Aztec node is running
- Verify `ApiUrl` in `appsettings.json`
- Check firewall/network settings

### Issue: Proof Generation Failing
**Solution**:
- Current implementation uses placeholders
- For real proofs, integrate Aztec SDK
- Check Aztec SDK documentation

### Issue: Bridge Events Not Syncing
**Solution**:
- Verify HolonManager is initialized
- Check OASIS provider configuration
- Review bridge event storage logs

---

## Resources

- **Aztec SDK Starter**: https://github.com/AztecProtocol/aztec-sdk-starter
- **Aztec Connect Bridges**: https://github.com/AztecProtocol/aztec-connect-bridges
- **Aztec Documentation**: https://docs.aztec.network
- **Aztec Network**: https://aztec.network

---

## For Hackathon Demo

For the Zypherpunk hackathon, you can:

1. **Use Mock Mode**: The bridge already supports mock responses for demos
2. **Show Workflow**: Demonstrate the complete bridge flow even without live Aztec node
3. **Highlight Features**: 
   - Private note creation
   - Viewing key audit trail
   - STARK proof verification (conceptual)
   - Holon-based event storage

The bridge architecture is ready - it just needs the Aztec SDK integration for production use.

---

**Last Updated**: 2024-01-15
**Status**: Configuration ready, awaiting Aztec SDK integration

