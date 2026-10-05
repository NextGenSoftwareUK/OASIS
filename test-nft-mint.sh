#!/bin/bash

echo "🔐 Step 1: Authenticating..."
AUTH_RESPONSE=$(curl -k -s -X POST https://localhost:5004/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username":"metabricks_admin","password":"Uppermall1!"}')

echo "$AUTH_RESPONSE" | grep -q "Avatar Successfully Authenticated"
if [ $? -eq 0 ]; then
    echo "✅ Authentication successful!"
else
    echo "❌ Authentication failed!"
    echo "$AUTH_RESPONSE"
    exit 1
fi

# Extract JWT token
JWT_TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"jwtToken":"[^"]*"' | cut -d'"' -f4)
echo "🔑 JWT Token: ${JWT_TOKEN:0:50}..."

echo ""
echo "🎨 Step 2: Minting NFT on Solana..."
NFT_RESPONSE=$(curl -k -s -X POST https://localhost:5004/api/nft/mint-nft \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "Title": "OASIS Test NFT",
    "Description": "Testing multi-chain NFT functionality on Solana",
    "ImageUrl": "https://raw.githubusercontent.com/solana-labs/token-list/main/assets/mainnet/So11111111111111111111111111111111111111112/logo.png",
    "OnChainProvider": "SolanaOASIS",
    "OffChainProvider": "MongoDBOASIS",
    "NFTOffChainMetaType": "OASIS",
    "NFTStandardType": "SPL",
    "NumberToMint": 1,
    "Price": 0.01,
    "Symbol": "OASISNFT",
    "StoreNFTMetaDataOnChain": false,
    "WaitTillNFTMinted": true,
    "WaitForNFTToMintInSeconds": 120
  }')

echo ""
echo "📝 Response:"
echo "$NFT_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$NFT_RESPONSE"

echo ""
echo "$NFT_RESPONSE" | grep -q '"isError":false'
if [ $? -eq 0 ]; then
    echo "✅ NFT minted successfully!"
else
    echo "❌ NFT minting failed!"
fi





