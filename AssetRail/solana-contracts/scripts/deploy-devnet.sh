#!/bin/bash

echo "🚀 Deploying AssetRail Contracts to Devnet..."
echo ""

# Check wallet balance
BALANCE=$(solana balance --url devnet)
echo "Wallet balance: $BALANCE"
echo ""

if (( $(echo "$BALANCE < 5" | bc -l) )); then
    echo "⚠️  Low balance! Requesting airdrop..."
    solana airdrop 2 --url devnet
    sleep 2
fi

# Build contracts
echo "📦 Building contracts..."
anchor build

# Deploy to devnet
echo "🌐 Deploying to devnet..."
anchor deploy --provider.cluster devnet

echo ""
echo "✅ Deployment complete!"
echo ""
echo "Program IDs:"
echo "  - DAT Integration: $(solana address -k target/deploy/dat_integration-keypair.json)"
echo "  - NFT Airdrop: $(solana address -k target/deploy/nft_airdrop-keypair.json)"
echo ""
echo "🔗 Verify on Solana Explorer:"
echo "  https://explorer.solana.com/?cluster=devnet"
echo ""






