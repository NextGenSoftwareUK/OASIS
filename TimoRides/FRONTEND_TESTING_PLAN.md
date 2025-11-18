# Frontend Testing Plan - Android App & Telegram Bot

**Date:** 2025-11-16  
**Status:** Ready to Test

---

## 🎯 Testing Overview

We need to test two frontend components:
1. **Android App** - Rider booking interface
2. **Telegram Bot** - Driver channel and rider booking via Telegram

---

## 📱 Part 1: Android App Testing

### Prerequisites

1. **Backend Running** ✅
   - Server: `http://localhost:4205`
   - Health check passing
   - Database seeded

2. **Android App Configuration**
   - Base URL: Configured via `TIMORIDES_BASE_URL` build config
   - Default: `http://10.0.2.2:4205` (Android emulator)
   - For physical device: Use your machine's IP (e.g., `http://192.168.1.x:4205`)

### Test 1: App Build & Installation

```bash
cd TimoRides/Timo-Android-App
./gradlew assembleDebug

# APK location: app/build/outputs/apk/debug/app-debug.apk
# Or use Android Studio to build and install
```

**Expected:** App builds successfully, APK generated

### Test 2: Login Flow

**Steps:**
1. Open app
2. Enter credentials:
   - Email: `rider@timorides.com`
   - Password: `RiderDemo123!`
3. Tap Login

**Expected:**
- API call to `/api/auth/login`
- JWT token received
- Token stored securely (EncryptedSharedPreferences)
- Navigation to HomepageActivity

**Verify:**
- Check server logs for login request
- Check token in SessionManager
- Verify navigation occurred

### Test 3: Fetch Nearby Drivers

**Steps:**
1. After login, navigate to booking screen
2. Enter pickup location
3. Enter destination
4. Request nearby drivers

**Expected:**
- API call to `/api/cars/proximity`
- Driver list displayed
- Driver cards show: name, rating, vehicle, fare, ETA

**Verify:**
- Check server logs for proximity request
- Verify response contains driver data
- UI displays drivers correctly

### Test 4: Create Booking

**Steps:**
1. Select a driver
2. Confirm booking details
3. Submit booking

**Expected:**
- API call to `/api/bookings`
- Booking created with status `pending`
- Booking ID returned
- Confirmation screen shown

**Verify:**
- Check server logs for booking creation
- Verify booking in database
- Booking status is `pending`

### Test 5: View Ride History

**Steps:**
1. Navigate to history/rides screen
2. Request ride history

**Expected:**
- API call to `/api/bookings` (with user filter)
- List of past bookings displayed
- Booking details shown correctly

---

## 🤖 Part 2: Telegram Bot Testing

### Prerequisites

1. **ONODE API Running**
   - Port: 5000
   - Telegram bot token configured
   - TimoRides services registered

2. **Telegram Bot Setup**
   - Bot token in `OASIS_DNA.json` or `appsettings.json`
   - Bot created via @BotFather

### Test 1: Start ONODE API

```bash
cd /Volumes/Storage/OASIS_CLEAN
dotnet run --project ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
```

**Expected:**
- API starts on port 5000
- Telegram bot initializes
- Log: "Telegram bot started receiving messages"

### Test 2: Bot Connection

**Steps:**
1. Open Telegram
2. Search for your bot (name from @BotFather)
3. Send `/start`

**Expected:**
- Bot responds with welcome message
- Avatar linking prompt (if not linked)
- Bot is ready to receive commands

**Verify:**
- Check ONODE logs for message received
- Verify bot response sent

### Test 3: Book Ride Command

**Steps:**
1. Send `/bookride`
2. Share location (or enter address)
3. Share destination
4. View driver cards
5. Select driver
6. Choose payment method
7. Confirm booking

**Expected:**
- Bot requests pickup location
- Bot requests destination
- Driver cards displayed with inline buttons
- Booking created in backend
- Confirmation message sent

**Verify:**
- Check backend logs for booking creation
- Verify booking in database
- Booking status is `pending`

### Test 4: My Rides Command

**Steps:**
1. Send `/myrides`

**Expected:**
- List of user's bookings displayed
- Booking status shown
- Booking details accessible

**Verify:**
- API call to backend for bookings
- Data displayed correctly

### Test 5: Track Ride Command

**Steps:**
1. Send `/track <bookingId>`

**Expected:**
- Booking details displayed
- Current status shown
- Driver location (if available)
- ETA information

**Verify:**
- API call to backend for booking
- Real-time status displayed

### Test 6: Driver Actions (via Telegram)

**Steps:**
1. Driver receives booking notification
2. Driver sends location update
3. Driver accepts booking
4. Driver starts ride
5. Driver completes ride

**Expected:**
- Each action updates booking status
- Backend receives driver signals
- Booking lifecycle progresses correctly

**Verify:**
- Check backend logs for driver signals
- Verify booking status updates
- Timeline timestamps recorded

---

## 🔧 Configuration Checklist

### Android App

- [ ] Base URL configured correctly
- [ ] Backend accessible from device/emulator
- [ ] API endpoints match backend routes
- [ ] Authentication tokens stored securely
- [ ] Error handling implemented

### Telegram Bot

- [ ] ONODE API running
- [ ] Bot token configured
- [ ] TimoRides services registered
- [ ] Backend URL configured in TimoRidesApiService
- [ ] Google Maps API key (if using maps)

---

## 🐛 Common Issues & Solutions

### Android App

**Issue:** Cannot connect to backend
- **Solution:** Check base URL (use `10.0.2.2` for emulator, machine IP for device)
- **Solution:** Verify backend is running and accessible
- **Solution:** Check network permissions in AndroidManifest

**Issue:** Login fails
- **Solution:** Verify credentials match seeded data
- **Solution:** Check API endpoint matches backend
- **Solution:** Check server logs for errors

**Issue:** No drivers showing
- **Solution:** Verify seed script created drivers
- **Solution:** Check proximity endpoint parameters
- **Solution:** Verify driver location is set

### Telegram Bot

**Issue:** Bot doesn't respond
- **Solution:** Check ONODE API is running
- **Solution:** Verify bot token is correct
- **Solution:** Check ONODE logs for errors

**Issue:** `/bookride` fails
- **Solution:** Verify TimoRidesApiService is configured
- **Solution:** Check backend URL in service
- **Solution:** Verify backend is accessible from ONODE

**Issue:** No drivers in Telegram
- **Solution:** Check backend has active drivers
- **Solution:** Verify proximity API is working
- **Solution:** Check demo mode settings

---

## 📊 Test Results Template

```markdown
## Android App Tests
- [ ] App builds successfully
- [ ] Login works
- [ ] Nearby drivers fetched
- [ ] Booking created
- [ ] Ride history displayed

## Telegram Bot Tests
- [ ] ONODE API starts
- [ ] Bot responds to /start
- [ ] /bookride works
- [ ] Driver cards displayed
- [ ] Booking created via bot
- [ ] /myrides works
- [ ] /track works
- [ ] Driver actions work
```

---

## 🚀 Quick Start Testing

### Android App
```bash
# 1. Ensure backend is running
cd TimoRides/ride-scheduler-be
npm start

# 2. Build Android app
cd TimoRides/Timo-Android-App
./gradlew assembleDebug

# 3. Install on device/emulator
adb install app/build/outputs/apk/debug/app-debug.apk

# 4. Test login with: rider@timorides.com / RiderDemo123!
```

### Telegram Bot
```bash
# 1. Start ONODE API
cd /Volumes/Storage/OASIS_CLEAN
dotnet run --project ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI

# 2. Open Telegram, find your bot
# 3. Send /start
# 4. Send /bookride
# 5. Follow the flow
```

---

**Ready to start testing!** 🎯

