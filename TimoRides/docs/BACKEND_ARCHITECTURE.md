# TimoRides Backend Architecture

**Complete guide to understanding the TimoRides backend system**

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Request Flow](#request-flow)
4. [Core Components](#core-components)
5. [API Endpoints](#api-endpoints)
6. [Data Models](#data-models)
7. [Key Features](#key-features)
8. [Ride Lifecycle](#ride-lifecycle)
9. [Integration Points](#integration-points)
10. [Configuration](#configuration)

---

## 🎯 Overview

The TimoRides backend is a **Node.js/Express** API server that manages:
- Ride bookings and lifecycle
- Driver location tracking and status
- Payment processing (Paystack)
- PathPulse integration for routing
- Webhook processing queue
- User authentication and authorization

**Tech Stack:**
- **Framework:** Express.js
- **Database:** MongoDB (via Mongoose)
- **Authentication:** JWT (JSON Web Tokens)
- **Payment:** Paystack
- **Documentation:** Swagger/OpenAPI

---

## 🏗️ Architecture

### High-Level Structure

```
Request → Middleware → Routes → Controllers → Services → Database
```

### Project Structure

```
ride-scheduler-be/
├── config/           # Configuration files (.env)
├── controllers/      # Request handlers (business logic entry points)
├── services/         # Core business logic
├── models/           # MongoDB schemas (Mongoose models)
├── routes/           # API route definitions
├── middleware/       # Auth, validation, rate limiting
├── validators/       # Input validation rules
├── utils/            # Helper functions
├── scripts/          # Seed data & utility scripts
└── server.js         # Application entry point
```

### Request Processing Flow

1. **Request arrives** → Express receives HTTP request
2. **CORS check** → Validates origin (allowed origins configured)
3. **Middleware chain:**
   - Request context middleware (adds trace IDs, logging)
   - Body parsing (JSON, URL-encoded)
   - Authentication (JWT verification for protected routes)
   - Rate limiting (prevents abuse)
   - Validation (input sanitization)
4. **Route matching** → Express router finds matching route
5. **Controller** → Handles request, calls services
6. **Service layer** → Business logic, database operations
7. **Response** → JSON response sent back to client

---

## 🔄 Request Flow

### Example: Creating a Booking

```
1. POST /api/bookings
   ↓
2. Rate Limiter (bookingLimiter)
   ↓
3. Authentication Middleware (checks JWT token)
   ↓
4. Validation Middleware (bookingValidation)
   ↓
5. Controller: createBooking()
   ↓
6. Service: createBookingUtil()
   ↓
7. Database: Booking.create()
   ↓
8. Response: { booking: {...} }
```

### Protected vs Public Routes

**Public Routes:**
- `/api/auth/login`
- `/api/auth/register`
- `/api/health`
- `/api/webhooks/*` (webhooks use signature verification instead)

**Protected Routes:**
- All other `/api/*` routes require JWT token
- Token in header: `Authorization: Bearer <token>`

---

## 🧩 Core Components

### 1. Authentication System

**Location:** `routes/authRoutes.js`, `controllers/authController.js`

**How it works:**
- Users login with email/password
- Backend validates credentials
- Generates JWT tokens:
  - **Access token** (short-lived, ~15min)
  - **Refresh token** (long-lived, ~7 days)
- Tokens stored in HTTP-only cookies or returned in response
- Subsequent requests include token in `Authorization` header

**Endpoints:**
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/auth/refresh` - Refresh access token

### 2. Booking System

**Location:** `routes/bookingRoutes.js`, `controllers/bookingController.js`

**Booking Lifecycle:**
```
pending → accepted → started → completed
         ↓
      cancelled
```

**Key Functions:**
- `createBooking()` - Rider creates a new booking
- `acceptBooking()` - Driver accepts the ride
- `cancelBooking()` - User or driver cancels
- `updatePayment()` - Payment status updates

**Booking States:**
- `pending` - Waiting for driver acceptance
- `accepted` - Driver accepted, en route to pickup
- `started` - Ride in progress
- `completed` - Ride finished
- `cancelled` - Ride cancelled

### 3. Driver Management

**Location:** `routes/driverRoutes.js`, `controllers/driverController.js`

**Features:**
- Location tracking (`PATCH /api/drivers/:id/location`)
- Status management (`PATCH /api/drivers/:id/status`)
- Proximity search (`GET /api/cars/proximity`)
- Driver profile management

**Driver Status:**
- `online` - Available for rides
- `offline` - Not accepting rides
- `in_ride` - Currently on a trip

### 4. Driver Signals System

**Location:** `routes/driverSignalRoutes.js`, `services/driverSignalService.js`

**Purpose:** Handles driver actions and PathPulse integration

**Endpoints:**
- `POST /api/driver-signal/driver-actions` - Driver accepts/starts/completes ride
- `POST /api/driver-signal/driver-location` - Real-time location updates
- `POST /api/driver-signal/driver-webhooks/pathpulse` - PathPulse webhook

**Webhook Queue:**
- Incoming webhooks are queued for processing
- Prevents duplicate processing
- Retries failed webhooks with exponential backoff
- See `services/driverWebhookQueueService.js`

### 5. Payment System

**Location:** `routes/webhookRoutes.js`, `services/gateways/paystackGateway.js`

**Integration:** Paystack (Nigerian payment gateway)

**Flow:**
1. Rider initiates payment
2. Frontend calls Paystack
3. Paystack processes payment
4. Paystack sends webhook to `/api/webhooks/paystack`
5. Backend verifies webhook signature
6. Updates booking payment status
7. Processes driver payout (if applicable)

**Webhook Events Handled:**
- `charge.success` - Payment successful
- `transfer.success` - Driver payout successful
- `transfer.failed` - Driver payout failed

### 6. Distance Calculation

**Location:** `routes/distanceRoutes.js`, `utils/distanceCalculation.js`

**Current Implementation:**
- Uses DistanceMatrix.ai API
- Calculates distance and fare between pickup and destination

**Endpoint:**
- `POST /api/distance/calculate-distance-amount`

**Future:** Will integrate with PathPulse for intelligent routing

---

## 📡 API Endpoints

### Authentication
```
POST   /api/auth/login          - User login
POST   /api/auth/register       - User registration
POST   /api/auth/refresh        - Refresh access token
```

### Bookings
```
GET    /api/bookings            - List bookings (filtered by user role)
POST   /api/bookings            - Create new booking
GET    /api/bookings/:id        - Get booking details
POST   /api/bookings/confirm-acceptance-status - Driver accepts booking
POST   /api/bookings/cancel-acceptance - Cancel booking
PATCH  /api/bookings/:id/payment - Update payment status
```

### Drivers
```
GET    /api/drivers/:id/status  - Get driver status
PATCH  /api/drivers/:id/status - Update driver status (online/offline)
PATCH  /api/drivers/:id/location - Update driver location
GET    /api/cars/proximity      - Find nearby drivers
```

### Driver Signals
```
POST   /api/driver-signal/driver-actions - Driver action (accept/start/complete)
POST   /api/driver-signal/driver-location - Location update
POST   /api/driver-signal/driver-webhooks/pathpulse - PathPulse webhook
```

### Webhooks
```
POST   /api/webhooks/paystack  - Paystack payment webhook
```

### Health & Metrics
```
GET    /api/health              - System health check
GET    /api/metrics             - System metrics
```

---

## 🗄️ Data Models

### User Model
- **Role:** `user` (rider) or `driver` or `admin`
- **Fields:** email, password (hashed), fullName, phone, wallet balance
- **Location:** `models/userModel.js`

### Driver Model
- **Extends:** User model
- **Fields:** location (lat/lng), status, completedRides, rating
- **Location:** `models/driverModel.js`

### Car Model
- **Fields:** driver reference, vehicle details (make, model, reg number), location
- **Location:** `models/carModal.js`

### Booking Model
- **Fields:**
  - User reference (rider)
  - Car reference (driver's vehicle)
  - Source/destination locations
  - Trip details (distance, duration, amount)
  - Status (pending/accepted/started/completed/cancelled)
  - Payment information
  - Timeline (createdAt, acceptedAt, startedAt, completedAt)
- **Location:** `models/bookingModal.js`

### DriverWebhookQueue Model
- **Purpose:** Queue for processing webhook events
- **Fields:** eventType, source, payload, status, attempts, nextAttemptAt
- **Location:** `models/driverWebhookQueue.js`

### DriverSignalLog Model
- **Purpose:** Audit log of driver actions
- **Fields:** driverId, action, bookingId, timestamp, metadata
- **Location:** `models/driverSignalLog.js`

---

## ⚙️ Key Features

### 1. Webhook Queue System

**Why it exists:**
- Prevents duplicate processing
- Handles webhook failures gracefully
- Retries with exponential backoff

**How it works:**
1. Webhook arrives → Added to queue with status `pending`
2. Worker process claims next pending webhook
3. Processes webhook (updates booking, logs action, etc.)
4. On success → Status set to `completed`
5. On failure → Status set to `failed`, `nextAttemptAt` scheduled
6. Retries up to `maxAttempts` (default: 3)

**Location:** `services/driverWebhookQueueService.js`

**Worker Script:** `scripts/processDriverWebhooks.js`
```bash
npm run driver-signal-worker
```

### 2. PathPulse Integration

**Webhook Endpoint:** `POST /api/driver-signal/driver-webhooks/pathpulse`

**What it receives:**
- Driver location updates
- Driver actions (accept/start/complete ride)
- Route information
- ETA updates

**Security:**
- Signature verification using webhook secret
- IP whitelist (optional)
- Rate limiting

**Processing:**
- Webhook verified → Added to queue
- Queue worker processes → Updates booking/driver status
- Logs all events for audit trail

### 3. Payment Processing

**Integration:** Paystack

**Flow:**
1. Rider selects payment method (cash/wallet/card)
2. If card → Frontend calls Paystack SDK
3. Paystack processes payment
4. Webhook sent to `/api/webhooks/paystack`
5. Backend verifies webhook signature
6. Updates booking payment status
7. If driver payout → Initiates Paystack transfer

**Payment States:**
- `unpaid` - No payment yet
- `pending` - Payment initiated
- `paid` - Payment confirmed
- `refunded` - Payment refunded

### 4. Proximity Search

**Endpoint:** `GET /api/cars/proximity`

**How it works:**
1. Receives pickup location (lat/lng)
2. Queries MongoDB for nearby drivers
3. Filters by:
   - Driver status (online)
   - Car availability
   - Distance radius
4. Returns sorted by distance (nearest first)

**Current:** Uses geolib for distance calculation
**Future:** Will use PathPulse for intelligent driver matching

### 5. Rate Limiting

**Purpose:** Prevent API abuse

**Applied to:**
- Authentication routes (login/register)
- Booking creation
- Payment updates

**Configuration:** `middleware/rateLimiters.js`

---

## 🚗 Ride Lifecycle

### Complete Flow

```
1. RIDER CREATES BOOKING
   POST /api/bookings
   → Status: pending
   → Booking saved to database

2. SYSTEM FINDS DRIVERS
   GET /api/cars/proximity?lat=X&lng=Y
   → Returns nearby available drivers
   → Frontend shows driver options

3. DRIVER ACCEPTS RIDE
   POST /api/driver-signal/driver-actions
   { action: 'accept', bookingId: '...' }
   → Status: accepted
   → Booking linked to driver's car
   → Rider notified

4. DRIVER ARRIVES AT PICKUP
   (Driver updates location continuously)
   POST /api/driver-signal/driver-location
   → Location stored
   → ETA calculated

5. DRIVER STARTS RIDE
   POST /api/driver-signal/driver-actions
   { action: 'start', bookingId: '...' }
   → Status: started
   → Trip start time recorded

6. DRIVER COMPLETES RIDE
   POST /api/driver-signal/driver-actions
   { action: 'complete', bookingId: '...' }
   → Status: completed
   → Trip end time recorded
   → Payment processed

7. PAYMENT CONFIRMED
   POST /api/webhooks/paystack
   → Payment status: paid
   → Driver payout initiated (if applicable)

8. RATING SUBMITTED
   (Handled in frontend, stored in booking)
```

### Status Transitions

```
pending
  ↓ (driver accepts)
accepted
  ↓ (driver starts ride)
started
  ↓ (driver completes)
completed

OR

pending/accepted/started
  ↓ (user or driver cancels)
cancelled
```

---

## 🔗 Integration Points

### PathPulse Integration

**Webhook Endpoint:** `POST /api/driver-signal/driver-webhooks/pathpulse`

**Current Implementation:**
- Receives webhook events from PathPulse
- Verifies signature
- Queues for processing
- Updates driver location/status

**Future Enhancements:**
- Route calculation API integration
- Turn-by-turn navigation
- ETA accuracy improvements
- Traffic-aware routing

**Configuration:**
- Webhook secret in `.env`: `PATHPULSE_WEBHOOK_SECRET`
- Signature verification enabled

### Paystack Integration

**Webhook Endpoint:** `POST /api/webhooks/paystack`

**Events Handled:**
- `charge.success` - Payment successful
- `transfer.success` - Driver payout successful
- `transfer.failed` - Driver payout failed

**Configuration:**
- Secret key: `PAYSTACK_SECRET_KEY`
- Webhook secret: `PAYSTACK_WEBHOOK_SECRET`

### Distance Calculation

**Current:** DistanceMatrix.ai API
**Endpoint:** `POST /api/distance/calculate-distance-amount`

**Future:** PathPulse routing API for intelligent distance/route calculation

---

## ⚙️ Configuration

### Environment Variables

**Required:**
```env
Database_Url=mongodb://...          # MongoDB connection string
ACCESS_TOKEN_SECRET=...             # JWT signing secret
REFRESH_TOKEN_SECRET=...            # JWT refresh secret
PORT=4205                           # Server port
```

**Optional:**
```env
PAYSTACK_SECRET_KEY=...             # Paystack secret key
PAYSTACK_WEBHOOK_SECRET=...         # Webhook verification
PATHPULSE_WEBHOOK_SECRET=...        # PathPulse webhook secret
TWILIO_ACCOUNT_SID=...              # SMS notifications (optional)
NODE_ENV=development                # Environment mode
```

### Setup Steps

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Configure environment:**
   ```bash
   cp config/env.example config/.env
   # Edit config/.env with your settings
   ```

3. **Seed demo data:**
   ```bash
   npm run seed
   ```
   Creates:
   - Admin: `admin@timorides.com` / `ChangeMe123!`
   - Driver: `driver@timorides.com` / `DriverDemo123!`
   - Rider: `rider@timorides.com` / `RiderDemo123!`
   - Sample bookings

4. **Start server:**
   ```bash
   npm start
   ```

5. **Access API docs:**
   - Swagger UI: http://localhost:4205/api-docs
   - Health check: http://localhost:4205/api/health

---

## 🧪 Testing

### Postman Collection
- Location: `tests/driver-signal.postman_collection.json`
- Includes: Driver signal endpoints, booking endpoints

### REST Client
- Location: `tests/payments.rest`
- Payment webhook testing

### Seed Script
```bash
npm run seed
```
Creates demo accounts and sample bookings for testing.

---

## 📊 Monitoring

### Health Endpoint
`GET /api/health`

Returns:
- MongoDB connection status
- Pending booking counts
- Driver webhook queue depth
- Driver signal metrics
- System uptime

### Metrics Endpoint
`GET /api/metrics`

Returns system performance metrics (if enabled).

---

## 🔐 Security

### Authentication
- JWT tokens with expiration
- Refresh token rotation
- Password hashing (bcrypt)

### Webhook Security
- Signature verification (HMAC)
- IP whitelisting (optional)
- Rate limiting

### CORS
- Configured allowed origins
- Development vs production settings

---

## 🚀 Deployment

1. Set environment variables on hosting platform
2. Configure MongoDB connection (Atlas recommended)
3. Set Paystack keys
4. Set PathPulse webhook secret
5. Deploy and start:
   ```bash
   npm start
   ```

**Recommended Platforms:**
- Render.com
- Railway
- Heroku
- AWS/DigitalOcean

---

## 📝 Key Files Reference

| File | Purpose |
|------|---------|
| `server.js` | Application entry point, Express setup |
| `routes/index.js` | All API routes registration |
| `controllers/bookingController.js` | Booking business logic |
| `services/driverSignalService.js` | Driver signal processing |
| `services/driverWebhookQueueService.js` | Webhook queue management |
| `models/bookingModal.js` | Booking database schema |
| `middleware/authMiddleware.js` | JWT authentication |
| `utils/distanceCalculation.js` | Distance/fare calculation |

---

## 🐛 Troubleshooting

### MongoDB Connection Failed
- Check connection string format
- Verify network access (for Atlas)
- Check credentials

### Port Already in Use
- Change `PORT` in `.env`
- Or kill process: `lsof -ti:4205 | xargs kill`

### Webhook Not Working
- Verify webhook secret matches
- Check signature verification
- Review webhook queue logs
- Check worker process is running: `npm run driver-signal-worker`

### Authentication Failing
- Verify JWT secrets are set
- Check token expiration
- Ensure token in `Authorization: Bearer <token>` header

---

## 📞 Support

- **API Documentation:** http://localhost:4205/api-docs
- **Health Check:** http://localhost:4205/api/health
- **Logs:** Check console output or log files

---

**TimoRides Backend API** - Built for premium ride-hailing 🚗

