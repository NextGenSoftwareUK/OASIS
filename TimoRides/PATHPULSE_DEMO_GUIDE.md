# TimoRides Demo Guide for PathPulse

**Date:** 2025-11-17  
**Purpose:** Complete walkthrough of TimoRides codebase and functionality

---

## 🎯 Overview

TimoRides is a ride-hailing platform with three main components:

1. **Backend API** (`ride-scheduler-be`) - Node.js/Express/MongoDB
2. **Android Rider App** (`Timo-Android-App`) - Java/Android
3. **Telegram Bot** (via OASIS ONODE) - C#/.NET for driver channel

---

## 📦 Component Architecture

### 1. Backend API (`ride-scheduler-be`)

**Tech Stack:**
- Node.js + Express
- MongoDB (Atlas)
- JWT Authentication
- Paystack (Payments)
- Twilio (SMS - stubbed)
- Swagger Documentation

**Key Features:**
- Ride booking lifecycle management
- Driver location tracking
- Payment processing (Paystack)
- Driver signal processing (accept/start/complete)
- PathPulse webhook integration
- Driver webhook queue with retries

**API Base:** `http://localhost:4205`  
**Swagger Docs:** `http://localhost:4205/api-docs`

### 2. Android Rider App (`Timo-Android-App`)

**Tech Stack:**
- Java (Android SDK 33)
- Google Maps API
- Retrofit (API client)
- Encrypted SharedPreferences (secure storage)

**Key Features:**
- User authentication
- Nearby driver discovery
- Ride booking
- Real-time ride tracking
- Payment integration ready

**Package:** `com.itechnotion.nextgen`

### 3. Telegram Bot (Driver Channel)

**Tech Stack:**
- C# / .NET (OASIS ONODE WebAPI)
- Telegram.Bot library
- Integration with TimoRides backend

**Key Features:**
- Driver ride notifications
- Driver action handling (accept/decline/start/complete)
- Location sharing
- Ride booking via Telegram

---

## 🚀 Quick Start Demo

### Step 1: Start Backend

```bash
cd TimoRides/ride-scheduler-be
cp config/env.example config/.env
# Edit .env with MongoDB URI and other configs
npm install
npm run seed  # Seed demo data
npm start     # Starts on port 4205
```

**Verify:** `curl http://localhost:4205/api/health`

### Step 2: Start Telegram Bot (Optional)

```bash
cd /Volumes/Storage/OASIS_CLEAN
dotnet run --project ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
```

**Verify:** Bot responds to `/start` in Telegram

### Step 3: Android App

**Build:**
```bash
cd TimoRides/Timo-Android-App
./gradlew assembleDebug
```

**Install:** Use Android Studio or `adb install app/build/outputs/apk/debug/app-debug.apk`

**Note:** Requires emulator with Google Play Services for Maps

---

## 📋 Demo Flow

### Scenario 1: Complete Ride Booking (Android App)

1. **Login**
   - Email: `rider@timorides.com`
   - Password: `RiderDemo123!`

2. **View Nearby Drivers**
   - App fetches drivers from `/api/cars/proximity`
   - Shows driver cards with ratings, vehicle info, ETA

3. **Create Booking**
   - Select driver
   - Set pickup location (GPS)
   - Set destination (long-press map)
   - Enter phone number
   - Submit booking

4. **Track Ride**
   - Booking status updates
   - Driver location visible (if driver updates location)

### Scenario 2: Driver Actions (Telegram Bot)

1. **Driver Receives Notification**
   - Telegram bot sends booking notification
   - Shows booking details

2. **Driver Accepts**
   - Tap "Accept" button
   - Backend updates booking status to `accepted`

3. **Driver Starts Ride**
   - Tap "Start Ride"
   - Backend updates to `started`
   - OTP verification (optional)

4. **Driver Completes Ride**
   - Tap "Complete Ride"
   - Backend updates to `completed`
   - Driver wallet credited

### Scenario 3: PathPulse Integration

1. **Driver Location Updates**
   - PathPulse sends webhook to `/api/driver-signals/pathpulse`
   - Backend processes location update
   - Updates driver and car location in database

2. **Driver Actions via PathPulse**
   - PathPulse Scout sends driver actions
   - Backend processes via driver signal service
   - Updates booking status accordingly

---

## 🔑 Key Endpoints

### Backend API

**Authentication:**
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration

**Bookings:**
- `GET /api/bookings` - List bookings
- `POST /api/bookings` - Create booking
- `GET /api/bookings/:id` - Get booking details
- `PATCH /api/bookings/:id/payment` - Update payment

**Drivers:**
- `GET /api/cars/proximity` - Find nearby drivers
- `PATCH /api/drivers/:id/location` - Update driver location
- `PATCH /api/drivers/:id/status` - Update driver status

**Driver Signals:**
- `POST /api/driver-signals/action` - Driver action (accept/start/complete)
- `POST /api/driver-signals/pathpulse` - PathPulse webhook
- `POST /api/driver-signals/location` - Location update

**Health:**
- `GET /api/health` - System health check

### Telegram Bot Commands

- `/start` - Initialize bot
- `/bookride` - Book a ride via Telegram
- `/myrides` - View ride history
- `/track <bookingId>` - Track specific ride
- `/cancel <bookingId>` - Cancel booking

---

## 💾 Database Schema

### Key Collections

**Users:**
- Admins, Riders, Drivers (discriminated by `type`)
- Authentication tokens
- Wallet balance

**Cars:**
- Vehicle details
- Driver assignment
- Location tracking
- Active status

**Bookings:**
- Ride details (pickup, destination)
- Status lifecycle (pending → accepted → started → completed)
- Payment information
- Timeline tracking

**Driver Signals:**
- Action logs
- Location updates
- Webhook queue

---

## 🔐 Authentication

**JWT Tokens:**
- Access token (short-lived)
- Refresh token (long-lived)
- Stored securely in Android app (EncryptedSharedPreferences)

**Service Tokens:**
- For driver signal endpoints
- Configured in backend `.env`

---

## 💳 Payment Integration

**Paystack:**
- Webhook endpoint: `/api/webhooks/paystack`
- Handles `charge.success`, `transfer.success`, `transfer.failed`
- Driver payouts via Paystack transfers

**Payment Flow:**
1. Rider initiates payment
2. Paystack processes
3. Webhook confirms payment
4. Wallet funded
5. Driver payout processed

---

## 📊 Monitoring & Health

**Health Endpoint:**
```bash
curl http://localhost:4205/api/health
```

**Returns:**
- MongoDB connection status
- Pending booking counts
- Driver webhook queue depth
- Driver signal metrics

---

## 🧪 Testing

### Backend Tests

**Seed Data:**
```bash
npm run seed
```

**Test Endpoints:**
- Use Postman collection: `tests/driver-signal.postman_collection.json`
- Use REST client: `tests/payments.rest`

### Android App Tests

**Login Credentials:**
- Email: `rider@timorides.com`
- Password: `RiderDemo123!`

**Driver Credentials:**
- Email: `driver@timorides.com`
- Password: `DriverDemo123!`

### Telegram Bot Tests

1. Find bot in Telegram
2. Send `/start`
3. Follow booking flow

---

## 🔗 Integration Points

### PathPulse Integration

**Webhook Endpoint:**
```
POST /api/driver-signals/pathpulse
```

**Expected Payload:**
- Driver location updates
- Driver actions
- Signature verification

**Configuration:**
- PathPulse webhook secret in `.env`
- Signature verification in `driverSignalService.js`

### OASIS Integration

**Services:**
- `TimoRidesApiService` - Backend API client
- `RideBookingStateManager` - State management
- `GoogleMapsService` - Maps integration

**Location:**
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/`

---

## 📁 Repository Structure

```
TimoRides/
├── ride-scheduler-be/          # Backend API
│   ├── controllers/           # Request handlers
│   ├── services/              # Business logic
│   ├── models/                # Database schemas
│   ├── routes/                # API routes
│   └── scripts/               # Seed & utilities
│
├── Timo-Android-App/           # Android rider app
│   ├── app/src/main/java/     # Java source
│   └── app/src/main/res/      # Resources
│
└── push-to-timo.sh            # Git subtree push script
```

---

## 🚢 Deployment

### Backend

**Environment Variables:**
- `Database_Url` - MongoDB connection
- `PAYSTACK_SECRET_KEY` - Payment gateway
- `PAYSTACK_WEBHOOK_SECRET` - Webhook verification
- `ACCESS_TOKEN_SECRET` - JWT signing

**Start:**
```bash
npm start
```

### Android App

**Build:**
```bash
./gradlew assembleRelease
```

**APK Location:**
```
app/build/outputs/apk/release/app-release.apk
```

---

## 📝 Key Files to Show

### Backend
- `server.js` - Main entry point
- `services/rideService.js` - Ride lifecycle
- `services/driverSignalService.js` - Driver actions
- `services/paymentsService.js` - Payment processing
- `controllers/bookingController.js` - Booking endpoints

### Android
- `LoginActivity.java` - Authentication
- `HomepageActivity.java` - Main screen
- `BookingRequestActivity.java` - Booking creation
- `ApiService.java` - API client

### Telegram
- `TelegramBotService.cs` - Bot handler
- `TimoRidesApiService.cs` - Backend client

---

## 🎬 Demo Script

1. **Show Backend Health**
   - `curl http://localhost:4205/api/health`
   - Show Swagger docs

2. **Show Android App**
   - Login flow
   - Driver discovery
   - Booking creation

3. **Show Telegram Bot**
   - `/bookride` command
   - Driver action buttons

4. **Show PathPulse Integration**
   - Webhook endpoint
   - Location update flow

5. **Show Database**
   - MongoDB collections
   - Sample bookings
   - Driver records

---

## 🔧 Troubleshooting

**Backend won't start:**
- Check MongoDB connection
- Verify `.env` file exists
- Check port 4205 is free

**Android app crashes:**
- Ensure Google Play Services on emulator
- Check backend is running
- Verify base URL in `gradle.properties`

**Telegram bot not responding:**
- Check ONODE API is running
- Verify bot token in config
- Check backend URL in `TimoRidesApiService`

---

## 📞 Support

**Documentation:**
- Backend: `ride-scheduler-be/README.md`
- Android: `Timo-Android-App/README.md`
- Testing: `TESTING_PLAN.md`

**GitHub Repositories:**
- Backend: `timo-org/ride-scheduler-be`
- Android: `timo-org/Timo-Android-App`

---

**Ready for PathPulse demo!** 🚀

