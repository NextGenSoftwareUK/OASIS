# Starting Shipex Pro Services

## Architecture

Shipex Pro runs as a **separate API service** that integrates with OASIS API for authentication.

```
┌──────────────────┐
│ Shipex Pro UI    │  (Frontend)
│ Port: 8000       │
└────────┬─────────┘
         │
    ┌────┴────┐
    │        │
    ▼        ▼
┌─────────┐ ┌──────────┐
│ Shipex  │ │  OASIS   │
│ Pro API │ │   API    │
│ :5005   │ │  :5002   │
└────┬────┘ └──────────┘
     │
     ▼
┌─────────┐
│ MongoDB │
│ :27017  │
└─────────┘
```

## Quick Start

### 1. Start OASIS API (for authentication)

```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

**Expected:** API starts on `https://localhost:5002`

### 2. Start Shipex Pro API

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex/ShipexPro.API
dotnet run
```

**Expected:** API starts on `https://localhost:5005`

### 3. Start Frontend

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex/shipex-pro-frontend
./start.sh
```

**Expected:** Frontend on `http://localhost:8000`

## Service URLs

- **OASIS API:** `https://localhost:5002` (Avatar authentication)
- **Shipex Pro API:** `https://localhost:5005` (Business operations)
- **Frontend:** `http://localhost:8000` (UI)

## Configuration

### Shipex Pro API (`appsettings.json`)

```json
{
  "OASIS": {
    "ApiUrl": "https://localhost:5002"
  },
  "ShipexPro": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ShipexPro"
  }
}
```

### Frontend (`js/shipex-api.js`)

- `baseURL`: Shipex Pro API (`https://localhost:5005`)
- `oasisApiURL`: OASIS API (`https://localhost:5002`)

## Testing Flow

1. **Frontend** → Calls OASIS API for avatar login
2. **Frontend** → Gets JWT token
3. **Frontend** → Calls Shipex Pro API with JWT token
4. **Shipex Pro API** → Validates token (optional) or just uses it
5. **Shipex Pro API** → Stores merchant data in MongoDB

## Troubleshooting

### CORS Errors

If frontend can't call APIs:
- OASIS API already has CORS configured
- Shipex Pro API has CORS in `Program.cs`
- Both allow `http://localhost:8000`

### Port Conflicts

- OASIS API: Change port in `launchSettings.json`
- Shipex Pro API: Change in `Program.cs` or `appsettings.json`
- Frontend: Change in `start.sh` or server command

### MongoDB Not Running

```bash
# Start MongoDB (macOS)
brew services start mongodb-community

# Or Docker
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

---

**Status:** Ready to start all services  
**Last Updated:** January 2025
