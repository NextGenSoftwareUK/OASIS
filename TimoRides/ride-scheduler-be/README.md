# TimoRides Backend API

**Ride-hailing backend service for TimoRides platform**

---

## 🚀 Quick Start

### Prerequisites
- Node.js 18+
- MongoDB (Atlas or local)
- npm 9+

### Installation

```bash
npm install
```

### Configuration

1. Copy environment template:
```bash
cp config/env.example config/.env
```

2. Edit `config/.env` with your settings:
   - MongoDB connection string
   - JWT secrets
   - Paystack keys (for payments)
   - Twilio credentials (optional, for SMS)

### Start Server

```bash
npm start
```

Server runs on `http://localhost:4205`

### Seed Demo Data

```bash
npm run seed
```

Creates:
- Admin account: `admin@timorides.com` / `ChangeMe123!`
- Driver account: `driver@timorides.com` / `DriverDemo123!`
- Rider account: `rider@timorides.com` / `RiderDemo123!`
- Sample bookings

---

## 📚 API Documentation

**Swagger UI:** http://localhost:4205/api-docs

Interactive API documentation with request/response examples.

---

## 🔑 Key Features

- **Ride Booking** - Complete booking lifecycle management
- **Driver Management** - Location tracking, status updates
- **Payment Processing** - Paystack integration for fiat payments
- **Driver Signals** - Accept/start/complete ride actions
- **PathPulse Integration** - Webhook endpoint for driver location/actions
- **Webhook Queue** - Reliable webhook processing with retries

---

## 📡 API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration

### Bookings
- `GET /api/bookings` - List bookings
- `POST /api/bookings` - Create booking
- `GET /api/bookings/:id` - Get booking details
- `PATCH /api/bookings/:id/payment` - Update payment

### Drivers
- `GET /api/cars/proximity` - Find nearby drivers
- `PATCH /api/drivers/:id/location` - Update driver location
- `PATCH /api/drivers/:id/status` - Update driver status

### Driver Signals
- `POST /api/driver-signals/action` - Driver action (accept/start/complete)
- `POST /api/driver-signals/pathpulse` - PathPulse webhook
- `POST /api/driver-signals/location` - Location update

### Webhooks
- `POST /api/webhooks/paystack` - Paystack payment webhook

### Health
- `GET /api/health` - System health check

---

## 🗂️ Project Structure

```
ride-scheduler-be/
├── config/           # Configuration files
├── controllers/      # Request handlers
├── services/         # Business logic
├── models/           # Database schemas
├── routes/           # API routes
├── middleware/       # Auth, validation, etc.
├── validators/       # Input validation
├── utils/            # Helper functions
├── scripts/          # Seed & utility scripts
└── server.js         # Entry point
```

---

## 🔐 Authentication

Uses JWT tokens:
- Access token (short-lived)
- Refresh token (long-lived)

Include in requests:
```
Authorization: Bearer <access_token>
```

---

## 💳 Payment Integration

**Paystack:**
- Webhook: `/api/webhooks/paystack`
- Handles: `charge.success`, `transfer.success`, `transfer.failed`
- Driver payouts via Paystack transfers

---

## 🔗 PathPulse Integration

**Webhook Endpoint:** `POST /api/driver-signals/pathpulse`

Receives:
- Driver location updates
- Driver actions (accept/start/complete)

**Configuration:**
- PathPulse webhook secret in `.env`
- Signature verification enabled

---

## 🧪 Testing

**Postman Collection:**
- `tests/driver-signal.postman_collection.json`

**REST Client:**
- `tests/payments.rest`

**Seed Script:**
```bash
npm run seed
```

---

## 📊 Health & Monitoring

**Health Endpoint:** `GET /api/health`

Returns:
- MongoDB connection status
- Pending booking counts
- Driver webhook queue depth
- Driver signal metrics

---

## 🚀 Deployment

1. Set environment variables on hosting platform
2. Configure MongoDB connection
3. Set Paystack keys
4. Deploy and start:
```bash
npm start
```

---

## 📝 Environment Variables

See `config/env.example` for all available options.

**Required:**
- `Database_Url` - MongoDB connection string
- `ACCESS_TOKEN_SECRET` - JWT signing secret
- `REFRESH_TOKEN_SECRET` - JWT refresh secret

**Optional:**
- `PAYSTACK_SECRET_KEY` - For payments
- `TWILIO_ACCOUNT_SID` - For SMS (can be stubbed)
- `PAYSTACK_WEBHOOK_SECRET` - Webhook verification

---

## 🐛 Troubleshooting

**MongoDB Connection Failed:**
- Check connection string format
- Verify network access (for Atlas)
- Check credentials

**Port Already in Use:**
- Change `PORT` in `.env`
- Or kill process on port 4205

**Webhook Not Working:**
- Verify webhook secret matches
- Check signature verification
- Review webhook queue logs

---

## 📞 Support

For issues or questions, see:
- API Documentation: http://localhost:4205/api-docs
- Health Check: http://localhost:4205/api/health

---

**TimoRides Backend API** - Built for premium ride-hailing 🚗
