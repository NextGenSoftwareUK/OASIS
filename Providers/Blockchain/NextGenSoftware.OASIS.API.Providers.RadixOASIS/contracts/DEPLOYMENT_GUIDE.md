# OASIS Storage Component Deployment Guide

This guide explains how to deploy the OASIS Storage Scrypto component to Radix Stokenet (testnet).

## Prerequisites

1. **Radix CLI Tools**: You need either `scrypto` or `resim` installed
   - **resim**: Radix Simulator (for local testing) - [Installation Guide](https://docs.radixdlt.com/docs/resim-installation)
   - **scrypto**: Scrypto CLI tools (for building and deploying) - [Installation Guide](https://docs.radixdlt.com/docs/scrypto-installation)

2. **Radix Account**: A Stokenet account with XRD for transaction fees
   - Create account via Radix Wallet: https://wallet.radixdlt.com/
   - Fund via Stokenet faucet: https://stokenet-faucet.radixdlt.com/

3. **Rust**: Rust toolchain (already installed)

## Deployment Options

### Option 1: Deploy via Resim (Local Simulator - Testing Only)

For local testing and development:

```bash
cd contracts

# Build the package
scrypto build

# Start resim (if not already running)
resim start

# Set default account (use your Stokenet account address)
resim set-default-account <ACCOUNT_ADDRESS> <PRIVATE_KEY>

# Publish package
resim publish .

# Note the package address from output (package_rdx1...)

# Instantiate component
resim call-function <PACKAGE_ADDRESS> OasisStorage instantiate

# Note the component address from output (component_rdx1...)
```

**Note**: This deploys to the local simulator, not Stokenet. Use for testing only.

### Option 2: Deploy via Gateway API (Stokenet/Mainnet)

To deploy to actual Stokenet network:

1. **Build the Package**:
   ```bash
   cd contracts
   scrypto build
   ```

2. **Publish Package via Gateway API**:
   - Use Radix Wallet to publish the package
   - Or use Gateway API transaction submission
   - Package file location: `target/wasm32-unknown-unknown/release/oasis_storage.wasm`

3. **Instantiate Component via Transaction**:
   - Create transaction manifest to call `instantiate` function
   - Submit via Gateway API or Radix Wallet
   - Get component address from transaction receipt

### Option 3: Deploy via Radix Wallet (Easiest)

1. **Build Package**:
   ```bash
   cd contracts
   scrypto build
   ```

2. **Use Radix Wallet**:
   - Open Radix Wallet (https://wallet.radixdlt.com/)
   - Connect to Stokenet network
   - Use "Advanced" → "Publish Package" feature
   - Upload the WASM file: `target/wasm32-unknown-unknown/release/oasis_storage.wasm`
   - Get package address

3. **Instantiate Component**:
   - In Radix Wallet, use "Advanced" → "Call Function"
   - Package address: `<PACKAGE_ADDRESS>`
   - Blueprint: `OasisStorage`
   - Function: `instantiate`
   - Arguments: (none)
   - Get component address from transaction receipt

## Configuration After Deployment

Once you have the component address, update your configuration:

1. **Update OASIS_DNA.json**:
   ```json
   "RadixOASIS": {
     "HostUri": "https://stokenet.radixdlt.com",
     "NetworkId": 2,
     "AccountAddress": "account_tdx_2_1...",
     "PrivateKey": "...",
     "ComponentAddress": "component_tdx_2_1..."  // Add this
   }
   ```

2. **Update Test Harness** (if using):
   ```csharp
   public const string ComponentAddress = "component_tdx_2_1...";
   ```

## Verification

After deployment, verify the component is working:

1. **Check Component State**:
   ```bash
   # Query component state via Gateway API
   curl -X POST https://stokenet.radixdlt.com/state/entity/details \
     -H "Content-Type: application/json" \
     -d '{"addresses": ["component_tdx_2_1..."]}'
   ```

2. **Test via RadixOASIS Provider**:
   - Use the test harness to call `SaveAvatar` and `LoadAvatar`
   - Verify data is stored and retrieved correctly

## Troubleshooting

### Build Errors

If `scrypto build` fails:
- Ensure Scrypto is installed: `scrypto --version`
- Check Rust version: `rustc --version` (should be 1.70+)
- Update dependencies: `cargo update`

### Deployment Errors

If deployment fails:
- Ensure account has sufficient XRD balance
- Check network ID matches (2 for Stokenet, 1 for Mainnet)
- Verify package address is correct
- Check transaction fees are covered

### Component Not Found

If component address is not found:
- Verify component address format (starts with `component_tdx_2_1` for Stokenet)
- Check component was successfully instantiated
- Ensure Gateway API URL is correct for network

## Next Steps

After successful deployment:

1. Update configuration with component address
2. Test Save/Load operations via RadixOASIS provider
3. Test all methods (SaveAvatar, LoadAvatar, SaveHolon, etc.)
4. Verify Gateway API state queries work correctly

## Resources

- [Radix Documentation](https://docs.radixdlt.com/)
- [Scrypto Documentation](https://docs.radixdlt.com/docs/scrypto)
- [Gateway API Docs](https://radix-babylon-gateway-api.redoc.ly/)
- [Radix Wallet](https://wallet.radixdlt.com/)
- [Stokenet Faucet](https://stokenet-faucet.radixdlt.com/)

