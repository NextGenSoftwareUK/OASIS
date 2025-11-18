# Frontend Testing Checklist

**Date:** 2025-11-16  
**Components:** Android App + Telegram Bot

---

## 📱 Android App Testing

### Setup
- [ ] Backend running on `http://localhost:4205`
- [ ] Base URL configured: `http://10.0.2.2:4205` (emulator) or machine IP (device)
- [ ] App built and installed on device/emulator

### Test 1: Login ✅
**Credentials:**
- Email: `rider@timorides.com`
- Password: `RiderDemo123!`

**Steps:**
1. Open app
2. Enter email and password
3. Tap "Continue"

**Expected:**
- [ ] API call to `/api/auth/login` succeeds
- [ ] JWT token received and stored
- [ ] Navigation to HomepageActivity
- [ ] No errors in logs

**Verify:**
```bash
# Check server logs for:
POST /api/auth/login
Status: 200
```

### Test 2: Fetch Nearby Drivers
**Steps:**
1. On homepage, enter pickup location
2. Enter destination
3. Request drivers

**Expected:**
- [ ] API call to `/api/cars/proximity` with correct params
- [ ] Driver list displayed
- [ ] Driver cards show: name, rating, vehicle, fare

**Verify:**
```bash
# Check server logs for:
GET /api/cars/proximity?sourceLatitude=...&sourceLongitude=...
Status: 200
Response contains cars array
```

### Test 3: Create Booking
**Steps:**
1. Select a driver
2. Confirm booking details
3. Submit

**Expected:**
- [ ] API call to `/api/bookings` with booking data
- [ ] Booking created (status: `pending`)
- [ ] Booking ID returned
- [ ] Confirmation shown

**Verify:**
```bash
# Check server logs for:
POST /api/bookings
Status: 201
Response contains booking ID
```

### Test 4: View Booking Status
**Steps:**
1. Navigate to active booking
2. Check status

**Expected:**
- [ ] API call to `/api/bookings/{id}`
- [ ] Current status displayed
- [ ] Updates reflect driver actions

---

## 🤖 Telegram Bot Testing

### Setup
- [ ] ONODE API running on port 5000
- [ ] Bot token configured in `OASIS_DNA.json` or `appsettings.json`
- [ ] Backend accessible from ONODE (http://localhost:4205)

### Test 1: Start ONODE API
```bash
cd /Volumes/Storage/OASIS_CLEAN
dotnet run --project ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
```

**Expected:**
- [ ] API starts successfully
- [ ] Log: "Telegram bot started receiving messages"
- [ ] No build errors

### Test 2: Bot Connection
**Steps:**
1. Open Telegram
2. Find your bot
3. Send `/start`

**Expected:**
- [ ] Bot responds immediately
- [ ] Welcome message received
- [ ] Avatar linking prompt (if not linked)

**Verify:**
- [ ] Check ONODE logs for message received
- [ ] No errors in logs

### Test 3: Book Ride Flow
**Steps:**
1. Send `/bookride`
2. Share location (or type address)
3. Share destination
4. View driver cards
5. Select driver
6. Choose payment
7. Confirm

**Expected:**
- [ ] Each step works correctly
- [ ] Driver cards displayed
- [ ] Booking created in backend
- [ ] Confirmation message sent

**Verify:**
```bash
# Check backend logs for:
POST /api/bookings
Status: 201
```

### Test 4: My Rides
**Steps:**
1. Send `/myrides`

**Expected:**
- [ ] List of bookings displayed
- [ ] Status shown for each
- [ ] Booking details accessible

### Test 5: Track Ride
**Steps:**
1. Send `/track <bookingId>`

**Expected:**
- [ ] Booking details shown
- [ ] Current status displayed
- [ ] Driver location (if available)

### Test 6: Driver Actions (via Telegram)
**Steps:**
1. Driver receives booking notification
2. Driver accepts via button
3. Driver starts ride
4. Driver completes ride

**Expected:**
- [ ] Each action updates booking
- [ ] Backend receives driver signals
- [ ] Status transitions correctly

**Verify:**
```bash
# Check backend logs for:
POST /api/driver-actions
Status: 200
Booking status updated
```

---

## 🔧 Configuration Verification

### Android App
- [x] Base URL: `http://10.0.2.2:4205` (emulator)
- [ ] Network permissions in AndroidManifest
- [ ] Retrofit client configured
- [ ] Session manager working

### Telegram Bot
- [ ] Bot token configured
- [ ] TimoRidesApiService backend URL: `http://localhost:4205/api`
- [ ] Google Maps API key (if using maps)
- [ ] Service token configured

---

## 🐛 Troubleshooting

### Android App Issues

**Cannot connect to backend:**
- Check base URL (use `10.0.2.2` for emulator)
- Verify backend is running
- Check network permissions

**Login fails:**
- Verify credentials match seeded data
- Check API endpoint
- Check server logs

**No drivers showing:**
- Verify seed script created drivers
- Check proximity endpoint
- Verify driver locations set

### Telegram Bot Issues

**Bot doesn't respond:**
- Check ONODE API is running
- Verify bot token
- Check ONODE logs

**/bookride fails:**
- Verify TimoRidesApiService configured
- Check backend URL
- Verify backend accessible

---

## 📊 Test Results

### Android App
- [ ] Login works
- [ ] Drivers fetched
- [ ] Booking created
- [ ] Status updates work

### Telegram Bot
- [ ] Bot responds
- [ ] /bookride works
- [ ] Driver cards shown
- [ ] Booking created
- [ ] Driver actions work

---

**Ready to test!** 🚀

