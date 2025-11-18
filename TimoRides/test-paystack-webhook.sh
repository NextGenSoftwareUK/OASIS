#!/bin/bash

# Test Paystack Webhook by Creating a Test Transaction
# This will automatically trigger a charge.success webhook

PAYSTACK_SECRET="sk_test_c80ec29cce4d86ffc92cd2e24e8a8faa8135f246"
BASE_URL="https://api.paystack.co"

echo "🚀 Creating test transaction to trigger webhook..."
echo ""

# Initialize a test transaction
RESPONSE=$(curl -s -X POST "$BASE_URL/transaction/initialize" \
  -H "Authorization: Bearer $PAYSTACK_SECRET" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@timorides.com",
    "amount": 15000,
    "currency": "ZAR",
    "metadata": {
      "bookingId": "test_booking_123",
      "test": true
    }
  }')

echo "Response:"
echo "$RESPONSE" | jq '.'

REFERENCE=$(echo "$RESPONSE" | jq -r '.data.reference')
AUTHORIZATION_URL=$(echo "$RESPONSE" | jq -r '.data.authorization_url')

if [ "$REFERENCE" != "null" ] && [ -n "$REFERENCE" ]; then
  echo ""
  echo "✅ Transaction initialized!"
  echo "📝 Reference: $REFERENCE"
  echo ""
  echo "🔗 Authorization URL: $AUTHORIZATION_URL"
  echo ""
  echo "📋 Next steps:"
  echo "1. Open the authorization URL above in your browser"
  echo "2. Use Paystack test card: 4084 0840 8408 4081"
  echo "   - Expiry: 12/25 (any future date)"
  echo "   - CVV: 123"
  echo "3. Complete the payment"
  echo "4. Watch your backend logs for the webhook!"
  echo ""
  echo "Or verify the transaction directly:"
  echo "curl -X GET \"$BASE_URL/transaction/verify/$REFERENCE\" \\"
  echo "  -H \"Authorization: Bearer $PAYSTACK_SECRET\""
else
  echo "❌ Failed to initialize transaction"
  echo "$RESPONSE" | jq '.'
fi

