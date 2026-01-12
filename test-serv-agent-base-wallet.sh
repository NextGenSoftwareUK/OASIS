#!/bin/bash

# SERV Agent Base Wallet Creation - Using Keys API (Following Solana Pattern)
# This follows the correct order: generate keypair, link public key first, link private key second

API_URL="https://localhost:5004"
ADMIN_USER="OASIS_ADMIN"
ADMIN_PASS="Uppermall1!"

echo "=== SERV Agent Base Wallet Creation Test ==="
echo ""

# Step 1: Authenticate
echo "1. Authenticating..."
AUTH_RESPONSE=$(curl -s -k -X POST "$API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$ADMIN_USER\",\"password\":\"$ADMIN_PASS\"}")

TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"jwtToken":"[^"]*"' | cut -d'"' -f4)
AVATAR_ID=$(echo "$AUTH_RESPONSE" | python3 -c "import sys, json; d=json.load(sys.stdin); print(d.get('result', {}).get('result', {}).get('id', ''))" 2>/dev/null)

if [ -z "$TOKEN" ]; then
  echo "❌ Authentication failed"
  exit 1
fi
echo "✅ Authenticated"
if [ -n "$AVATAR_ID" ]; then
  echo "   Avatar ID: $AVATAR_ID"
fi
echo ""

# Step 2: Register and activate Base provider
echo "2. Registering Base provider..."
curl -s -k -X POST "$API_URL/api/provider/register-provider-type/6" \
  -H "Authorization: Bearer $TOKEN" > /dev/null
echo "✅ Registered"

echo "3. Activating Base provider..."
curl -s -k -X POST "$API_URL/api/provider/activate-provider/6" \
  -H "Authorization: Bearer $TOKEN" > /dev/null
echo "✅ Activated"
echo ""

# Step 3: Generate Base keypair
echo "4. Generating Base keypair..."
KEYPAIR_RESPONSE=$(curl -s -k -X POST "$API_URL/api/keys/generate_keypair_with_wallet_address_for_provider/BaseOASIS" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN")

echo "$KEYPAIR_RESPONSE" | python3 -m json.tool | head -15

PRIVATE_KEY=$(echo "$KEYPAIR_RESPONSE" | python3 -c "import sys, json; d=json.load(sys.stdin); print(d.get('result', {}).get('privateKey', ''))" 2>/dev/null)
PUBLIC_KEY=$(echo "$KEYPAIR_RESPONSE" | python3 -c "import sys, json; d=json.load(sys.stdin); print(d.get('result', {}).get('publicKey', ''))" 2>/dev/null)
WALLET_ADDRESS=$(echo "$KEYPAIR_RESPONSE" | python3 -c "import sys, json; d=json.load(sys.stdin); r=d.get('result', {}); print(r.get('walletAddress') or r.get('walletAddressLegacy', ''))" 2>/dev/null)

if [ -z "$PUBLIC_KEY" ] || [ -z "$WALLET_ADDRESS" ]; then
  echo "❌ Key generation failed"
  exit 1
fi
echo "✅ Keypair generated"
echo "   Public Key: $PUBLIC_KEY"
echo "   Wallet Address: $WALLET_ADDRESS"
echo ""

# Step 4: Link public key FIRST (creates wallet)
echo "5. Linking public key (creates wallet)..."
if [ -z "$AVATAR_ID" ]; then
  echo "   Using username instead of ID"
  LINK_PUBLIC_RESPONSE=$(curl -s -k -X POST "$API_URL/api/keys/link_provider_public_key_to_avatar_by_username" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d "{
      \"Username\": \"$ADMIN_USER\",
      \"ProviderType\": \"BaseOASIS\",
      \"ProviderKey\": \"$PUBLIC_KEY\",
      \"WalletAddress\": \"$WALLET_ADDRESS\"
    }")
else
  LINK_PUBLIC_RESPONSE=$(curl -s -k -X POST "$API_URL/api/keys/link_provider_public_key_to_avatar_by_id" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d "{
      \"AvatarID\": \"$AVATAR_ID\",
      \"ProviderType\": \"BaseOASIS\",
      \"ProviderKey\": \"$PUBLIC_KEY\",
      \"WalletAddress\": \"$WALLET_ADDRESS\"
    }")
fi

echo "$LINK_PUBLIC_RESPONSE" | python3 -m json.tool | head -15

WALLET_ID=$(echo "$LINK_PUBLIC_RESPONSE" | python3 -c "import sys, json; d=json.load(sys.stdin); r=d.get('result', {}); print(r.get('walletId') or r.get('id', ''))" 2>/dev/null)

if [ -z "$WALLET_ID" ]; then
  echo "❌ Public key linking failed"
  echo "Full response:"
  echo "$LINK_PUBLIC_RESPONSE"
  exit 1
fi
echo "✅ Public key linked"
echo "   Wallet ID: $WALLET_ID"
echo ""

# Step 5: Link private key SECOND (completes wallet)
echo "6. Linking private key (completes wallet)..."
if [ -z "$AVATAR_ID" ]; then
  LINK_PRIVATE_RESPONSE=$(curl -s -k -X POST "$API_URL/api/keys/link_provider_private_key_to_avatar_by_username" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d "{
      \"WalletId\": \"$WALLET_ID\",
      \"Username\": \"$ADMIN_USER\",
      \"ProviderType\": \"BaseOASIS\",
      \"ProviderKey\": \"$PRIVATE_KEY\"
    }")
else
  LINK_PRIVATE_RESPONSE=$(curl -s -k -X POST "$API_URL/api/keys/link_provider_private_key_to_avatar_by_id" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d "{
      \"WalletId\": \"$WALLET_ID\",
      \"AvatarID\": \"$AVATAR_ID\",
      \"ProviderType\": \"BaseOASIS\",
      \"ProviderKey\": \"$PRIVATE_KEY\"
    }")
fi

echo "$LINK_PRIVATE_RESPONSE" | python3 -m json.tool | head -10

if echo "$LINK_PRIVATE_RESPONSE" | python3 -c "import sys, json; d=json.load(sys.stdin); exit(0 if not d.get('isError') else 1)" 2>/dev/null; then
  echo "✅ Private key linked"
else
  echo "❌ Private key linking failed"
  echo "Full response:"
  echo "$LINK_PRIVATE_RESPONSE"
  exit 1
fi
echo ""

# Step 6: Check SERV balance
echo "7. Checking SERV token balance..."
BALANCE_RESPONSE=$(curl -s -k -X GET "$API_URL/api/a2a/serv/balance" \
  -H "Authorization: Bearer $TOKEN")

echo "$BALANCE_RESPONSE" | python3 -m json.tool
echo ""

echo "=== Test Complete ==="
echo "Summary:"
echo "  ✅ Base wallet created successfully"
echo "  ✅ Wallet Address: $WALLET_ADDRESS"
echo "  ✅ Wallet ID: $WALLET_ID"
echo ""
echo "Next steps:"
echo "  - Fund the wallet with SERV tokens on Base"
echo "  - Test SERV payment to another agent using:"
echo "    POST /api/a2a/serv/payment"
echo "    {"
echo "      \"toAgentId\": \"<target-agent-id>\","
echo "      \"amount\": 10.5,"
echo "      \"description\": \"Payment for service\","
echo "      \"autoExecute\": true"
echo "    }"
