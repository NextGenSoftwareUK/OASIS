# Deployment Next Steps - WASM Build Successful! ✅

## Step 1: Download the WASM File (2 minutes)

1. Go to your GitHub repository
2. Click **Actions** tab
3. Click on the latest successful workflow run
4. Scroll down to the **Artifacts** section
5. Download **`oasis-storage-wasm`**
6. Extract the ZIP file - you'll find `oasis_storage.wasm`

**Location on your Mac**: Save it somewhere accessible, like `~/Downloads/oasis_storage.wasm`

---

## Step 2: Deploy Package to Stokenet (10-15 minutes)

### 2.1 Open Developer Console

1. Go to: **https://console.radixdlt.com/deploy-package**
2. Make sure you're on **Stokenet** network (not Mainnet)

### 2.2 Connect Your Radix Wallet

1. Click the **"Connect"** button (top right)
2. A QR code or connection prompt will appear
3. Open your **Radix Wallet** mobile app
4. Switch to **Stokenet** network in the app
5. Scan the QR code or approve the connection

### 2.3 Upload and Deploy Package

1. Once connected, you should see a file upload interface
2. Click **"Choose File"** or drag-and-drop
3. Select the `oasis_storage.wasm` file you downloaded
4. Review the transaction details
5. **Sign the transaction** using your Radix Wallet
6. Wait for transaction confirmation (usually 20-30 seconds)

### 2.4 Get Package Address

After the transaction confirms:

1. Look for the **Package Address** in the transaction receipt or success message
2. Format: `package_tdx_2_1...` or `package_rdx1...`
3. **Copy and save this address!** You'll need it for the next step

---

## Step 3: Instantiate Component (5 minutes)

### Option A: Using Developer Console (If Available)

1. In Developer Console, look for "Call Function" or "Instantiate Component"
2. Fill in:
   - **Package Address**: The address from Step 2.4
   - **Blueprint Name**: `OasisStorage` (exact name, case-sensitive)
   - **Function Name**: `instantiate`
   - **Arguments**: (leave empty - no arguments needed)
3. Sign and submit the transaction
4. Get the **Component Address** from the receipt

### Option B: Using Transaction Manifest

If instantiation isn't available in console:

1. Go to: **https://console.radixdlt.com/transaction-manifest**
2. You'll need to create a transaction manifest to call the instantiate function
3. This requires more technical knowledge - let me know if you need help

### Option C: Ask for Help

If instantiation is complex, you could:
- Check Radix documentation for component instantiation
- Ask in Radix community forums
- I can help create the transaction manifest if needed

### 3.1 Get Component Address

After instantiation succeeds:
- Look for **Component Address** in transaction receipt
- Format: `component_tdx_2_1...` or `component_rdx1...`
- **This is what you need for configuration!**

---

## Step 4: Update Configuration (2 minutes)

1. Open `/Volumes/Storage/OASIS_CLEAN/OASIS_DNA.json`
2. Find the `RadixOASIS` section (around line 266)
3. Update with your component address:

```json
"RadixOASIS": {
    "HostUri": "https://stokenet.radixdlt.com",
    "NetworkId": 2,
    "AccountAddress": "account_tdx_2_1...",  // Your Stokenet account
    "PrivateKey": "your_private_key_here",   // Your account private key
    "ComponentAddress": "component_tdx_2_1..." // ← Component address from Step 3.1
}
```

**Important**: 
- Ensure `AccountAddress` and `PrivateKey` are set (needed for transactions)
- The `ComponentAddress` is what connects the provider to your deployed component

---

## Step 5: Test the Integration (30-60 minutes)

### 5.1 Basic Provider Test

```csharp
var config = new RadixOASISConfig
{
    HostUri = "https://stokenet.radixdlt.com",
    NetworkId = 2,
    AccountAddress = "your_account_address",
    PrivateKey = "your_private_key",
    ComponentAddress = "your_component_address"  // From Step 3.1
};

var provider = new RadixOASIS(config);
var result = await provider.ActivateProviderAsync();

if (result.IsError)
{
    Console.WriteLine($"❌ Error: {result.Message}");
}
else
{
    Console.WriteLine("✅ Provider activated successfully!");
}
```

### 5.2 Test SaveAvatar

```csharp
var avatar = new Avatar 
{ 
    Id = Guid.NewGuid(),
    Username = "testuser",
    Email = "test@example.com",
    // ... other properties
};

var saveResult = await provider.SaveAvatarAsync(avatar);

if (saveResult.IsError)
{
    Console.WriteLine($"❌ Save failed: {saveResult.Message}");
}
else
{
    Console.WriteLine($"✅ Avatar saved! Transaction: {saveResult.Result?.TransactionHash}");
}
```

### 5.3 Test LoadAvatar

```csharp
var loadResult = await provider.LoadAvatarAsync(avatar.Id);

if (loadResult.IsError)
{
    Console.WriteLine($"❌ Load failed: {loadResult.Message}");
}
else if (loadResult.Result == null)
{
    Console.WriteLine("⚠️ Avatar not found");
}
else
{
    Console.WriteLine($"✅ Avatar loaded! Username: {loadResult.Result.Username}");
}
```

### 5.4 Verify on Radix Explorer

1. Go to: **https://stokenet-dashboard.radixdlt.com/**
2. Search for your **component address**
3. View transaction history
4. Check component state (should show KeyValueStore entries)

---

## Troubleshooting

### Issue: Can't find instantiation option in Developer Console

**Solutions**:
- Check if there's a different tab or section for component operations
- Use "Send Raw Transaction" with a transaction manifest
- Check Radix documentation for component instantiation methods
- Let me know and I can help create the transaction manifest

### Issue: Component address not found after instantiation

**Solutions**:
- Check transaction receipt carefully
- Look at transaction output/logs
- Verify transaction succeeded on Radix Explorer
- The address format might vary - check for any `component_` prefix

### Issue: Provider activation fails

**Solutions**:
- Verify component address is correct
- Check you're using Stokenet network (NetworkId: 2)
- Verify account address and private key are correct
- Check that component is actually deployed (verify on Explorer)

---

## Success Criteria

You'll know it's working when:

✅ Provider activates without errors  
✅ SaveAvatar creates a transaction on Radix  
✅ LoadAvatar retrieves the saved data  
✅ Transactions appear on Radix Explorer  
✅ Component state shows stored data  

---

## Need Help?

If you get stuck at any step, let me know:
- Where you are in the process
- What error messages you're seeing
- Screenshots if helpful

I can help troubleshoot or create transaction manifests as needed!



