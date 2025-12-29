#!/bin/bash

# Test script for wallet generation using Keys API
# This script tests the wallet generation endpoint

set -e

BASE_URL="${1:-https://pangea-production-128d.up.railway.app/api}"
PROVIDER_TYPE="${2:-SolanaOASIS}"

echo "🧪 Testing Wallet Generation"
echo "Base URL: $BASE_URL"
echo "Provider Type: $PROVIDER_TYPE"
echo ""

# Step 1: Login with OASIS_ADMIN to get JWT token and avatar ID
echo "Step 1: Logging in with OASIS_ADMIN..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "OASIS_ADMIN",
    "password": "Uppermall1!"
  }')

echo "Login response: $LOGIN_RESPONSE"
echo ""

# Extract token and avatar ID
TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token // empty')
AVATAR_ID=$(echo $LOGIN_RESPONSE | jq -r '.user.avatarId // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
  echo "❌ Failed to get token from login response"
  echo "Response: $LOGIN_RESPONSE"
  exit 1
fi

if [ -z "$AVATAR_ID" ] || [ "$AVATAR_ID" = "null" ]; then
  echo "❌ Failed to get avatarId from login response"
  echo "Response: $LOGIN_RESPONSE"
  exit 1
fi

echo "✅ Login successful"
echo "Token: ${TOKEN:0:50}..."
echo "Avatar ID: $AVATAR_ID"
echo ""

# Step 2: Generate wallet
echo "Step 2: Generating wallet..."
WALLET_RESPONSE=$(curl -s -X POST "$BASE_URL/wallet/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{
    \"providerType\": \"$PROVIDER_TYPE\",
    \"setAsDefault\": true
  }")

echo "Wallet generation response:"
echo "$WALLET_RESPONSE" | jq '.'
echo ""

# Check if wallet generation was successful
WALLET_ID=$(echo $WALLET_RESPONSE | jq -r '.wallet.walletId // empty')
if [ -z "$WALLET_ID" ] || [ "$WALLET_ID" = "null" ]; then
  echo "❌ Wallet generation failed"
  exit 1
fi

echo "✅ Wallet generated successfully!"
echo "Wallet ID: $WALLET_ID"
WALLET_ADDRESS=$(echo $WALLET_RESPONSE | jq -r '.wallet.walletAddress')
echo "Wallet Address: $WALLET_ADDRESS"
echo ""

# Step 3: Verify wallet exists by getting balances
echo "Step 3: Verifying wallet by fetching balances..."
BALANCE_RESPONSE=$(curl -s -X GET "$BASE_URL/wallet/balance" \
  -H "Authorization: Bearer $TOKEN")

echo "Balance response:"
echo "$BALANCE_RESPONSE" | jq '.'
echo ""

echo "✅ Test completed successfully!"



