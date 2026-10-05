# Quick Start - Your Miden Wallet is Ready! 🚀

## ✅ Your Wallet Address

```
mtst1aqwg0x9w9wcrvyqdx6ftykeh65v9cn9c_qruqqypuyph
```

This is your **Miden testnet address**. It's configured and ready to use!

## 🎯 Next Steps (5 Minutes)

### Step 1: Get Testnet Tokens

1. **Visit Faucet**: https://faucet.testnet.miden.io/
2. **Paste Your Address**:
   ```
   mtst1aqwg0x9w9wcrvyqdx6ftykeh65v9cn9c_qruqqypuyph
   ```
3. **Click**: "Send Public Note" or "Send Private Note"
4. **Wait**: 1-2 minutes for transaction
5. **Claim**: Open Miden Wallet → Receive → Claim

### Step 2: Configure Environment

```bash
# Copy template to actual env file
cp zypherpunk/env.miden.template zypherpunk/.env.miden

# Load environment variables
source zypherpunk/.env.miden

# Verify
echo $MIDEN_WALLET_ADDRESS
# Should show: mtst1aqwg0x9w9wcrvyqdx6ftykeh65v9cn9c_qruqqypuyph
```

### Step 3: Update OASIS DNA

Add to `OASIS_DNA.json`:

```json
{
  "Providers": {
    "MidenOASIS": {
      "IsEnabled": true,
      "ApiUrl": "https://testnet.miden.xyz",
      "Network": "testnet",
      "WalletAddress": "mtst1aqwg0x9w9wcrvyqdx6ftykeh65v9cn9c_qruqqypuyph"
    }
  }
}
```

### Step 4: Test Connection

```csharp
var midenProvider = new MidenOASIS();
await midenProvider.ActivateProviderAsync();

var balance = await midenProvider.GetAccountBalanceAsync(
    "mtst1aqwg0x9w9wcrvyqdx6ftykeh65v9cn9c_qruqqypuyph"
);
Console.WriteLine($"Balance: {balance.Result}");
```

## 📋 Checklist

- [x] Wallet created
- [x] Address configured: `mtst1aqwg0x9w9wcrvyqdx6ftykeh65v9cn9c_qruqqypuyph`
- [ ] Tokens requested from faucet
- [ ] Tokens claimed in wallet
- [ ] Environment variables loaded
- [ ] OASIS DNA updated
- [ ] Connection tested
- [ ] Ready for bridge testing!

## 🔗 Quick Links

- **Faucet**: https://faucet.testnet.miden.io/
- **Your Address**: `mtst1aqwg0x9w9wcrvyqdx6ftykeh65v9cn9c_qruqqypuyph`
- **Testnet API**: https://testnet.miden.xyz

## 💡 Pro Tip

Save this address somewhere accessible - you'll need it for:
- Bridge operations
- Receiving tokens
- Testing private notes
- STARK proof operations

---

**You're all set!** Get tokens from the faucet and start testing! 🎉

