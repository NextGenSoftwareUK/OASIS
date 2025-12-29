# Deploy Package: Owner Role Configuration

## Important: Your Component Uses `OwnerRole::None`

Looking at your Scrypto code, the component is configured with:

```rust
.prepare_to_globalize(OwnerRole::None)
.globalize()
```

This means **no owner role is required** when deploying the package. The component itself is ownerless when instantiated.

---

## Understanding Owner Role

### For Package Deployment:
- **Owner Role**: Controls who can update/upgrade the package itself
- **Resource Address**: Not typically required for package deployment
- **Your Case**: Package deployment may ask for owner role, but since your component uses `OwnerRole::None`, you can often:
  - Leave it blank/empty
  - Or select "None" if available
  - Or use your account address if required

### For Component Instantiation:
- Your component uses `OwnerRole::None` - so **no owner role needed** when instantiating
- The component will be completely ownerless (anyone can call its methods)

---

## Troubleshooting "Can't Send to Radix Wallet"

### Issue 1: Wallet Not Connected

**Symptoms**: "Please connect your Radix Wallet to get started"

**Solution**:
1. **Install Radix Connector Extension** (if not installed):
   - Chrome: https://chrome.google.com/webstore/detail/radix-wallet-connector/bfeplaecgkoeckiidkgkmlllfbaeplgm
   - Install and refresh the page

2. **Link Wallet to Connector**:
   - Open Radix Wallet mobile app
   - Go to Settings → Linked Connectors
   - Tap "Link New Connector"
   - Scan QR code from browser extension
   - Or use the linking feature in the extension

3. **Connect to Developer Console**:
   - Click "Connect" button in Developer Console
   - Approve connection in Radix Wallet app
   - Ensure you're on **Stokenet** network (not Mainnet)

### Issue 2: Extension Not Working

**Check**:
- Is Radix Connector extension installed?
- Is it enabled in browser?
- Try refreshing the page
- Try a different browser
- Check browser console for errors

### Issue 3: Network Mismatch

**Check**:
- Developer Console should be on **Stokenet** (testnet)
- Radix Wallet app should also be on **Stokenet**
- Both must match for connection to work

---

## Alternative: Use Transaction Manifest

If the Developer Console deployment doesn't work, you can use the transaction manifest approach:

### Step 1: Create Transaction Manifest

Use "Send Raw Transaction" tab to create a manifest for package deployment.

### Step 2: Deploy Package

The manifest will handle the package deployment transaction.

---

## Owner Role Options When Deploying Package

When the deployment form asks for owner role/resource address:

### Option 1: Leave Empty (If Allowed)
- Some deployments allow empty owner role
- Package will be deployed without owner

### Option 2: Use Your Account Address
- If owner role is required, use your Stokenet account address
- Format: `account_tdx_2_1...`
- This makes you the package owner (you can upgrade it later)

### Option 3: Use Resource Address (If Required)
- This is usually for fungible/non-fungible resources
- **Not needed for your component** since it's ownerless
- If required, you can create a placeholder or use XRD

---

## Quick Checklist

Before deploying:
- [ ] Radix Connector extension installed
- [ ] Wallet linked to connector
- [ ] Wallet on Stokenet network
- [ ] Developer Console on Stokenet
- [ ] WASM file downloaded from GitHub Actions
- [ ] Have Stokenet account with XRD for fees

During deployment:
- [ ] Connect wallet successfully
- [ ] Upload WASM file
- [ ] Owner role: Leave empty OR use account address
- [ ] Resource address: Not needed (leave empty if asked)
- [ ] Sign transaction in wallet
- [ ] Copy package address from receipt

---

## Need More Help?

If you're still stuck:
1. **Check browser console** for errors (F12 → Console tab)
2. **Verify Radix Connector** is working (check extension settings)
3. **Try different browser** (Chrome usually works best)
4. **Check wallet connection** in Radix Wallet app

Let me know what specific error message you're seeing and I can help troubleshoot further!



