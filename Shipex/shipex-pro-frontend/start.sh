#!/bin/bash

# Shipex Pro Frontend - Quick Start Script

echo "🚀 Starting Shipex Pro Frontend..."
echo ""

# Check if Python 3 is available
if command -v python3 &> /dev/null; then
    echo "✅ Using Python 3 HTTP server"
    echo "📡 Server starting on http://localhost:8000"
    echo "🌐 Open http://localhost:8000 in your browser"
    echo ""
    echo "Press Ctrl+C to stop the server"
    echo ""
    python3 -m http.server 8000
elif command -v python &> /dev/null; then
    echo "✅ Using Python HTTP server"
    echo "📡 Server starting on http://localhost:8000"
    echo "🌐 Open http://localhost:8000 in your browser"
    echo ""
    echo "Press Ctrl+C to stop the server"
    echo ""
    python -m SimpleHTTPServer 8000
else
    echo "❌ Python not found. Please install Python 3 or use Node.js:"
    echo "   npx http-server -p 8000 -c-1"
    exit 1
fi
