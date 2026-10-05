#!/usr/bin/env bash
set -euo pipefail

# Test Paystack webhook endpoint
# Usage: ./scripts/testPaystackWebhook.sh [bookingId]

BASE_URL="${BASE_URL:-http://localhost:4205}"
BOOKING_ID="${1:-}"

if [[ -z "$BOOKING_ID" ]]; then
  echo "Usage: $0 <bookingId>"
  echo "Example: $0 6919b0e3af316af23dc348dc"
  exit 1
fi

# Simulate a Paystack charge.success webhook
WEBHOOK_PAYLOAD=$(cat <<EOF
{
  "event": "charge.success",
  "data": {
    "id": 123456789,
    "domain": "test",
    "status": "success",
    "reference": "test_ref_$(date +%s)",
    "amount": 27550,
    "currency": "ZAR",
    "paid_at": "$(date -u +%Y-%m-%dT%H:%M:%S.000Z)",
    "metadata": {
      "bookingId": "$BOOKING_ID"
    },
    "customer": {
      "email": "rider@timorides.com"
    }
  }
}
EOF
)

echo "🧪 Testing Paystack webhook endpoint..."
echo "📤 Sending charge.success event for booking: $BOOKING_ID"
echo ""

# Send webhook (without signature in dev mode)
RESPONSE=$(curl -s -X POST \
  "${BASE_URL}/webhooks/paystack" \
  -H "Content-Type: application/json" \
  -d "$WEBHOOK_PAYLOAD")

echo "📥 Response:"
echo "$RESPONSE" | jq . || echo "$RESPONSE"
echo ""

# Check if booking payment was updated
echo "🔍 Verifying booking payment status..."
node -e "
require('dotenv').config({path: './config/.env'});
const mongoose = require('mongoose');
const Booking = require('./models/bookingModal');
(async () => {
  await mongoose.connect(process.env.Database_Url);
  const booking = await Booking.findById('$BOOKING_ID').lean();
  if (booking) {
    console.log('Booking payment status:', booking.payment?.status || 'not set');
    console.log('Payment reference:', booking.payment?.reference || 'not set');
    console.log('Payment method:', booking.payment?.method || 'not set');
  } else {
    console.log('Booking not found');
  }
  await mongoose.disconnect();
})();
"

