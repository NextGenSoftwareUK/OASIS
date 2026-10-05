# Complete Testnet Setup Guide - Miden & Zcash

## 🎯 Overview

This guide helps you set up both **Miden** and **Zcash** testnets for testing the Zcash ↔ Miden bridge (Track 4).

## 📋 Quick Checklist

- [ ] Miden Wallet installed
- [ ] Miden wallet created and funded
- [ ] Zcash testnet RPC configured
- [ ] Zcash testnet wallet/address ready
- [ ] Environment variables set
- [ ] OASIS DNA configured
- [ ] Bridge tested

---

## Part 1: Miden Testnet Setup

### Step 1: Install Miden Wallet (5 minutes)

1. **Visit**: https://miden.xyz/
2. **Download**: Browser extension wallet
3. **Install**: Follow browser instructions

### Step 2: Create Wallet

1. Open Miden Wallet extension
2. Create new wallet
3. **Save recovery phrase** (12-24 words)
4. Copy your address (starts with `miden1...`)

### Step 3: Get Testnet Tokens

1. **Visit Faucet**: https://faucet.testnet.miden.io/
2. Enter your Miden address
3. Click "Send Public Note"
4. Wait 1-2 minutes
5. In wallet: **Receive → Claim**

### Step 4: Verify

- Check balance in wallet
- Should show testnet tokens

**✅ Miden Setup Complete!**

---

## Part 2: Zcash Testnet Setup

### Option A: Public RPC Endpoint (Recommended - No Installation)

**Use public testnet RPC - no local node needed!**

#### Step 1: Get Public RPC Endpoint

**Available Endpoints:**
- `https://zcash-testnet.gateway.tatum.io` (Tatum Gateway)
- `https://testnet.z.cash` (if available)
- Other community endpoints

#### Step 2: Configure OASIS

```bash
export ZCASH_RPC_URL="https://zcash-testnet.gateway.tatum.io"
export ZCASH_RPC_USER=""
export ZCASH_RPC_PASSWORD=""
export ZCASH_NETWORK="testnet"
```

#### Step 3: Get Testnet Address

**Option 1: Use Zcash Wallet (if installed)**
- Create shielded address (starts with `zs1...`)

**Option 2: Generate via OASIS**
```csharp
var zcashProvider = new ZcashOASIS();
await zcashProvider.ActivateProviderAsync();
var addressResult = await zcashProvider.GetNewShieldedAddressAsync();
// Use addressResult.Result
```

**Option 3: Use Public Testnet Explorer**
- Visit Zcash testnet explorer
- Generate address online (for testing only)

#### Step 4: Get Testnet ZEC

**Faucet Options:**
1. **Zcash Testnet Faucet** (if available)
   - Visit: https://faucet.zcash-testnet.com (check if exists)
   - Enter your `zs1...` address
   - Request testnet ZEC

2. **Community Faucets**
   - Check Zcash Discord/Telegram
   - Request testnet ZEC from community

3. **Pre-funded Accounts**
   - Use provided testnet accounts (if available)
   - For hackathon demos only

**✅ Zcash Setup Complete!**

---

### Option B: Local Zcash Node (Advanced)

If you need a local node:

#### Step 1: Install Zcash

**macOS:**
```bash
# Check if available
brew search zcash

# Or build from source
git clone https://github.com/zcash/zcash.git
cd zcash
./zcutil/build.sh -j$(nproc)
```

**Linux:**
```bash
# Ubuntu/Debian
sudo apt-get install zcash

# Or build from source
git clone https://github.com/zcash/zcash.git
cd zcash
./zcutil/build.sh -j$(nproc)
```

#### Step 2: Configure Testnet

```bash
# Create config file
mkdir -p ~/.zcash
cat > ~/.zcash/zcash.conf << EOF
testnet=1
server=1
rpcuser=oasis
rpcpassword=Uppermall1!
rpcallowip=127.0.0.1
rpcport=18232
EOF
```

#### Step 3: Start Testnet Node

```bash
zcashd -testnet
```

#### Step 4: Wait for Sync

- Wait for blockchain to sync (can take hours)
- Check status: `zcash-cli -testnet getblockchaininfo`

#### Step 5: Get Testnet Address

```bash
zcash-cli -testnet z_getnewaddress
# Returns: zs1...
```

#### Step 6: Get Testnet ZEC

```bash
# Request from faucet or use testnet mining
zcash-cli -testnet generate 100
```

---

## Part 3: OASIS Configuration

### Environment Variables

Create `.env.testnet`:

```bash
# Miden Configuration
export MIDEN_API_URL="https://testnet.miden.xyz"
export MIDEN_API_KEY=""
export MIDEN_WALLET_ADDRESS="miden1your_address"
export MIDEN_BRIDGE_POOL_ADDRESS="miden_bridge_pool"
export MIDEN_NETWORK="testnet"

# Zcash Configuration (Public RPC)
export ZCASH_RPC_URL="https://zcash-testnet.gateway.tatum.io"
export ZCASH_RPC_USER=""
export ZCASH_RPC_PASSWORD=""
export ZCASH_NETWORK="testnet"
export ZCASH_BRIDGE_POOL_ADDRESS="zt1bridgepool"

# Or if using local node
# export ZCASH_RPC_URL="http://localhost:18232"
# export ZCASH_RPC_USER="oasis"
# export ZCASH_RPC_PASSWORD="Uppermall1!"
```

### Load Environment

```bash
source .env.testnet
```

### OASIS DNA Configuration

Update `OASIS_DNA.json`:

```json
{
  "Providers": {
    "MidenOASIS": {
      "IsEnabled": true,
      "ApiUrl": "https://testnet.miden.xyz",
      "ApiKey": "",
      "Network": "testnet",
      "WalletAddress": "${MIDEN_WALLET_ADDRESS}"
    },
    "ZcashOASIS": {
      "IsEnabled": true,
      "RpcUrl": "${ZCASH_RPC_URL}",
      "RpcUser": "${ZCASH_RPC_USER}",
      "RpcPassword": "${ZCASH_RPC_PASSWORD}",
      "Network": "testnet"
    }
  },
  "Bridges": {
    "ZEC": "ZcashBridgeService",
    "MIDEN": "MidenBridgeService"
  }
}
```

---

## Part 4: Testing

### Test 1: Miden Connection

```csharp
var midenProvider = new MidenOASIS();
await midenProvider.ActivateProviderAsync();

var balance = await midenProvider.GetAccountBalanceAsync("miden1your_address");
Console.WriteLine($"Miden Balance: {balance.Result}");
```

### Test 2: Zcash Connection

```csharp
var zcashProvider = new ZcashOASIS();
await zcashProvider.ActivateProviderAsync();

var address = await zcashProvider.GetNewShieldedAddressAsync();
Console.WriteLine($"Zcash Address: {address.Result}");
```

### Test 3: Bridge Operation

```csharp
var request = new CreateBridgeOrderRequest
{
    FromToken = "ZEC",
    ToToken = "MIDEN",
    Amount = 0.1m,
    FromAddress = "zs1your_zcash_address",
    DestinationAddress = "miden1your_miden_address",
    UserId = userId,
    EnableViewingKeyAudit = true,
    RequireProofVerification = true
};

var result = await bridgeManager.CreateBridgeOrderAsync(request);
if (!result.IsError)
{
    Console.WriteLine($"✅ Bridge completed! Tx: {result.Result.TransactionId}");
}
```

---

## Troubleshooting

### Miden Issues

**Faucet not working?**
- Wait 24 hours (rate limit)
- Try different note type
- Check Miden Discord for status

**Can't connect to API?**
- Verify `MIDEN_API_URL` is correct
- Check network connectivity
- Try alternative endpoints

### Zcash Issues

**Public RPC not working?**
- Try alternative endpoint
- Check if endpoint requires API key
- Verify endpoint is for testnet

**Local node not syncing?**
- Wait longer (can take hours)
- Check disk space
- Verify network connectivity

**Can't get testnet ZEC?**
- Try community faucets
- Check Zcash Discord
- Use pre-funded accounts for demos

---

## Quick Reference

### Miden
- **Website**: https://miden.xyz/
- **Faucet**: https://faucet.testnet.miden.io/
- **API**: https://testnet.miden.xyz
- **Address Format**: `miden1...`

### Zcash
- **Website**: https://z.cash/
- **Testnet Explorer**: https://explorer.testnet.z.cash/
- **Public RPC**: `https://zcash-testnet.gateway.tatum.io`
- **Address Format**: `zs1...` (shielded)

---

## Next Steps

1. ✅ Set up both testnets
2. ✅ Get testnet tokens
3. ✅ Configure OASIS
4. ✅ Test connections
5. ✅ Test bridge operations
6. ✅ Prepare demo

**Ready to test the bridge!** 🚀

