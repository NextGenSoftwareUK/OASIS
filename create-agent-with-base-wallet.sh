#!/bin/bash

# Create Agent Avatar with Base Wallet
# This script creates an agent avatar and equips them with a Base wallet for SERV token payments

API_URL="https://localhost:5004"
ADMIN_USER="OASIS_ADMIN"
ADMIN_PASS="Uppermall1!"

# Agent details (you can customize these)
AGENT_USERNAME="serv_agent_$(date +%s)"
AGENT_EMAIL="serv_agent_$(date +%s)@example.com"
AGENT_PASSWORD="SecurePassword123!"
AGENT_FIRST_NAME="SERV"
AGENT_LAST_NAME="Agent"

echo "=== Creating Agent Avatar with Base Wallet ==="
echo ""

# Step 1: Authenticate as admin
echo "1. Authenticating as admin..."
AUTH_RESPONSE=$(curl -s -k -X POST "$API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$ADMIN_USER\",\"password\":\"$ADMIN_PASS\"}")

ADMIN_TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"jwtToken":"[^"]*"' | cut -d'"' -f4)
ADMIN_AVATAR_ID=$(echo "$AUTH_RESPONSE" | python3 -c "import sys, json; d=json.load(sys.stdin); print(d.get('result', {}).get('result', {}).get('id', ''))" 2>/dev/null)

if [ -z "$ADMIN_TOKEN" ]; then
  echo "❌ Authentication failed"
  exit 1
fi
echo "✅ Authenticated as admin"
if [ -n "$ADMIN_AVATAR_ID" ]; then
  echo "   Admin Avatar ID: $ADMIN_AVATAR_ID"
fi
echo ""

# Step 2: Create agent avatar
echo "2. Creating agent avatar..."
echo "   Username: $AGENT_USERNAME"
echo "   Email: $AGENT_EMAIL"

AGENT_CREATE_RESPONSE=$(curl -s -k -X POST "$API_URL/api/avatar/register" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{
    \"username\": \"$AGENT_USERNAME\",
    \"email\": \"$AGENT_EMAIL\",
    \"password\": \"$AGENT_PASSWORD\",
    \"confirmPassword\": \"$AGENT_PASSWORD\",
    \"firstName\": \"$AGENT_FIRST_NAME\",
    \"lastName\": \"$AGENT_LAST_NAME\",
    \"avatarType\": \"Agent\",
    \"ownerAvatarId\": \"$ADMIN_AVATAR_ID\",
    \"acceptTerms\": true
  }")

echo "$AGENT_CREATE_RESPONSE" | python3 -m json.tool | head -20

AGENT_AVATAR_ID=$(echo "$AGENT_CREATE_RESPONSE" | python3 -c "import sys, json; d=json.load(sys.stdin); r=d.get('result', {}).get('result', {}); print(r.get('avatarId') or r.get('id', ''))" 2>/dev/null)

if [ -z "$AGENT_AVATAR_ID" ]; then
  echo "❌ Agent creation failed"
  echo "Full response:"
  echo "$AGENT_CREATE_RESPONSE"
  exit 1
fi
echo "✅ Agent created"
echo "   Agent Avatar ID: $AGENT_AVATAR_ID"
echo ""

# Step 3: Authenticate as the agent
echo "3. Authenticating as agent..."
AGENT_AUTH_RESPONSE=$(curl -s -k -X POST "$API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$AGENT_USERNAME\",\"password\":\"$AGENT_PASSWORD\"}")

AGENT_TOKEN=$(echo "$AGENT_AUTH_RESPONSE" | grep -o '"jwtToken":"[^"]*"' | cut -d'"' -f4)

if [ -z "$AGENT_TOKEN" ]; then
  echo "❌ Agent authentication failed"
  exit 1
fi
echo "✅ Agent authenticated"
echo ""

# Step 4: Register and activate Base provider
echo "4. Registering Base provider..."
curl -s -k -X POST "$API_URL/api/provider/register-provider-type/6" \
  -H "Authorization: Bearer $AGENT_TOKEN" > /dev/null
echo "✅ Registered"

echo "5. Activating Base provider..."
curl -s -k -X POST "$API_URL/api/provider/activate-provider/6" \
  -H "Authorization: Bearer $AGENT_TOKEN" > /dev/null
echo "✅ Activated"
echo ""

# Step 5: Generate Base keypair
echo "6. Generating Base keypair..."
KEYPAIR_RESPONSE=$(curl -s -k -X POST "$API_URL/api/keys/generate_keypair_with_wallet_address_for_provider/BaseOASIS" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AGENT_TOKEN")

echo "$KEYPAIR_RESPONSE" | python3 -m json.tool | head -10

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

# Step 6: Link public key FIRST (creates wallet)
echo "7. Linking public key (creates wallet)..."
LINK_PUBLIC_RESPONSE=$(curl -s -k -X POST "$API_URL/api/keys/link_provider_public_key_to_avatar_by_id" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AGENT_TOKEN" \
  -d "{
    \"AvatarID\": \"$AGENT_AVATAR_ID\",
    \"ProviderType\": \"BaseOASIS\",
    \"ProviderKey\": \"$PUBLIC_KEY\",
    \"WalletAddress\": \"$WALLET_ADDRESS\"
  }")

echo "$LINK_PUBLIC_RESPONSE" | python3 -m json.tool | head -10

WALLET_ID=$(echo "$LINK_PUBLIC_RESPONSE" | python3 -c "import sys, json; d=json.load(sys.stdin); r=d.get('result', {}); print(r.get('walletId') or r.get('id', ''))" 2>/dev/null)

if [ -z "$WALLET_ID" ]; then
  echo "❌ Public key linking failed"
  exit 1
fi
echo "✅ Public key linked"
echo "   Wallet ID: $WALLET_ID"
echo ""

# Step 7: Link private key SECOND (completes wallet)
echo "8. Linking private key (completes wallet)..."
LINK_PRIVATE_RESPONSE=$(curl -s -k -X POST "$API_URL/api/keys/link_provider_private_key_to_avatar_by_id" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AGENT_TOKEN" \
  -d "{
    \"WalletId\": \"$WALLET_ID\",
    \"AvatarID\": \"$AGENT_AVATAR_ID\",
    \"ProviderType\": \"BaseOASIS\",
    \"ProviderKey\": \"$PRIVATE_KEY\"
  }")

echo "$LINK_PRIVATE_RESPONSE" | python3 -m json.tool | head -10

if echo "$LINK_PRIVATE_RESPONSE" | python3 -c "import sys, json; d=json.load(sys.stdin); exit(0 if not d.get('isError') else 1)" 2>/dev/null; then
  echo "✅ Private key linked"
else
  echo "❌ Private key linking failed"
  exit 1
fi
echo ""

# Step 8: Verify SERV balance endpoint works
echo "9. Testing SERV balance endpoint..."
BALANCE_RESPONSE=$(curl -s -k -X GET "$API_URL/api/a2a/serv/balance" \
  -H "Authorization: Bearer $AGENT_TOKEN")

echo "$BALANCE_RESPONSE" | python3 -m json.tool
echo ""

echo "=== Success! ==="
echo ""
echo "Agent Created:"
echo "  Username: $AGENT_USERNAME"
echo "  Email: $AGENT_EMAIL"
echo "  Password: $AGENT_PASSWORD"
echo "  Avatar ID: $AGENT_AVATAR_ID"
echo ""
echo "Base Wallet:"
echo "  Wallet Address: $WALLET_ADDRESS"
echo "  Wallet ID: $WALLET_ID"
echo ""
echo "Next Steps:"
echo "  1. Fund the wallet with SERV tokens on Base:"
echo "     Address: $WALLET_ADDRESS"
echo "  2. Test SERV payment to another agent:"
echo "     POST /api/a2a/serv/payment"
echo "     {"
echo "       \"toAgentId\": \"<target-agent-id>\","
echo "       \"amount\": 10.5,"
echo "       \"description\": \"Payment for service\","
echo "       \"autoExecute\": true"
echo "     }"
echo ""
echo "Agent Credentials (save these!):"
echo "  Username: $AGENT_USERNAME"
echo "  Password: $AGENT_PASSWORD"
echo "  Avatar ID: $AGENT_AVATAR_ID"
