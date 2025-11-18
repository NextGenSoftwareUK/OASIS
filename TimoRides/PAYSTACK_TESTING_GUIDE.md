# Paystack Integration Testing Guide

**Status:** Ready to test real payment integration  
**Last Updated:** Today

---

## 🎯 What's Different Now?

Previously, your trip flow updated the driver's wallet in MongoDB directly. Now with Paystack connected:

1. **Real Payment Processing** - Rider payments go through Paystack (test mode)
2. **Webhook Events** - Paystack sends events to your backend when payments succeed
3. **Automatic Updates** - Booking payment status updates automatically via webhook
4. **Audit Trail** - All payment events are logged in audit logs

---

## ✅ Phase 1: Verify Webhook Endpoint is Live

### 1.1 Check Backend is Running
```bash
cd /Volumes/Storage/OASIS_CLEAN/TimoRides/ride-scheduler-be
# Server should be running on port 4205
curl http://localhost:4205/health
```

**Expected:** Health check returns OK

### 1.2 Verify Ngrok Tunnel is Active
```bash
# Check ngrok is still running
curl http://127.0.0.1:4040/api/tunnels | jq '.tunnels[0].public_url'
```

**Expected:** Returns your ngrok URL (e.g., `https://annabella-overscrupulous-unplacidly.ngrok-free.dev`)

### 1.3 Test Webhook Endpoint Directly
```bash
# Replace with your actual ngrok URL
curl -X POST https://annabella-overscrupulous-unplacidly.ngrok-free.dev/webhooks/paystack \
  -H "Content-Type: application/json" \
  -d '{"test": "ping"}'
```

**Expected:** Should return a response (even if it's an error about missing signature - that's OK, means endpoint is reachable)

---

## ✅ Phase 2: Test Paystack Webhook (Simulated)

### 2.1 Send Test Webhook from Paystack Dashboard

1. **Go to Paystack Dashboard:**
   - Login at https://dashboard.paystack.com
   - Navigate to: **Settings → API Keys & Webhooks**

2. **Send Test Webhook:**
   - Scroll to "Test Webhook" section
   - Click **"Send test webhook"** button
   - Select event type: **`charge.success`**
   - Click **"Send"**

3. **Check Your Backend Logs:**
   ```bash
   # Watch your backend terminal for:
   # "📥 Paystack webhook received: charge.success"
   ```

**Expected:** 
- Backend logs show webhook received
- No errors in console
- Response 200 OK

### 2.2 Verify Webhook Secret (After First Real Event)

After the first webhook fires, Paystack will show the signing secret:
1. Go to **Settings → API Keys & Webhooks**
2. Look for **"Webhook Signing Secret"** (appears after first webhook)
3. Copy it and update your `.env`:
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN/TimoRides/ride-scheduler-be/config
   # Edit .env and replace:
   PAYSTACK_WEBHOOK_SECRET=<paste_secret_here>
   ```
4. Restart backend server

---

## ✅ Phase 3: Test Real Payment Flow (End-to-End)

### 3.1 Create a Test Booking with Payment Reference

```bash
# First, get auth token
curl -X POST http://localhost:4205/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@timorides.com",
    "password": "ChangeMe123!"
  }'

# Save the token from response
export AUTH_TOKEN="<token_from_response>"

# Create booking (note: we'll add payment reference later)
curl -X POST http://localhost:4205/api/bookings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d '{
    "car": "<carId_from_seed>",
    "tripAmount": 150.00,
    "isCash": false,
    "payment": {
      "method": "paystack",
      "status": "pending"
    },
    "sourceLocation": {
      "latitude": -26.11,
      "longitude": 28.02,
      "address": "Test Pickup"
    },
    "destinationLocation": {
      "latitude": -26.12,
      "longitude": 28.03,
      "address": "Test Dropoff"
    },
    "phoneNumber": "+27700000000",
    "email": "rider@test.com",
    "fullName": "Test Rider"
  }'
```

**Save the booking ID from response**

### 3.2 Simulate Paystack Payment (Using Paystack Test Card)

**Option A: Use Paystack Test Transaction Page**

1. Go to Paystack Dashboard → **Transactions**
2. Click **"Test Transaction"**
3. Use test card: `4084 0840 8408 4081`
   - Expiry: Any future date (e.g., `12/25`)
   - CVV: `123`
   - Amount: `150.00` (match your booking)
4. In **Metadata** field, add:
   ```json
   {"bookingId": "<your_booking_id>"}
   ```
5. Complete the transaction

**Option B: Use Paystack API Directly**

```bash
# Initialize transaction
curl -X POST https://api.paystack.co/transaction/initialize \
  -H "Authorization: Bearer sk_test_c80ec29cce4d86ffc92cd2e24e8a8faa8135f246" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "rider@test.com",
    "amount": 15000,
    "currency": "ZAR",
    "metadata": {
      "bookingId": "<your_booking_id>"
    }
  }'
```

**Save the `reference` from response**

### 3.3 Verify Transaction in Paystack

```bash
# Verify the transaction
curl -X GET "https://api.paystack.co/transaction/verify/<reference>" \
  -H "Authorization: Bearer sk_test_c80ec29cce4d86ffc92cd2e24e8a8faa8135f246"
```

**Expected:** Transaction status is `success`

### 3.4 Check Webhook Was Received

```bash
# Check backend logs for:
# "📥 Paystack webhook received: charge.success"
# "Payment processed successfully"
```

### 3.5 Verify Booking Updated in Database

```bash
# Query the booking to see payment status
curl -X GET "http://localhost:4205/api/bookings/<booking_id>" \
  -H "Authorization: Bearer $AUTH_TOKEN"
```

**Expected:**
- `payment.status` = `"paid"`
- `payment.reference` = Paystack transaction reference
- `payment.paidAt` = timestamp
- `payment.method` = `"paystack"`

---

## ✅ Phase 4: Test Driver Payout Flow

### 4.1 Create Payment Request (Driver Withdrawal)

```bash
# Login as driver
curl -X POST http://localhost:4205/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "driver@timorides.com",
    "password": "DriverDemo123!"
  }'

export DRIVER_TOKEN="<token>"

# Request payment withdrawal
curl -X POST http://localhost:4205/api/users/request-payment \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $DRIVER_TOKEN" \
  -d '{
    "amount": 50.00
  }'
```

**Expected:** Payment request created with status `pending`

### 4.2 Set Up Driver Bank Details (For Paystack Transfer)

**Note:** For now, this requires manual setup. In production, drivers would add bank details via UI.

The payout flow requires:
- Driver's bank account number
- Bank code (Paystack bank code for South African banks)

### 4.3 Simulate Transfer Webhook

Since we're in test mode, you can simulate a transfer webhook:

```bash
# Create a test transfer webhook payload
curl -X POST https://annabella-overscrupulous-unplacidly.ngrok-free.dev/webhooks/paystack \
  -H "Content-Type: application/json" \
  -H "x-paystack-signature: <signature>" \
  -d '{
    "event": "transfer.success",
    "data": {
      "transfer_code": "TRF_test123",
      "reference": "ref_test123",
      "amount": 5000,
      "currency": "ZAR",
      "status": "success",
      "recipient": {
        "recipient_code": "RCP_test123"
      }
    }
  }'
```

**Note:** For real testing, you'd need to:
1. Create transfer recipient via Paystack API
2. Initiate actual transfer
3. Wait for Paystack to send webhook

---

## 📊 What You'll See in Database

### Before Paystack Integration:
```json
{
  "payment": {
    "status": "pending",
    "method": "cash"
  }
}
```

### After Paystack Webhook:
```json
{
  "payment": {
    "status": "paid",
    "method": "paystack",
    "reference": "ref_abc123xyz",
    "paidAt": "2024-11-16T12:34:56.789Z",
    "amount": 150.00,
    "currency": "ZAR"
  }
}
```

### Audit Logs:
Check `auditLogs` collection for entries like:
```json
{
  "action": "booking.payment.completed",
  "metadata": {
    "provider": "paystack",
    "reference": "ref_abc123xyz",
    "transactionId": "123456"
  }
}
```

---

## 🔍 Verification Checklist

After running tests, verify:

- [ ] Webhook endpoint is reachable via ngrok
- [ ] Paystack dashboard shows webhook URL configured
- [ ] Test webhook from Paystack dashboard is received
- [ ] Backend logs show webhook processing
- [ ] Booking payment status updates to `paid`
- [ ] Payment reference is stored in booking
- [ ] Audit log entry created for payment event
- [ ] No errors in backend console

---

## 🐛 Troubleshooting

### Webhook Not Received

1. **Check ngrok is running:**
   ```bash
   curl http://127.0.0.1:4040/api/tunnels
   ```

2. **Check Paystack webhook URL matches ngrok URL:**
   - Should be: `https://<your-ngrok-url>/webhooks/paystack`

3. **Check backend logs for errors:**
   - Look for signature verification errors
   - Check if webhook secret is set

### Payment Status Not Updating

1. **Check booking ID in metadata:**
   - Paystack transaction metadata must include `bookingId`

2. **Verify transaction reference:**
   - Check Paystack dashboard for transaction status
   - Ensure transaction is `success`, not `pending`

3. **Check database directly:**
   ```bash
   # Connect to MongoDB and query booking
   db.bookings.findOne({ _id: ObjectId("<booking_id>") })
   ```

---

## 🚀 Quick Test Script

```bash
#!/bin/bash
# Quick Paystack webhook test

BASE_URL="http://localhost:4205"
NGROK_URL="https://annabella-overscrupulous-unplacidly.ngrok-free.dev"

echo "1. Testing webhook endpoint..."
curl -X POST "$NGROK_URL/webhooks/paystack" \
  -H "Content-Type: application/json" \
  -d '{"test": "ping"}' \
  -v

echo -e "\n\n2. Check backend health..."
curl "$BASE_URL/health" | jq

echo -e "\n\n3. Check ngrok status..."
curl http://127.0.0.1:4040/api/tunnels | jq '.tunnels[0].public_url'
```

---

## 📝 Next Steps

1. **Complete test transaction** using Paystack test card
2. **Verify webhook processes** and updates booking
3. **Check audit logs** for payment events
4. **Test driver payout flow** (requires bank details setup)
5. **Document any issues** found during testing

---

**Ready to test? Start with Phase 1!** 🎯

