# Fixing Scrypto Installation Issue

## Problem

The repository `radixdlt/scrypto` doesn't exist. The correct repository is `radixdlt/radixdlt-scrypto`.

## Solution Options

### Option 1: Use Official Installer (Recommended)

Follow the official Radix documentation:
1. Visit: https://docs.radixdlt.com/docs/getting-rust-scrypto
2. Use the official installer script or method provided there

### Option 2: Install from Correct Repository

If you want to build from source:

```bash
# Clone the correct repository
git clone https://github.com/radixdlt/radixdlt-scrypto.git
cd radixdlt-scrypto

# Build and install (check their README for exact instructions)
cargo build --release
# Then install the binaries as needed
```

### Option 3: Use Resim (Alternative)

`resim` (Radix Engine Simulator) includes scrypto tools. You can:

```bash
# Check if resim is already installed
resim --version

# If not, install resim (it includes scrypto):
# Follow: https://docs.radixdlt.com/docs/resim-installation
```

### Option 4: Build Without CLI Tools (Alternative Approach)

You can actually build Scrypto packages using standard Rust tooling if you have the `scrypto` crate as a dependency (which we do in Cargo.toml). However, you'll still need the CLI tools for deployment.

## Quick Check

Try this to see what's available:

```bash
# Check if resim is installed (includes scrypto)
which resim

# Check if scrypto is available via another method
scrypto --version

# Check Rust and Cargo
rustc --version
cargo --version
```

## Official Documentation Links

- Getting Rust & Scrypto: https://docs.radixdlt.com/docs/getting-rust-scrypto
- Scrypto Repository: https://github.com/radixdlt/radixdlt-scrypto
- Resim Installation: https://docs.radixdlt.com/docs/resim-installation

## For Deployment

If you can't install the CLI tools locally, you can:
1. Build the package using standard Rust (`cargo build --release`)
2. Use Radix Wallet to deploy the WASM file directly
3. Or use a CI/CD pipeline that has the tools installed

See `QUICK_START.md` for deployment alternatives.

