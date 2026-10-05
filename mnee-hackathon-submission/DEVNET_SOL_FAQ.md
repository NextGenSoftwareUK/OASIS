# Devnet SOL Request - FAQ

## Automatic SOL Request

When you generate a Solana wallet for an agent, the system automatically requests 2 SOL from the devnet faucet.

### How It Works

1. **Wallet Generation** → Creates Solana keypair and links to avatar
2. **Automatic Faucet Request** → Requests 2 SOL from Solana devnet
3. **Balance Check** → Verifies SOL arrived (after 3 second wait)

### Example Output

```
💰 Requesting devnet SOL from faucet...
✅ Devnet SOL requested: 2.0 SOL
   Transaction: 2FbYVsfDGBp3mZNQNUnU3xp6eWPjhGWr1MeS2rusXRapkMGFEK4qZHts9Na3Z2bjrz3TS7DcUzeQnBXuuoj2tJ4k
   Method: json-rpc
   Balance will update in a few seconds...
```

## Troubleshooting

### If Automated Request Fails

The system tries multiple methods:
1. **Primary**: Solana JSON-RPC `requestAirdrop`
2. **Fallback**: Alternative faucet endpoints
3. **Manual Options**: Provides links and CLI commands

### Common Issues

#### Rate Limiting
- **Error**: "Rate limited" or 429 error
- **Solution**: Wait a few minutes and try again, or use manual methods

#### Internal Error
- **Error**: "Internal error" from RPC
- **Solution**: System automatically tries alternative methods

#### Network Timeout
- **Error**: Request timed out
- **Solution**: Check internet connection, try manual methods

## Manual Methods

If automated request fails, you can manually request SOL:

### 1. Web Faucet (Easiest)
```
https://faucet.solana.com/?address=YOUR_WALLET_ADDRESS
```

### 2. Solana CLI
```bash
solana airdrop 2 YOUR_WALLET_ADDRESS --url devnet
```

### 3. Python Script
```python
from solana.rpc.api import Client

client = Client('https://api.devnet.solana.com')
result = client.request_airdrop('YOUR_WALLET_ADDRESS', 2_000_000_000)
print(f"Transaction: {result.value}")
```

### 4. JSON-RPC (curl)
```bash
curl -X POST https://api.devnet.solana.com \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "requestAirdrop",
    "params": ["YOUR_WALLET_ADDRESS", 2000000000]
  }'
```

## Verify Balance

### Using Python
```python
import requests

rpc_url = "https://api.devnet.solana.com"
payload = {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "getBalance",
    "params": ["YOUR_WALLET_ADDRESS"]
}
response = requests.post(rpc_url, json=payload)
data = response.json()
lamports = data["result"]["value"]
sol_balance = lamports / 1_000_000_000
print(f"Balance: {sol_balance} SOL")
```

### Using Solana CLI
```bash
solana balance YOUR_WALLET_ADDRESS --url devnet
```

### Using Explorer
```
https://explorer.solana.com/address/YOUR_WALLET_ADDRESS?cluster=devnet
```

## Rate Limits

- **Solana Devnet Faucet**: Usually 1-2 requests per 24 hours per IP
- **If rate limited**: Wait a few hours or use alternative methods
- **Alternative faucets**: May have different limits

## Notes

- **Network**: Always use **devnet**, not mainnet
- **Amount**: Typically 1-2 SOL per request
- **Confirmation**: Transactions usually confirm within 1-2 seconds
- **Balance Update**: May take 3-5 seconds to appear

## Integration

The automatic SOL request is integrated into:
- `OASISClient.generate_wallet()` - Automatically requests SOL after wallet creation
- `test_agent_registration.py` - Tests the full flow including SOL request
- Agent registration flow - Agents get funded automatically

---

**For production/mainnet**: Never use automated faucet requests. Use proper funding methods.

