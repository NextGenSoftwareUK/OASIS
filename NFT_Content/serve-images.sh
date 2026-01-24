#!/bin/bash

# Simple HTTP server to serve images from this directory
# This allows you to reference images as http://localhost:8000/your-image.png

PORT=${1:-8000}

echo "🖼️  Starting image server on port $PORT"
echo "📁 Serving files from: $(pwd)"
echo ""
echo "Access your images at:"
echo "   http://localhost:$PORT/your-image.png"
echo ""
echo "Press Ctrl+C to stop the server"
echo ""

# Try Python 3 first, fallback to Python 2
if command -v python3 &> /dev/null; then
    python3 -m http.server $PORT
elif command -v python &> /dev/null; then
    python -m SimpleHTTPServer $PORT
else
    echo "❌ Error: Python not found. Please install Python to use this script."
    exit 1
fi
