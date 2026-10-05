# GitHub Actions Build Guide

## Overview

This guide explains how to use GitHub Actions to build the Scrypto package and get the WASM file for deployment.

## Quick Start

### Step 1: Push Code to GitHub

If you haven't already, commit and push your code to GitHub:

```bash
cd /Volumes/Storage/OASIS_CLEAN

# Add the workflow file
git add .github/workflows/build-scrypto.yml

# Commit
git commit -m "Add GitHub Actions workflow for building Scrypto package"

# Push to GitHub
git push origin <your-branch>
```

### Step 2: Trigger the Workflow

The workflow can be triggered in two ways:

#### Option A: Manual Trigger (Recommended for first time)

1. Go to your GitHub repository
2. Click on **Actions** tab
3. Select **Build Scrypto Package** workflow
4. Click **Run workflow** button (top right)
5. Select your branch
6. Click **Run workflow**

#### Option B: Automatic Trigger

The workflow will automatically run when:
- You push changes to files in `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts/**`
- You open a pull request that modifies contract files

### Step 3: Monitor the Build

1. Click on the running workflow to see progress
2. Wait for all steps to complete (usually 5-15 minutes)
3. Check that the build succeeded (green checkmark)

### Step 4: Download the WASM File

1. After the workflow completes successfully, scroll to the bottom
2. Find the **Artifacts** section
3. Click on **oasis-storage-wasm** artifact
4. Download the file

The downloaded file will be named `oasis_storage.wasm` - this is your compiled package!

### Step 5: Deploy to Radix

Now you can deploy using the Radix Wallet Developer Console:

1. **Go to Developer Console**: https://console.radixdlt.com/deploy-package
2. **Connect your Radix Wallet**
3. **Upload the WASM file** you just downloaded
4. **Get package address** from the transaction receipt
5. **Instantiate component** to get component address
6. **Update OASIS_DNA.json** with the component address

See `STEP_BY_STEP_WALLET_DEPLOYMENT.md` for detailed deployment instructions.

---

## Workflow Details

### What the Workflow Does

1. **Checkout code**: Gets your repository code
2. **Install Rust**: Sets up Rust toolchain with WASM target
3. **Cache dependencies**: Speeds up subsequent builds
4. **Install Scrypto CLI**: Installs radix-clis tools
5. **Build package**: Runs `scrypto build`
6. **Verify WASM file**: Checks that the file was created
7. **Upload artifact**: Makes the WASM file downloadable

### Build Time

- First run: ~10-15 minutes (installs dependencies)
- Subsequent runs: ~5-10 minutes (uses cached dependencies)

### Artifact Retention

The WASM artifact is retained for **30 days** after the workflow run.

---

## Troubleshooting

### Workflow Fails to Start

**Issue**: Workflow doesn't appear in Actions tab
**Solution**: 
- Ensure `.github/workflows/build-scrypto.yml` is in your repository
- Make sure it's pushed to GitHub
- Check that the YAML syntax is valid

### Build Fails

**Issue**: Build step fails with errors
**Solution**:
- Check the workflow logs for specific error messages
- Ensure Cargo.toml is valid
- Verify all dependencies are available on crates.io

### Artifact Not Found

**Issue**: Can't download artifact after build completes
**Solution**:
- Make sure the build succeeded (green checkmark)
- Check that the artifact upload step completed
- Artifacts are only available for successful workflow runs

### Slow Build Times

**Issue**: Build takes too long
**Solution**:
- First build is always slower (installing dependencies)
- Subsequent builds use cache and are faster
- Consider using a self-hosted runner if needed

---

## Re-building

To rebuild the package:

1. **Option 1**: Manually trigger the workflow (Actions → Build Scrypto Package → Run workflow)
2. **Option 2**: Make a change to any file in `contracts/` directory and push
3. **Option 3**: Open a pull request with contract changes

---

## Next Steps After Getting WASM

1. ✅ Download WASM file from GitHub Actions artifact
2. ⏳ Deploy to Stokenet via Developer Console
3. ⏳ Get component address
4. ⏳ Update OASIS_DNA.json
5. ⏳ Test the integration

---

## Additional Resources

- GitHub Actions Documentation: https://docs.github.com/en/actions
- Radix Developer Console: https://console.radixdlt.com/
- Scrypto Documentation: https://docs.radixdlt.com/docs/scrypto-1

