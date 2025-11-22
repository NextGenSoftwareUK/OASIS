# Zcash Setup Guide for OASIS Bridge

## Current Status

Zcash is **not installed locally**. We need to configure the OASIS bridge to connect to a Zcash testnet node.

## Options

### Option 1: Public Testnet RPC Endpoint (Recommended for Development)

Use a public Zcash testnet RPC endpoint - no local installation needed.

**Pros:**
- ✅ No installation required
- ✅ Quick setup
- ✅ No local node maintenance
- ✅ Perfect for development/testing

**Cons:**
- ⚠️ Dependency on external service
- ⚠️ Potential rate limits
- ⚠️ Less control

**Configuration:**
Update `appsettings.json`:
```json
"ZcashBridge": {
  "RpcUrl": "http://testnet.z.cash:18232",
  "RpcUser": "test",
  "RpcPassword": "test",
  "Network": "testnet"
}
```

### Option 2: Local Zcash Node (For Production)

Install and run a local Zcash testnet node.

**Installation Steps:**
```bash
# Clone Zcash repository
git clone https://github.com/zcash/zcash.git
cd zcash
git checkout v6.10.0

# Fetch parameters
./zcutil/fetch-params.sh

# Build (requires dependencies)
./zcutil/build.sh -j$(nproc)

# Configure for testnet
mkdir -p ~/.zcash
cat > ~/.zcash/zcash.conf << EOF
testnet=1
server=1
rpcuser=oasis
rpcpassword=Uppermall1!
rpcallowip=127.0.0.1
rpcport=18232
addnode=testnet.z.cash
txindex=1
EOF

# Start testnet node
./src/zcashd -testnet
```

**Configuration:**
```json
"ZcashBridge": {
  "RpcUrl": "http://localhost:18232",
  "RpcUser": "oasis",
  "RpcPassword": "Uppermall1!",
  "Network": "testnet"
}
```

### Option 3: Docker (Previously Attempted)

We tried Docker earlier but had configuration issues. Could retry if needed.

## Testing the Connection

Once configured, test the connection:

```bash
# Using curl
curl -X POST http://localhost:18232 \
  -u oasis:Uppermall1! \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"getinfo","params":[],"id":1}'

# Or test via OASIS API
curl http://localhost:5003/api/v1/bridge/networks
```

## Recommended Approach for Hackathon

**Use Option 1 (Public Testnet RPC)** for the demo:
- Fastest to set up
- No local node maintenance
- Sufficient for demonstrating bridge functionality
- Can switch to local node later if needed

## Next Steps

1. **Find/Verify Public Testnet Endpoint**: Test connectivity to `testnet.z.cash:18232`
2. **Update Configuration**: Modify `appsettings.json` with public endpoint
3. **Test Connection**: Verify ZcashRPCClient can connect
4. **Test Bridge Operations**: Try creating a bridge order

---

**Status**: ⏸️ **Awaiting public testnet endpoint verification**

