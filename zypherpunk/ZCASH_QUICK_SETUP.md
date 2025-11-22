# Zcash Quick Setup for Hackathon Demo

## Current Situation

- Zcash is not installed locally
- Public testnet RPC endpoints are not readily available
- We need Zcash working for the bridge demo

## Recommended Solution: Use Public Testnet Node

For the hackathon demo, we'll configure the bridge to use a public testnet node endpoint.

### Step 1: Find/Use Public Testnet Endpoint

Try these known testnet endpoints:
- `http://testnet.z.cash:18232` (may require authentication)
- Community-run testnet nodes
- Third-party RPC services

### Step 2: Update Configuration

Update `appsettings.json`:

```json
"ZcashBridge": {
  "RpcUrl": "http://testnet.z.cash:18232",
  "RpcUser": "test",
  "RpcPassword": "test",
  "Network": "testnet",
  "UsePublicEndpoint": true
}
```

### Step 3: Test Connection

```bash
# Test RPC connection
curl -X POST http://testnet.z.cash:18232 \
  -u test:test \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"getinfo","params":[],"id":1}'
```

## Alternative: Minimal Local Testnet Node

If public endpoints don't work, set up a minimal local testnet node:

```bash
# Quick testnet setup (if Zcash is installed)
zcashd -testnet -server -rpcuser=oasis -rpcpassword=Uppermall1! -rpcallowip=127.0.0.1
```

## For Demo Purposes

If we can't get a real Zcash node working, we can:
1. Show the bridge architecture and code
2. Demonstrate Aztec side (which is working)
3. Explain Zcash integration points
4. Show how viewing keys work conceptually

---

**Next Step**: Test public endpoint connectivity or set up minimal local node.
