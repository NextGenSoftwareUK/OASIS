#!/bin/bash

# Simple OASIS API Authentication Helper
# Usage: source ./auth-oasis.sh
# Or: ./auth-oasis.sh (will export token to file)

API_URL="${NEXT_PUBLIC_OASIS_API_URL:-https://localhost:5004}"
TOKEN_FILE="${HOME}/.oasis_token"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🔐 OASIS API Authentication${NC}"
echo "================================"
echo ""

# Check if API is running
if ! curl -k -s "$API_URL/api/avatar/health" > /dev/null 2>&1; then
    echo -e "${RED}❌ API is not running at $API_URL${NC}"
    echo "Please start the API first."
    exit 1
fi

echo -e "${GREEN}✅ API is running${NC}"
echo ""

# Get credentials
read -p "Username: " USERNAME
read -s -p "Password: " PASSWORD
echo ""
echo ""

echo "🔑 Authenticating..."
AUTH_RESPONSE=$(curl -k -s -X POST "$API_URL/api/avatar/authenticate" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

# Try multiple possible response formats
TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.jwtToken // .result.jwtToken // .result.token // .jwtToken // .token // empty' 2>/dev/null)

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ] || [ "$TOKEN" = "" ]; then
    echo -e "${RED}❌ Authentication failed${NC}"
    echo ""
    echo "Response:"
    echo "$AUTH_RESPONSE" | jq '.' 2>/dev/null || echo "$AUTH_RESPONSE"
    exit 1
fi

echo -e "${GREEN}✅ Authentication successful!${NC}"
echo ""

# Extract avatar ID for verification
AVATAR_ID=$(echo "$TOKEN" | cut -d'.' -f2 | base64 -d 2>/dev/null | jq -r '.id // empty' 2>/dev/null || echo "")

# Add padding if needed
if [ -n "$AVATAR_ID" ]; then
    echo -e "${BLUE}Avatar ID: $AVATAR_ID${NC}"
fi

echo ""
echo -e "${GREEN}Token saved!${NC}"
echo ""

# Export token
export OASIS_TOKEN="$TOKEN"

# Save to file for persistence
echo "$TOKEN" > "$TOKEN_FILE"
chmod 600 "$TOKEN_FILE"

echo "📝 Token exported to:"
echo "   - Environment variable: OASIS_TOKEN"
echo "   - File: $TOKEN_FILE"
echo ""
echo "💡 To use in this session:"
echo "   export OASIS_TOKEN=\"$TOKEN\""
echo ""
echo "💡 To use in other sessions:"
echo "   source $TOKEN_FILE"
echo "   # or"
echo "   export OASIS_TOKEN=\$(cat $TOKEN_FILE)"
echo ""

# Test the token
echo "🧪 Testing token..."
TEST_RESPONSE=$(curl -k -s -X GET "$API_URL/api/avatar" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" 2>&1)

if echo "$TEST_RESPONSE" | jq -e '.isError == false' > /dev/null 2>&1 || echo "$TEST_RESPONSE" | grep -q "id"; then
    echo -e "${GREEN}✅ Token is valid and working!${NC}"
else
    echo -e "${YELLOW}⚠️  Token may not be working correctly${NC}"
    echo "Response: $TEST_RESPONSE" | head -3
fi

echo ""
echo -e "${GREEN}Ready to use!${NC}"
echo ""
echo "You can now run:"
echo "  ./test-wallet-addresses.sh"
echo "  ./test-address-derivation.sh"
echo "  or any other script that uses OASIS_TOKEN"

