#!/bin/bash

echo "🌡️  ========================================"
echo "🌡️  BillionsHealed Startup Script"
echo "🌡️  ========================================"
echo ""

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Start backend
echo -e "${BLUE}Starting backend server...${NC}"
cd backend
npm install --silent
npm start &
BACKEND_PID=$!

echo -e "${GREEN}✅ Backend started (PID: $BACKEND_PID)${NC}"
echo ""

# Wait for backend to be ready
echo "⏳ Waiting for backend to be ready..."
sleep 3

# Check if backend is running
if curl -s http://localhost:3002/health > /dev/null; then
    echo -e "${GREEN}✅ Backend is healthy${NC}"
else
    echo "❌ Backend health check failed"
    exit 1
fi

echo ""
echo "🌡️  ========================================"
echo "🌡️  BillionsHealed is running!"
echo "🌡️  ========================================"
echo ""
echo -e "${GREEN}Backend API:${NC} http://localhost:3002"
echo -e "${GREEN}Frontend:${NC} Open frontend/index.html in your browser"
echo ""
echo "Or serve frontend with:"
echo "  cd frontend && python3 -m http.server 8000"
echo ""
echo "Press Ctrl+C to stop the backend server"
echo ""

# Keep script running
wait $BACKEND_PID

