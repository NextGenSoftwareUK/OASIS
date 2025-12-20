# Deploy Without CLI Tools

If you can't install `scrypto` or `resim` CLI tools, you can still deploy using Radix Wallet directly.

## Build the Package (Standard Rust)

Even without scrypto CLI, we can try to build using standard Rust:

```bash
cd contracts

# Try building with standard Rust
cargo build --target wasm32-unknown-unknown --release
```

**Note**: This might not produce the exact WASM format Scrypto expects, but it's worth trying.

## Deploy via Radix Wallet (Easiest Method)

### Step 1: Prepare Package File

1. The build should produce: `target/wasm32-unknown-unknown/release/oasis_storage.wasm`
2. If that doesn't work, you may need to install scrypto tools first

### Step 2: Use Radix Wallet

1. **Open Radix Wallet**: https://wallet.radixdlt.com/
2. **Connect to Stokenet** (testnet)
3. **Go to Advanced** → **Publish Package**
4. **Upload the WASM file**
5. **Get Package Address** from transaction receipt

### Step 3: Instantiate Component

1. In Radix Wallet, go to **Advanced** → **Call Function**
2. **Package Address**: `<PACKAGE_ADDRESS>` (from step 2)
3. **Blueprint**: `OasisStorage`
4. **Function**: `instantiate`
5. **Arguments**: (none - instantiate takes no arguments)
6. **Get Component Address** from transaction receipt

### Step 4: Update Configuration

Update `OASIS_DNA.json`:

```json
"RadixOASIS": {
  "HostUri": "https://stokenet.radixdlt.com",
  "NetworkId": 2,
  "AccountAddress": "account_tdx_2_1...",
  "PrivateKey": "...",
  "ComponentAddress": "component_tdx_2_1..."  // Add this
}
```

## Alternative: Use Docker (If Available)

If you have Docker, you might be able to use a Scrypto Docker image:

```bash
# Check if Radix provides Docker images
docker pull radixdlt/scrypto:latest  # Example - check actual image name
```

## Install Tools Properly (Recommended)

To install the tools correctly:

1. **Visit Official Docs**: https://docs.radixdlt.com/docs/getting-rust-scrypto
2. **Follow their installation guide** - they have specific instructions for your OS
3. **Use their official installer** or package manager method

The CLI tools make deployment much easier, so it's worth installing them if possible.

