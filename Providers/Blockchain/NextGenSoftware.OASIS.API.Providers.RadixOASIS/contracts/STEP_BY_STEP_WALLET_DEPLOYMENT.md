# Step-by-Step: Deploy OASIS Storage Component via Radix Wallet

## Prerequisites Checklist

- [ ] Radix Wallet mobile app installed (iOS/Android)
- [ ] Radix Connector browser extension installed (Chrome)
- [ ] Wallet connected to browser extension
- [ ] Stokenet account created in wallet
- [ ] Account funded with XRD (via faucet: https://stokenet-faucet.radixdlt.com/)

---

## Step 1: Build the Scrypto Package

**IMPORTANT**: Before deploying, you must build the package to generate the WASM file.

### Option A: Install Scrypto CLI Tools (Recommended)

If you don't have Scrypto CLI tools yet, install them:

```bash
# Follow the official installation guide:
# https://docs.radixdlt.com/docs/getting-rust-scrypto

# Or try installing via cargo (may require additional setup):
cargo install --git https://github.com/radixdlt/radixdlt-scrypto scrypto
```

After installing, verify:
```bash
scrypto --version
```

### Option B: Try Building with Standard Rust (May Not Work)

Scrypto packages typically require the `scrypto` CLI, but you can try:

```bash
cd /Volumes/Storage/OASIS_CLEAN/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts

# Try building (this will likely fail without scrypto CLI)
cargo build --target wasm32-unknown-unknown --release
```

**Note**: If this fails, you'll need to install Scrypto CLI tools (Option A).

### Build the Package

Once you have Scrypto CLI tools:

```bash
cd /Volumes/Storage/OASIS_CLEAN/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts

# Build the package
scrypto build
```

**Expected Output**:
- Success message
- WASM file created at: `target/wasm32-unknown-unknown/release/oasis_storage.wasm`

**Verify the file exists**:
```bash
ls -lh target/wasm32-unknown-unknown/release/oasis_storage.wasm
```

---

## Step 2: Prepare for Deployment

1. **Locate your WASM file**:
   - Full path: `/Volumes/Storage/OASIS_CLEAN/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts/target/wasm32-unknown-unknown/release/oasis_storage.wasm`
   - Note the file size (should be several MB)

2. **Ensure Radix Wallet is ready**:
   - Open Radix Wallet mobile app
   - Switch to **Stokenet** network (not Mainnet)
   - Ensure you have XRD balance for fees
   - Link wallet to browser extension if not already done

---

## Step 3: Deploy Package via Developer Console

### 3.1 Open Developer Console

1. Navigate to: **https://console.radixdlt.com/deploy-package**
2. The page should show "Deploy Package" heading

### 3.2 Connect Radix Wallet

1. Click the **"Connect"** button (top right)
2. A QR code or connection prompt should appear
3. Open your **Radix Wallet** mobile app
4. Scan the QR code or approve the connection request
5. Ensure you're connected to **Stokenet** network

### 3.3 Upload and Deploy Package

1. Once connected, you should see a file upload interface
2. Click **"Choose File"** or drag-and-drop
3. Select: `target/wasm32-unknown-unknown/release/oasis_storage.wasm`
4. Review the transaction details
5. **Sign the transaction** using your Radix Wallet
6. Wait for transaction confirmation

### 3.4 Get Package Address

After the transaction confirms:

1. Look for the **Package Address** in the transaction receipt or success message
2. Format: `package_tdx_2_1...` or `package_rdx1...`
3. **Copy and save this address** - you'll need it for the next step!

---

## Step 4: Instantiate the Component

After deploying the package, you need to instantiate (create) the component.

### Option A: Using Developer Console (If Available)

1. Look for an "Instantiate Component" or "Call Function" option in the Developer Console
2. Or navigate to: **https://console.radixdlt.com/** and look for component instantiation tools
3. Fill in:
   - **Package Address**: The address from Step 3.4
   - **Blueprint**: `OasisStorage`
   - **Function**: `instantiate`
   - **Arguments**: (leave empty - instantiate takes no arguments)
4. Sign and submit the transaction

### Option B: Using Transaction Manifest

If instantiation isn't available in the console, you can use "Send Raw Transaction":

1. Navigate to: **https://console.radixdlt.com/transaction-manifest**
2. Create a transaction manifest to call the instantiate function
3. You may need to use RadixEngineToolkit or reference Radix documentation for the manifest format

### Option C: Using Scrypto CLI (If Available)

```bash
# If you have resim or scrypto CLI with network access
resim call-function <PACKAGE_ADDRESS> OasisStorage instantiate
```

### 4.1 Get Component Address

After instantiation succeeds:

1. Look for the **Component Address** in the transaction receipt
2. Format: `component_tdx_2_1...` or `component_rdx1...`
3. **This is what you need for configuration!**

---

## Step 5: Update Configuration

Update `OASIS_DNA.json` with your component address:

```json
"RadixOASIS": {
    "HostUri": "https://stokenet.radixdlt.com",
    "NetworkId": 2,
    "AccountAddress": "account_tdx_2_1...",  // Your Stokenet account address
    "PrivateKey": "your_private_key_here",   // Your account private key
    "ComponentAddress": "component_tdx_2_1..."  // ← Component address from Step 4.1
}
```

**File location**: `/Volumes/Storage/OASIS_CLEAN/OASIS_DNA.json`

---

## Step 6: Verify Deployment

### Check on Radix Explorer

1. Open **Stokenet Explorer**: https://stokenet-dashboard.radixdlt.com/
2. Search for your **component address**
3. Verify:
   - Component exists
   - Transaction history shows deployment and instantiation
   - Component state is accessible

### Test the Component

You can now test the component using the RadixOASIS provider:

```csharp
var config = new RadixOASISConfig
{
    HostUri = "https://stokenet.radixdlt.com",
    NetworkId = 2,
    AccountAddress = "your_account_address",
    PrivateKey = "your_private_key",
    ComponentAddress = "your_component_address"  // From Step 4.1
};

var provider = new RadixOASIS(config);
var result = await provider.ActivateProviderAsync();

if (result.IsError)
{
    Console.WriteLine($"Error: {result.Message}");
}
else
{
    Console.WriteLine("Provider activated successfully!");
    
    // Test SaveAvatar
    var avatar = new Avatar { /* ... */ };
    var saveResult = await provider.SaveAvatarAsync(avatar);
    Console.WriteLine($"Save result: {saveResult.IsError}");
}
```

---

## Troubleshooting

### Problem: "Scrypto command not found"

**Solution**: Install Scrypto CLI tools:
```bash
# Follow: https://docs.radixdlt.com/docs/getting-rust-scrypto
# Or try:
cargo install --git https://github.com/radixdlt/radixdlt-scrypto scrypto
```

### Problem: Build fails

**Solutions**:
- Ensure Rust toolchain is up to date: `rustup update`
- Verify Scrypto version matches: Check `Cargo.toml` for `scrypto = "1.3.0"`
- Check error messages for specific dependency issues

### Problem: Can't connect wallet to Developer Console

**Solutions**:
- Ensure Radix Connector extension is installed
- Ensure wallet mobile app is linked to the extension
- Try refreshing the page
- Check that both wallet and browser are on the same network (Stokenet)

### Problem: Deployment transaction fails

**Solutions**:
- Verify you have sufficient XRD for fees
- Ensure you're on Stokenet network (not Mainnet)
- Check transaction error messages
- Verify WASM file is valid (not corrupted)

### Problem: Can't find instantiation option

**Solutions**:
- Use "Send Raw Transaction" with a transaction manifest
- Use Scrypto CLI if available: `resim call-function ...`
- Check Radix documentation for component instantiation

### Problem: Component address not found after instantiation

**Solutions**:
- Check transaction receipt carefully - address may be in different format
- Look at transaction output for "Component Address" or "Component: component_..."
- Verify transaction succeeded on explorer
- Check component state on Radix Explorer using package address

---

## Quick Reference

### Important Addresses

- **Stokenet Gateway**: https://stokenet.radixdlt.com
- **Stokenet Explorer**: https://stokenet-dashboard.radixdlt.com/
- **Stokenet Faucet**: https://stokenet-faucet.radixdlt.com/
- **Developer Console**: https://console.radixdlt.com/

### File Locations

- **WASM File**: `contracts/target/wasm32-unknown-unknown/release/oasis_storage.wasm`
- **Configuration**: `OASIS_DNA.json`
- **Scrypto Source**: `contracts/src/oasis_storage.rs`

### Key Commands

```bash
# Build package
cd contracts
scrypto build

# Verify WASM file
ls -lh target/wasm32-unknown-unknown/release/oasis_storage.wasm

# Check Scrypto version
scrypto --version
```

---

## Next Steps After Deployment

1. ✅ Update `OASIS_DNA.json` with component address
2. ✅ Test provider activation
3. ✅ Test SaveAvatar operation
4. ✅ Test LoadAvatar operation
5. ✅ Test DeleteAvatar operation
6. ✅ Verify transactions on Radix Explorer

See `NEXT_STEPS.md` for full testing checklist.

