# Stokenet Account Setup Guide

## Quick Setup

To get a Stokenet (testnet) account for testing:

### Method 1: Radix Wallet (Easiest)

1. **Download Radix Wallet**: https://wallet.radixdlt.com/
2. **Create Account**:
   - Open wallet
   - Switch network to "Stokenet" (testnet)
   - Create new account
   - Save your seed phrase securely
3. **Get Account Details**:
   - Copy account address (starts with `account_tdx_2_1...`)
   - Export private key (in wallet settings)
4. **Fund Account**: Visit https://faucet.radixdlt.com/
5. **Update TestData**: Add address and private key to `Program.cs`

### Method 2: Radix CLI (Advanced)

If you have Radix CLI installed:

```bash
# Generate new account
radix-wallet-cli account create --network stokenet

# Export private key
radix-wallet-cli account export --network stokenet <account-address>
```

### Method 3: Use Your Mainnet Account

**⚠️ WARNING**: Only if you want to test with real funds!

Your mainnet account: `account_rdx129970k0h0x37rq60vk8fzrftxluzc4f39j9gzqpwmd3z3luvmtacrw`

To use it:
1. Change `HostUri` to `"https://mainnet.radixdlt.com"`
2. Change `NetworkId` to `1`
3. Add your mainnet private key
4. **Be careful** - transactions will use real XRD!

## Recommended: Use Stokenet

For testing, always use Stokenet (testnet):
- Free test XRD from faucet
- No risk to real funds
- Safe for development

---

**Next Steps**: Once you have your Stokenet account, update `TestData` in `Program.cs` and run the tests!

