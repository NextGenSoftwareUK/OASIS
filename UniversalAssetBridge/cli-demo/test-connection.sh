#!/bin/bash

# Simple test script to verify bridge connection
# Tests Solana Devnet connectivity

echo "=========================================="
echo "OASIS BRIDGE - CONNECTION TEST"
echo "=========================================="
echo ""

cd "$(dirname "$0")"

echo "🔧 Building project..."
dotnet build --no-restore > /dev/null 2>&1

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

echo "✅ Build successful"
echo ""
echo "📡 Testing Solana Devnet connection..."
echo ""
echo "Note: This will start the interactive demo."
echo "Select option [2] to check a balance, or [0] to exit."
echo ""
echo "To test with a known address, use:"
echo "  9WzDXwBbmkg8ZTbNMqUxvQRAyrZzDsGYdLVL9zYtAWWM"
echo ""
echo "Starting demo..."
echo ""

dotnet run

