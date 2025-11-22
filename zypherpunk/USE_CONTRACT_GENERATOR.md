# 🎯 Using Contract Generator API for Aztec Bridge Contract

## Overview

Your contract generator API at `/Volumes/Storage/OASIS_CLEAN/contract-generator` handles the **full lifecycle**:
1. **Generate** - Creates the contract code (using AI/OpenAI)
2. **Compile** - Compiles the contract for the target platform
3. **Deploy** - Deploys the contract to the network

## Why We Need This Contract

The Aztec bridge contract is **essential** for the Zypherpunk hackathon bridge:

- **Handles Deposits**: When ZEC is locked on Zcash, users deposit on Aztec via the contract
- **Manages Private State**: Stores deposits/withdrawals privately on Aztec network
- **Enables Withdrawals**: Users can withdraw back to Zcash through the contract
- **Replaces Placeholder**: `AztecBridgeService` currently uses `0x0000000000000000000000000000000000000000` and needs the real contract address

**Without this contract, the bridge cannot process deposits or withdrawals on Aztec.**

## Quick Start

### 1. Start the Contract Generator API

```bash
cd /Volumes/Storage/OASIS_CLEAN/contract-generator/api/src/SmartContractGen/ScGen.API

# Set OpenAI API key (if not in appsettings.json)
export OpenAI__ApiKey="sk-..."

# Run the API
DOTNET_ENVIRONMENT=Development dotnet run
```

The API will run on `http://localhost:5000` by default.

### 2. Generate, Compile & Deploy Aztec Bridge Contract

Use the provided script (handles all 3 steps):

```bash
/Volumes/Storage/OASIS_CLEAN/generate-aztec-bridge-contract.sh
```

The script will:
1. ✅ Generate the contract code
2. ✅ Compile it for Aztec
3. ✅ Deploy to Aztec testnet
4. ✅ Return the contract address

### 3. Update OASIS Configuration

After deployment, update `appsettings.json`:

```json
"AztecBridge": {
  "BridgeContractAddress": "0x...", // From deployment response
  "NodeUrl": "https://aztec-testnet-fullnode.zkv.xyz"
}
```

And update `AztecBridgeService.cs` line 166:
```csharp
var bridgeContractAddress = configuration["AztecBridge:BridgeContractAddress"] 
    ?? throw new InvalidOperationException("Bridge contract address not configured");
```

## API Endpoints (Expected Structure)

Based on the full lifecycle, the API likely has:

- `POST /api/contracts/generate` - Generate contract code
- `POST /api/contracts/{id}/compile` - Compile contract
- `POST /api/contracts/{id}/deploy` - Deploy contract
- `GET /api/contracts/{id}` - Get contract details
- `GET /api/contracts/{id}/status` - Get deployment status

## Manual API Calls (if needed)

### Generate Only
```bash
curl -X POST http://localhost:5000/api/contracts/generate \
    -H "Content-Type: application/json" \
    -d '{
        "platform": "aztec",
        "contractType": "bridge",
        "contractName": "BridgeContract",
        "description": "Bridge contract for Zcash <-> Aztec"
    }'
```

### Compile
```bash
curl -X POST http://localhost:5000/api/contracts/{contractId}/compile
```

### Deploy
```bash
curl -X POST http://localhost:5000/api/contracts/{contractId}/deploy \
    -H "Content-Type: application/json" \
    -d '{
        "network": "testnet",
        "accountAlias": "maxgershfield",
        "nodeUrl": "https://aztec-testnet-fullnode.zkv.xyz"
    }'
```

## Troubleshooting

- **API not running**: Start it with `dotnet run` in the ScGen.API directory
- **OpenAI key missing**: Set `OpenAI__ApiKey` environment variable
- **Wrong endpoint**: Check the API's Swagger docs at `http://localhost:5000/swagger`
- **Deployment fails**: Ensure Aztec account `maxgershfield` exists and is deployed

---

**Note**: The exact endpoint structure may vary. Check the API's Swagger documentation for the correct endpoints.
