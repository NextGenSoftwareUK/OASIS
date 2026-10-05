# Getting XRD for Radix Deployment

## Stokenet (Testnet) - Free Test XRD

Since you're deploying to **Stokenet** (testnet), you can get free test XRD from the faucet:

### Option 1: Stokenet Faucet (Easiest)

1. **Visit the Stokenet Faucet:**
   - https://stokenet-faucet.radixdlt.com/
   - Or: https://faucet.radixdlt.com/ (may redirect to Stokenet)

2. **Request Test XRD:**
   - Enter your Stokenet account address
   - Format: `account_tdx_2_1...` (starts with `account_tdx_2_`)
   - Click "Request Test XRD"
   - Wait a few minutes for the tokens to arrive

3. **Check Your Balance:**
   - Open Radix Wallet app
   - Switch to Stokenet network (if not already)
   - Check your XRD balance
   - Should see test XRD arrive within a few minutes

### Option 2: Discord Faucet

1. **Join Radix Discord:**
   - https://discord.gg/radixdlt
   
2. **Find Faucet Channel:**
   - Look for `#stokenet-faucet` channel
   - Post your Stokenet account address
   - Bot will send test XRD

### Option 3: Radix Wallet Developer Tools

Some Developer Console tools may provide test XRD:
- Check: https://console.radixdlt.com/
- Look for "Faucet" or "Get Test XRD" option

## How Much Do You Need?

For deploying a Scrypto package, you typically need:
- **Package Deployment**: ~0.1 - 0.5 XRD (depends on package size)
- **Component Instantiation**: ~0.01 - 0.1 XRD
- **Transaction Fees**: Very low on Stokenet

**Recommendation**: Request 5-10 XRD from the faucet to have enough for:
- Package deployment
- Component instantiation
- Multiple test transactions

## Verify Your Account Address

To get your Stokenet account address:

1. **From Radix Wallet App:**
   - Open Radix Wallet
   - Make sure you're on **Stokenet** network (not Mainnet)
   - Tap on your account
   - Copy the account address (starts with `account_tdx_2_`)

2. **From Developer Console:**
   - If connected, the console may show your account address
   - Or check the wallet connection details

## Troubleshooting

### "Faucet is Empty" or "Rate Limited"
- Wait 24 hours between requests
- Try the Discord faucet instead
- Try again later

### "Invalid Address"
- Make sure you're using **Stokenet** address (starts with `account_tdx_2_`)
- Not Mainnet address (which starts with `account_rdx1_`)
- Double-check the address has no typos

### "Not Enough XRD"
- Make sure you're on **Stokenet** network in wallet
- Check if test XRD arrived (may take a few minutes)
- Request more from faucet if needed

### Switch Networks

**To switch to Stokenet in Radix Wallet:**
1. Open Radix Wallet app
2. Go to Settings
3. Select "Network"
4. Choose "Stokenet" (not "Mainnet")
5. Your account address will change to Stokenet format

## Quick Steps Summary

1. ✅ Make sure Radix Wallet is on **Stokenet** network
2. ✅ Copy your Stokenet account address (`account_tdx_2_1...`)
3. ✅ Visit: https://stokenet-faucet.radixdlt.com/
4. ✅ Paste address and request test XRD
5. ✅ Wait 2-5 minutes for tokens to arrive
6. ✅ Verify balance in Radix Wallet
7. ✅ Deploy package!

## Links

- **Stokenet Faucet**: https://stokenet-faucet.radixdlt.com/
- **Radix Wallet**: https://wallet.radixdlt.com/
- **Developer Console**: https://console.radixdlt.com/
- **Radix Discord**: https://discord.gg/radixdlt



