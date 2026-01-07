# API Status & Startup Guide

## Current Status

### OASIS API
- **Port:** 5000
- **Status:** Port 5000 is in use (may already be running)
- **Check:** `curl http://localhost:5000/api/health`

### SmartContractGenerator API
- **Port:** 5001 (or 5000 if OASIS not running)
- **Status:** Requires .NET 9.0 SDK
- **Issue:** Only .NET 8.0.22 and 10.0.1 are installed
- **Solution:** Install .NET 9.0 SDK or update project to use .NET 8/10

## Quick Start Commands

### Start OASIS API
```bash
cd /Users/maxgershfield/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run --urls "http://localhost:5000"
```

### Start SmartContractGenerator API (after installing .NET 9.0)
```bash
cd /Users/maxgershfield/OASIS_CLEAN/SmartContractGenerator/src/SmartContractGen/ScGen.API
dotnet run --urls "http://localhost:5001"
```

### Or use the startup script:
```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP
./start-apis.sh
```

## Check if APIs are Running

```bash
# Check OASIS API
curl http://localhost:5000/api/health

# Check SmartContractGenerator API
curl http://localhost:5001/api/v1/contracts/cache-stats
```

## Fix SmartContractGenerator .NET Version Issue

**Option 1: Install .NET 9.0**
```bash
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0
# Or use Homebrew:
brew install --cask dotnet-sdk
```

**Option 2: Update SmartContractGenerator to use .NET 8 or 10**
- Edit `SmartContractGenerator/src/SmartContractGen/ScGen.API/ScGen.API.csproj`
- Change `<TargetFramework>net9.0</TargetFramework>` to `net8.0` or `net10.0`

## Stop APIs

```bash
# Stop OASIS API
pkill -f "dotnet run.*OASIS.API.ONODE.WebAPI"
# Or find and kill by port:
lsof -ti:5000 | xargs kill -9

# Stop SmartContractGenerator API
pkill -f "dotnet run.*ScGen.API"
# Or find and kill by port:
lsof -ti:5001 | xargs kill -9
```





















