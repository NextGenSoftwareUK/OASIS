# Quick Start: Deploy OASIS Storage Component

## Prerequisites Check

Before deploying, ensure you have:

1. **Rust installed** (✓ Already installed)
2. **Scrypto CLI tools** (Need to install)
3. **Radix account** with XRD balance (For Stokenet deployment)

## Install Scrypto Tools

### Option 1: Use Official Installer (Recommended)

**Follow the official Radix documentation:**
- Visit: https://docs.radixdlt.com/docs/getting-rust-scrypto
- Use their official installation method (usually an installer script or package manager)

### Option 2: Install Resim (Includes Scrypto)

`resim` (Radix Engine Simulator) includes scrypto tools:

```bash
# Check official docs for resim installation
# https://docs.radixdlt.com/docs/resim-installation
```

### Option 3: Build from Source (Advanced)

If you need to build from source, the correct repository is:
- Repository: `https://github.com/radixdlt/radixdlt-scrypto` (not `radixdlt/scrypto`)
- Follow their build instructions in the repository

**Note**: The repository `radixdlt/scrypto` doesn't exist - use `radixdlt/radixdlt-scrypto` if building from source.

## Deploy to Local Simulator (Testing)

```bash
cd contracts
./deploy.sh local
```

This will:
1. Build the package
2. Start resim (if not running)
3. Publish the package
4. Instantiate the component
5. Output the component address

## Deploy to Stokenet (Testnet)

### Step 1: Get Stokenet Account

1. Create account via Radix Wallet: https://wallet.radixdlt.com/
2. Switch to Stokenet network
3. Fund via faucet: https://stokenet-faucet.radixdlt.com/
4. Note your account address and private key

### Step 2: Build Package

```bash
cd contracts
scrypto build
```

### Step 3: Publish via Radix Wallet

1. Open Radix Wallet (https://wallet.radixdlt.com/)
2. Connect to Stokenet
3. Go to "Advanced" → "Publish Package"
4. Upload: `target/wasm32-unknown-unknown/release/oasis_storage.wasm`
5. Note the package address

### Step 4: Instantiate Component

1. In Radix Wallet, go to "Advanced" → "Call Function"
2. Package: `<PACKAGE_ADDRESS>`
3. Blueprint: `OasisStorage`
4. Function: `instantiate`
5. Arguments: (none)
6. Note the component address from transaction receipt

### Step 5: Update Configuration

Update `OASIS_DNA.json`:

```json
"RadixOASIS": {
  "HostUri": "https://stokenet.radixdlt.com",
  "NetworkId": 2,
  "AccountAddress": "account_tdx_2_1...",
  "PrivateKey": "...",
  "ComponentAddress": "component_tdx_2_1..."
}
```

## Verify Deployment

Test the component:

```bash
# Query component state
curl -X POST https://stokenet.radixdlt.com/state/entity/details \
  -H "Content-Type: application/json" \
  -d '{"addresses": ["component_tdx_2_1..."]}'
```

## Troubleshooting

### "scrypto: command not found"

Install Scrypto tools (see above).

### Build Errors

- Ensure Rust is up to date: `rustup update`
- Check Scrypto version: `scrypto --version`
- Try: `cargo clean && scrypto build`

### Deployment Errors

- Ensure account has XRD balance
- Check network ID (2 for Stokenet, 1 for Mainnet)
- Verify account address format

### Component Not Found

- Double-check component address format
- Ensure component was successfully instantiated
- Verify Gateway API URL matches network

## Next Steps

After successful deployment:

1. Update configuration with component address
2. Test SaveAvatar/LoadAvatar via RadixOASIS provider
3. Test all methods (SaveHolon, LoadHolon, etc.)
4. Verify Gateway API state queries work

## Resources

- [Radix Documentation](https://docs.radixdlt.com/)
- [Scrypto Docs](https://docs.radixdlt.com/docs/scrypto)
- [Radix Wallet](https://wallet.radixdlt.com/)
- [Stokenet Faucet](https://stokenet-faucet.radixdlt.com/)

