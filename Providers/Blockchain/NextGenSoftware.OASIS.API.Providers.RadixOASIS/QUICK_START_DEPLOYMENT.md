# Quick Start: Deploy OASIS Storage Component

## ✅ Build Complete!

Your WASM file has been successfully built via GitHub Actions. Now let's deploy it!

---

## Step 1: Download WASM File (2 minutes)

1. **Go to GitHub Actions**:
   - Repository → **Actions** tab
   - Click on the latest successful workflow run
   - Scroll to **Artifacts** section

2. **Download**:
   - Download `oasis-storage-wasm` artifact
   - Extract ZIP file
   - Find `oasis_storage.wasm`

---

## Step 2: Deploy to Stokenet (10-15 minutes)

### A. Open Developer Console
- Go to: **https://console.radixdlt.com/deploy-package**

### B. Connect Wallet
- Click **"Connect"** button (top right)
- Open Radix Wallet mobile app
- Switch to **Stokenet** network
- Scan QR code to connect

### C. Upload WASM
- Click **"Choose File"** or drag-and-drop
- Select `oasis_storage.wasm`
- Sign transaction
- **Copy Package Address** (format: `package_tdx_2_1...`)

---

## Step 3: Instantiate Component (5 minutes)

### Option 1: Developer Console (If Available)
- Look for "Call Function" or "Instantiate Component"
- Package Address: `[your package address]`
- Blueprint: `OasisStorage`
- Function: `instantiate`
- Arguments: (none)
- Sign and submit
- **Copy Component Address** (format: `component_tdx_2_1...`)

### Option 2: Transaction Manifest
- Go to: https://console.radixdlt.com/transaction-manifest
- Need help? Let me know and I'll create the manifest

---

## Step 4: Update Configuration (2 minutes)

**File**: `/Volumes/Storage/OASIS_CLEAN/OASIS_DNA.json`

Find the `RadixOASIS` section and update:

```json
"RadixOASIS": {
    "HostUri": "https://stokenet.radixdlt.com",
    "NetworkId": 2,
    "AccountAddress": "account_tdx_2_1...",  // Your Stokenet account
    "PrivateKey": "your_private_key",         // Your private key
    "ComponentAddress": "component_tdx_2_1..." // ← Component address from Step 3
}
```

---

## Step 5: Test (30-60 minutes)

```csharp
var provider = new RadixOASIS(config);
await provider.ActivateProviderAsync();

// Test Save
var avatar = new Avatar { /* ... */ };
await provider.SaveAvatarAsync(avatar);

// Test Load
var loaded = await provider.LoadAvatarAsync(avatar.Id);

// Verify on Explorer: https://stokenet-dashboard.radixdlt.com/
```

---

## Need Help?

- **Can't find instantiation option?** → Let me know, I'll create a transaction manifest
- **Component address not showing?** → Check transaction receipt/logs
- **Provider activation fails?** → Verify addresses and network match

---

**You're almost there!** 🚀



