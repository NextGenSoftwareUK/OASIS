# Shipex Pro API

Standalone API service for Shipex Pro logistics management.

## Why Separate?

Shipex Pro is a **business application**, not a core OASIS provider. Running it as a separate service provides:

- ✅ **Modularity** - Independent deployment and scaling
- ✅ **Separation of Concerns** - Business logic separate from core OASIS
- ✅ **Flexibility** - Can be deployed independently
- ✅ **Clean Architecture** - Follows microservices principles

## Architecture

```
┌─────────────────┐
│  Shipex Pro UI  │
│  (Frontend)     │
└────────┬────────┘
         │
         ├─────────────────┐
         │                 │
         ▼                 ▼
┌─────────────────┐  ┌─────────────────┐
│  Shipex Pro API │  │   OASIS API     │
│  (This Service) │  │  (Auth Provider)│
│  Port: 5005     │  │  Port: 5002     │
└────────┬────────┘  └─────────────────┘
         │
         ▼
┌─────────────────┐
│    MongoDB      │
│  (ShipexPro DB) │
└─────────────────┘
```

## Setup

### 1. Build the API

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex/ShipexPro.API
dotnet build
```

### 2. Configure

Edit `appsettings.json`:
- MongoDB connection string
- OASIS API URL (for avatar auth)
- iShip/Shipox API keys (if available)
- QuickBooks OAuth credentials

### 3. Run

```bash
dotnet run
```

API will be available at: `https://localhost:5005` (or configured port)

## API Endpoints

All endpoints are under `/api/shipexpro/`:

- `POST /api/shipexpro/merchant/create-from-avatar` - Create merchant
- `GET /api/shipexpro/merchant/by-avatar/{avatarId}` - Get merchant
- `GET /api/shipexpro/shipments?merchantId={id}` - List shipments
- `POST /api/shipexpro/shipox/quote-request` - Request quotes
- `POST /api/shipexpro/shipox/confirm-shipment` - Create shipment
- `GET /api/shipexpro/shipox/track/{trackingNumber}` - Track shipment
- `GET /api/shipexpro/markups` - List markups
- `POST /api/shipexpro/markups` - Create markup
- `GET /api/shipexpro/quickbooks/authorize` - QuickBooks OAuth

## OASIS Integration

The Shipex Pro API calls OASIS API for:
- Avatar authentication (`POST /api/avatar/authenticate`)
- Avatar registration (`POST /api/avatar/register`)

Configure OASIS API URL in `appsettings.json`:
```json
{
  "OASIS": {
    "ApiUrl": "https://localhost:5002"
  }
}
```

## Frontend Configuration

Update frontend `js/shipex-api.js`:
```javascript
this.baseURL = 'https://localhost:5005'; // Shipex Pro API
```

For OASIS avatar auth, frontend calls OASIS API directly.

---

**Status:** Ready to run as standalone service  
**Port:** 5005 (configurable)
