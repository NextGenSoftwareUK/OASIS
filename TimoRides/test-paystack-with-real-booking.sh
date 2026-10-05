#!/bin/bash

# Test Paystack webhook with a real booking from database
# First, get a real booking ID from your database

PAYSTACK_SECRET="sk_test_c80ec29cce4d86ffc92cd2e24e8a8faa8135f246"
BASE_URL="https://api.paystack.co"
BACKEND_URL="http://localhost:4205"

echo "🔍 Getting a real booking ID from database..."
echo ""

# Get auth token first
AUTH_RESPONSE=$(curl -s -X POST "$BACKEND_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@timorides.com",
    "password": "ChangeMe123!"
  }')

TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.accessToken // .token // .data.token // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
  echo "❌ Failed to get auth token"
  echo "Response was:"
  echo "$AUTH_RESPONSE" | jq '.'
  echo ""
  echo "Trying alternative endpoint format..."
  # Try without jq parsing
  TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)
  if [ -z "$TOKEN" ]; then
    exit 1
  fi
fi

echo "✅ Got auth token"
echo ""

# Get bookings
BOOKINGS_RESPONSE=$(curl -s -X GET "$BACKEND_URL/api/bookings" \
  -H "Authorization: Bearer $TOKEN")

BOOKING_ID=$(echo "$BOOKINGS_RESPONSE" | jq -r '.bookings[0].id // .bookings[0]._id // empty')

if [ -z "$BOOKING_ID" ] || [ "$BOOKING_ID" = "null" ]; then
  echo "❌ No bookings found. Please create a booking first."
  echo ""
  echo "You can create one with:"
  echo "curl -X POST $BACKEND_URL/api/bookings \\"
  echo "  -H \"Authorization: Bearer $TOKEN\" \\"
  echo "  -H \"Content-Type: application/json\" \\"
  echo "  -d '{...booking data...}'"
  exit 1
fi

echo "✅ Found booking: $BOOKING_ID"
echo ""
echo "🚀 Creating Paystack transaction with real booking ID..."
echo ""

# Initialize transaction with real booking ID
RESPONSE=$(curl -s -X POST "$BASE_URL/transaction/initialize" \
  -H "Authorization: Bearer $PAYSTACK_SECRET" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"test@timorides.com\",
    \"amount\": 15000,
    \"currency\": \"ZAR\",
    \"metadata\": {
      \"bookingId\": \"$BOOKING_ID\"
    }
  }")

echo "Response:"
echo "$RESPONSE" | jq '.'

REFERENCE=$(echo "$RESPONSE" | jq -r '.data.reference')
AUTHORIZATION_URL=$(echo "$RESPONSE" | jq -r '.data.authorization_url')

if [ "$REFERENCE" != "null" ] && [ -n "$REFERENCE" ]; then
  echo ""
  echo "✅ Transaction initialized!"
  echo "📝 Reference: $REFERENCE"
  echo "📋 Booking ID: $BOOKING_ID"
  echo ""
  echo "🔗 Authorization URL: $AUTHORIZATION_URL"
  echo ""
  echo "📋 Next steps:"
  echo "1. Open the authorization URL above"
  echo "2. Click 'Success' (or use test card: 4084 0840 8408 4081)"
  echo "3. Watch your backend logs - the booking should update!"
  echo ""
  echo "After payment, verify booking was updated:"
  echo "curl -X GET \"$BACKEND_URL/api/bookings/$BOOKING_ID\" \\"
  echo "  -H \"Authorization: Bearer $TOKEN\" | jq '.payment'"
else
  echo "❌ Failed to initialize transaction"
  echo "$RESPONSE" | jq '.'
fi

