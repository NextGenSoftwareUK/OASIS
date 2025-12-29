#!/bin/bash

# Wallet Generation Debug Test Script
# Tests wallet generation with detailed logging

set -e

BASE_URL="${BASE_URL:-https://pangea-production-128d.up.railway.app/api}"
OASIS_ADMIN_EMAIL="${OASIS_ADMIN_EMAIL:-OASIS_ADMIN}"
OASIS_ADMIN_PASSWORD="${OASIS_ADMIN_PASSWORD:-Uppermall1!}"

echo "🧪 Testing Wallet Generation"
echo "============================"
echo "Base URL: $BASE_URL"
echo ""

# Step 1: Login and get token
echo "Step 1: Authenticating..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$OASIS_ADMIN_EMAIL\",\"password\":\"$OASIS_ADMIN_PASSWORD\"}")

TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.token')
USER_DATA=$(echo "$LOGIN_RESPONSE" | jq '.user')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
  echo "❌ Login failed!"
  echo "Response: $LOGIN_RESPONSE"
  exit 1
fi

echo "✅ Login successful"
echo "Token: ${TOKEN:0:50}..."
AVATAR_ID=$(echo "$USER_DATA" | jq -r '.avatarId')
echo "Avatar ID: $AVATAR_ID"
echo ""

# Step 2: Test wallet generation
echo "Step 2: Generating Solana wallet..."
WALLET_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST "$BASE_URL/wallet/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "providerType": "SolanaOASIS",
    "setAsDefault": true
  }')

HTTP_STATUS=$(echo "$WALLET_RESPONSE" | grep "HTTP_STATUS:" | cut -d: -f2)
WALLET_BODY=$(echo "$WALLET_RESPONSE" | sed '/HTTP_STATUS:/d')

echo "HTTP Status: $HTTP_STATUS"
echo "Response:"
echo "$WALLET_BODY" | jq '.' 2>/dev/null || echo "$WALLET_BODY"
echo ""

if [ "$HTTP_STATUS" == "200" ] || [ "$HTTP_STATUS" == "201" ]; then
  echo "✅ Wallet generation successful!"
  WALLET_ID=$(echo "$WALLET_BODY" | jq -r '.wallet.walletId // .walletId')
  WALLET_ADDRESS=$(echo "$WALLET_BODY" | jq -r '.wallet.walletAddress // .walletAddress')
  echo "Wallet ID: $WALLET_ID"
  echo "Wallet Address: $WALLET_ADDRESS"
  
  # Step 3: Test balance retrieval
  echo ""
  echo "Step 3: Retrieving wallet balance..."
  BALANCE_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X GET "$BASE_URL/wallet/balance" \
    -H "Authorization: Bearer $TOKEN")
  
  BALANCE_HTTP_STATUS=$(echo "$BALANCE_RESPONSE" | grep "HTTP_STATUS:" | cut -d: -f2)
  BALANCE_BODY=$(echo "$BALANCE_RESPONSE" | sed '/HTTP_STATUS:/d')
  
  echo "HTTP Status: $BALANCE_HTTP_STATUS"
  echo "Balance Response:"
  echo "$BALANCE_BODY" | jq '.' 2>/dev/null || echo "$BALANCE_BODY"
  
  if [ "$BALANCE_HTTP_STATUS" == "200" ]; then
    echo "✅ Balance retrieval successful!"
  else
    echo "⚠️ Balance retrieval failed"
  fi
else
  echo "❌ Wallet generation failed!"
  echo "Check the logs above for details"
  exit 1
fi

echo ""
echo "✅ All tests completed!"


