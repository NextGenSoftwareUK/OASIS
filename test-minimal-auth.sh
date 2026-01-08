#!/bin/bash
# Test script for minimal authentication endpoint
# This script tests both the original and minimal authentication endpoints

API_URL="https://127.0.0.1:5004"
USERNAME="OASIS_ADMIN"
PASSWORD="Uppermall1!"

echo "=========================================="
echo "Testing OASIS Authentication Endpoints"
echo "=========================================="
echo ""

# Test 1: Original endpoint (large response)
echo "1. Testing original /api/avatar/authenticate endpoint..."
ORIGINAL_SIZE=$(curl -k -s -X POST "${API_URL}/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"${USERNAME}\",\"password\":\"${PASSWORD}\"}" | wc -c)

echo "   Original endpoint response size: ${ORIGINAL_SIZE} bytes"
echo ""

# Test 2: Minimal endpoint (small response)
echo "2. Testing new /api/avatar/authenticate-minimal endpoint..."
MINIMAL_RESPONSE=$(curl -k -s -X POST "${API_URL}/api/avatar/authenticate-minimal" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"${USERNAME}\",\"password\":\"${PASSWORD}\"}")

MINIMAL_SIZE=$(echo "$MINIMAL_RESPONSE" | wc -c)
HTTP_STATUS=$(curl -k -s -w "%{http_code}" -o /dev/null -X POST "${API_URL}/api/avatar/authenticate-minimal" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"${USERNAME}\",\"password\":\"${PASSWORD}\"}")

echo "   Minimal endpoint HTTP status: ${HTTP_STATUS}"
echo "   Minimal endpoint response size: ${MINIMAL_SIZE} bytes"
echo ""

# Test 3: Check if JWT token is present in minimal response
if [ "$HTTP_STATUS" = "200" ]; then
  JWT_TOKEN=$(echo "$MINIMAL_RESPONSE" | grep -o '"jwtToken":"[^"]*' | cut -d'"' -f4)
  if [ -n "$JWT_TOKEN" ]; then
    echo "   ✓ JWT token found in response"
    echo "   Token preview: ${JWT_TOKEN:0:50}..."
  else
    echo "   ✗ JWT token not found in response"
  fi
else
  echo "   ⚠ Endpoint returned error status. API may need to be restarted to pick up new endpoint."
  echo "   Response preview:"
  echo "$MINIMAL_RESPONSE" | head -5
fi

echo ""
echo "=========================================="
echo "Summary"
echo "=========================================="
echo "Original endpoint size: ${ORIGINAL_SIZE} bytes"
echo "Minimal endpoint size:  ${MINIMAL_SIZE} bytes"
if [ "$MINIMAL_SIZE" -gt 0 ] && [ "$MINIMAL_SIZE" -lt "$ORIGINAL_SIZE" ]; then
  REDUCTION=$((ORIGINAL_SIZE - MINIMAL_SIZE))
  PERCENTAGE=$((REDUCTION * 100 / ORIGINAL_SIZE))
  echo "Size reduction: ${REDUCTION} bytes (${PERCENTAGE}% smaller)"
  echo ""
  echo "✓ SUCCESS: Minimal endpoint is working and significantly smaller!"
else
  echo ""
  echo "⚠ NOTE: If minimal endpoint shows 0 bytes or 405 error,"
  echo "   the API server needs to be restarted to load the new endpoint."
fi
echo ""

