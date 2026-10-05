# Miden Wallet Setup - Step by Step

## Overview

This guide walks you through getting Miden testnet access, creating a wallet, and funding it for Zypherpunk Track 4 testing.

## Method 1: Browser Extension Wallet (Recommended)

### Step 1: Install Miden Wallet Extension

1. **Visit Miden Website**
   - **Official Site**: https://miden.fi/
   - **Testnet Page**: https://miden.xyz/testnet
   - Look for "Wallet" or "Get Started" section

2. **Download Extension**
   - Visit: https://miden.fi/
   - Look for "Install Wallet" or "Get Wallet" button
   - Chrome/Edge: Chrome Web Store link
   - Firefox: Firefox Add-ons link
   - Install the extension

3. **Verify Installation**
   - Check browser extensions/toolbar
   - Miden Wallet icon should appear

### Step 2: Create New Wallet

1. **Open Miden Wallet**
   - Click the Miden Wallet icon in your browser
   - Click "Create New Wallet" or "Get Started"

2. **Set Password**
   - Choose a strong password
   - Confirm password
   - ⚠️ **Remember this password** - you'll need it to unlock the wallet

3. **Save Recovery Phrase**
   - Wallet will generate a 12-24 word recovery phrase
   - ⚠️ **CRITICAL**: Write this down and store securely
   - This is the ONLY way to recover your wallet
   - Example format:
     ```
     word1 word2 word3 word4 word5 word6
     word7 word8 word9 word10 word11 word12
     ```

4. **Verify Recovery Phrase**
   - Wallet will ask you to confirm words
   - Select words in correct order
   - This ensures you saved it correctly

5. **Wallet Created!**
   - You should see your wallet dashboard
   - Note your wallet address (starts with `miden1...`)

### Step 3: Get Testnet Tokens

1. **Visit Miden Faucet**
   - URL: https://faucet.testnet.miden.io/
   - This is the official testnet faucet

2. **Enter Your Address**
   - Copy your wallet address from Miden Wallet
   - Paste into the faucet address field

3. **Choose Note Type**
   - **Public Note**: Visible on-chain (good for testing)
   - **Private Note**: Private (more realistic for production)

4. **Request Tokens**
   - Click "Send Public Note" or "Send Private Note"
   - Wait for confirmation (usually shows transaction hash)

5. **Claim in Wallet**
   - Open Miden Wallet
   - Go to "Receive" tab/section
   - Look for pending transactions
   - Click "Claim" to add tokens to your wallet

6. **Verify Balance**
   - Check your wallet balance
   - Should show testnet tokens (usually 100-1000 tokens)

## Method 2: Programmatic Setup (For Developers)

### Using Miden Client Library

#### Node.js/TypeScript

```bash
# Install Miden client
npm install @0xpolygon/miden-client
# or
yarn add @0xpolygon/miden-client
```

```typescript
import { Wallet } from '@0xpolygon/miden-client';

// Create new wallet
const wallet = Wallet.create();
const address = wallet.getAddress();
const privateKey = wallet.getPrivateKey();

console.log('Miden Address:', address);
console.log('Private Key:', privateKey); // ⚠️ Store securely!

// Save to file (encrypted)
const walletData = {
    address,
    privateKey, // In production, encrypt this
    network: 'testnet'
};

// Use wallet to interact with Miden
const balance = await wallet.getBalance();
console.log('Balance:', balance);
```

#### Python

```bash
# Install Miden client (if available)
pip install miden-client
# or check: https://github.com/0xMiden/miden-client
```

```python
from miden_client import Wallet

# Create new wallet
wallet = Wallet.create()
address = wallet.get_address()
private_key = wallet.get_private_key()

print(f'Miden Address: {address}')
print(f'Private Key: {private_key}')  # ⚠️ Store securely!
```

### Using Miden CLI (If Available)

```bash
# Install Miden CLI
# Check: https://github.com/0xMiden/miden-client

# Create wallet
miden wallet create --network testnet

# Get address
miden wallet address

# Request from faucet
miden faucet request --address YOUR_ADDRESS
```

## Configuration for OASIS

### Environment Variables

Create a `.env.miden` file:

```bash
# Miden Testnet Configuration
MIDEN_API_URL="https://testnet.miden.xyz"
MIDEN_API_KEY=""  # Usually not required for testnet
MIDEN_WALLET_ADDRESS="miden1your_address_here"
MIDEN_PRIVATE_KEY=""  # Only if using programmatic access
MIDEN_BRIDGE_POOL_ADDRESS="miden_bridge_pool"
MIDEN_NETWORK="testnet"
```

### Load Environment Variables

```bash
# Source the file
source .env.miden

# Or export individually
export MIDEN_API_URL="https://testnet.miden.xyz"
export MIDEN_WALLET_ADDRESS="miden1your_address"
```

### Update OASIS DNA

Add to your `OASIS_DNA.json`:

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

### Test 1: Verify Wallet Balance

```bash
# Using curl (if API supports it)
curl -X POST https://testnet.miden.xyz/api/balance \
  -H "Content-Type: application/json" \
  -d '{"address": "miden1your_address"}'
```

### Test 2: Create Private Note (C#)

```csharp
var midenProvider = new MidenOASIS(
    apiBaseUrl: "https://testnet.miden.xyz",
    apiKey: null,
    network: "testnet"
);

await midenProvider.ActivateProviderAsync();

var noteResult = await midenProvider.CreatePrivateNoteAsync(
    value: 1.0m,
    ownerPublicKey: "miden1your_address",
    assetId: "ZEC"
);

if (!noteResult.IsError)
{
    Console.WriteLine($"✅ Private note created: {noteResult.Result.NoteId}");
}
else
{
    Console.WriteLine($"❌ Error: {noteResult.Message}");
}
```

### Test 3: Generate STARK Proof

```csharp
var proofResult = await midenProvider.GenerateSTARKProofAsync(
    programHash: "bridge_program_hash",
    inputs: new { amount = 1.0m, source = "Zcash" },
    outputs: new { noteId = "test_note" }
);

if (!proofResult.IsError)
{
    Console.WriteLine($"✅ STARK proof generated");
    
    // Verify proof
    var verifyResult = await midenProvider.VerifySTARKProofAsync(proofResult.Result);
    if (!verifyResult.IsError && verifyResult.Result)
    {
        Console.WriteLine($"✅ STARK proof verified");
    }
}
```

## Troubleshooting

### Issue: Faucet Not Working

**Possible Causes**:
- Rate limiting (wait 24 hours between requests)
- Invalid address format
- Testnet maintenance

**Solutions**:
1. Verify address format (should start with `miden1...`)
2. Wait 24 hours if rate limited
3. Try different note type (public vs private)
4. Check Miden Discord/Telegram for testnet status

### Issue: Can't Connect to Testnet API

**Possible Causes**:
- Incorrect API URL
- Network connectivity
- API authentication required

**Solutions**:
1. Verify `MIDEN_API_URL` is correct
2. Test with curl: `curl https://testnet.miden.xyz/health`
3. Check if API key is required
4. Try alternative endpoints if available

### Issue: Wallet Not Showing Balance

**Possible Causes**:
- Transaction still pending
- Wrong network (mainnet vs testnet)
- Transaction failed

**Solutions**:
1. Refresh wallet extension
2. Check transaction status
3. Verify you're on testnet
4. Check transaction hash on explorer

### Issue: Private Note Creation Fails

**Possible Causes**:
- Insufficient balance
- Invalid address
- API connection issues

**Solutions**:
1. Verify wallet has testnet tokens
2. Check address format
3. Test API connection
4. Check error message for details

## Quick Reference

### Important URLs

- **Miden Website**: https://miden.xyz/
- **Miden Faucet**: https://faucet.testnet.miden.io/
- **Miden Docs**: https://docs.miden.xyz/
- **Miden GitHub**: https://github.com/0xPolygonMiden
- **Miden Client**: https://github.com/0xMiden/miden-client

### Testnet Details

- **Network**: Miden Testnet
- **API URL**: https://testnet.miden.xyz
- **Faucet**: https://faucet.testnet.miden.io/
- **Explorer**: (Check Miden docs for explorer URL)

### Security Checklist

- [ ] Recovery phrase saved securely (offline)
- [ ] Password is strong and remembered
- [ ] Wallet address copied correctly
- [ ] Testnet tokens received
- [ ] Environment variables set
- [ ] OASIS DNA configured

## Next Steps

1. ✅ Install Miden Wallet
2. ✅ Create wallet and save recovery phrase
3. ✅ Get testnet tokens from faucet
4. ✅ Configure OASIS environment variables
5. ✅ Update OASIS DNA configuration
6. ✅ Test private note creation
7. ✅ Test STARK proof generation
8. ✅ Test bridge operations

## Support

If you encounter issues:

1. **Check Miden Documentation**: https://docs.miden.xyz/
2. **Miden Discord**: (Check Miden website for Discord link)
3. **Miden GitHub Issues**: https://github.com/0xPolygonMiden/issues
4. **OASIS Documentation**: Check OASIS docs for provider setup

---

**Ready to test!** Once you have your wallet funded, you can start testing the Zcash ↔ Miden bridge operations.

