#!/bin/bash

# Authenticate OASIS_ADMIN avatar and get Solana wallet
# This will be used for smart contract operations

set -e

# Configuration
API_URL="${OASIS_API_URL:-http://api.oasisweb4.com}"
USERNAME="OASIS_ADMIN"
PASSWORD="Uppermall1!"

echo "🔐 Authenticating OASIS_ADMIN avatar..."
echo "API URL: $API_URL"
echo ""

# Step 1: Authenticate
AUTH_RESPONSE=$(curl -s -X POST "$API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

# Check if authentication was successful
if echo "$AUTH_RESPONSE" | jq -e '.isError == true' > /dev/null 2>&1; then
    echo "❌ Authentication failed:"
    echo "$AUTH_RESPONSE" | jq -r '.message // .'
    exit 1
fi

# Extract JWT token (handle nested response structure)
TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.result.jwtToken // .result.result.jwtToken // empty')
AVATAR_ID=$(echo "$AUTH_RESPONSE" | jq -r '.result.avatarId // .result.result.avatarId // .result.id // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
    echo "❌ No JWT token received"
    echo "Response:"
    echo "$AUTH_RESPONSE" | jq .
    exit 1
fi

echo "✅ Authentication successful"
echo "👤 Avatar ID: $AVATAR_ID"
echo "🎫 JWT Token: ${TOKEN:0:50}..."
echo ""

# Step 2: Get wallets for the avatar
echo "🔍 Fetching wallets for avatar..."
WALLETS_RESPONSE=$(curl -s -X GET "$API_URL/api/wallet/avatar/$AVATAR_ID/wallets" \
  -H "Authorization: Bearer $TOKEN")

# Check if request was successful
if echo "$WALLETS_RESPONSE" | jq -e '.isError == true' > /dev/null 2>&1; then
    echo "❌ Failed to fetch wallets:"
    echo "$WALLETS_RESPONSE" | jq -r '.message // .'
    exit 1
fi

# Extract wallets array
WALLETS=$(echo "$WALLETS_RESPONSE" | jq -r '.result // .result.result // . // []')

if [ "$WALLETS" == "[]" ] || [ -z "$WALLETS" ]; then
    echo "⚠️  No wallets found for avatar"
    echo "Attempting to generate Solana wallet..."
    
    # Generate Solana wallet
    GENERATE_RESPONSE=$(curl -s -X POST "$API_URL/api/wallet/avatar/$AVATAR_ID/generate" \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d '{"providerType":"SolanaOASIS","setAsDefault":true}')
    
    if echo "$GENERATE_RESPONSE" | jq -e '.isError == true' > /dev/null 2>&1; then
        echo "❌ Failed to generate wallet:"
        echo "$GENERATE_RESPONSE" | jq -r '.message // .'
        exit 1
    fi
    
    echo "✅ Solana wallet generated"
    # Fetch wallets again
    WALLETS_RESPONSE=$(curl -s -X GET "$API_URL/api/wallet/avatar/$AVATAR_ID/wallets" \
      -H "Authorization: Bearer $TOKEN")
    WALLETS=$(echo "$WALLETS_RESPONSE" | jq -r '.result // .result.result // . // []')
fi

# Find Solana wallet
SOLANA_WALLET=$(echo "$WALLETS" | jq -r '.[] | select(.providerType == "SolanaOASIS" or .providerType == "Solana" or .blockchain == "Solana") | .')

if [ -z "$SOLANA_WALLET" ] || [ "$SOLANA_WALLET" == "null" ]; then
    echo "❌ No Solana wallet found"
    echo "Available wallets:"
    echo "$WALLETS" | jq -r '.[] | "  - \(.providerType // .blockchain // "Unknown")"'
    exit 1
fi

# Extract wallet details
WALLET_ADDRESS=$(echo "$SOLANA_WALLET" | jq -r '.address // .publicKey // .walletAddress // empty')
WALLET_ID=$(echo "$SOLANA_WALLET" | jq -r '.id // .walletId // empty')
PROVIDER_TYPE=$(echo "$SOLANA_WALLET" | jq -r '.providerType // "SolanaOASIS"')

echo "✅ Found Solana wallet:"
echo "   Address: $WALLET_ADDRESS"
echo "   Wallet ID: $WALLET_ID"
echo "   Provider: $PROVIDER_TYPE"
echo ""

# Step 3: Request devnet SOL
echo "💰 Requesting devnet SOL for wallet..."
echo "   Wallet: $WALLET_ADDRESS"
echo ""

# Solana devnet faucet URL
FAUCET_URL="https://api.devnet.solana.com"

# Request airdrop (2 SOL for testing)
AIRDROP_RESPONSE=$(curl -s -X POST "$FAUCET_URL" \
  -H "Content-Type: application/json" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"requestAirdrop\",\"params\":[\"$WALLET_ADDRESS\", 2000000000]}")

AIRDROP_SIGNATURE=$(echo "$AIRDROP_RESPONSE" | jq -r '.result // empty')

if [ -z "$AIRDROP_SIGNATURE" ] || [ "$AIRDROP_SIGNATURE" == "null" ]; then
    echo "⚠️  Airdrop request may have failed or is pending"
    echo "Response:"
    echo "$AIRDROP_RESPONSE" | jq .
    echo ""
    echo "You can manually request SOL from:"
    echo "  https://faucet.solana.com/?address=$WALLET_ADDRESS"
else
    echo "✅ Airdrop requested!"
    echo "   Transaction: $AIRDROP_SIGNATURE"
    echo "   Check status: https://explorer.solana.com/tx/$AIRDROP_SIGNATURE?cluster=devnet"
fi

echo ""
echo "========================================"
echo "📋 Summary"
echo "========================================"
echo "Avatar ID: $AVATAR_ID"
echo "Solana Wallet: $WALLET_ADDRESS"
echo "Wallet ID: $WALLET_ID"
echo "JWT Token: ${TOKEN:0:50}..."
echo ""
echo "💾 Save these for smart contract operations:"
echo "export OASIS_AVATAR_ID=\"$AVATAR_ID\""
echo "export OASIS_SOLANA_WALLET=\"$WALLET_ADDRESS\""
echo "export OASIS_WALLET_ID=\"$WALLET_ID\""
echo "export OASIS_JWT_TOKEN=\"$TOKEN\""
echo ""

# Save to file for use in scripts
cat > /tmp/oasis-wallet-config.sh << EOF
#!/bin/bash
# OASIS Avatar Wallet Configuration
# Generated: $(date)

export OASIS_API_URL="$API_URL"
export OASIS_AVATAR_ID="$AVATAR_ID"
export OASIS_SOLANA_WALLET="$WALLET_ADDRESS"
export OASIS_WALLET_ID="$WALLET_ID"
export OASIS_JWT_TOKEN="$TOKEN"
export OASIS_PROVIDER_TYPE="$PROVIDER_TYPE"
EOF

echo "💾 Configuration saved to: /tmp/oasis-wallet-config.sh"
echo "   Source it with: source /tmp/oasis-wallet-config.sh"


