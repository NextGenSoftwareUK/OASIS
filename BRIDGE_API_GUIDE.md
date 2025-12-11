# OASIS Bridge API Guide

## 🌉 How the Bridge Works

### Architecture

The bridge uses a **bridge map** to route swaps between different blockchains:

```
Bridge Map:
- SOL → SolanaBridgeService
- ETH → EthereumBridgeService  
- ZEC → ZcashBridgeService
- AZTEC → AztecBridgeService
- STARKNET → StarknetBridge
- XRD → SolanaBridgeService (placeholder)
```

### Swap Flow

1. **User creates swap order** via `POST /api/v1/orders`
2. **Bridge validates** request (tokens, amounts, addresses)
3. **Gets exchange rate** from CoinGecko API
4. **Checks bridge type**:
   - Private bridge pairs (Zcash ↔ Aztec/Miden/Starknet)
   - Standard bridge pairs (SOL ↔ ETH, SOL ↔ XRD)
5. **Resolves bridge services** from bridge map
6. **Executes atomic swap** with automatic rollback on failure
7. **Returns order ID** and transaction details

### Supported Swap Pairs

**Standard Swaps:**
- ✅ SOL ↔ XRD
- ✅ SOL ↔ ETH (NEW)
- ✅ ETH ↔ SOL (NEW)

**Private Bridge Swaps:**
- ✅ Zcash ↔ Aztec
- ✅ Zcash ↔ Miden
- ✅ Zcash ↔ Starknet

## 🧪 Testing

### Quick Test

```bash
# 1. Authenticate
./auth-oasis.sh

# 2. Test bridge API
./test-bridge-swap.sh
```

### Test Script Features

The `test-bridge-swap.sh` script tests:
1. ✅ Get supported networks
2. ✅ Get exchange rate (SOL → ETH)
3. ✅ Create swap order (SOL → ETH)
4. ✅ Check order balance

### Manual Testing

```bash
# Get exchange rate
curl -k "https://localhost:5004/api/v1/exchange-rate?fromToken=SOL&toToken=ETH"

# Create swap order
curl -k -X POST "https://localhost:5004/api/v1/orders" \
  -H "Authorization: Bearer $OASIS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "your-avatar-id",
    "fromToken": "SOL",
    "toToken": "ETH",
    "amount": 0.1,
    "fromNetwork": "Solana",
    "toNetwork": "Ethereum",
    "destinationAddress": "0x..."
  }'
```

## ⚠️ Current Limitations

1. **Testnet Tokens Required**: 
   - Zcash/Aztec/Starknet swaps need testnet tokens
   - SOL ↔ ETH works with Solana devnet tokens

2. **Ethereum Bridge**: 
   - Uses placeholder account (needs proper config in production)
   - May fail on execution without proper setup

3. **Order Execution**:
   - Orders are created successfully
   - Execution depends on bridge service availability and funds

## 🔧 Configuration

Bridge services are configured in `BridgeService.cs`:
- Solana: `SolanaBridgeOptions:RpcUrl`
- Ethereum: `EthereumBridge:RpcUrl`, `EthereumBridge:Network`
- Zcash: `ZcashBridge:RpcUrl`, `ZcashBridge:RpcUser`, `ZcashBridge:RpcPassword`
- Aztec: `AztecBridge:NodeUrl`, `AztecBridge:PxeUrl`
- Starknet: `StarknetBridge:RpcUrl`, `StarknetBridge:Network`

## 📝 API Endpoints

- `POST /api/v1/orders` - Create swap order
- `GET /api/v1/orders/{orderId}/check-balance` - Check order status
- `GET /api/v1/exchange-rate?fromToken=X&toToken=Y` - Get exchange rate
- `GET /api/v1/networks` - Get supported networks

