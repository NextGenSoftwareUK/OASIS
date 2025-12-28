# Fixing "Send to Radix Wallet" Issue

## The Problem

You're trying to deploy but:
- Need to define owner role/resource address
- Can't send transaction to Radix Wallet

## Solutions

### Step 1: Fix Wallet Connection

**The "Send to Radix Wallet" button requires:**
1. ✅ Radix Connector extension installed
2. ✅ Wallet linked to connector  
3. ✅ Wallet connected to Developer Console

**To fix**:

1. **Install Radix Connector** (if not installed):
   ```
   https://chrome.google.com/webstore/detail/radix-wallet-connector/bfeplaecgkoeckiidkgkmlllfbaeplgm
   ```

2. **Link Your Wallet**:
   - Open Radix Wallet mobile app
   - Settings → Linked Connectors
   - Tap "Link New Connector"
   - Scan QR code from browser extension popup
   - Confirm link

3. **Connect in Developer Console**:
   - Click "Connect" button (top right)
   - Approve in Radix Wallet app
   - Make sure both are on **Stokenet** network

### Step 2: Owner Role Configuration

Since your component uses `OwnerRole::None`:

**For Package Deployment**:
- **Owner Role**: Use your account address OR leave empty (if allowed)
  - Your Stokenet account: `account_tdx_2_1...`
- **Resource Address**: Leave empty (not needed for packages)

**The component itself is ownerless** - this setting is just for the package deployment.

### Step 3: Alternative - Use Transaction Manifest

If Developer Console deployment continues to have issues:

1. Use "Send Raw Transaction" tab
2. Create a transaction manifest manually
3. This bypasses the Developer Console UI limitations

---

## What Each Field Means

### Owner Role (Package Deployment)
- Controls who can upgrade the package code
- Can be your account address (you own it)
- Can be empty/None (no owner, immutable package)
- **Recommendation**: Use your account address for flexibility

### Resource Address
- Only needed if deploying fungible/non-fungible resources
- **Not needed** for component packages
- Leave empty

---

## Quick Fix Checklist

- [ ] Install Radix Connector extension
- [ ] Link wallet to connector (scan QR)
- [ ] Connect wallet in Developer Console
- [ ] Verify both on Stokenet network
- [ ] Try deployment again
- [ ] Use account address for owner role if required
- [ ] Leave resource address empty

---

## If Still Not Working

**Option A: Check Extension**
- Open browser DevTools (F12)
- Check Console tab for errors
- Verify extension is enabled

**Option B: Use Transaction Manifest**
- I can help create a transaction manifest
- Deploy directly without Developer Console UI

Let me know what specific error you see when trying to connect/send!



