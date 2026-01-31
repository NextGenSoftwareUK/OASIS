# ✅ APIs Status

## Current Status

### ✅ SmartContractGenerator API
- **Status:** ✅ **RUNNING**
- **Port:** 5001
- **URL:** http://localhost:5001
- **Test:** `curl http://localhost:5001/api/v1/contracts/cache-stats`
- **Response:** `{"enabled":false,"message":"sccache not found"}` (This is OK - sccache is optional)

### ✅ OASIS API (ONODE WebAPI)
- **Status:** Built and runs on ports from launchSettings (not 5000 unless overridden)
- **Ports:** HTTP **5003**, HTTPS **5004** (see `Properties/launchSettings.json`)
- **URL (HTTP):** http://localhost:5003
- **URL (HTTPS):** https://localhost:5004
- **Test:** `curl http://localhost:5003/api/health`
- **MCP:** Set `OASIS_API_URL=http://localhost:5003` in MCP `.env` (default in config is now 5003)

## Quick Commands

### Check API Status
```bash
# OASIS API (HTTP 5003 or HTTPS 5004)
curl http://localhost:5003/api/health
# or: curl -k https://localhost:5004/api/health

# SmartContractGenerator API
curl http://localhost:5001/api/v1/contracts/cache-stats
```

### Build & Start APIs Manually

**OASIS API (build then run):**
```bash
cd /Users/maxgershfield/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet build
dotnet run
# Listens on http://localhost:5003 and https://localhost:5004 by default
```

**SmartContractGenerator API:**
```bash
cd /Users/maxgershfield/OASIS_CLEAN/SmartContractGenerator/src/SmartContractGen/ScGen.API
dotnet run --urls "http://localhost:5001"
```

### Stop APIs
```bash
# Stop OASIS API
pkill -f "dotnet.*OASIS.API.ONODE.WebAPI"

# Stop SmartContractGenerator API
pkill -f "dotnet.*ScGen.API"

# Or by port
lsof -ti:5003 | xargs kill -9   # OASIS API HTTP
lsof -ti:5004 | xargs kill -9   # OASIS API HTTPS
lsof -ti:5001 | xargs kill -9   # SmartContractGenerator
```

## Test MCP Server Now

With SmartContractGenerator API running, you can test:

```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP

# Test health check
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"scgen_get_cache_stats","arguments":{}}}' | node dist/index.js
```

## Next Steps

1. ✅ SmartContractGenerator API is running on port 5001
2. ✅ Build OASIS API (`dotnet build` in ONODE WebAPI); run with `dotnet run` (ports 5003/5004)
3. ✅ Test MCP server with both APIs
4. ✅ Configure Cursor to use MCP server





















