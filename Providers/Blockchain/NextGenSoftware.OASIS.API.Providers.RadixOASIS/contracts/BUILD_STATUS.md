# Build Status and Issues

## Current Status: ⚠️ Build Blocked

### Issue 1: macOS Native Build
- **Problem**: Apple's clang doesn't support WASM targets required by `blst` C library
- **Status**: ❌ Cannot build natively on macOS

### Issue 2: Docker Build  
- **Problem**: Docker image `radixdlt/scrypto-builder:v1.3.0` has Cargo 1.81.0, but dependencies require newer Cargo with `edition2024` support
- **Error**: `feature 'edition2024' is required` but not available in Cargo 1.81.0
- **Status**: ❌ Docker build also fails

## Workarounds to Try

### Option 1: Try Newer Docker Image
Check if there's a newer version of the builder:
```bash
# List available tags (if repository supports it)
docker search radixdlt/scrypto-builder

# Try latest tag
docker pull radixdlt/scrypto-builder:latest
```

### Option 2: Use WASI SDK (Recommended Next Step)
Since Docker has version issues, try the WASI SDK approach:
See `MACOS_BUILD_WORKAROUNDS.md` for instructions

### Option 3: Build on Linux
If you have access to a Linux machine, build there and copy the WASM file back.

### Option 4: Check Dependency Versions
The issue might be that Scrypto 1.3.0 requires a newer builder. Check Radix documentation for the correct builder version match.

## Next Steps

1. Try WASI SDK installation (see `MACOS_BUILD_WORKAROUNDS.md`)
2. Or check for newer Docker builder images
3. Or use a Linux build environment

