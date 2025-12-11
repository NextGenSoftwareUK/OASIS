# Zcash Public Testnet RPC Endpoints

## Found Public Endpoints

### 1. Tatum Gateway (Recommended)
- **JSON-RPC**: `https://zcash-testnet.gateway.tatum.io`
- **REST API**: `https://zcash-testnet.gateway.tatum.io/rest`
- **Rate Limit**: 5 requests per minute (free tier)
- **Provider**: FREERPC.com / Tatum
- **Status**: ✅ Available

### 2. Stardust Staking
- **Service**: Public RPC services for Zcash
- **Access**: May require contact/registration
- **Provider**: Stardust Staking
- **Status**: ⚠️ Contact required

## Testing Tatum Endpoint

### JSON-RPC Test
```bash
curl -X POST https://zcash-testnet.gateway.tatum.io \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"getinfo","params":[],"id":1}'
```

### REST API Test
```bash
curl https://zcash-testnet.gateway.tatum.io/rest/chain/info
```

## Configuration for OASIS

### Option 1: Use Tatum JSON-RPC (if compatible)

Update `appsettings.json`:
```json
"ZcashBridge": {
  "RpcUrl": "https://zcash-testnet.gateway.tatum.io",
  "RpcUser": "",
  "RpcPassword": "",
  "Network": "testnet",
  "UsePublicEndpoint": true
}
```

**Note**: Tatum endpoints may not require authentication (RPC user/password), but our `ZcashRPCClient` uses Basic Auth. We may need to:
- Modify `ZcashRPCClient` to handle endpoints without auth
- Or use empty strings for user/password

### Option 2: Use Tatum REST API

If Tatum uses REST instead of JSON-RPC, we'd need to:
- Create a `ZcashRESTClient` adapter
- Or modify `ZcashRPCClient` to support REST endpoints

## Next Steps

1. **Test Tatum JSON-RPC**: Verify if it accepts standard Zcash RPC calls
2. **Test Authentication**: Check if auth is required
3. **Update Configuration**: Modify `appsettings.json` with working endpoint
4. **Modify Client if Needed**: Update `ZcashRPCClient` to handle public endpoints

---

**Status**: 🔍 **Testing Tatum endpoint compatibility**

