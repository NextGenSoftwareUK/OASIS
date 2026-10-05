# macOS Build Workarounds for Scrypto

## Problem

The build fails because `blst` (a cryptographic library) requires a C compiler that can target WebAssembly, but Apple's clang doesn't support WASM targets by default.

## Solution Options (Ranked by Reliability)

### Option 1: Use Docker Scrypto Builder (RECOMMENDED) ✅

This is the **official recommended approach** by Radix for consistent builds.

#### Prerequisites
- Docker installed and running

#### Steps

1. **Pull the Scrypto Builder Docker image** (version should match your Scrypto version 1.3.0):
   ```bash
   DOCKER_DEFAULT_PLATFORM=linux/amd64 docker pull radixdlt/scrypto-builder:v1.3.0
   ```

2. **Build using Docker**:
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts
   
   DOCKER_DEFAULT_PLATFORM=linux/amd64 docker run \
     -v "$(pwd):/src" \
     -w /src \
     radixdlt/scrypto-builder:v1.3.0 \
     scrypto build
   ```

3. **Find the WASM file**:
   The WASM file will be created at: `target/wasm32-unknown-unknown/release/oasis_storage.wasm`

**Pros**: Official method, consistent builds, no macOS-specific issues
**Cons**: Requires Docker

---

### Option 2: Install WASI SDK (Good Alternative) ⚠️

Provides a WASM-compatible C compiler toolchain.

#### Steps

1. **Download and Install WASI SDK**:
   ```bash
   # Download WASI SDK
   cd ~/Downloads
   curl -LO https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-20/wasi-sdk-20.0-macos.tar.gz
   
   # Extract
   tar -xvf wasi-sdk-20.0-macos.tar.gz
   
   # Move to a permanent location (requires sudo)
   sudo mv wasi-sdk-20.0 /opt/wasi-sdk
   ```

2. **Set Environment Variables** (add to `~/.zshrc` or `~/.bash_profile`):
   ```bash
   export WASI_SDK_PATH="/opt/wasi-sdk"
   export CC="$WASI_SDK_PATH/bin/clang"
   export CXX="$WASI_SDK_PATH/bin/clang++"
   export AR="$WASI_SDK_PATH/bin/llvm-ar"
   export CFLAGS="--sysroot=$WASI_SDK_PATH/share/wasi-sysroot"
   export CXXFLAGS="--sysroot=$WASI_SDK_PATH/share/wasi-sysroot"
   ```

3. **Apply changes**:
   ```bash
   source ~/.zshrc  # or source ~/.bash_profile
   ```

4. **Try building again**:
   ```bash
   cd contracts
   scrypto build
   ```

**Pros**: Local build, no Docker needed
**Cons**: More setup, may still have compatibility issues

---

### Option 3: Use Emscripten (Alternative) ⚠️

Another WASM compiler option.

#### Steps

1. **Install Emscripten**:
   ```bash
   # Clone emsdk
   git clone https://github.com/emscripten-core/emsdk.git ~/emsdk
   cd ~/emsdk
   
   # Install latest SDK
   ./emsdk install latest
   ./emsdk activate latest
   
   # Activate in current shell
   source ./emsdk_env.sh
   ```

2. **Configure build to use Emscripten**:
   ```bash
   export CC=emcc
   export CXX=em++
   ```

3. **Try building**:
   ```bash
   cd contracts
   scrypto build
   ```

**Pros**: Full WASM toolchain
**Cons**: Large installation, may be overkill

---

### Option 4: Build on Linux (If Available)

If you have access to a Linux machine (physical, VM, or cloud instance):

```bash
# On Linux machine
cd contracts
scrypto build
```

Then copy the WASM file back to macOS.

**Pros**: Native build, no workarounds needed
**Cons**: Requires Linux environment

---

## Quick Test: Try Docker First

The Docker approach is the most reliable. Let's try it:

```bash
# Check if Docker is installed
docker --version

# If Docker is available, try:
cd /Volumes/Storage/OASIS_CLEAN/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts

# Pull the builder (check version - might be v1.3.0 or different)
DOCKER_DEFAULT_PLATFORM=linux/amd64 docker pull radixdlt/scrypto-builder:v1.3.0

# Build
DOCKER_DEFAULT_PLATFORM=linux/amd64 docker run \
  -v "$(pwd):/src" \
  -w /src \
  radixdlt/scrypto-builder:v1.3.0 \
  scrypto build
```

---

## Verifying the Build

After successful build, verify the WASM file exists:

```bash
ls -lh target/wasm32-unknown-unknown/release/oasis_storage.wasm
```

The file should be several MB in size.

---

## Next Steps After Successful Build

Once you have the WASM file:

1. **Deploy via Radix Wallet Developer Console**:
   - Go to: https://console.radixdlt.com/deploy-package
   - Connect your wallet
   - Upload the WASM file
   - Get package address

2. **Instantiate Component**:
   - Use package address to instantiate `OasisStorage` component
   - Get component address

3. **Update Configuration**:
   - Update `OASIS_DNA.json` with component address

See `STEP_BY_STEP_WALLET_DEPLOYMENT.md` for full deployment instructions.

---

## References

- Scrypto Builder: https://docs.radixdlt.com/docs/productionize-your-code
- WASI SDK: https://github.com/WebAssembly/wasi-sdk
- Emscripten: https://emscripten.org/docs/getting_started/downloads.html

