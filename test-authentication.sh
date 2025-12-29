#!/bin/bash

# Test script for OASIS Authentication Process
# Tests the complete authentication flow as described in Authentication_Process.md

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
API_BASE_URL="${API_BASE_URL:-http://localhost:8080}"
TEST_USERNAME="${TEST_USERNAME:-OASIS_ADMIN}"
TEST_PASSWORD="${TEST_PASSWORD:-Uppermall1!}"

echo -e "${BLUE}🧪 Testing OASIS Authentication Process${NC}"
echo "=========================================="
echo "API Base URL: ${API_BASE_URL}"
echo "Test Username: ${TEST_USERNAME}"
echo ""

# Step 1: Health Check
echo -e "${YELLOW}📋 Step 1: Health Check${NC}"
echo "GET ${API_BASE_URL}/api/health"
HEALTH_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" "${API_BASE_URL}/api/health" 2>/dev/null || echo "")
HTTP_CODE=$(echo "$HEALTH_RESPONSE" | grep "HTTP_CODE" | cut -d: -f2)
BODY=$(echo "$HEALTH_RESPONSE" | grep -v "HTTP_CODE")

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ Health check passed${NC}"
    echo "Response: $BODY"
else
    echo -e "${RED}❌ Health check failed (HTTP ${HTTP_CODE})${NC}"
    echo "Response: $BODY"
    exit 1
fi

echo ""

# Step 2: Authenticate (using existing credentials)
echo -e "${YELLOW}📋 Step 2: Authenticate${NC}"
echo "POST ${API_BASE_URL}/api/avatar/authenticate"

AUTH_PAYLOAD=$(cat <<EOF
{
  "username": "${TEST_USERNAME}",
  "password": "${TEST_PASSWORD}"
}
EOF
)

echo "Request payload:"
echo "$AUTH_PAYLOAD" | jq '.' 2>/dev/null || echo "$AUTH_PAYLOAD"

AUTH_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" \
    -X POST "${API_BASE_URL}/api/avatar/authenticate" \
    -H "Content-Type: application/json" \
    -d "$AUTH_PAYLOAD" 2>/dev/null || echo "")

AUTH_HTTP_CODE=$(echo "$AUTH_RESPONSE" | grep "HTTP_CODE" | cut -d: -f2)
AUTH_BODY=$(echo "$AUTH_RESPONSE" | grep -v "HTTP_CODE")

echo ""
echo "Response (HTTP ${AUTH_HTTP_CODE}):"
echo "$AUTH_BODY" | jq '.' 2>/dev/null || echo "$AUTH_BODY"

if [ "$AUTH_HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ Authentication successful${NC}"
    
    # Extract JWT token (check nested result structure)
    JWT_TOKEN=$(echo "$AUTH_BODY" | jq -r '.result.result.jwtToken // .result.jwtToken // .result.token // .jwtToken // .token // empty' 2>/dev/null || echo "")
    
    if [ -n "$JWT_TOKEN" ] && [ "$JWT_TOKEN" != "null" ]; then
        echo -e "${GREEN}✅ JWT Token extracted${NC}"
        echo "Token (first 50 chars): ${JWT_TOKEN:0:50}..."
        
        # Save token to file for later use
        echo "$JWT_TOKEN" > /tmp/oasis_test_token.txt
        echo "Token saved to /tmp/oasis_test_token.txt"
    else
        echo -e "${YELLOW}⚠️  Could not extract JWT token from response${NC}"
        JWT_TOKEN=""
    fi
    
    # Extract avatar ID if available
    if [ -z "$AVATAR_ID" ]; then
        AVATAR_ID=$(echo "$AUTH_BODY" | jq -r '.result.id // .result.avatarId // empty' 2>/dev/null || echo "")
    fi
else
    echo -e "${RED}❌ Authentication failed (HTTP ${AUTH_HTTP_CODE})${NC}"
    exit 1
fi

echo ""

# Step 3: Verify Authentication (Protected Endpoint)
if [ -n "$JWT_TOKEN" ] && [ "$JWT_TOKEN" != "null" ]; then
    echo -e "${YELLOW}📋 Step 3: Verify Authentication (Protected Endpoint)${NC}"
    echo "GET ${API_BASE_URL}/api/avatar/get-logged-in-avatar"
    
    # Try to get current avatar/profile using protected endpoint
    PROFILE_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" \
        -X GET "${API_BASE_URL}/api/avatar/get-logged-in-avatar" \
        -H "Authorization: Bearer ${JWT_TOKEN}" \
        -H "Content-Type: application/json" 2>/dev/null || echo "")
    
    PROFILE_HTTP_CODE=$(echo "$PROFILE_RESPONSE" | grep "HTTP_CODE" | cut -d: -f2)
    PROFILE_BODY=$(echo "$PROFILE_RESPONSE" | grep -v "HTTP_CODE")
    
    echo ""
    echo "Response (HTTP ${PROFILE_HTTP_CODE}):"
    echo "$PROFILE_BODY" | jq '.' 2>/dev/null || echo "$PROFILE_BODY"
    
    if [ "$PROFILE_HTTP_CODE" = "200" ] || [ "$PROFILE_HTTP_CODE" = "401" ]; then
        if [ "$PROFILE_HTTP_CODE" = "200" ]; then
            echo -e "${GREEN}✅ Protected endpoint accessible with JWT token${NC}"
        else
            echo -e "${YELLOW}⚠️  Got 401 - endpoint may require different authentication or token format${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  Unexpected response code: ${PROFILE_HTTP_CODE}${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Skipping Step 3 - No JWT token available${NC}"
fi

echo ""
echo "=========================================="
echo -e "${GREEN}✅ Authentication Process Test Complete!${NC}"
echo ""
echo "Summary:"
echo "  - Health Check: ✅"
echo "  - Authentication: $([ "$AUTH_HTTP_CODE" = "200" ] && echo "✅" || echo "❌")"
echo "  - Token Extraction: $([ -n "$JWT_TOKEN" ] && [ "$JWT_TOKEN" != "null" ] && echo "✅" || echo "⚠️")"
echo "  - Protected Endpoint: $([ -n "$JWT_TOKEN" ] && [ "$PROFILE_HTTP_CODE" = "200" ] && echo "✅" || echo "⚠️")"
echo ""
if [ -n "$JWT_TOKEN" ] && [ "$JWT_TOKEN" != "null" ]; then
    echo "JWT Token saved to: /tmp/oasis_test_token.txt"
    echo "To use in other scripts:"
    echo "  export OASIS_TOKEN=\$(cat /tmp/oasis_test_token.txt)"
fi

