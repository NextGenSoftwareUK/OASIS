#!/bin/bash

echo "=========================================="
echo "TESTING OASIS BRIDGE CLI FUNCTIONALITY"
echo "=========================================="
echo ""

cd "$(dirname "$0")"

echo "1️⃣  Testing Solana Connection..."
echo ""

# Create a simple test that creates a wallet
dotnet run <<EOF
1
0
EOF

echo ""
echo "=========================================="
echo "TEST COMPLETE"
echo "=========================================="

