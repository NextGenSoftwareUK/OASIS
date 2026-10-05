# Aztec Bridge Service - Node.js

Node.js service for submitting real Aztec transactions using Aztec.js SDK.

## Setup

```bash
cd aztec-bridge-service
npm install
```

## Configuration

Set environment variables:

```bash
export AZTEC_NODE_URL=https://aztec-testnet-fullnode.zkv.xyz
export AZTEC_PXE_URL=https://aztec-testnet-fullnode.zkv.xyz
export PORT=3002
```

## Start Service

```bash
npm start
```

## API Endpoints

### POST /api/send-transaction
Submit a transaction to Aztec network.

**Request:**
```json
{
  "accountAlias": "maxgershfield",
  "contractAddress": "0x...",
  "functionName": "deposit",
  "args": ["0x...", "0.5"]
}
```

**Response:**
```json
{
  "success": true,
  "txHash": "0x...",
  "blockNumber": 12345,
  "status": "mined"
}
```

### POST /api/deploy-contract
Deploy a contract to Aztec network.

**Request:**
```json
{
  "accountAlias": "maxgershfield",
  "contractName": "BridgeContract",
  "constructorArgs": []
}
```

### GET /api/account/:alias
Get account information.

### GET /api/health
Health check endpoint.

## Integration with .NET

Update `AztecCLIService` to call this Node.js service via HTTP:

```csharp
var response = await _httpClient.PostAsync(
    "http://localhost:3002/api/send-transaction",
    new StringContent(json, Encoding.UTF8, "application/json")
);
```

