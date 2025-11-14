#!/bin/bash

echo "🔧 Setting up TimoRides Telegram Bot..."

# Kill any existing processes
echo "Killing old processes..."
pkill -9 -f "dotnet.*WebAPI" 2>/dev/null
lsof -ti:5003 | xargs kill -9 2>/dev/null
sleep 2

# Delete webhook
echo "Removing webhook..."
curl -s "https://api.telegram.org/bot8000192131:AAE3DY-AxbnhaPBaLF_mBogV169CeRXGleg/deleteWebhook"
echo ""

# Rebuild
echo "Building API..."
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet build --no-restore -v quiet

# Start
echo ""
echo "🚀 Starting API..."
echo "Watch for: 'Telegram bot started receiving messages'"
echo ""
dotnet run --no-build

