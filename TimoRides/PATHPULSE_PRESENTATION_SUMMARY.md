# TimoRides - PathPulse Presentation Summary

**Date:** 2025-11-17  
**For:** PathPulse Team

---

## 🎯 What is TimoRides?

TimoRides is a ride-hailing platform that connects riders with premium drivers. The platform features:

- **Rider Marketplace** - Android app for booking rides
- **Driver Channel** - Telegram bot + PathPulse Scout integration
- **Backend API** - Node.js service managing bookings, payments, and driver signals
- **Payment Processing** - Paystack integration for fiat transactions

---

## 🏗️ Architecture Overview

```
┌─────────────────┐
│  Android App    │ ← Riders book rides
│  (Rider)        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Backend API    │ ← Central booking & payment system
│  (Node.js)      │
└────────┬────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌─────────┐ ┌──────────────┐
│Telegram │ │  PathPulse   │ ← Driver channels
│  Bot    │ │    Scout     │
└─────────┘ └──────────────┘
```

---

## 📦 Components

### 1. Backend API (`ride-scheduler-be`)

**Location:** `TimoRides/ride-scheduler-be/`

**Tech:**
- Node.js + Express
- MongoDB (Atlas)
- JWT Authentication
- Paystack Payments

**Key Features:**
- ✅ Ride booking lifecycle (pending → accepted → started → completed)
- ✅ Driver location tracking
- ✅ Payment processing (Paystack webhooks)
- ✅ Driver signal processing (accept/start/complete/cancel)
- ✅ PathPulse webhook integration
- ✅ Driver webhook queue with retry logic
- ✅ Swagger API documentation

**Start:**
```bash
cd TimoRides/ride-scheduler-be
npm install
npm run seed  # Creates demo data
npm start     # Runs on port 4205
```

**API Docs:** http://localhost:4205/api-docs

### 2. Android Rider App (`Timo-Android-App`)

**Location:** `TimoRides/Timo-Android-App/`

**Tech:**
- Java (Android SDK 33)
- Google Maps API
- Retrofit (API client)
- Secure token storage

**Key Features:**
- ✅ User authentication
- ✅ Nearby driver discovery
- ✅ Ride booking
- ✅ Real-time tracking (when Maps works)
- ✅ Payment integration ready

**Build:**
```bash
cd TimoRides/Timo-Android-App
./gradlew assembleDebug
```

**Note:** Requires emulator with Google Play Services for Maps

### 3. Telegram Bot (Driver Channel)

**Location:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/`

**Tech:**
- C# / .NET
- Telegram.Bot library
- OASIS ONODE integration

**Key Features:**
- ✅ Driver ride notifications
- ✅ Driver action buttons (accept/decline/start/complete)
- ✅ Location sharing
- ✅ Ride booking via Telegram

**Start:**
```bash
cd /Volumes/Storage/OASIS_CLEAN
dotnet run --project ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
```

---

## 🔗 PathPulse Integration

### Webhook Endpoint

**URL:** `POST /api/driver-signals/pathpulse`

**Purpose:**
- Receive driver location updates from PathPulse Scout
- Process driver actions (accept/start/complete)
- Update booking status in real-time

**Implementation:**
- `services/driverSignalService.js` - Processes webhooks
- Signature verification for security
- Queue system with retry logic

**Configuration:**
- PathPulse webhook secret in backend `.env`
- Webhook URL configured in PathPulse dashboard

---

## 💳 Payment Flow

1. **Rider initiates booking** → Backend creates pending booking
2. **Payment processed** → Paystack webhook confirms payment
3. **Wallet funded** → Rider wallet credited
4. **Ride completed** → Driver wallet credited
5. **Driver payout** → Paystack transfer to driver bank account

**Endpoints:**
- `POST /api/webhooks/paystack` - Paystack webhook handler
- `PATCH /api/bookings/:id/payment` - Update payment status

---

## 🧪 Demo Data

**Seeded Accounts:**
- Admin: `admin@timorides.com` / `ChangeMe123!`
- Rider: `rider@timorides.com` / `RiderDemo123!`
- Driver: `driver@timorides.com` / `DriverDemo123!`

**Run Seed:**
```bash
cd TimoRides/ride-scheduler-be
npm run seed
```

---

## 📊 Key Metrics & Health

**Health Endpoint:** `GET /api/health`

**Returns:**
- MongoDB connection status
- Pending booking counts
- Driver webhook queue depth
- Driver signal metrics

---

## 🚀 Quick Demo Flow

### 1. Start Backend
```bash
cd TimoRides/ride-scheduler-be
npm start
```

### 2. Test Health
```bash
curl http://localhost:4205/api/health
```

### 3. View API Docs
Open: http://localhost:4205/api-docs

### 4. Android App
- Build and install APK
- Login with rider credentials
- View nearby drivers
- Create booking

### 5. Telegram Bot
- Start ONODE API
- Send `/start` to bot
- Send `/bookride` to test booking flow

---

## 📁 Code Structure

### Backend
```
ride-scheduler-be/
├── controllers/     # API endpoints
├── services/        # Business logic
├── models/          # Database schemas
├── routes/          # Route definitions
└── scripts/         # Seed & utilities
```

### Android
```
Timo-Android-App/
├── app/src/main/java/     # Java source code
└── app/src/main/res/      # UI resources
```

---

## 🔐 Security

- JWT authentication
- Encrypted token storage (Android)
- Webhook signature verification
- Rate limiting
- Audit logging

---

## 📝 Documentation

- **Backend:** `ride-scheduler-be/README.md`
- **Android:** `Timo-Android-App/README.md`
- **Testing:** `TESTING_PLAN.md`
- **Demo Guide:** `PATHPULSE_DEMO_GUIDE.md`

---

## 🎬 Presentation Flow

1. **Architecture Overview** (5 min)
   - Show component diagram
   - Explain data flow

2. **Backend Demo** (10 min)
   - Start server
   - Show Swagger docs
   - Demonstrate API endpoints
   - Show database structure

3. **Android App Demo** (10 min)
   - Show login flow
   - Demonstrate driver discovery
   - Create a booking
   - Show booking status

4. **PathPulse Integration** (10 min)
   - Show webhook endpoint
   - Demonstrate location updates
   - Show driver action processing

5. **Payment Flow** (5 min)
   - Show Paystack integration
   - Demonstrate webhook handling
   - Show driver payout flow

6. **Q&A** (10 min)

---

## 🔗 GitHub Repositories

- **Backend:** https://github.com/timo-org/ride-scheduler-be
- **Android:** https://github.com/timo-org/Timo-Android-App

---

## ✅ Pre-Presentation Checklist

- [ ] Backend running and healthy
- [ ] Android app built and ready
- [ ] Telegram bot configured (optional)
- [ ] Demo data seeded
- [ ] Swagger docs accessible
- [ ] All code pushed to GitHub
- [ ] Presentation flow rehearsed

---

**Ready for PathPulse presentation!** 🚀

