#!/bin/bash

CLUSTER=${1:-devnet}

echo "🔍 Verifying deployment on $CLUSTER..."
echo ""

DAT_ID=$(solana address -k target/deploy/dat_integration-keypair.json 2>/dev/null)
NFT_ID=$(solana address -k target/deploy/nft_airdrop-keypair.json 2>/dev/null)

if [ -n "$DAT_ID" ]; then
    echo "✅ DAT Integration Program: $DAT_ID"
    solana program show $DAT_ID --url $CLUSTER
    echo ""
else
    echo "❌ DAT Integration not deployed"
fi

if [ -n "$NFT_ID" ]; then
    echo "✅ NFT Airdrop Program: $NFT_ID"
    solana program show $NFT_ID --url $CLUSTER
    echo ""
else
    echo "❌ NFT Airdrop not deployed"
fi

echo "Done!"






