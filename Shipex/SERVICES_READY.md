# ✅ Services Ready to Start

## Architecture Decision

**Shipex Pro runs as a separate API service** (not integrated into OASIS API).

### Why Separate?
- ✅ **Business Application** - Shipex Pro is logistics/shipping business logic, not core OASIS infrastructure
- ✅ **Modularity** - Independent deployment and scaling
- ✅ **Clean Architecture** - Clear separation of concerns
- ✅ **Flexibility** - Can be updated/replaced independently

## Services to Start

### 1. OASIS API (Port 5002)
**Purpose:** Avatar authentication only

```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

**URL:** `https://localhost:5002`

### 2. Shipex Pro API (Port 5005)
**Purpose:** All Shipex Pro business operations

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex/ShipexPro.API
dotnet run
```

**URL:** `https://localhost:5005`

### 3. Frontend (Port 8000)
**Purpose:** User interface

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex/shipex-pro-frontend
./start.sh
```

**URL:** `http://localhost:8000`

## Service Communication

```
Frontend (8000)
    │
    ├─→ OASIS API (5002) - Avatar auth
    │
    └─→ Shipex Pro API (5005) - Business ops
            │
            └─→ MongoDB (27017) - Data storage
```

## Configuration

### Frontend (`js/shipex-api.js`)
- `baseURL`: `https://localhost:5005` (Shipex Pro API)
- `oasisApiURL`: `https://localhost:5002` (OASIS API)

### Shipex Pro API (`appsettings.json`)
- `OASIS:ApiUrl`: `https://localhost:5002`
- MongoDB connection string
- iShip/Shipox/QuickBooks API keys

## Quick Start Script

Create `start-all.sh`:

```bash
#!/bin/bash

echo "🚀 Starting all Shipex Pro services..."

# Start OASIS API (background)
echo "📡 Starting OASIS API..."
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run > /tmp/oasis-api.log 2>&1 &
OASIS_PID=$!

# Wait for OASIS API to start
sleep 5

# Start Shipex Pro API (background)
echo "📦 Starting Shipex Pro API..."
cd /Volumes/Storage/OASIS_CLEAN/Shipex/ShipexPro.API
dotnet run > /tmp/shipex-api.log 2>&1 &
SHIPEX_PID=$!

# Wait for Shipex Pro API to start
sleep 5

# Start Frontend
echo "🌐 Starting Frontend..."
cd /Volumes/Storage/OASIS_CLEAN/Shipex/shipex-pro-frontend
python3 -m http.server 8000

# Cleanup on exit
trap "kill $OASIS_PID $SHIPEX_PID" EXIT
```

## Testing

1. **Start OASIS API** - Provides avatar authentication
2. **Start Shipex Pro API** - Provides business endpoints
3. **Start Frontend** - User interface
4. **Test Flow:**
   - Open `http://localhost:8000`
   - Click "Skip for Testing" OR login with OASIS avatar
   - Test quote request, shipment creation, tracking

## API Endpoints

### OASIS API (Port 5002)
- `POST /api/avatar/authenticate` - Login
- `POST /api/avatar/register` - Register

### Shipex Pro API (Port 5005)
- `GET /api/shipexpro/merchant/by-avatar/{avatarId}`
- `POST /api/shipexpro/merchant/create-from-avatar`
- `GET /api/shipexpro/shipments?merchantId={id}`
- `POST /api/shipexpro/shipox/quote-request`
- `POST /api/shipexpro/shipox/confirm-shipment`
- `GET /api/shipexpro/shipox/track/{trackingNumber}`
- `GET /api/shipexpro/markups`
- `GET /api/shipexpro/quickbooks/authorize`

---

**Status:** ✅ **All Services Ready**  
**Last Updated:** January 2025
