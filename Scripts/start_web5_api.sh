#!/usr/bin/env bash
# Start only the WEB5 (STAR) OASIS API. Linux/macOS equivalent of start_web5_api.bat.
# Stops any existing process on port 5556 so dotnet run can build and copy DLLs.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
WEB5_PROJECT="$REPO_ROOT/STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj"
WEB5_DIR="$REPO_ROOT/STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
PORT=5556

echo ""
echo "========================================"
echo "Starting WEB5 STAR API"
echo "========================================"
echo ""

if [[ ! -f "$WEB5_PROJECT" ]]; then
  echo "Error: WEB5 project not found at:"
  echo "  $WEB5_PROJECT"
  exit 1
fi

echo "Checking for existing WEB5 STAR API on port $PORT..."
if command -v lsof &>/dev/null; then
  pids=$(lsof -ti:$PORT 2>/dev/null) || true
  if [[ -n "$pids" ]]; then
    echo "$pids" | xargs kill -9 2>/dev/null || true
    echo "Stopped existing process on port $PORT."
  fi
elif command -v fuser &>/dev/null; then
  fuser -k $PORT/tcp 2>/dev/null || true
  echo "Port check completed."
fi

echo ""
echo "Starting WEB5 STAR API on http://localhost:$PORT..."
echo "Press Ctrl+C to stop the API."
echo ""

cd "$WEB5_DIR"
exec dotnet run --no-launch-profile --project "$WEB5_PROJECT" -c Release --urls "http://localhost:$PORT"
