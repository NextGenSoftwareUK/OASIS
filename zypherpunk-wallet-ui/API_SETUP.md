# API Setup for Zypherpunk Wallet UI

## Current Issue

The external OASIS API (`http://api.oasisplatform.world`) is currently returning HTML (bot protection) instead of JSON responses. This is a known issue.

## Solutions

### Option 1: Use Local OASIS API (Recommended)

1. **Start your local OASIS API server** (usually on port 5000)
2. **Create `.env.local` file** in the project root:
   ```bash
   NEXT_PUBLIC_OASIS_API_URL=http://localhost:5000
   ```
3. **Restart the Next.js dev server**

### Option 2: Disable API Proxy (Direct Connection)

If you want to connect directly to the API (bypassing the proxy), set:

```bash
NEXT_PUBLIC_USE_API_PROXY=false
```

### Option 3: Use Mock Data (Development Only)

For development/testing without an API, you can modify the store to return mock data.

## Current Behavior

- ✅ **UI compiles and runs** - The app will load even if the API is unavailable
- ⚠️ **Error message displayed** - A helpful error message shows when API is unavailable
- ✅ **Graceful degradation** - The UI still works, just without wallet data

## Proxy Route

The app uses a Next.js API proxy route (`/app/api/proxy/wallet/[...path]/route.ts`) to avoid CORS issues. This proxy:
- Forwards requests from the frontend to the OASIS API
- Adds proper headers to avoid bot protection
- Handles errors gracefully

## Testing API Connection

You can test if the API is accessible:

```bash
curl http://api.oasisplatform.world/api/wallet/load_wallets_by_id/12345678-1234-1234-1234-123456789012
```

If it returns HTML instead of JSON, the API is blocked by bot protection.

