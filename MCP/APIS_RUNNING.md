# ✅ APIs Status

## Current Status

### ✅ SmartContractGenerator API
- **Status:** ✅ **RUNNING**
- **Port:** 5001
- **URL:** http://localhost:5001
- **Test:** `curl http://localhost:5001/api/v1/contracts/cache-stats`
- **Response:** `{"enabled":false,"message":"sccache not found"}` (This is OK - sccache is optional)

### ⚠️ OASIS API
- **Status:** Starting/Checking...
- **Port:** 5000
- **URL:** http://localhost:5000
- **Test:** `curl http://localhost:5000/api/health`

## Quick Commands

### Check API Status
```bash
# OASIS API
curl http://localhost:5000/api/health

# SmartContractGenerator API
curl http://localhost:5001/api/v1/contracts/cache-stats
```

### Start APIs Manually

**OASIS API:**
```bash
cd /Users/maxgershfield/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run --urls "http://localhost:5000"
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
lsof -ti:5000 | xargs kill -9
lsof -ti:5001 | xargs kill -9
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
2. ⚠️ Start OASIS API on port 5000 (if not already running)
3. ✅ Test MCP server with both APIs
4. ✅ Configure Cursor to use MCP server





















