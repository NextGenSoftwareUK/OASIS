# OASIS Ecosystem Startup Guide

## Quick Start

### 🚀 Start Everything
```bash
# Double-click or run:
start-oasis-ecosystem.bat
```

### 🛑 Stop Everything
```bash
# Double-click or run:
stop-oasis-ecosystem.bat
```

## What Gets Started

### 1. **ONODE WebAPI (Web4 OASIS API)** - Port 5000
- **URL**: http://localhost:5000
- **Health Check**: http://localhost:5000/api/health
- **Subscription API**: http://localhost:5000/api/subscription

### 2. **STAR Web UI** - Port 3000
- **URL**: http://localhost:3000
- Complete OASIS management interface
- Avatar management, wallets, NFTs, quests, etc.

### 3. **OASIS Web4 Marketing Site** - Port 5173
- **URL**: http://localhost:5173
- Modern marketing site with plans, providers, APIs
- Integrated checkout system

## API Endpoints

### Subscription API
- `GET /api/subscription/plans` - List available plans
- `POST /api/subscription/checkout/session` - Create checkout session
- `GET /api/subscription/subscriptions/me` - User subscriptions
- `GET /api/subscription/orders/me` - User orders
- `POST /api/subscription/webhook` - Stripe webhook handler

### Core APIs
- `GET /api/avatar/*` - Avatar management
- `GET /api/wallet/*` - Universal wallet system
- `GET /api/nft/*` - NFT operations
- `GET /api/karma/*` - Karma system
- `GET /api/data/*` - Data management

## Prerequisites

- **.NET 8 SDK** - For the WebAPI
- **Node.js 18+** - For React applications
- **npm** - Package manager

## Manual Startup (if needed)

### Start WebAPI only:
```bash
cd "..\ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI"
dotnet run --urls "http://localhost:5000"
```

### Start STAR Web UI only:
```bash
cd "..\STAR ODK\NextGenSoftware.OASIS.STAR.WebUI\ClientApp"
npm start
```

### Start Marketing Site only:
```bash
# Already in oasisweb4.com directory
npm run dev
```

## Troubleshooting

### Port Already in Use
- Check if services are already running
- Use `stop-oasis-ecosystem.bat` to stop all services
- Or manually kill processes using Task Manager

### Build Failures
- Ensure all dependencies are installed
- Run `npm install` in each project directory
- Check .NET SDK version compatibility

### Services Not Starting
- Check Windows Firewall settings
- Ensure ports 3000, 5000, 5173 are available
- Check console output for error messages

## Development Mode

For development, you can start services individually:

```bash
# Terminal 1 - WebAPI
cd "..\ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI"
dotnet run --urls "http://localhost:5000"

# Terminal 2 - STAR Web UI
cd "..\STAR ODK\NextGenSoftware.OASIS.STAR.WebUI\ClientApp"
npm start

# Terminal 3 - Marketing Site (from oasisweb4.com directory)
npm run dev
```

## Production Deployment

For production deployment:

1. **Build for production**:
   ```bash
   # WebAPI
   cd "..\ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI"
   dotnet publish -c Release
   
   # STAR Web UI
   cd "..\STAR ODK\NextGenSoftware.OASIS.STAR.WebUI\ClientApp"
   npm run build
   
   # Marketing Site (from oasisweb4.com directory)
   npm run build
   ```

2. **Deploy to hosting service** (Azure, AWS, etc.)

3. **Configure environment variables** for production

## Support

If you encounter issues:
1. Check the console output for error messages
2. Ensure all prerequisites are installed
3. Verify port availability
4. Check firewall settings
