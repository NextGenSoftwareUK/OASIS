# Deploying OASIS Storage Component via Radix Wallet

## Prerequisites

1. **Radix Wallet App** (Mobile):
   - Download from: https://wallet.radixdlt.com/
   - iOS: App Store
   - Android: Google Play

2. **Radix Connector Browser Extension**:
   - Install from: https://chrome.google.com/webstore/detail/radix-wallet-connector/bfeplaecgkoeckiidkgkmlllfbaeplgm
   - Link your mobile wallet to the extension

3. **Stokenet Account with XRD**:
   - Create account in Radix Wallet
   - Switch to Stokenet network
   - Fund via faucet: https://stokenet-faucet.radixdlt.com/

## Step 1: Build the Scrypto Package

**IMPORTANT**: You need to build the Scrypto package first to get the WASM file.

### Option A: Using Scrypto CLI (If Available)

```bash
cd Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts
scrypto build
```

The WASM file will be at: `target/wasm32-unknown-unknown/release/oasis_storage.wasm`

### Option B: Using Standard Rust (May Not Work)

Scrypto packages typically require the `scrypto` CLI tool, but you can try:

```bash
cd Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts
cargo build --target wasm32-unknown-unknown --release
```

**Note**: This may fail if Scrypto-specific build steps are required. If it fails, you'll need to install Scrypto CLI tools first.

### Option C: Install Scrypto Tools

Follow the official guide: https://docs.radixdlt.com/docs/getting-rust-scrypto

Or try:
```bash
cargo install --git https://github.com/radixdlt/radixdlt-scrypto scrypto
```

## Step 2: Deploy Package via Radix Wallet

### Using Developer Console (Recommended)

1. **Open Developer Console**: https://console.radixdlt.com/

2. **Navigate to Package Publishing**:
   - Look for "Publish Package" or "Deploy Package" option
   - Switch to Stokenet network (if available)

3. **Upload WASM File**:
   - Select the WASM file: `target/wasm32-unknown-unknown/release/oasis_storage.wasm`
   - Sign the transaction using your Radix Wallet (via connector)

4. **Get Package Address**:
   - After transaction confirms, you'll receive a package address
   - Format: `package_tdx_2_1...` or `package_rdx1...`
   - **Save this address!**

### Alternative: Using Radix Wallet Mobile App

If the Developer Console doesn't have package publishing:

1. Some Scrypto deployment tools may be available in the wallet app's developer section
2. Check wallet app → Settings → Developer Options (if available)

## Step 3: Instantiate the Component

### Using Developer Console

1. **Navigate to Function Call**:
   - In Developer Console, find "Call Function" or "Instantiate Component"

2. **Fill in Details**:
   - **Package Address**: The package address from Step 2
   - **Blueprint Name**: `OasisStorage`
   - **Function Name**: `instantiate`
   - **Arguments**: (none required - instantiate takes no arguments)

3. **Sign and Submit**:
   - Review the transaction
   - Sign using Radix Wallet
   - Submit the transaction

4. **Get Component Address**:
   - After transaction confirms, you'll receive a component address
   - Format: `component_tdx_2_1...` or `component_rdx1...`
   - **This is what you need for configuration!**

## Step 4: Update Configuration

Update `OASIS_DNA.json`:

```json
"RadixOASIS": {
    "HostUri": "https://stokenet.radixdlt.com",
    "NetworkId": 2,
    "AccountAddress": "account_tdx_2_1...",
    "PrivateKey": "your_private_key_here",
    "ComponentAddress": "component_tdx_2_1..."  // ← Add your component address here
}
```

## Troubleshooting

### Problem: Can't find package publishing in Developer Console

**Solution**: The Developer Console may have limited features. You may need to:
- Use the Scrypto CLI tools (`resim` or `scrypto`) for deployment
- Check if there's a different Radix developer tool
- Use Gateway API directly with transaction manifests

### Problem: Build fails without Scrypto CLI

**Solution**: You must install Scrypto CLI tools:
```bash
# Follow official installation guide
# https://docs.radixdlt.com/docs/getting-rust-scrypto

# Or try:
cargo install --git https://github.com/radixdlt/radixdlt-scrypto scrypto
```

### Problem: Transaction fails

**Solutions**:
- Ensure you have sufficient XRD for fees (get from faucet if needed)
- Verify you're on Stokenet network (not Mainnet)
- Check that your account is properly connected
- Verify package was published successfully before instantiating

### Problem: Component instantiation fails

**Solutions**:
- Verify package address is correct
- Ensure blueprint name is exactly `OasisStorage`
- Ensure function name is exactly `instantiate`
- Check transaction error messages for details

## Alternative: Deploy via CLI Tools

If wallet deployment doesn't work, you can use CLI tools:

```bash
# Install resim (includes scrypto)
# Follow: https://docs.radixdlt.com/docs/resim-installation

cd contracts
scrypto build
resim publish .
# Note package address

resim call-function <PACKAGE_ADDRESS> OasisStorage instantiate
# Note component address
```

## Verification

After deployment, verify on Radix Explorer:

1. **Stokenet Explorer**: https://stokenet-dashboard.radixdlt.com/
2. Search for your component address
3. View component state and transaction history

## Next Steps

After deployment:
1. Update `OASIS_DNA.json` with component address
2. Test SaveAvatar operation
3. Test LoadAvatar operation
4. Verify transactions on explorer
5. See `NEXT_STEPS.md` for full testing checklist

