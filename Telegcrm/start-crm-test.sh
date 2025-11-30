#!/bin/bash

echo "🚀 Starting Telegram CRM Test Server..."
echo ""

cd "$(dirname "$0")/TestServer"

# Check if already running
if lsof -Pi :5001 -sTCP:LISTEN -t >/dev/null 2>&1; then
    echo "⚠️  Server already running on port 5001"
    echo "   Access at: http://localhost:5001/swagger"
    exit 0
fi

# Build first
echo "🔨 Building..."
if ! dotnet build -q; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful"
echo ""
echo "🌐 Starting server on http://localhost:5001"
echo "📚 Swagger UI: http://localhost:5001/swagger"
echo ""
echo "Press Ctrl+C to stop"
echo ""

# Run in foreground
dotnet run
