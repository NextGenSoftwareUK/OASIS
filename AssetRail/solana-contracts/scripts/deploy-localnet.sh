#!/bin/bash

echo "🚀 Deploying AssetRail Contracts to Localnet..."
echo ""

# Build contracts
echo "📦 Building contracts..."
anchor build

# Deploy to localnet
echo "🌐 Deploying to localnet..."
anchor deploy --provider.cluster localnet

echo ""
echo "✅ Deployment complete!"
echo ""
echo "Program IDs:"
echo "  - DAT Integration: $(solana address -k target/deploy/dat_integration-keypair.json)"
echo "  - NFT Airdrop: $(solana address -k target/deploy/nft_airdrop-keypair.json)"
echo ""






