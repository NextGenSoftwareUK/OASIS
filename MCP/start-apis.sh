#!/bin/bash

# Start OASIS API and SmartContractGenerator API
# Usage: ./start-apis.sh

echo "🚀 Starting OASIS APIs..."
echo ""

# Get the OASIS_CLEAN directory
OASIS_CLEAN_DIR="/Users/maxgershfield/OASIS_CLEAN"
OASIS_API_DIR="${OASIS_CLEAN_DIR}/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
SC_API_DIR="${OASIS_CLEAN_DIR}/SmartContractGenerator/src/SmartContractGen/ScGen.API"

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
  echo "❌ .NET SDK not found!"
  echo "   Please install .NET 8 SDK: https://dotnet.microsoft.com/download"
  exit 1
fi

# Check if directories exist
if [ ! -d "$OASIS_API_DIR" ]; then
  echo "❌ OASIS API directory not found: ${OASIS_API_DIR}"
  exit 1
fi

if [ ! -d "$SC_API_DIR" ]; then
  echo "❌ SmartContractGenerator API directory not found: ${SC_API_DIR}"
  exit 1
fi

# Function to check if port is in use
check_port() {
  local port=$1
  if lsof -Pi :${port} -sTCP:LISTEN -t >/dev/null 2>&1 ; then
    echo "⚠️  Port ${port} is already in use!"
    read -p "Kill existing process and start new one? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
      kill -9 $(lsof -ti:${port}) 2>/dev/null
      sleep 2
    else
      return 1
    fi
  fi
  return 0
}

# Start OASIS API on port 5000
echo "📡 Starting OASIS API on port 5000..."
if ! check_port 5000; then
  echo "Skipping OASIS API (port 5000 in use)"
else
  cd "$OASIS_API_DIR"
  echo "🔨 Building OASIS API..."
  dotnet build --configuration Debug --verbosity quiet > /dev/null 2>&1
  
  if [ $? -eq 0 ]; then
    echo "✅ OASIS API built successfully"
    echo "🚀 Starting OASIS API..."
    # Start in background
    dotnet run --urls "http://localhost:5000" > /tmp/oasis-api.log 2>&1 &
    OASIS_PID=$!
    echo "   PID: $OASIS_PID"
    echo "   Logs: /tmp/oasis-api.log"
    sleep 5
    # Check if it's running
    if ps -p $OASIS_PID > /dev/null; then
      echo "✅ OASIS API started on http://localhost:5000"
    else
      echo "❌ OASIS API failed to start. Check logs: /tmp/oasis-api.log"
    fi
  else
    echo "❌ OASIS API build failed"
  fi
fi

echo ""

# Start SmartContractGenerator API on port 5000 (if OASIS not using it) or 5001
SC_PORT=5001
if ! lsof -Pi :5000 -sTCP:LISTEN -t >/dev/null 2>&1 ; then
  SC_PORT=5000
fi

echo "📡 Starting SmartContractGenerator API on port ${SC_PORT}..."
if ! check_port $SC_PORT; then
  echo "Skipping SmartContractGenerator API (port ${SC_PORT} in use)"
else
  cd "$SC_API_DIR"
  echo "🔨 Building SmartContractGenerator API..."
  dotnet build --configuration Debug --verbosity quiet > /dev/null 2>&1
  
  if [ $? -eq 0 ]; then
    echo "✅ SmartContractGenerator API built successfully"
    echo "🚀 Starting SmartContractGenerator API..."
    # Start in background
    dotnet run --urls "http://localhost:${SC_PORT}" > /tmp/sc-api.log 2>&1 &
    SC_PID=$!
    echo "   PID: $SC_PID"
    echo "   Logs: /tmp/sc-api.log"
    sleep 5
    # Check if it's running
    if ps -p $SC_PID > /dev/null; then
      echo "✅ SmartContractGenerator API started on http://localhost:${SC_PORT}"
    else
      echo "❌ SmartContractGenerator API failed to start. Check logs: /tmp/sc-api.log"
    fi
  else
    echo "❌ SmartContractGenerator API build failed"
  fi
fi

echo ""
echo "🎉 APIs starting..."
echo ""
echo "📍 OASIS API: http://localhost:5000"
echo "📍 SmartContractGenerator API: http://localhost:${SC_PORT}"
echo ""
echo "💡 To check status:"
echo "   curl http://localhost:5000/api/health"
echo "   curl http://localhost:${SC_PORT}/api/v1/contracts/cache-stats"
echo ""
echo "🛑 To stop:"
echo "   pkill -f 'dotnet run.*OASIS.API.ONODE.WebAPI'"
echo "   pkill -f 'dotnet run.*ScGen.API'"
echo ""





















