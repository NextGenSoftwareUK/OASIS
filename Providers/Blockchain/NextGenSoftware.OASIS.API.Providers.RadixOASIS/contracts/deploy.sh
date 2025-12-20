#!/bin/bash
# Deployment script for OASIS Storage Scrypto component

set -e

echo "=== OASIS Storage Component Deployment Script ==="
echo ""

# Check if scrypto is installed
if ! command -v scrypto &> /dev/null; then
    echo "ERROR: 'scrypto' command not found!"
    echo ""
    echo "Please install Scrypto CLI tools first:"
    echo "  Visit: https://docs.radixdlt.com/docs/getting-rust-scrypto"
    echo ""
    echo "Or install via:"
    echo "  cargo install --git https://github.com/radixdlt/scrypto.git scrypto"
    echo ""
    exit 1
fi

# Check if we're in the contracts directory
if [ ! -f "Cargo.toml" ] || [ ! -d "src" ]; then
    echo "ERROR: Must run this script from the contracts directory"
    exit 1
fi

echo "Step 1: Building Scrypto package..."
scrypto build

if [ $? -ne 0 ]; then
    echo "ERROR: Build failed!"
    exit 1
fi

echo ""
echo "✓ Build successful!"
echo ""

# Check if we want to deploy to Stokenet or local simulator
DEPLOY_TARGET=${1:-local}

if [ "$DEPLOY_TARGET" == "stokenet" ]; then
    echo "Step 2: Publishing to Stokenet..."
    echo ""
    echo "To publish to Stokenet, you'll need to:"
    echo "1. Have a Stokenet account with XRD balance"
    echo "2. Use Radix Wallet or Gateway API to publish the package"
    echo "3. Package location: target/wasm32-unknown-unknown/release/oasis_storage.wasm"
    echo ""
    echo "For detailed instructions, see: DEPLOYMENT_GUIDE.md"
    echo ""
elif [ "$DEPLOY_TARGET" == "local" ]; then
    echo "Step 2: Publishing to local simulator (resim)..."
    
    if ! command -v resim &> /dev/null; then
        echo "ERROR: 'resim' command not found!"
        echo ""
        echo "To use local simulator, install resim:"
        echo "  Visit: https://docs.radixdlt.com/docs/resim-installation"
        echo ""
        exit 1
    fi
    
    # Check if resim is running
    if ! resim show-ledger &> /dev/null; then
        echo "Starting resim simulator..."
        resim start
        sleep 2
    fi
    
    echo "Publishing package..."
    PACKAGE_OUTPUT=$(resim publish . 2>&1)
    PACKAGE_ADDRESS=$(echo "$PACKAGE_OUTPUT" | grep -oP 'Package:\s+\K[^\s]+' | head -1)
    
    if [ -z "$PACKAGE_ADDRESS" ]; then
        echo "ERROR: Could not extract package address from output"
        echo "Output: $PACKAGE_OUTPUT"
        exit 1
    fi
    
    echo "✓ Package published: $PACKAGE_ADDRESS"
    echo ""
    echo "Step 3: Instantiating component..."
    
    INSTANTIATE_OUTPUT=$(resim call-function "$PACKAGE_ADDRESS" OasisStorage instantiate 2>&1)
    COMPONENT_ADDRESS=$(echo "$INSTANTIATE_OUTPUT" | grep -oP 'Component:\s+\K[^\s]+' | head -1)
    
    if [ -z "$COMPONENT_ADDRESS" ]; then
        echo "ERROR: Could not extract component address from output"
        echo "Output: $INSTANTIATE_OUTPUT"
        exit 1
    fi
    
    echo "✓ Component instantiated: $COMPONENT_ADDRESS"
    echo ""
    echo "=== Deployment Complete ==="
    echo ""
    echo "Package Address: $PACKAGE_ADDRESS"
    echo "Component Address: $COMPONENT_ADDRESS"
    echo ""
    echo "Update your OASIS_DNA.json with:"
    echo "  \"ComponentAddress\": \"$COMPONENT_ADDRESS\""
    echo ""
else
    echo "ERROR: Unknown deployment target: $DEPLOY_TARGET"
    echo "Usage: ./deploy.sh [local|stokenet]"
    exit 1
fi

