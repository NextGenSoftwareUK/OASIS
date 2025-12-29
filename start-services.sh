#!/bin/bash

# Start all services for Smart Contract Generator with Portal

echo "🚀 Starting Smart Contract Generator Services"
echo "=============================================="
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Kill existing processes
echo "🧹 Cleaning up existing processes..."
lsof -ti :5000 | xargs kill -9 2>/dev/null
lsof -ti :3001 | xargs kill -9 2>/dev/null
lsof -ti :8080 | xargs kill -9 2>/dev/null
sleep 2

# Start API
echo ""
echo "📡 Starting Smart Contract Generator API (port 5000)..."
cd /Volumes/Storage/OASIS_CLEAN/SmartContractGenerator
dotnet run --project src/SmartContractGen/ScGen.API/ScGen.API.csproj > /tmp/scgen-api.log 2>&1 &
API_PID=$!
echo "   PID: $API_PID"
echo "   Log: /tmp/scgen-api.log"

# Start UI
echo ""
echo "🎨 Starting Smart Contract Generator UI (port 3001)..."
cd /Volumes/Storage/OASIS_CLEAN/SmartContractGenerator/ScGen.UI
# Ensure dependencies are installed
if [ ! -d "node_modules" ]; then
    echo "   Installing dependencies..."
    npm install > /dev/null 2>&1
fi
npm run dev > /tmp/scgen-ui.log 2>&1 &
UI_PID=$!
echo "   PID: $UI_PID"
echo "   Log: /tmp/scgen-ui.log"

# Start Portal
echo ""
echo "🌐 Starting Portal Server (port 8080)..."
cd /Volumes/Storage/OASIS_CLEAN/portal
python3 -m http.server 8080 > /tmp/portal-server.log 2>&1 &
PORTAL_PID=$!
echo "   PID: $PORTAL_PID"
echo "   Log: /tmp/portal-server.log"

# Wait for services to start
echo ""
echo "⏳ Waiting for services to start..."
sleep 10

# Check status
echo ""
echo "📊 Service Status:"
echo ""

check_service() {
    local name=$1
    local port=$2
    local url=$3
    
    if curl -s "$url" > /dev/null 2>&1; then
        echo -e "  ${GREEN}✅${NC} $name (port $port) - Running"
        return 0
    else
        echo -e "  ${YELLOW}⏳${NC} $name (port $port) - Starting..."
        return 1
    fi
}

check_service "API" "5000" "http://localhost:5000/swagger/index.html"
check_service "UI" "3001" "http://localhost:3001"
check_service "Portal" "8080" "http://localhost:8080/portal.html"

echo ""
echo "🌐 URLs:"
echo "  Portal: http://localhost:8080/portal.html"
echo "  API:    http://localhost:5000/swagger"
echo "  UI:     http://localhost:3001"
echo ""
echo "📝 To stop services:"
echo "  kill $API_PID $UI_PID $PORTAL_PID"
echo ""
echo "📋 To view logs:"
echo "  API:    tail -f /tmp/scgen-api.log"
echo "  UI:     tail -f /tmp/scgen-ui.log"
echo "  Portal: tail -f /tmp/portal-server.log"
echo ""


