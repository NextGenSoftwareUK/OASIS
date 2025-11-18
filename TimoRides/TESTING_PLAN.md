# TimoRides Testing Plan

**Status:** Ready to begin systematic testing  
**Last Updated:** Today

---

## 🎯 Testing Overview

We have three main components to test:
1. **Backend API** (`ride-scheduler-be`) - Node.js/Express/MongoDB
2. **Driver Signal System** - New webhook/action endpoints
3. **ONODE Telegram Bot** - C# integration (needs build verification)

---

## ✅ Phase 1: Backend Health & Basic Endpoints

### 1.1 Health Check
```bash
cd TimoRides/ride-scheduler-be
npm install  # if needed
npm start    # starts on port 4205

# In another terminal:
curl http://localhost:4205/health
```

**Expected:** JSON response with status, timestamp, uptime

### 1.2 Database Connection
```bash
# Check server logs for:
# "Database connected well !!!"
```

**If fails:** Check `config/.env` has correct `Database_Url`

### 1.3 Seed Database
```bash
npm run seed
```

**Expected:** Creates admin user, driver, car, global settings

**Verify:**
- Check MongoDB Atlas for `TimoDev` database
- Should see `users`, `drivers`, `cars`, `globalsettings` collections

---

## ✅ Phase 2: Driver Signal Endpoints

### 2.1 Setup Test Environment
```bash
export BASE_URL="http://localhost:4205/api"
export SERVICE_TOKEN="dev-service-token"  # Set in .env
export DRIVER_ID="<from seed output>"
export BOOKING_ID="<create a booking first>"
```

### 2.2 Test Driver Actions
```bash
cd TimoRides/ride-scheduler-be
chmod +x scripts/driverSignalTest.sh

# Test accept action
./scripts/driverSignalTest.sh action accept "test reason"

# Test start
./scripts/driverSignalTest.sh action start

# Test complete
./scripts/driverSignalTest.sh action complete
```

**Expected:** JSON response with `success: true`, booking object, driver snapshot

### 2.3 Test Location Updates
```bash
./scripts/driverSignalTest.sh location -26.11 28.02 10.5 90
```

**Expected:** Driver location updated in database

### 2.4 Test Metrics Endpoint
```bash
curl http://localhost:4205/api/metrics/driver-signal | jq
```

**Expected:** Metrics showing action counts, latency, success rates

---

## ✅ Phase 3: Booking Lifecycle

### 3.1 Create Test Booking
```bash
# First, get auth token
curl -X POST http://localhost:4205/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@timorides.com",
    "password": "ChangeMe123!"
  }'

# Use token to create booking
curl -X POST http://localhost:4205/api/bookings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "car": "<carId from seed>",
    "tripAmount": 150.00,
    "isCash": true,
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

**Expected:** Booking created with status `pending`

### 3.2 Test Driver Accept Flow
```bash
# Use the booking ID from above
export BOOKING_ID="<bookingId>"
./scripts/driverSignalTest.sh action accept
```

**Expected:** Booking status changes to `accepted`

### 3.3 Test Ride Completion
```bash
./scripts/driverSignalTest.sh action start
./scripts/driverSignalTest.sh action complete
```

**Expected:** Booking status → `started` → `completed`

---

## ✅ Phase 4: Postman Collection Tests

### 4.1 Import Collection
1. Open Postman
2. Import `TimoRides/tests/driver-signal.postman_collection.json`
3. Set environment variables:
   - `baseUrl`: `http://localhost:4205/api`
   - `serviceToken`: `<from .env>`
   - `driverId`: `<from seed>`
   - `bookingId`: `<create booking first>`

### 4.2 Run Collection
- Run all requests in sequence
- Verify responses match expected schemas

---

## ✅ Phase 5: ONODE Telegram Bot

### 5.1 Build Verification
```bash
cd /Volumes/Storage/OASIS_CLEAN
dotnet build ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj
```

**Expected:** Build succeeds (we fixed Telegram references earlier)

### 5.2 Start ONODE API
```bash
dotnet run --project ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
```

**Expected:** API starts on port 5000, Telegram bot initializes

### 5.3 Test Bot Commands
1. Open Telegram
2. Find your bot (token in `appsettings.json` or `OASIS_DNA.json`)
3. Send `/start`
4. Send `/bookride`
5. Share location

**Expected:** Bot responds, shows driver cards (if demo mode enabled)

---

## ✅ Phase 6: Integration Tests

### 6.1 End-to-End Booking Flow
1. **Rider creates booking** (via API or Android app)
2. **Driver receives notification** (Telegram bot)
3. **Driver accepts** (via Telegram or PathPulse)
4. **Driver starts ride** (location updates)
5. **Driver completes** (payment recorded)

### 6.2 PathPulse Webhook Simulation
```bash
./scripts/driverSignalTest.sh pathpulse telemetry
```

**Expected:** Webhook enqueued, processed by worker

---

## 🐛 Known Issues to Test Around

1. **Twilio SMS** - Stubbed out, won't send real SMS
2. **Google Maps API** - Needs API key for full functionality
3. **Paystack** - Payment webhooks need real credentials
4. **Telegram Bot** - Needs valid bot token

---

## 📊 Test Results Template

```markdown
## Test Run: [Date]

### Backend Health
- [ ] Health endpoint responds
- [ ] Database connected
- [ ] Seed script runs

### Driver Signals
- [ ] Accept action works
- [ ] Start action works
- [ ] Complete action works
- [ ] Location updates work
- [ ] Metrics endpoint works

### Booking Lifecycle
- [ ] Create booking
- [ ] Accept booking
- [ ] Start ride
- [ ] Complete ride
- [ ] Payment recording

### Telegram Bot
- [ ] Bot builds successfully
- [ ] Bot responds to /start
- [ ] Bot handles /bookride
- [ ] Location sharing works

### Integration
- [ ] End-to-end flow works
- [ ] Webhook processing works
```

---

## 🚀 Quick Start Testing

**Fastest path to verify everything works:**

```bash
# Terminal 1: Start backend
cd TimoRides/ride-scheduler-be
npm install && npm start

# Terminal 2: Run health check
sleep 5 && curl http://localhost:4205/health

# Terminal 3: Seed database
cd TimoRides/ride-scheduler-be
npm run seed

# Terminal 4: Test driver signal
export SERVICE_TOKEN="dev-token"
export DRIVER_ID="<from seed>"
export BOOKING_ID="<create booking>"
./scripts/driverSignalTest.sh action accept
```

---

## 📝 Next Steps After Testing

1. **Fix any failing tests**
2. **Document edge cases found**
3. **Update briefs with test results**
4. **Create automated test suite** (Brief 05)
5. **Set up CI/CD** (Brief 06)

---

**Ready to start? Let's begin with Phase 1!** 🎯

