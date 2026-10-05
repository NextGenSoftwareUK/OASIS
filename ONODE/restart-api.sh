#!/bin/bash

# Script to restart the OASIS API server with proper environment

echo "🔄 Restarting OASIS API Server..."
echo ""

# Find and kill existing process
PID=$(ps aux | grep -i "dotnet.*OASIS.*WebAPI\|NextGenSoftware.*ONODE" | grep -v grep | awk '{print $2}')

if [ ! -z "$PID" ]; then
    echo "🛑 Stopping existing API server (PID: $PID)..."
    kill $PID 2>/dev/null
    sleep 2
    
    # Force kill if still running
    if ps -p $PID > /dev/null 2>&1; then
        echo "⚠️  Force killing process..."
        kill -9 $PID 2>/dev/null
    fi
else
    echo "ℹ️  No existing API server process found"
fi

echo ""
echo "🚀 Starting OASIS API server..."
echo "📍 Environment: Development (HTTP enabled on port 5003)"
echo ""

cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI

# Set environment to Development explicitly
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://localhost:5003;https://localhost:5004"

# Start the server
dotnet run









