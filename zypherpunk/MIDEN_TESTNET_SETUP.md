# Miden Testnet Setup Guide

## Quick Start

### Step 1: Install Miden Wallet

1. **Visit**: https://miden.xyz/
2. **Download**: Miden Wallet browser extension
3. **Install**: Follow browser extension installation steps

### Step 2: Create Wallet

1. **Open Miden Wallet extension**
2. **Create New Wallet**
   - Click "Create Wallet"
   - Save your **recovery phrase** securely (12-24 words)
   - Set a password
3. **Note Your Address**
   - Copy your Miden wallet address (starts with `miden1...` or similar)
   - This is your public address for receiving tokens

### Step 3: Get Testnet Tokens (Faucet)

1. **Visit Miden Faucet**: https://faucet.testnet.miden.io/
2. **Enter Your Address**
   - Paste your Miden wallet address
3. **Choose Note Type**:
   - **Public Note**: Visible on-chain (for testing)
   - **Private Note**: Private (for production-like testing)
4. **Request Tokens**
   - Click "Send Public Note" or "Send Private Note"
   - Wait for transaction to process (usually 1-2 minutes)
5. **Claim Tokens**
   - Open Miden Wallet
   - Go to "Receive" section
   - Click "Claim" to add tokens to your wallet

### Step 4: Verify Funding

1. **Check Balance** in Miden Wallet
2. **Verify Transaction** on testnet explorer (if available)

## Alternative: Programmatic Setup

If you need to set up wallets programmatically for testing:

### Using Miden Client Library

```bash
# Install Miden client
npm install @0xpolygon/miden-client
# or
pip install miden-client
```

### Create Wallet Programmatically

```typescript
import { Wallet } from '@0xpolygon/miden-client';

// Create new wallet
const wallet = Wallet.create();
const address = wallet.getAddress();
const privateKey = wallet.getPrivateKey();

console.log('Miden Address:', address);
console.log('Private Key:', privateKey); // Store securely!
```

## Configuration for OASIS

### Environment Variables

```bash
# Miden Testnet Configuration
export MIDEN_API_URL="https://testnet.miden.xyz"
export MIDEN_API_KEY=""  # Usually not required for testnet
export MIDEN_WALLET_ADDRESS="miden1your_address_here"
export MIDEN_PRIVATE_KEY=""  # Only if using programmatic access
export MIDEN_BRIDGE_POOL_ADDRESS="miden_bridge_pool"
```

### Update OASIS DNA

```json
{
  "Providers": {
    "MidenOASIS": {
      "IsEnabled": true,
      "ApiUrl": "https://testnet.miden.xyz",
      "ApiKey": "",
      "Network": "testnet",
      "WalletAddress": "${MIDEN_WALLET_ADDRESS}",
      "PrivateKey": "${MIDEN_PRIVATE_KEY}"
    }
  }
}
```

## Testing Your Setup

### Test 1: Check API Connection

```bash
curl https://testnet.miden.xyz/health
# Should return status 200
```

### Test 2: Query Balance

```bash
curl -X POST https://testnet.miden.xyz/api/balance \
  -H "Content-Type: application/json" \
  -d '{"address": "miden1your_address"}'
```

### Test 3: Create Private Note (via OASIS)

```csharp
var midenProvider = new MidenOASIS();
await midenProvider.ActivateProviderAsync();

var noteResult = await midenProvider.CreatePrivateNoteAsync(
    value: 1.0m,
    ownerPublicKey: "miden1your_address",
    assetId: "ZEC"
);

if (!noteResult.IsError)
{
    Console.WriteLine($"Private note created: {noteResult.Result.NoteId}");
}
```

## Troubleshooting

### Issue: Faucet Not Working

**Solutions**:
1. Check if faucet has rate limits (wait 24 hours)
2. Try different note type (public vs private)
3. Verify wallet address is correct
4. Check Miden testnet status

### Issue: Can't Connect to Testnet

**Solutions**:
1. Verify `MIDEN_API_URL` is correct
2. Check network connectivity
3. Try alternative testnet endpoint if available
4. Check Miden testnet status page

### Issue: Wallet Not Showing Balance

**Solutions**:
1. Refresh wallet extension
2. Check if transaction is still pending
3. Verify you're on testnet (not mainnet)
4. Check transaction hash on explorer

## Resources

- **Miden Website**: https://miden.xyz/
- **Miden Faucet**: https://faucet.testnet.miden.io/
- **Miden Documentation**: https://docs.miden.xyz/
- **Miden GitHub**: https://github.com/0xPolygonMiden
- **Miden Client**: https://github.com/0xMiden/miden-client

## Next Steps

1. ✅ Install Miden Wallet
2. ✅ Create wallet and save recovery phrase
3. ✅ Get testnet tokens from faucet
4. ✅ Configure OASIS with wallet address
5. ✅ Test private note creation
6. ✅ Test bridge operations

## Security Notes

⚠️ **Important**:
- Never share your private key or recovery phrase
- Testnet tokens have no value - use freely for testing
- Mainnet wallets require different setup (not covered here)
- Always verify you're on testnet before transactions

