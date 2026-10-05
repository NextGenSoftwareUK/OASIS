# RPD File for Package Deployment

## What is an RPD File?

**RPD** = **Radix Package Descriptor** file. It's a packaged format that contains:
- The compiled WASM file
- Package metadata
- Required for deployment via Radix Wallet Developer Console

## Do We Have One?

**Current Status**: ❌ **No RPD file yet**

We have:
- ✅ WASM file built via GitHub Actions (`oasis_storage.wasm`)
- ❌ RPD file (needs to be created)

## Creating the RPD File

### Option 1: Using GitHub Actions (Recommended)

I've updated the workflow to create the RPD file automatically. The next build will:
1. Build the WASM file
2. Create `oasis_storage.rpd` using `scrypto package publish`
3. Upload both WASM and RPD as artifacts

**To trigger**: Push the workflow changes or manually trigger the workflow in GitHub Actions.

### Option 2: Create Locally (If You Have Scrypto CLI)

If you can get Scrypto CLI working locally:

```bash
cd Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts
scrypto package publish --output oasis_storage.rpd
```

This requires:
- Rust 1.81.0 installed
- Scrypto CLI tools installed (`cargo install radix-clis`)
- Working WASM build

### Option 3: Developer Console Might Accept WASM Directly

**Try this first** before creating RPD:
1. Download the WASM artifact from GitHub Actions
2. Go to https://console.radixdlt.com/deploy-package
3. Try uploading the `.wasm` file directly
4. If it accepts it, great! If not, you'll need the RPD file.

## Current Workflow Status

The GitHub Actions workflow now:
- ✅ Builds WASM file
- 🔄 **Will create RPD file** (after next run)
- ✅ Uploads both as artifacts

## Next Steps

1. **Immediate**: Try uploading WASM file directly to Developer Console
   - If it works: Deploy!
   - If it doesn't: Proceed to step 2

2. **If WASM doesn't work**: Trigger GitHub Actions workflow to create RPD
   - Go to GitHub Actions
   - Run "Build Scrypto Package" workflow
   - Download the artifact (will include both WASM and RPD)
   - Upload RPD file to Developer Console

3. **Deploy**: Once you have the right file, follow deployment steps in `WALLET_CONNECTION_FIX.md`

## File Locations

**After GitHub Actions build**:
- Artifact: `oasis-storage-package`
  - `oasis_storage.wasm`
  - `oasis_storage.rpd` (after workflow update)

**Local (if building locally)**:
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts/target/wasm32-unknown-unknown/release/oasis_storage.wasm`
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts/oasis_storage.rpd`

## Troubleshooting

**If `scrypto package publish` fails**:
- Check that WASM file exists
- Verify Scrypto CLI version: `scrypto --version`
- Try: `scrypto build` first, then `scrypto package publish`

**If Developer Console rejects WASM file**:
- You'll need the RPD file
- Make sure to download the latest artifact from GitHub Actions
- RPD file should be in the same artifact as WASM

Let me know if you want me to trigger a new build to create the RPD file!

